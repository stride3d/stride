// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core.Assets.Editor.Internal;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Settings;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Components.Build;

/// <summary>
/// Reloads the game code when it changes, as the debugging view model of the WPF editor reloads the game assemblies: it watches the
/// code of the projects the editor loads (<see cref="GameCodeWatcher"/>), asks whether to reload, and reloads.
/// </summary>
/// <remarks>
/// <para>
/// Reloading builds the changed projects, so that an error shows in the Output pane and keeps the session as it is, then opens the
/// session again in a fresh editor (the delegate given to the constructor, File &gt; Reload project): the new editor loads the new
/// assemblies. The WPF editor reloads them in its process instead.
/// </para>
/// <para>
/// The question honors the settings of the WPF editor: <see cref="EditorSettings.AskBeforeReloadingAssemblies"/> and, when it does not
/// ask, <see cref="EditorSettings.AutoReloadAssemblies"/>. It is asked once while changes are pending, when the main window is in
/// front, and after the build in progress. The changes stay pending until a reload: <see cref="ReloadCommand"/> reloads them later.
/// </para>
/// </remarks>
public sealed class GameCodeReloadViewModel : DispatcherViewModel
{
    private static readonly StringComparer PathComparer = OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

    private readonly BuildAndRunViewModel build;
    private readonly Func<Task<bool>> reloadSession;
    // The project files of the changed projects
    private readonly HashSet<string> changedProjects = new(PathComparer);
    private SessionViewModel? session;
    private GameCodeWatcher? watcher;
    private CancellationTokenSource? sessionCancellation;
    private bool hasPendingChanges;
    // A failed build keeps its changes pending: the next change asks again, as in WPF
    private bool askOnNextChange;
    // The question waits for the build in progress, or for the main window
    private bool questionRequested;
    private bool asking;
    private bool reloading;

