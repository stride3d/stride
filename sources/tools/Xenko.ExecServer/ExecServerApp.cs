// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xenko.Core.VisualStudio;
using Binding = System.ServiceModel.Channels.Binding;

namespace Xenko.ExecServer
{
    /// <summary>
    /// ExecServer allows to keep in memory an exec loaded into an AppDomain with the benefits that JIT
    /// has been ran already once and the code will run quicker on next run. This is more convenient 
    /// alternative than using NGEN, as we have the benefit of a better code gen while still having the 
    /// benefits of fast startup.
    /// Also the server doesn't lock original assemblies but it shadows copy them (including native dlls
    /// from DllImport), and tracks if original assemblies changed (in this case it will automatically
    /// shutdown).
    /// </summary>
    public class ExecServerApp
    {
        private const int ExitCodeServerAlreadyInUse = 0x10;

        private const string DisableExecServerAppDomainCaching = "DisableExecServerAppDomainCaching";
        private const int MaxRetryStartedProcess = 20;
        private const int RetryStartedProcessWait = 100; // in ms

        private const int MaxRetryCount = 60; // * 1s
        private const int RetryWait = 1000; // in ms

        /// <summary>
        /// Runs the specified arguments copy.
        /// </summary>
        /// <param name="argsCopy">The arguments copy.</param>
        /// <returns>System.Int32.</returns>
        public int Run(string[] argsCopy)
        {
            if (argsCopy.Length == 0)
            {
                Console.WriteLine("Usage ExecServer.exe [/direct executablePath|/server entryAssemblyPath executablePath CPUindex] /shadow [executableArguments]");
                return 0;
            }
            var args = new List<string>(argsCopy);

            if (args[0] == "/direct")
            {
                args.RemoveAt(0);
                var executablePath = ExtractPath(args, "executable");
                var execServerApp = new ExecServerRemote(executablePath, executablePath, false, false, true);
                int result = execServerApp.Run(Environment.CurrentDirectory, new Dictionary<string, string>(), args.ToArray(), false, null);
                return result;
            }

            if (args[0] == "/server")
            {
                args.RemoveAt(0);
                var entryAssemblyPath = ExtractPath(args, "entryAssembly");
                var executablePath = ExtractPath(args, "executable");
                var cpu = int.Parse(args[0]);
                args.RemoveAt(0);
                int result = 0;
                try
                {
                    result = RunServer(entryAssemblyPath, executablePath, cpu);
                }
                catch (Exception ex)
                {
                    try
                    {
                        var pid = Process.GetCurrentProcess().Id;
                        var logPath = GetExecServerErrorLogFilePath(executablePath, pid);
                        File.AppendAllText(logPath, $"Unexpected error while trying to run ExecServerApp [{executablePath}]. Exception: {ex}");
                    }
                    catch (Exception)
                    {
                        // Don't try to log an error
                    }
                    result = 1;
                }
                return result;
            }
            else
            {
                bool useShadowCache = false;
                if (args[0] == "/shadow")
                {
                    args.RemoveAt(0);
                    useShadowCache = true;
                }

                var executablePath = ExtractPath(args, "executable");
                var workingDirectory = ExtractPath(args, "working directory");

                // Collect environment variables
                var environmentVariables = new Dictionary<string, string>();
                foreach (DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables())
                    environmentVariables.Add((string)environmentVariable.Key, (string)environmentVariable.Value);

                int? debuggerProcessId = null;
                using (var debugger = VisualStudioDebugger.GetAttached())
                {
                    if (debugger != null)
                    {
                        debuggerProcessId = debugger.ProcessId;
                    }
                }

                var result = RunClient(executablePath, workingDirectory, environmentVariables, args, useShadowCache, debuggerProcessId);
                return result;
            }
        }

        private static string GetExecServerErrorLogFilePath(string executablePath, int pid)
        {
            return Path.Combine(Path.GetDirectoryName(executablePath), $"XenkoExecServer.pid{pid}.log");
        }

