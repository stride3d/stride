// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.ConnectionRouter;
using Xenko.Engine.Network;
using Xenko.Games.Testing;
using Xenko.Games.Testing.Requests;
using Xenko.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xenko.Core.Extensions;
using Xenko.Graphics.Regression;

namespace Xenko.SamplesTestServer
{
    public class SamplesTestServer : RouterServiceServer
    {
        private class TestPair
        {
            public SocketMessageLayer TesterSocket;
            public SocketMessageLayer GameSocket;
            public string GameName;
            public Process Process;
            public Action TestEndAction;
        }

        private readonly Dictionary<string, TestPair> processes = new Dictionary<string, TestPair>();

        private readonly Dictionary<SocketMessageLayer, TestPair> testerToGame = new Dictionary<SocketMessageLayer, TestPair>();
        private readonly Dictionary<SocketMessageLayer, TestPair> gameToTester = new Dictionary<SocketMessageLayer, TestPair>();

        private SocketMessageLayer currentTester;
        private readonly object loggerLock = new object();

        public SamplesTestServer() : base($"/service/{XenkoVersion.NuGetVersion}/Xenko.SamplesTestServer.exe")
        {
            GameTestingSystem.Initialized = true;

            //start logging the iOS device if we have the proper tools avail
            if (IosTracker.CanProxy())
            {
                var loggerProcess = Process.Start(new ProcessStartInfo($"{ Environment.GetEnvironmentVariable("XenkoDir") }\\Bin\\Windows\\idevicesyslog.exe", "-d")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                });

                if (loggerProcess != null)
                {
                    loggerProcess.OutputDataReceived += (sender, args) =>
                    {
                        try
                        {
                            lock (loggerLock)
                            {
                                currentTester?.Send(new LogRequest { Message = $"STDIO: {args.Data}" }).Wait();
                            }
                        }
                        catch
                        {
                        }
                    };

                    loggerProcess.ErrorDataReceived += (sender, args) =>
                    {
                        try
                        {
                            lock (loggerLock)
                            {
                                currentTester?.Send(new LogRequest { Message = $"STDERR: {args.Data}" }).Wait();
                            }
                        }
                        catch
                        {
                        }
                    };

                    loggerProcess.BeginOutputReadLine();
                    loggerProcess.BeginErrorReadLine();

                    new AttachedChildProcessJob(loggerProcess);
                }
            }

            //Start also adb in case of android device
            var adbPath = AndroidDeviceEnumerator.GetAdbPath();
            if (!string.IsNullOrEmpty(adbPath) && AndroidDeviceEnumerator.ListAndroidDevices().Length > 0)
            {
                //clear the log first
                ShellHelper.RunProcessAndGetOutput("cmd.exe", $"/C {adbPath} logcat -c");

                //start logger
                var loggerProcess = Process.Start(new ProcessStartInfo("cmd.exe", $"/C {adbPath} logcat")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                });

