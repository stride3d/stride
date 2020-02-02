// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Xenko.Core.VisualStudio;
using static Xenko.ExecServer.AppDomainShadow;
using static Xenko.ExecServer.ExecServerApp;

namespace Xenko.ExecServer
{
    /// <summary>
    /// Remote implementation of the ServiceWire server
    /// </summary>
    internal class ExecServerRemote : IExecServerRemote
    {
        public const int BusyReturnCode = -8000;

        private const int ShutdownExecServerAfterSeconds = 60;
        private const int DisposeAppDomainsAfterSeconds = 60;
        private const int ShutdownExecServerAfterSecondsMain = 10 * 60;
        private const int DisposeAppDomainsAfterSecondsMain = 10 * 60;

        private readonly AppDomainShadowManager shadowManager;

        private readonly Thread trackingThread;

        private readonly Stopwatch upTime;

        private readonly object runLock = new object();

        private readonly bool isMainDomain;

        public event EventHandler<EventArgs> ShuttingDown;

        public ExecServerRemote(string entryAssemblyPath, string executablePath, bool trackingServer, bool cachingAppDomain, bool isMainDomain)
        {
            // TODO: List of native dll directory is hardcoded here. Instead, it should be extracted from .exe.config file for example
            shadowManager = new AppDomainShadowManager(entryAssemblyPath, executablePath, new[] { IntPtr.Size == 8 ? "x64" : "x86" })
            {
                IsCachingAppDomain = cachingAppDomain
            };

            this.isMainDomain = isMainDomain;

            upTime = Stopwatch.StartNew();

            if (trackingServer)
            {
                trackingThread = new Thread(TrackExecutablePath)
                {
                    IsBackground = true
                };

                trackingThread.Start();
            }
        }

        public void Check()
        {
        }

        public int Run(string currentDirectory, Dictionary<string, string> environmentVariables, string[] args, bool shadowCache, int? debuggerProcessId, IServerLogger logger)
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(runLock, ref lockTaken);
                if (!lockTaken)
                {
                    // Busy, exit right away
                    return BusyReturnCode;
                }

                // Do your stuff...
                Console.WriteLine("Run Received {0}", string.Join(" ", args));

                upTime.Restart();

                if (debuggerProcessId.HasValue && !Debugger.IsAttached)
                {
                    using (var debugger = VisualStudioDebugger.GetByProcess(debuggerProcessId.Value))
                    {
                        debugger?.Attach();
                    }
                }

                var result = shadowManager.Run(currentDirectory, environmentVariables, args, shadowCache,logger);
                return result;
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(runLock);
            }
        }

        public void Wait()
        {
            if (trackingThread != null)
            {
                trackingThread.Join();

                shadowManager.Dispose();
            }
        }

        private void TrackExecutablePath()
        {
            var thisAssembly = typeof(ExecServerRemote).Assembly;
            var assemblyName = thisAssembly.GetCustomAttributes<AssemblyProductAttribute>().First();
            var originalAssemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyName.Product) + ".exe";
            var assemblyPath = thisAssembly.Location;
            var thisAssemblyTime = File.GetLastWriteTimeUtc(assemblyPath);

            var shutdownExecServerAfterSeconds = isMainDomain ? ShutdownExecServerAfterSecondsMain : ShutdownExecServerAfterSeconds;
            var disposeAppDomainsAfterSeconds = isMainDomain ? DisposeAppDomainsAfterSecondsMain : DisposeAppDomainsAfterSeconds;

            try
            {
                while (true)
                {
                    Thread.Sleep(500);

                    var localUpTime = GetUpTime();
                    if (localUpTime > TimeSpan.FromSeconds(shutdownExecServerAfterSeconds))
                    {
                        Console.WriteLine("Shutdown server after {0}s of inactivity", shutdownExecServerAfterSeconds);
                        break;
                    }


                    // If this exec server is no longer up-to-date with its original exe, we can close it
                    if (!File.Exists(originalAssemblyPath) || File.GetLastWriteTimeUtc(originalAssemblyPath) != thisAssemblyTime)
                    {
                        Console.WriteLine("Shutdown server as original exe [{0}] has changed", originalAssemblyPath);
                        break;
                    }

                    shadowManager.Recycle(TimeSpan.FromSeconds(disposeAppDomainsAfterSeconds));
                }
            }
            finally
            {

                OnShuttingDown();
            }
        }

        private TimeSpan GetUpTime()
        {
            lock (upTime)
            {
                return upTime.Elapsed;
            }
        }

        private void OnShuttingDown()
        {
            ShuttingDown?.Invoke(this, EventArgs.Empty);
        }
    }
}
