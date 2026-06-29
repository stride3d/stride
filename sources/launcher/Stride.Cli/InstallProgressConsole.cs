// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading;
using Stride.Cli.Core;

// Renders install/update/uninstall progress on a single console line: a spinner with the downloaded size
// during download, then "installing X/Y (package)" (or "removing X/Y" on uninstall) as packages are processed.
// Falls back to plain discrete lines when output is redirected (logs/CI).
internal sealed class InstallProgressConsole : IProgress<InstallProgress>, IDisposable
{
    private static readonly char[] Frames = ['|', '/', '-', '\\'];
    private readonly bool plain = Console.IsOutputRedirected;
    private readonly object gate = new();
    private readonly Timer? spinner;
    private InstallProgress current;
    private int frame;
    private int lastLength;
    private bool wrote;
    private bool disposed;
    private bool downloadAnnounced;

    public InstallProgressConsole()
    {
        // Tick the spinner so the long download phase doesn't look frozen.
        if (!plain)
            spinner = new Timer(_ => Tick(), null, 0, 120);
    }

    public void Report(InstallProgress value)
    {
        lock (gate)
        {
            current = value;
            if (plain)
            {
                // Discrete lines; download progress fires repeatedly, so announce it only once.
                if (value.Stage == InstallStage.Downloading && !downloadAnnounced)
                {
                    downloadAnnounced = true;
                    Console.WriteLine($"Stride {value.Version}: downloading...");
                }
                else if (value.Stage == InstallStage.SettingUp)
                    Console.WriteLine($"Stride {value.Version}: setting up {value.Package}...");
            }
            else
            {
                Render();
            }
        }
    }

    private void Tick()
    {
        lock (gate)
        {
            if (disposed)
                return;
            frame = (frame + 1) % Frames.Length;
            Render();
        }
    }

    private void Render()
    {
        var text = current.Version is null
            ? "Resolving Stride version"
            : current.Stage switch
            {
                InstallStage.Installing => $"Stride {current.Version}: installing {current.Completed}/{current.Total} ({current.Package})",
                InstallStage.SettingUp => $"Stride {current.Version}: setting up {current.Package}",
                InstallStage.Removing => $"Stride {current.Version}: removing {current.Completed}/{current.Total} ({current.Package})",
                _ when current.DownloadedBytes > 0 => $"Stride {current.Version}: downloading {Mb(current.DownloadedBytes)} MB",
                // No bytes flowing (resolving, or restoring from the local cache).
                _ => $"Stride {current.Version}: restoring",
            };
        var line = $"  {Frames[frame]} {text}";
        Console.Write('\r' + line.PadRight(lastLength));
        lastLength = line.Length;
        wrote = true;
    }

    private static string Mb(long bytes) => (bytes / 1048576.0).ToString("0.0");

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
                return;
            disposed = true;
        }
        spinner?.Dispose();
        // Finish the in-place line so the caller's next message starts cleanly.
        if (!plain && wrote)
            Console.WriteLine();
    }
}
