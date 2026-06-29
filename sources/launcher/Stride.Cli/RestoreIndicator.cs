// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading;

// Shows a transient spinner on stderr while a NuGet restore runs, but only after a short delay so the common
// fast no-op (~0.7s) stays silent and an occasional slow restore isn't a mystery pause. Wired to
// StrideVersionManager's RestoreStarting / RestoreFinished events.
internal sealed class RestoreIndicator
{
    private static readonly char[] Frames = ['|', '/', '-', '\\'];
    private readonly bool plain = Console.IsErrorRedirected;
    private readonly object gate = new();
    private Timer? timer;
    private string project = "";
    private int frame;
    private int lastLength;
    private bool shown;

    public void Start(string projectName)
    {
        lock (gate)
        {
            project = projectName;
            frame = 0;
            shown = false;
            timer?.Dispose();
            // First tick after ~1s (so a fast no-op restore prints nothing), then spin.
            timer = new Timer(_ => Tick(), null, 1000, 120);
        }
    }

    public void Stop()
    {
        lock (gate)
        {
            timer?.Dispose();
            timer = null;
            if (shown && !plain)
                Console.Error.Write('\r' + new string(' ', lastLength) + '\r');
            shown = false;
            lastLength = 0;
        }
    }

    private void Tick()
    {
        lock (gate)
        {
            if (timer is null)
                return;

            if (plain)
            {
                if (!shown)
                {
                    shown = true;
                    Console.Error.WriteLine($"Restoring {project}...");
                }
                return;
            }

            shown = true;
            var line = $"  {Frames[frame]} Restoring {project}...";
            frame = (frame + 1) % Frames.Length;
            Console.Error.Write('\r' + line.PadRight(lastLength));
            lastLength = line.Length;
        }
    }
}