        /// <summary>
        /// Runs ExecServer in server mode (waiting for connection from ExecServer clients)
        /// </summary>
        /// <param name="entryAssemblyPath">Path to the client assembly in case we need to start another instance of same process.</param>
        /// <param name="executablePath">Path of the executable to run from this ExecServer instance</param>
        private int RunServer(string entryAssemblyPath, string executablePath, int serverInstanceIndex)
        {
            var address = GetEndpointAddress(executablePath, serverInstanceIndex);

            // TODO: The setting of disabling caching should be done per EXE (via config file) instead of global settings for ExecServer
            var useAppDomainCaching = Environment.GetEnvironmentVariable(DisableExecServerAppDomainCaching) != "true";

            // Start WCF pipe for communication with process
            var execServerApp = new ExecServerRemote(entryAssemblyPath, executablePath, true, useAppDomainCaching, serverInstanceIndex == 0);
            var host = new ServiceHost(execServerApp);
            host.AddServiceEndpoint(typeof(IExecServerRemote), new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
            {
                MaxReceivedMessageSize = int.MaxValue,
                SendTimeout = TimeSpan.FromHours(1),
                ReceiveTimeout = TimeSpan.FromHours(1),
            }, address);

            try
            {
                host.Open();
            }
            catch (AddressAlreadyInUseException)
            {
                // Uncomment the following line to see which process got a ExitCodeServerAlreadyInUse
                // File.WriteAllText(Path.Combine(Environment.CurrentDirectory, $"test_ExecServer{Process.GetCurrentProcess().Id}.log"), $"Exit code: {ExitCodeServerAlreadyInUse}\r\n");

                // Silently exit if the server is already running
                return ExitCodeServerAlreadyInUse;
            }

            Console.WriteLine("Server [{0}] is running", executablePath);

            // Register for shutdown
            execServerApp.ShuttingDown += (sender, args) => host.Close();

            // Wait for the server to shutdown
            execServerApp.Wait();

            return 0;
        }

