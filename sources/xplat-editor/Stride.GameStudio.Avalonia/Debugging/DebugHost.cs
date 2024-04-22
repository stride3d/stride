// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using ServiceWire.NamedPipes;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Debugger.Target;

namespace Stride.GameStudio.Avalonia.Debugging;

internal sealed class DebugHost : IDisposable
{
#if FIXME_xplat_editor
    private AttachedChildProcessJob? attachedChildProcessJob;
#else
    private Process? process;
#endif

    public GameDebuggerHost? GameHost { get; private set; }
    public NpHost? ServiceHost { get; private set; }

    public void Dispose()
    {
        Stop();
    }

    internal void Start(UDirectory workingDirectory, LoggerResult logger)
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

        process = new Process { StartInfo = startInfo };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

#if FIXME_xplat_editor
        // Make sure proces will be killed if our process is finished unexpectedly
        attachedChildProcessJob = new AttachedChildProcessJob(process);
#endif
        GameHost = gameDebuggerHost;
    }

    internal void Stop()
    {
#if FIXME_xplat_editor
        if (attachedChildProcessJob is not null)
        {
            attachedChildProcessJob.Dispose();
            attachedChildProcessJob = null;
        }
#else
        if (process is not null)
        {
            try
            {
                process.Kill();
            }
            catch { }
            process.Dispose();
            process = null;
        }
#endif

        if (GameHost is not null)
        {
            GameHost.Dispose();
            GameHost = null;
        }
        if (ServiceHost is not null)
        {
            ServiceHost.Close();
            ServiceHost = null;
        }
    }
}
