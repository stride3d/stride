// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Xenko.Core;
using Xenko.Engine.Network;

namespace Xenko.Samples.Tests
{
    //This is how we inject the assembly to run automatically at game start, paired with Xenko.targets and the msbuild property XenkoAutoTesting
    internal class LocalConnectionRouterInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            // Locate connection router
            var connectionRouterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Xenko.ConnectionRouter.exe");
            if (!File.Exists(connectionRouterPath))
                throw new InvalidOperationException("Connection router not found");

            // Kill any existing connection router
            foreach (var process in Process.GetProcessesByName("Xenko.ConnectionRouter"))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit();
                    break;
                }
                catch (Exception)
                {
                }
            }

            // Start connection router
            var connectionRouterProcess = Process.Start(connectionRouterPath);
            // attach job so that it gets killed when tests are finished
            new AttachedChildProcessJob(connectionRouterProcess);

            // Wait for port to open
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                // Try during 5 seconds (10 * 500 msec)
                for (int i = 0; i < 10; ++i)
                {
                    try
                    {
                        socket.Connect("localhost", RouterClient.DefaultPort);
                    }
                    catch (SocketException)
                    {
                        // Try again in 500 msec
                        Thread.Sleep(500);
                        continue;
                    }
                    break;
                }
            }
        }
    }
}
