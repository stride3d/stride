// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Linq;
using Avalonia;
using Stride.CrashReport;

namespace Stride.CrashReporter;

/// <summary>
/// The out-of-process crash reporter. A headless tool that crashed (or its native fault handler) spawns this,
/// pointing it at the run directory the crashes were written to; it shows the report window and sends what the
/// user approves. Two forms:
/// <list type="bullet">
/// <item><c>Stride.CrashReporter &lt;run-directory&gt; [--dsn &lt;url&gt;]</c> — show a run written by the host.</item>
/// <item><c>Stride.CrashReporter &lt;run-directory&gt; --host-pid &lt;pid&gt; [--dsn &lt;url&gt;]</c> — a managed crash of the
/// host itself, which stays alive, blocked, until this window closes; its pid lets the window write a full memory
/// dump of it on demand.</item>
/// <item><c>Stride.CrashReporter --capture &lt;pid&gt; &lt;tid&gt; &lt;exception-pointers&gt; --event &lt;name&gt; --dump-dir &lt;dir&gt; [--dsn &lt;url&gt;]</c>
/// — capture a crashing host's dump from the outside (its native trigger is frozen waiting on the event), then
/// show it. See <see cref="NativeCapture"/>.</item>
/// </list>
/// </summary>
internal static class Program
{
    /// <summary>The loaded run, handed to the app once the arguments are validated (Avalonia owns Main's flow).</summary>
    internal static CrashSession? Session { get; private set; }

    [STAThread]
    private static int Main(string[] args)
    {
        string? runDirectory;
        if (Array.IndexOf(args, "--capture") >= 0)
        {
            // The crashing host is frozen waiting on the event; capture its dump before doing anything slower.
            runDirectory = GetOption(args, "--dump-dir");
            if (string.IsNullOrEmpty(runDirectory))
            {
                Console.Error.WriteLine("Usage: Stride.CrashReporter --capture <pid> <tid> <exception-pointers> --event <name> --dump-dir <dir>");
                return 1;
            }
            NativeCapture.Capture(args, runDirectory);

            // Unattended (save/send/CI): the dump and report are saved to the run; don't pop a window.
            if (CrashPolicy.ResolveAction() != CrashAction.Report)
                return 0;
        }
        else
        {
            runDirectory = args.FirstOrDefault(argument => !argument.StartsWith("--", StringComparison.Ordinal));
            if (string.IsNullOrEmpty(runDirectory) || !Directory.Exists(runDirectory))
            {
                Console.Error.WriteLine("Usage: Stride.CrashReporter <run-directory> [--dsn <url>]");
                return 1;
            }
        }

        // Only a plain run-directory launch is a live GameStudio session (the compiler-routing path); a --capture or
        // --host-pid launch is a host that itself crashed and is exiting, so a send there suppresses nothing session-scoped.
        var hostProcessId = int.TryParse(GetOption(args, "--host-pid"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid) ? pid : (int?)null;
        var sessionScoped = Array.IndexOf(args, "--capture") < 0 && hostProcessId is null;
        var session = CrashSession.Load(runDirectory, GetOption(args, "--dsn"), sessionScoped, hostProcessId);
        // Nothing to ask about (empty run, or every signature already suppressed): exit quietly, no window.
        if (session.Groups.Count == 0)
            return 0;

        Session = session;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    // Avalonia configuration, don't remove; also used by the visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static string? GetOption(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