                if (loggerProcess != null)
                {
                    loggerProcess.OutputDataReceived += (sender, args) =>
                    {
                        try
                        {
                            currentTester?.Send(new LogRequest { Message = $"STDIO: {args.Data}" }).Wait();
                        }
                        catch
                        {
                        }
                    };

                    loggerProcess.ErrorDataReceived += (sender, args) =>
                    {
                        try
                        {
                            currentTester?.Send(new LogRequest { Message = $"STDERR: {args.Data}" }).Wait();
                        }
                        catch
                        {
                        }
                    };

                    loggerProcess.BeginOutputReadLine();
                    loggerProcess.BeginErrorReadLine();

                    new AttachedChildProcessJob(loggerProcess);
                }
            }
        }

        protected override async void HandleClient(SimpleSocket clientSocket, string url)
        {
            await AcceptConnection(clientSocket);

            var socketMessageLayer = new SocketMessageLayer(clientSocket, true);

            socketMessageLayer.AddPacketHandler<TestRegistrationRequest>(request =>
            {
                if (request.Tester)
                {
                    switch (request.Platform)
                    {
                        case (int)PlatformType.Windows:
                            {
                                Process process = null;
                                var debugInfo = "";
                                try
                                {
                                    var workingDir = Path.GetDirectoryName(request.Cmd);
                                    if (workingDir != null)
                                    {
                                        var start = new ProcessStartInfo
                                        {
                                            WorkingDirectory = workingDir,
                                            FileName = request.Cmd
                                        };
                                        start.EnvironmentVariables["XenkoDir"] = Environment.GetEnvironmentVariable("XenkoDir");
                                        start.UseShellExecute = false;
                                        start.RedirectStandardError = true;
                                        start.RedirectStandardOutput = true;

                                        debugInfo = "Starting process " + start.FileName + " with path " + start.WorkingDirectory;
                                        socketMessageLayer.Send(new LogRequest { Message = debugInfo }).Wait();
                                        process = Process.Start(start);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    socketMessageLayer.Send(new StatusMessageRequest { Error = true, Message = "Launch exception: " + ex.Message }).Wait();
                                }

                                if (process == null)
                                {
                                    socketMessageLayer.Send(new StatusMessageRequest { Error = true, Message = "Failed to start game process. " + debugInfo }).Wait();
                                }
                                else
                                {
                                    process.OutputDataReceived += (sender, args) =>
                                    {
                                        try
                                        {
                                            socketMessageLayer.Send(new LogRequest { Message = $"STDIO: {args.Data}" }).Wait();
                                        }
                                        catch
                                        {
                                        }
                                    };

                                    process.ErrorDataReceived += (sender, args) =>
                                    {
                                        try
                                        {
                                            socketMessageLayer.Send(new LogRequest { Message = $"STDERR: {args.Data}" }).Wait();
                                        }
                                        catch
                                        {
                                        }
                                    };

                                    process.BeginOutputReadLine();
                                    process.BeginErrorReadLine();

                                    var currenTestPair = new TestPair { TesterSocket = socketMessageLayer, GameName = request.GameAssembly, Process = process };
                                    processes[request.GameAssembly] = currenTestPair;
                                    testerToGame[socketMessageLayer] = currenTestPair;
                                    socketMessageLayer.Send(new LogRequest { Message = "Process created, id: " + process.Id.ToString() }).Wait();
                                }
                                break;
                            }
                        case (int)PlatformType.Android:
                            {
                                Process process = null;
                                try
                                {
                                    process = Process.Start("cmd.exe", $"/C adb shell monkey -p {request.GameAssembly}.{request.GameAssembly} -c android.intent.category.LAUNCHER 1");
                                }
                                catch (Exception ex)
                                {
                                    socketMessageLayer.Send(new StatusMessageRequest { Error = true, Message = "Launch exception: " + ex.Message }).Wait();
                                }

                                if (process == null)
                                {
                                    socketMessageLayer.Send(new StatusMessageRequest { Error = true, Message = "Failed to start game process." }).Wait();
                                }
                                else
                                {
                                    lock (loggerLock)
                                    {
                                        currentTester = socketMessageLayer;
                                    }

                                    var currenTestPair = new TestPair
                                    {
                                        TesterSocket = socketMessageLayer,
                                        GameName = request.GameAssembly,
                                        Process = process,
                                        TestEndAction = () =>
{
                                            // force stop - only works for Android 3.0 and above.
                                            Process.Start("cmd.exe", $"/C adb shell am force-stop {request.GameAssembly}.{request.GameAssembly}");
}
                                    };
                                    processes[request.GameAssembly] = currenTestPair;
                                    testerToGame[socketMessageLayer] = currenTestPair;
                                    socketMessageLayer.Send(new LogRequest { Message = "Process created, id: " + process.Id.ToString() }).Wait();
                                }
                                break;
                            }
                        case (int)PlatformType.iOS:
                            {
                                Process process = null;
                                var debugInfo = "";
                                try
                                {
                                    Thread.Sleep(5000); //ios processes might be slow to close, we must make sure that we start clean
                                    var start = new ProcessStartInfo
                                    {
                                        WorkingDirectory = $"{Environment.GetEnvironmentVariable("XenkoDir")}\\Bin\\Windows\\",
                                        FileName = $"{Environment.GetEnvironmentVariable("XenkoDir")}\\Bin\\Windows\\idevicedebug.exe",
                                        Arguments = $"run com.your-company.{request.GameAssembly}",
                                        UseShellExecute = false
                                    };
                                    debugInfo = "Starting process " + start.FileName + " with path " + start.WorkingDirectory;
                                    process = Process.Start(start);
                                }
                                catch (Exception ex)
                                {
                                    socketMessageLayer.Send(new StatusMessageRequest { Error = true, Message = $"Launch exception: {ex.Message} info: {debugInfo}" }).Wait();
                                }

                                if (process == null)
                                {
                                    socketMessageLayer.Send(new StatusMessageRequest { Error = true, Message = "Failed to start game process. " + debugInfo }).Wait();
                                }
                                else
                                {
                                    lock (loggerLock)
                                    {
                                        currentTester = socketMessageLayer;
                                    }

                                    var currenTestPair = new TestPair { TesterSocket = socketMessageLayer, GameName = request.GameAssembly, Process = process };
                                    processes[request.GameAssembly] = currenTestPair;
                                    testerToGame[socketMessageLayer] = currenTestPair;
                                    socketMessageLayer.Send(new LogRequest { Message = "Process created, id: " + process.Id.ToString() }).Wait();
                                }
                                break;
                            }
                    }
                }
                else //Game process
                {
                    TestPair pair;
                    if (!processes.TryGetValue(request.GameAssembly, out pair)) return;

                    pair.GameSocket = socketMessageLayer;

                    testerToGame[pair.TesterSocket] = pair;
                    gameToTester[pair.GameSocket] = pair;

                    pair.TesterSocket.Send(new StatusMessageRequest { Error = false, Message = "Start" }).Wait();

                    Console.WriteLine($"Starting test {request.GameAssembly}");
                }
            });

            socketMessageLayer.AddPacketHandler<KeySimulationRequest>(request =>
            {
                var game = testerToGame[socketMessageLayer];
                game.GameSocket.Send(request).Wait();
            });

            socketMessageLayer.AddPacketHandler<TapSimulationRequest>(request =>
            {
                var game = testerToGame[socketMessageLayer];
                game.GameSocket.Send(request).Wait();
            });

            socketMessageLayer.AddPacketHandler<ScreenshotRequest>(request =>
            {
                var game = testerToGame[socketMessageLayer];
                game.GameSocket.Send(request).Wait();
            });

            socketMessageLayer.AddPacketHandler<TestEndedRequest>(request =>
            {
                var game = testerToGame[socketMessageLayer];
                game.GameSocket.Send(request).Wait();

                testerToGame.Remove(socketMessageLayer);
                gameToTester.Remove(game.GameSocket);
                processes.Remove(game.GameName);

                socketMessageLayer.Context.Dispose();
                game.GameSocket.Context.Dispose();

                game.Process.Kill();
                game.Process.Dispose();

                lock (loggerLock)
                {
                    currentTester = null;
                }

                game.TestEndAction?.Invoke();

                Console.WriteLine($"Finished test {game.GameName}");
            });

            socketMessageLayer.AddPacketHandler<TestAbortedRequest>(request =>
            {
                var game = testerToGame[socketMessageLayer];

                testerToGame.Remove(socketMessageLayer);
                processes.Remove(game.GameName);

                socketMessageLayer.Context.Dispose();

                game.Process.Kill();
                game.Process.Dispose();

                lock (loggerLock)
                {
                    currentTester = null;
                }

                game.TestEndAction?.Invoke();

                Console.WriteLine($"Aborted test {game.GameName}");
            });

            socketMessageLayer.AddPacketHandler<ScreenShotPayload>(request =>
            {
                var tester = gameToTester[socketMessageLayer];

                var imageData = new TestResultImage();
                var stream = new MemoryStream(request.Data);
                imageData.Read(new BinaryReader(stream));
                stream.Dispose();
                var resultFileStream = File.OpenWrite(request.FileName);
                imageData.Image.Save(resultFileStream, ImageFileType.Png);
                resultFileStream.Dispose();

                tester.TesterSocket.Send(new ScreenshotStored()).Wait();
            });

            Task.Run(async () =>
            {
                try
                {
                    await socketMessageLayer.MessageLoop();
                }
                catch
                {
                }
            });
        }
    }
}