        /// <summary>
        /// Runs the client side by calling ExecServer remote server and passing arguments. If ExecServer remote is not running,
        /// it will start it automatically.
        /// </summary>
        /// <param name="executablePath">The executable path.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="args">The arguments.</param>
        /// <param name="shadowCache">If [true], use shadow cache.</param>
        /// <returns>Return status.</returns>
        private int RunClient(string executablePath, string workingDirectory, Dictionary<string, string> environmentVariables, List<string> args, bool shadowCache, int? debuggerProcessId)
        {
            var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
            {
                MaxReceivedMessageSize = int.MaxValue,
                OpenTimeout = TimeSpan.FromMilliseconds(100),
                SendTimeout = TimeSpan.FromHours(1),
                ReceiveTimeout = TimeSpan.FromHours(1),
            };

            // Number of concurrent processes: use number of CPU, but no more than 8
            int maxServerInstanceIndex = Math.Max(Environment.ProcessorCount, 8);

            // All tried connections will be kept open until we're done
            // TODO: Small optimization: as soon as service.Run() actually start (not busy), we could close all other connections
            var clients = new ExecServerRemoteClient[maxServerInstanceIndex];

            try
            {
                for (int i = 0; i < MaxRetryCount; i++)
                {
                    // Try each available server
                    for (int serverInstanceIndex = 0; serverInstanceIndex < maxServerInstanceIndex; ++serverInstanceIndex)
                    {
                        int numberTriesAfterRunProcess = 0;
                        var address = GetEndpointAddress(executablePath, serverInstanceIndex);
                        var processHandle = IntPtr.Zero;
                        int processId = 0;

                    TrySameConnectionAgain:
                        var redirectLog = new RedirectLogger();
                        var client = clients[serverInstanceIndex] ?? new ExecServerRemoteClient(redirectLog, binding, new EndpointAddress(address));
                        // Console.WriteLine("{0}: ExecServer Try to connect", DateTime.Now);

                        var service = client.ChannelFactory.CreateChannel();
                        try
                        {
                            service.Check();

                            // Keep this connection
                            clients[serverInstanceIndex] = client;

                            //Console.WriteLine("{0}: ExecServer - running start", DateTime.Now);
                            try
                            {
                                var result = service.Run(workingDirectory, environmentVariables, args.ToArray(), shadowCache, debuggerProcessId);
                                if (result == ExecServerRemote.BusyReturnCode)
                                {
                                    // Try next server
                                    continue;
                                }
                                //Console.WriteLine("{0}: ExecServer - running end", DateTime.Now);
                                return result;
                            }
                            finally
                            {
                                CloseService(ref service);
                            }
                        }
                        catch (EndpointNotFoundException)
                        {
                            CloseService(ref service);

                            // Close and uncache client connection (server is not started yet)
                            clients[serverInstanceIndex] = null;
                            CloseClient(ref client);
                            string finalExecServerPath;

                            if (numberTriesAfterRunProcess++ == 0)
                            {
                                // The server is not running, we need to run it
                                if (!RunServerProcess(executablePath, serverInstanceIndex, out processHandle, out processId, out finalExecServerPath))
                                {
                                    Console.WriteLine($"Cannot launch exec server for [{finalExecServerPath}]. Trying next one");
                                    continue;
                                }
                            }

                            if (numberTriesAfterRunProcess > MaxRetryStartedProcess)
                            {
                                Console.WriteLine("ERROR cannot connect to newly started proxy server for: {0} {1}", executablePath, string.Join(" ", args));
                                continue;
                            }

                            // Wait for the process to startup before trying again
                            Console.WriteLine("Waiting {0}ms for the proxy server to start and connect to it", RetryStartedProcessWait);

                            Thread.Sleep(RetryStartedProcessWait);

                            // Check that the sever we tried to launch is still running, if not we have a severe error
                            if (processHandle != IntPtr.Zero)
                            {
                                int exitCode;
                                if (GetExitCodeProcess(processHandle, out exitCode))
                                {
                                    if (exitCode != ExitCodeServerAlreadyInUse && exitCode != PROCESS_STILL_ACTIVE)
                                    {
                                        Console.WriteLine($"Unexpected error: ExecServerApp has exited with the return code: {exitCode}");

                                        var logPath = GetExecServerErrorLogFilePath(executablePath, processId);
                                        if (File.Exists(logPath))
                                        {
                                            Console.WriteLine(File.ReadAllText(logPath));
                                            File.Delete(logPath);
                                        }
                                        return -300;
                                    }
                                }
                            }

                            goto TrySameConnectionAgain;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error {0}", e);
                            return -300;
                        }
                    }

                    // Wait for the process to startup before trying again
                    Console.WriteLine("Waiting {0}ms to create a new proxy server", RetryWait);

                    // Wait little bit before trying everything again
                    Thread.Sleep(RetryWait);
                }

                Console.WriteLine("ERROR cannot connect to proxy server for: {0} {1}", executablePath, string.Join(" ", args));
                return 1;
            }
            finally
            {
                // Close all services
                CloseClients(clients);
            }
        }

        /// <summary>
        /// Closes a WCF service.
        /// </summary>
        /// <param name="service">The service.</param>
        private static void CloseService(ref IExecServerRemote service)
        {
            if (service != null)
            {
                try
                {
                    var clientChannel = ((IClientChannel)service);
                    if (clientChannel.State == CommunicationState.Faulted)
                    {
                        clientChannel.Abort();
                    }
                    else
                    {
                        clientChannel.Close();
                    }

                    clientChannel.Dispose();
                    //Console.WriteLine("ExecServer - Close connection - Client channel state: {0}", clientChannel.State);
                }
                catch (Exception)
                {
                    //Console.WriteLine("Exception while closing connection {0}", ex);
                }
                service = null;
            }
        }

        private static void CloseClient(ref ExecServerRemoteClient client)
        {
            if (client != null)
            {
                try
                {
                    //Console.WriteLine("Closing client");
                    client.Close();
                }
                catch (Exception)
                {
                    //Console.WriteLine("Exception while closing {0}", client);
                }
                client = null;
            }
        }

