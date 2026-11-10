// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.GameStudio.Avalonia.Services;

/// <summary>
/// Opens another session in a new editor process, as the WPF Game Studio does (<c>Program.Restart</c>). A process loads the
/// assemblies of one session's packages only: they register their serializers for the whole process, and a second
/// <c>PackageSession</c> would load them again ("Two different classes have the same DataContract Alias").
/// </summary>
/// <remarks>
/// One instance lives as long as the process: the editor restarts in place (<see cref="App.Restart"/>) without a session, and
/// keeps it.
/// </remarks>
internal sealed class EditorProcessService
{
    private static readonly Logger Log = GlobalLogger.GetLogger("GameStudio");

    private readonly IEditorProcessLauncher launcher;
    private readonly StartupOptions options;

    public EditorProcessService(IEditorProcessLauncher launcher, StartupOptions options)
    {
        this.launcher = launcher;
        this.options = options;
    }

    /// <summary>
    /// Whether this process loaded a session, or started to: the next one opens in a new process.
    /// </summary>
    public bool HasLoadedSession { get; private set; }

    /// <summary>
    /// Records that a session is about to load in this process (it may load package assemblies even when it fails).
    /// </summary>
    public void NotifySessionLoading()
    {
        HasLoadedSession = true;
    }

    /// <summary>
    /// Starts a new editor on a session, or on the project selection, and exits this one. The session of this editor must be
    /// closed already: the main window then closes without asking.
    /// </summary>
    /// <param name="sessionPath">The session the new editor opens; <c>null</c> for the project selection (<c>/NewProject</c>).</param>
    public void Restart(UFile? sessionPath)
    {
        // The new editor does not tell the launcher again that it started
        var arguments = options.GetRestartArguments(sessionPath, launcherNotified: App.LauncherNotifier is null);
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // The new editor starts once this one is gone, as WPF's starts when its main window has closed: the window saved
            // its settings and the preview game has exited
            desktop.Exit += (_, _) => Start(arguments);
            if (desktop.ShutdownMode == ShutdownMode.OnMainWindowClose && desktop.MainWindow is { } mainWindow)
                mainWindow.Close();
            else
                desktop.TryShutdown();
        }
        else
        {
            // A host without a desktop lifetime owns its process
            Start(arguments);
        }
    }

    private void Start(IReadOnlyList<string> arguments)
    {
        try
        {
            launcher.Start(arguments);
        }
        catch (Exception e)
        {
            Log.Error($"The editor could not start again with [{string.Join(' ', arguments)}].", e);
        }
    }
}
