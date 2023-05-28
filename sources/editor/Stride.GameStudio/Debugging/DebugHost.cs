// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using ServiceWire.NamedPipes;
using Stride.Core.Diagnostics;
using Stride.Core.VisualStudio;
using Stride.Debugger.Target;

namespace Stride.GameStudio.Debugging
{
    /// <summary>
    /// Controls a <see cref="GameDebuggerHost"/>, the spawned process and its IPC communication.
    /// </summary>
    class DebugHost : IDisposable
    {
        private AttachedChildProcessJob attachedChildProcessJob;
        public NpHost ServiceHost { get; private set; }
        public GameDebuggerHost GameHost { get; private set; }

        public void Start(string workingDirectory, Process debuggerProcess, LoggerResult logger)
        {
            var gameHostAssembly = typeof(GameDebuggerTarget).Assembly.Location;

            var address = "Stride/Debugger/" + Guid.NewGuid();
            var arguments = $"--host=\"{address}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = gameHostAssembly,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            // Start ServiceWire pipe
            var gameDebuggerHost = new GameDebuggerHost(logger);
            ServiceHost = new NpHost(address, null, null);
            ServiceHost.AddService<IGameDebuggerHost>(gameDebuggerHost);
            ServiceHost.Open();

            var process = new Process { StartInfo = startInfo };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Make sure proces will be killed if our process is finished unexpectedly
            attachedChildProcessJob = new AttachedChildProcessJob(process);

            GameHost = gameDebuggerHost;
        }

        public void Stop()
        {
            if (attachedChildProcessJob != null)
            {
                attachedChildProcessJob.Dispose();
                attachedChildProcessJob = null;
            }
            if (ServiceHost != null)
            {
                ServiceHost.Close();
                ServiceHost = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