        private static void CloseClients(ExecServerRemoteClient[] clients)
        {
            for (int i = 0; i < clients.Length; ++i)
            {
                CloseClient(ref clients[i]);
            }
        }

        /// <summary>
        /// Runs the server process when it does not exist.
        /// </summary>
        /// <param name="executablePath">The executable path.</param>
        /// <param name="serverInstanceIndex">The server instance index.</param>
        private bool RunServerProcess(string executablePath, int serverInstanceIndex, out IntPtr processHandle, out int processId, out string finalExecServerPath)
        {
            processHandle = IntPtr.Zero;
            processId = 0;
            var originalExecServerAppPath = typeof(ExecServerApp).Assembly.Location;
            var originalTime = File.GetLastWriteTimeUtc(originalExecServerAppPath);


            finalExecServerPath = Path.Combine(Path.GetDirectoryName(executablePath),
                Path.GetFileNameWithoutExtension(executablePath) + "_ExecServer" + (serverInstanceIndex > 0 ? "" + serverInstanceIndex : string.Empty) + ".exe");

            // Avoid locking ExecServer.exe original file, so we are using the name of the executable path and append _ExecServer.exe
            var copyExecFile = false;
            if (File.Exists(finalExecServerPath))
            {
                var copyExecServerTime = File.GetLastWriteTimeUtc(finalExecServerPath);
                // If exec server has changed, we need to copy the new version to it
                copyExecFile = originalTime != copyExecServerTime;
            }
            else
            {
                copyExecFile = true;
            }

            if (copyExecFile)
            {
                try
                {
                    File.Copy(originalExecServerAppPath, finalExecServerPath, true);

                    // Copy the .config file as well
                    var executableConfigFile = executablePath + ".config";
                    if (File.Exists(executableConfigFile))
                    {
                        File.Copy(executableConfigFile, finalExecServerPath + ".config", true);
                    }
                }
                catch (IOException)
                {
                    return false;
                }
            }

            // NOTE: We are not using Process.Start as it is for some unknown reasons blocking the process calling this process on Process.ExitProcess
            // Handling the creation of the process with Win32 function directly solves this. Not sure why.
            // TODO: We might want the process to not inherit environment
            var result = ProcessHelper.LaunchProcess(finalExecServerPath, $"/server \"{Assembly.GetEntryAssembly()?.Location}\" \"{executablePath}\" {serverInstanceIndex}", out processHandle, out processId);
            return result;
        }

        private static string GetEndpointAddress(string executablePath, int serverInstanceIndex)
        {
            var executableKey = Regex.Replace(executablePath, "[:\\/#]", "_");
            var address = "net.pipe://localhost/" + executableKey + "_" + serverInstanceIndex;
            return address;
        }

        private static string ExtractPath(List<string> args, string type)
        {
            if (args.Count == 0)
            {
                throw new InvalidOperationException(string.Format("Expecting path to {0} argument", type));
            }

            var path = args[0];
            args.RemoveAt(0);
            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            return path;
        }

        private class ExecServerRemoteClient : DuplexClientBase<IExecServerRemote>
        {
            public ExecServerRemoteClient(IServerLogger logger, Binding binding, EndpointAddress remoteAddress)
                : base(logger, binding, remoteAddress) { }
        }

        /// <summary>
        /// Loggers that receive logs from the exec server for the running app.
        /// </summary>
        [CallbackBehavior(UseSynchronizationContext = false, AutomaticSessionShutdown = true)]
        private class RedirectLogger : IServerLogger
        {
            public void OnLog(string text, ConsoleColor color)
            {
                var backupColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.Out.WriteLine(text);
                Console.ForegroundColor = backupColor;
            }
        }

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetExitCodeProcess(IntPtr hProcess, out int lpExitCode);

        private const int PROCESS_STILL_ACTIVE = 259;
    }
}
