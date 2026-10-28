// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.IO;

namespace Stride.GameStudio.Avalonia;

/// <summary>
/// What the command line asks of the Game Studio: the session to open and the services to turn off. The desktop host
/// parses the arguments before the Avalonia application starts and hands them to <see cref="App.Options"/>.
/// </summary>
public sealed class StartupOptions
{
    /// <summary>
    /// The solution or package to open, when a path was given; otherwise the startup session of the settings or the last
    /// session decide.
    /// </summary>
    public UFile? InitialSessionPath { get; set; }

    /// <summary>
    /// <c>/NewProject</c>: start without a session, with the project selection window (a new project or a recent one).
    /// </summary>
    public bool NewProject { get; set; }

    /// <summary>
    /// <c>/DisableThumbnails</c> turns the thumbnail service off.
    /// </summary>
    public bool EnableThumbnails { get; set; } = true;

    /// <summary>
    /// <c>/DisablePreview</c> turns the asset preview service off.
    /// </summary>
    public bool DisablePreview { get; set; }

    /// <summary>
    /// <c>/DebugEditorGraphics</c> creates the editor's graphics devices in debug mode.
    /// </summary>
    public bool DebugEditorGraphics { get; set; }

    /// <summary>
    /// <c>--viewport-readback</c>: the viewports read their frames back through the CPU instead of sharing them with the
    /// compositor through its GPU interop, for support when a driver misbehaves.
    /// </summary>
    public bool ViewportReadback { get; set; }

    /// <summary>
    /// Called once after the main window is first loaded, to tell the launcher that the Game Studio has started.
    /// </summary>
    public Action? LauncherNotifier { get; set; }

    /// <summary>
    /// The arguments the parser did not recognize.
    /// </summary>
    public List<string> UnknownArguments { get; } = [];

    /// <summary>
    /// The options a restarted editor gets again, in the order of the command line, with their value if they take one: all the
    /// recognized ones except the session (a path, <c>/NewProject</c>).
    /// </summary>
    public List<(string Option, string? Value)> ForwardedArguments { get; } = [];

    /// <summary>
    /// Parses the command line of the WPF Game Studio: <c>/NewProject</c>, <c>/DisableThumbnails</c>, <c>/DisablePreview</c>,
    /// <c>/DebugEditorGraphics</c>, a session path; the launcher handshake (<c>/LauncherWindowHandle</c>, <c>/LauncherPipe</c>)
    /// and the host selection (<c>--graphics-api</c>) take a value and are consumed by the host. <c>--project &lt;path&gt;</c>
    /// (or <c>/project</c>) names the session explicitly, as the earlier Avalonia prototype did. <c>--viewport-readback</c> is
    /// specific to the Avalonia Game Studio.
    /// </summary>
    public static StartupOptions Parse(IReadOnlyList<string> args)
    {
        var options = new StartupOptions();
        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "/LauncherWindowHandle":
                case "/LauncherPipe":
                case "--graphics-api":
                case "/RecordEffects":
                    // Consumed by the host (or not supported yet): skip the value
                    options.ForwardedArguments.Add((args[i], i + 1 < args.Count ? args[i + 1] : null));
                    i++;
                    break;
                case "--project" or "/project" when i + 1 < args.Count:
                    options.InitialSessionPath = args[++i];
                    options.NewProject = false;
                    break;
                case "/NewProject":
                    options.NewProject = true;
                    options.InitialSessionPath = null;
                    break;
                case "/DisableThumbnails":
                    options.EnableThumbnails = false;
                    options.ForwardedArguments.Add((args[i], null));
                    break;
                case "/DisablePreview":
                    options.DisablePreview = true;
                    options.ForwardedArguments.Add((args[i], null));
                    break;
                case "/DebugEditorGraphics":
                    options.DebugEditorGraphics = true;
                    options.ForwardedArguments.Add((args[i], null));
                    break;
                case "--viewport-readback":
                    options.ViewportReadback = true;
                    options.ForwardedArguments.Add((args[i], null));
                    break;
                // An absolute path starts with a slash on Linux and macOS: a session file is never an option
                case var arg when (arg.StartsWith('/') || arg.StartsWith("--", StringComparison.Ordinal)) && !IsSessionFile(arg):
                    options.UnknownArguments.Add(arg);
                    break;
                case var path:
                    options.InitialSessionPath = path;
                    options.NewProject = false;
                    break;
            }
        }
        return options;
    }

    /// <summary>
    /// The command line of a new editor process that opens another session, as the WPF Game Studio builds it (<c>Program.RestartArgs</c>):
    /// the options of this command line, then the session to open, or <c>/NewProject</c> for the project selection.
    /// </summary>
    /// <param name="sessionPath">The session to open; <c>null</c> for the project selection.</param>
    /// <param name="launcherNotified">Whether this editor told the launcher that it started: the new one does not again.</param>
    public List<string> GetRestartArguments(UFile? sessionPath, bool launcherNotified)
    {
        var result = new List<string>();
        foreach (var (option, value) in ForwardedArguments)
        {
            if (launcherNotified && option.StartsWith("/Launcher", StringComparison.Ordinal))
                continue;
            result.Add(option);
            if (value is not null)
                result.Add(value);
        }
        result.Add(sessionPath is not null ? sessionPath.ToOSPath() : "/NewProject");
        return result;
    }

    /// <summary>
    /// Whether an argument names a session file: a solution, a package or a project, by its extension or because it exists.
    /// </summary>
    private static bool IsSessionFile(string arg)
    {
        var extension = Path.GetExtension(arg);
        return Core.Solutions.Solution.SolutionExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
            || string.Equals(extension, Package.PackageFileExtension, StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase)
            || File.Exists(arg);
    }
}
