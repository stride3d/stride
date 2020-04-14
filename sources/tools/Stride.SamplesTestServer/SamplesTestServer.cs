// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.ConnectionRouter;
using Stride.Engine.Network;
using Stride.Games.Testing;
using Stride.Games.Testing.Requests;
using Stride.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.Extensions;
using Stride.Graphics.Regression;

namespace Stride.SamplesTestServer
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

        public SamplesTestServer() : base($"/service/Stride.SamplesTestServer/{StrideVersion.NuGetVersion}/Stride.SamplesTestServer.exe")
        {
            GameTestingSystem.Initialized = true;

            //start logging the iOS device if we have the proper tools avail
            if (IosTracker.CanProxy())
            {
                var loggerProcess = Process.Start(new ProcessStartInfo($"idevicesyslog.exe", "-d")
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

            socketMessageLayer.AddPacketHandler<TestRegistrationRequest>(async request =>
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
                                        start.UseShellExecute = false;
                                        start.RedirectStandardError = true;
                                        start.RedirectStandardOutput = true;

                                        debugInfo = "Starting process " + start.FileName + " with path " + start.WorkingDirectory;
                                        await socketMessageLayer.Send(new LogRequest { Message = debugInfo });
                                        process = Process.Start(start);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    await socketMessageLayer.Send(new StatusMessageRequest { Error = true, Message = "Launch exception: " + ex.Message });
                                }

                                if (process == null)
                                {
                                    await socketMessageLayer.Send(new StatusMessageRequest { Error = true, Message = "Failed to start game process. " + debugInfo });
                                }
                                else
                                {
                                    process.OutputDataReceived += async (sender, args) =>
                                    {
                                        try
                                        {
                                            if (args.Data != null)
                                                await socketMessageLayer.Send(new LogRequest { Message = $"STDIO: {args.Data}" });
                                        }
                                        catch
                                        {
                                        }
                                    };

                                    process.ErrorDataReceived += async (sender, args) =>
                                    {
                                        try
                                        {
                                            if (args.Data != null)
                                            await socketMessageLayer.Send(new LogRequest { Message = $"STDERR: {args.Data}" });
                                        }
                                        catch
                                        {
                                        }
                                    };

                                    process.BeginOutputReadLine();
                                    process.BeginErrorReadLine();

                                    var currenTestPair = new TestPair { TesterSocket = socketMessageLayer, GameName = request.GameAssembly, Process = process };
                                    lock (processes)
                                    {
                                        processes[request.GameAssembly] = currenTestPair;
                                        testerToGame[socketMessageLayer] = currenTestPair;
                                    }
                                    await socketMessageLayer.Send(new LogRequest { Message = "Process created, id: " + process.Id.ToString() });
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
                                    await socketMessageLayer.Send(new StatusMessageRequest { Error = true, Message = "Launch exception: " + ex.Message });
                                }

                                if (process == null)
                                {
                                    await socketMessageLayer.Send(new StatusMessageRequest { Error = true, Message = "Failed to start game process." });
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
                                    lock (processes)
                                    {
                                        processes[request.GameAssembly] = currenTestPair;
                                        testerToGame[socketMessageLayer] = currenTestPair;
                                    }
                                    await socketMessageLayer.Send(new LogRequest { Message = "Process created, id: " + process.Id.ToString() });
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
                                        FileName = $"idevicedebug.exe",
                                        Arguments = $"run com.your-company.{request.GameAssembly}",
                                        UseShellExecute = false
                                    };
                                    debugInfo = "Starting process " + start.FileName + " with path " + start.WorkingDirectory;
                                    process = Process.Start(start);
                                }
                                catch (Exception ex)
                                {
                                    await socketMessageLayer.Send(new StatusMessageRequest { Error = true, Message = $"Launch exception: {ex.Message} info: {debugInfo}" });
                                }

                                if (process == null)
                                {
                                    await socketMessageLayer.Send(new StatusMessageRequest { Error = true, Message = "Failed to start game process. " + debugInfo });
                                }
                                else
                                {
                                    lock (loggerLock)
                                    {
                                        currentTester = socketMessageLayer;
                                    }

                                    var currenTestPair = new TestPair { TesterSocket = socketMessageLayer, GameName = request.GameAssembly, Process = process };
                                    lock (processes)
                                    {
                                        processes[request.GameAssembly] = currenTestPair;
                                        testerToGame[socketMessageLayer] = currenTestPair;
                                    }
                                    await socketMessageLayer.Send(new LogRequest { Message = "Process created, id: " + process.Id.ToString() });
                                }
                                break;
                            }
                    }
                }
                else //Game process
                {
                    TestPair pair;
                    lock (processes)
                    {
                        if (!processes.TryGetValue(request.GameAssembly, out pair)) return;

                        pair.GameSocket = socketMessageLayer;

                        testerToGame[pair.TesterSocket] = pair;
                        gameToTester[pair.GameSocket] = pair;
                    }

                    await pair.TesterSocket.Send(new StatusMessageRequest { Error = false, Message = "Start" });

                    Console.WriteLine($"Starting test {request.GameAssembly}");
                }
            });

            socketMessageLayer.AddPacketHandler<KeySimulationRequest>(async request =>
            {
                TestPair game;
                lock (processes)
                {
                    game = testerToGame[socketMessageLayer];
                }
                await game.GameSocket.Send(request);
            });

            socketMessageLayer.AddPacketHandler<TapSimulationRequest>(async request =>
            {
                TestPair game;
                lock (processes)
                {
                    game = testerToGame[socketMessageLayer];
                }
                await game.GameSocket.Send(request);
            });

            socketMessageLayer.AddPacketHandler<ScreenshotRequest>(async request =>
            {
                TestPair game;
                lock (processes)
                {
                    game = testerToGame[socketMessageLayer];
                }
                await game.GameSocket.Send(request);
            });

            socketMessageLayer.AddPacketHandler<TestEndedRequest>(async request =>
            {
                TestPair game;
                lock (processes)
                {
                    game = testerToGame[socketMessageLayer];
                }
                await game.GameSocket.Send(request);

                lock (processes)
                {
                    testerToGame.Remove(socketMessageLayer);
                    gameToTester.Remove(game.GameSocket);
                    processes.Remove(game.GameName);
                }

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
                TestPair game;
                lock (processes)
                {
                    game = testerToGame[socketMessageLayer];
                    testerToGame.Remove(socketMessageLayer);
                    processes.Remove(game.GameName);
                }

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

            socketMessageLayer.AddPacketHandler<ScreenShotPayload>(async request =>
            {
                TestPair tester;
                lock (processes)
                {
                    tester = gameToTester[socketMessageLayer];
                }

                var imageData = new TestResultImage();
                var stream = new MemoryStream(request.Data);
                imageData.Read(new BinaryReader(stream));
                stream.Dispose();
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(request.FileName));
                var resultFileStream = File.OpenWrite(request.FileName);
                imageData.Image.Save(resultFileStream, ImageFileType.Png);
                resultFileStream.Dispose();

                await tester.TesterSocket.Send(new ScreenshotStored());
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