    /// <param name="serviceProvider">The services of the editor: the dialogs.</param>
    /// <param name="build">The builds of the editor: the reload builds the changed projects with it, and waits for its builds.</param>
    /// <param name="reloadSession">Opens the session again in a fresh editor, after asking whether to save it; <c>false</c> when the
    /// user kept the session.</param>
    public GameCodeReloadViewModel(IViewModelServiceProvider serviceProvider, BuildAndRunViewModel build, Func<Task<bool>> reloadSession)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(build);
        ArgumentNullException.ThrowIfNull(reloadSession);
        this.build = build;
        this.reloadSession = reloadSession;
        ReloadCommand = new AnonymousTaskCommand(serviceProvider, OnReload) { IsEnabled = false };
        build.PropertyChanged += BuildPropertyChanged;
    }

    /// <summary>
    /// The session whose code is watched; <c>null</c> when none is open.
    /// </summary>
    public SessionViewModel? Session
    {
        get => session;
        set => SetValue(ref session, value, OnSessionChanged);
    }

    /// <summary>
    /// Whether the code changed since the session was opened, and was not reloaded yet.
    /// </summary>
    public bool HasPendingChanges
    {
        get => hasPendingChanges;
        private set => SetValue(ref hasPendingChanges, value);
    }

    /// <summary>
    /// The project files of the projects whose code changed and was not reloaded yet.
    /// </summary>
    public IReadOnlyCollection<string> ChangedProjects => [.. changedProjects];

    /// <summary>
    /// Builds the changed projects and opens the session again in a fresh editor (WPF's Reload game assemblies).
    /// </summary>
    public ICommandBase ReloadCommand { get; }

    /// <summary>
    /// Watches the code of a session; the tests give a watcher that does not watch the file system.
    /// </summary>
    internal Func<SessionViewModel, GameCodeWatcher> CreateWatcher { get; init; } = GameCodeWatcher.Create;

    /// <summary>
    /// The profile of the settings; <c>null</c> for the current profile of the editor settings.
    /// </summary>
    internal SettingsProfile? SettingsProfile { get; init; }

    /// <summary>
    /// Saves the settings after the user checked "Don't ask again".
    /// </summary>
    internal Action SaveSettings { get; init; } = EditorSettings.Save;

    /// <summary>
    /// The last question, with the reload it started; the tests wait for it.
    /// </summary>
    internal Task Question { get; private set; } = Task.CompletedTask;

    private IEditorDialogService Dialogs => ServiceProvider.Get<IEditorDialogService>();

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(GameCodeReloadViewModel));
        Session = null;
        build.PropertyChanged -= BuildPropertyChanged;
        base.Destroy();
    }

    /// <summary>
    /// Handles a burst of changes of the code: the first one asks whether to reload.
    /// </summary>
    internal void OnCodeChanged(IReadOnlyList<GameCodeChange> changes)
    {
        if (session is null || changes.Count == 0)
            return;

        // One question while changes are pending, as in WPF: they can be reloaded later with the command
        var shouldAsk = !HasPendingChanges || askOnNextChange;
        foreach (var change in changes)
            changedProjects.Add(change.ProjectPath);
        HasPendingChanges = true;
        UpdateCommands();
        if (shouldAsk)
        {
            askOnNextChange = false;
            questionRequested = true;
            TryAsk();
        }
    }

    private void OnSessionChanged()
    {
        if (watcher is not null)
        {
            watcher.Changed -= WatcherChanged;
            watcher.Dispose();
            watcher = null;
        }
        sessionCancellation?.Cancel();
        sessionCancellation?.Dispose();
        sessionCancellation = null;
        changedProjects.Clear();
        HasPendingChanges = false;
        askOnNextChange = false;
        questionRequested = false;

        if (session is not null)
        {
            sessionCancellation = new CancellationTokenSource();
            watcher = CreateWatcher(session);
            watcher.Changed += WatcherChanged;
        }
        UpdateCommands();
    }

    private void WatcherChanged(object? sender, GameCodeChangedEventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            // A burst of the watcher of a session that closed since
            if (ReferenceEquals(sender, watcher))
                OnCodeChanged(e.Changes);
        });
    }

    private void BuildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BuildAndRunViewModel.BuildInProgress))
            return;

        UpdateCommands();
        // The question waited for the end of the build
        TryAsk();
    }

    private void TryAsk()
    {
        if (!questionRequested || asking || reloading || build.BuildInProgress || sessionCancellation is null)
            return;

        asking = true;
        Question = AskAsync(sessionCancellation.Token);
    }

    private async Task AskAsync(CancellationToken token)
    {
        try
        {
            await Dialogs.WaitForMainWindowActiveAsync(token);
            // A build started meanwhile: the question waits for its end; the user reloaded with the command
            if (token.IsCancellationRequested || build.BuildInProgress || !questionRequested)
                return;
            questionRequested = false;

            bool reload;
            if (GetSetting(EditorSettings.AskBeforeReloadingAssemblies))
            {
                var message = Tr._p("Message", "Some game code files have been modified. Do you want to reload the assemblies?") + Environment.NewLine + Environment.NewLine +
                              Tr._p("Message", "The editor restarts on the project to load them.");
                var buttons = DialogButtons.Create([Tr._p("Button", "Reload"), Tr._p("Button", "Don't reload")], 1, 2);
                var result = await Dialogs.CheckedMessageBoxAsync(message, false, Tr._p("Settings", "Don't ask again"), buttons, MessageBoxImage.Question);
                // Closed without an answer: the changes stay pending
                if (result.Result == 0 || token.IsCancellationRequested)
                    return;

                reload = result.Result == 1;
                if (result.IsChecked == true)
                {
                    SetSetting(EditorSettings.AskBeforeReloadingAssemblies, false);
                    SetSetting(EditorSettings.AutoReloadAssemblies, reload);
                    SaveSettings();
                }
            }
            else
            {
                reload = GetSetting(EditorSettings.AutoReloadAssemblies);
            }

            if (reload)
                await ReloadAsync();
        }
        catch (OperationCanceledException)
        {
            // The session closed
        }
        finally
        {
            asking = false;
        }
    }

    private Task OnReload()
    {
        // The command answers the question that waits
        questionRequested = false;
        return ReloadAsync();
    }

    /// <summary>
    /// Builds the changed projects, then opens the session again in a fresh editor. The changes stay pending when the build fails
    /// (the next change asks again) or the user keeps the session.
    /// </summary>
    private async Task ReloadAsync()
    {
        if (!HasPendingChanges || session is not { } reloadedSession || reloading || build.BuildInProgress)
            return;

        reloading = true;
        UpdateCommands();
        try
        {
            var outcome = await build.BuildCodeAsync(GetProjectsToBuild(reloadedSession));
            if (outcome == GameBuildOutcome.Failed)
                askOnNextChange = true;
            if (outcome != GameBuildOutcome.Succeeded || session != reloadedSession)
                return;

            await reloadSession();
        }
        finally
        {
            reloading = false;
            UpdateCommands();
        }
    }

    /// <summary>
    /// The changed projects and the watched projects that depend on them, dependencies first: the new editor builds them all.
    /// </summary>
    private List<string> GetProjectsToBuild(SessionViewModel buildSession)
    {
        var codeProjects = (watcher?.CodeProjects ?? []).Concat(changedProjects).Distinct(PathComparer).ToList();
        var packages = buildSession.LocalPackages.OfType<ProjectViewModel>()
            .Where(x => codeProjects.Contains(Path.GetFullPath(x.ProjectPath.ToOSPath()), PathComparer))
            .ToDictionary(x => Path.GetFullPath(x.ProjectPath.ToOSPath()), PathComparer);
        var changedPackages = changedProjects.Select(x => packages.GetValueOrDefault(x)?.Package).NotNull().ToList();

        var result = new List<(string Path, int Depth)>();
        foreach (var projectPath in codeProjects)
        {
            var package = packages.GetValueOrDefault(projectPath);
            var dependencies = package?.PackageContainer.FlattenedDependencies.Select(x => x.Package).NotNull().ToList() ?? [];
            if (changedProjects.Contains(projectPath) || dependencies.Any(changedPackages.Contains))
                result.Add((projectPath, dependencies.Count));
        }
        // A project depends on fewer projects than those that depend on it
        return result.OrderBy(x => x.Depth).Select(x => x.Path).ToList();
    }

    private bool GetSetting(SettingsKey<bool> key) => SettingsProfile is { } profile ? key.GetValue(profile, true) : key.GetValue();

    private void SetSetting(SettingsKey<bool> key, bool value)
    {
        if (SettingsProfile is { } profile)
            key.SetValue(value, profile);
        else
            key.SetValue(value);
    }

    private void UpdateCommands()
    {
        ReloadCommand.IsEnabled = session is not null && HasPendingChanges && !reloading && !build.BuildInProgress;
    }
}
