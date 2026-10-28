// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Components.Transactions;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.MostRecentlyUsedFiles;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;
using Stride.Editor.Build;
using Stride.GameStudio.Avalonia.Helpers;
using Stride.GameStudio.Avalonia.Services;
using Stride.GameStudio.Avalonia.Settings;

namespace Stride.GameStudio.Avalonia.ViewModels;

internal sealed class MainViewModel : ViewModelBase, IMainViewModel
{
    private const int RecentFilesCount = 10;
    private static readonly string baseTitle = StrideGameStudio.EditorTitle;
    private static bool msBuildFound;
    private SessionViewModel? session;
    // The session whose unsaved changes the title shows
    private SessionViewModel? titleSession;
    private ActionHistoryViewModel? history;
    private PreviewViewModel? preview;
    private UFile? sessionPath;
    private string? sessionName;
    private string title = baseTitle;

#if DEBUG
    // Note: only required for the Avalonia designer
    public MainViewModel()
        : this(ViewModelServiceProvider.NullServiceProvider)
    { }
#endif

    public MainViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        AboutCommand = new AnonymousTaskCommand(serviceProvider, OnAbout, () => DialogService.HasMainWindow);
        CloseCommand = new AnonymousTaskCommand(serviceProvider, OnClose);
#if DEBUG
        CrashCommand = new AnonymousCommand(serviceProvider, () => throw new Exception("Boom!"));
#else
        CrashCommand = DisabledCommand.Instance;
#endif
        ExitCommand = new AnonymousCommand(serviceProvider, OnExit, () => DialogService.HasMainWindow);
        OpenCommand = new AnonymousTaskCommand<UFile?>(serviceProvider, OnOpen);
        NewSessionCommand = new AnonymousTaskCommand(serviceProvider, OnNewSession, () => DialogService.HasMainWindow);
        SelectProjectCommand = new AnonymousTaskCommand(serviceProvider, () => SelectProjectAsync(exitOnCancel: false), () => DialogService.HasMainWindow);
        SelectInitialProjectCommand = new AnonymousTaskCommand(serviceProvider, () => SelectProjectAsync(exitOnCancel: true), () => DialogService.HasMainWindow);
        OpenInitialSessionCommand = new AnonymousTaskCommand<UFile?>(serviceProvider, OnOpenInitialSession);
        ClearRecentFilesCommand = new AnonymousCommand(serviceProvider, ClearRecentFiles);
        OpenDebugWindowCommand = new AnonymousTaskCommand(serviceProvider, OnOpenDebugWindow, () => DialogService.HasMainWindow);
        OpenSettingsWindowCommand = new AnonymousTaskCommand(serviceProvider, OnOpenSettingsWindow, () => DialogService.HasMainWindow);
        OpenWebPageCommand = new AnonymousTaskCommand<string>(serviceProvider, OnOpenWebPage);
        ReloadCommand = new AnonymousTaskCommand(serviceProvider, OnReload);
        // Enabled while a session is open, even without a change, as in WPF
        SaveCommand = new AnonymousTaskCommand(serviceProvider, OnSave) { IsEnabled = false };
        UndoCommand = new AnonymousCommand(serviceProvider, () => history?.UndoCommand.Execute()) { IsEnabled = false };
        RedoCommand = new AnonymousCommand(serviceProvider, () => history?.RedoCommand.Execute()) { IsEnabled = false };

        Status = new StatusViewModel(ServiceProvider);
        Status.PushStatus("Ready");
        Notifications = serviceProvider.TryGet<INotificationService>();

        // The most recently used sessions of this and the earlier editor versions
        MRU = GameStudioInternalSettings.CreateMostRecentlyUsedFiles();
        MRU.MostRecentlyUsedFiles.CollectionChanged += (_, _) => UpdateRecentFiles();
        UpdateRecentFiles();
    }

    public SessionViewModel? Session
    {
        get => session;
        set => SetValue(ref session, value, OnSessionChanged);
    }

    public StatusViewModel Status { get; }

    /// <summary>
    /// The background operations shown in the notification bar; <c>null</c> when the host has no bar.
    /// </summary>
    public INotificationService? Notifications { get; }

    /// <summary>
    /// The asset preview panel of the session; <c>null</c> without a session.
    /// </summary>
    public PreviewViewModel? Preview
    {
        get => preview;
        private set => SetValue(ref preview, value);
    }

    public string Title
    {
        get => title;
        set => SetValue(ref title, value);
    }

    /// <summary>
    /// The most recently used sessions, as stored in the internal settings.
    /// </summary>
    public MostRecentlyUsedFileCollection MRU { get; }

    /// <summary>
    /// The recent sessions of this editor version or an earlier one, most recent first, for the File menu.
    /// </summary>
    public ObservableList<RecentSessionViewModel> RecentFiles { get; } = [];

    public ICommandBase AboutCommand { get; }

    public ICommandBase ClearRecentFilesCommand { get; }

    public ICommandBase CloseCommand { get; }

    public ICommandBase CrashCommand { get; }

    public ICommandBase ExitCommand { get; }

    /// <summary>
    /// File > New: closes the session, if any, and shows the project selection window (a new project from a template, or a recent
    /// project), as WPF's New command restarts the editor on the project selection.
    /// </summary>
    public ICommandBase NewSessionCommand { get; }

    /// <summary>
    /// Shows the project selection window over the empty editor (after <see cref="NewSessionCommand"/>). Cancelling leaves the editor empty.
    /// </summary>
    public ICommandBase SelectProjectCommand { get; }

    /// <summary>
    /// Shows the project selection window when the editor starts without a session. Cancelling exits the editor, as the WPF editor does.
    /// </summary>
    public ICommandBase SelectInitialProjectCommand { get; }

    public ICommandBase OpenCommand { get; }

    /// <summary>
    /// Opens the session the editor starts with; a crash while it loads is remembered, and the next start asks before trying again.
    /// </summary>
    public ICommandBase OpenInitialSessionCommand { get; }

    public ICommandBase OpenDebugWindowCommand { get; }

    public ICommandBase OpenSettingsWindowCommand { get; }

    public ICommandBase OpenWebPageCommand { get; }

    /// <summary>
    /// Opens the session again, after asking whether to save its changes.
    /// </summary>
    public ICommandBase ReloadCommand { get; }

    /// <summary>
    /// Saves the session (<see cref="SessionViewModel.SaveSessionCommand"/>).
    /// </summary>
    /// <remarks>
    /// The File menu binds this command rather than the session's: Avalonia evaluates the command of an item of a closed menu
    /// only when the menu opens, so the item's Ctrl+S would keep the state of the command it had when the editor started.
    /// </remarks>
    public ICommandBase SaveCommand { get; }

    /// <summary>
    /// Undoes the last action of the session (<see cref="ActionHistoryViewModel.UndoCommand"/>).
    /// </summary>
    /// <remarks>
    /// The Edit menu and the window's Ctrl+Z bind this command rather than the history's, for the same reason as <see cref="SaveCommand"/>:
    /// the command of the history changes with the session, and its state after every action.
    /// </remarks>
    public ICommandBase UndoCommand { get; }

    /// <summary>
    /// Redoes the last undone action of the session (<see cref="ActionHistoryViewModel.RedoCommand"/>).
    /// </summary>
    /// <remarks>See <see cref="UndoCommand"/>.</remarks>
    public ICommandBase RedoCommand { get; }

    private EditorDialogService DialogService => ServiceProvider.Get<EditorDialogService>();

    /// <summary>
    /// Starts the editor in a new process; <c>null</c> when the host cannot (the editor then restarts in this process).
    /// </summary>
    private EditorProcessService? ProcessService => ServiceProvider.TryGet<EditorProcessService>();

    /// <summary>
    /// Whether this process loaded a session already, even if it is closed now: the next one needs a new process.
    /// </summary>
    private bool NeedsNewProcess => ProcessService?.HasLoadedSession == true;

    /// <summary>
    /// Opens the session the editor was started with.
    /// </summary>
    /// <returns><c>true</c> when the session is open, <c>false</c> when it failed or the user declined, <c>null</c> when the user cancelled the loading.</returns>
    public async Task<bool?> OpenInitialSessionAsync(UFile? initialSessionPath, CancellationToken token = default)
    {
        if (UPath.IsNullOrEmpty(initialSessionPath))
            return false;

        // The LoadingStartupSession settings is true, which means that the editor crashed while loading the startup project last time.
        // Lets give the user a chance to fix the startup session.
        if (InternalSettings.LoadingStartupSession.GetValue())
        {
            var buttons = new[]
            {
                new DialogButtonInfo { Content = Tr._p("Button", "Try again"), Result = 1, IsDefault = true },
                new DialogButtonInfo { Content = Tr._p("Button", "Cancel"), Result = 2, IsCancel = true },
            };
            var message = string.Format(Tr._p("Message", "The last attempt to load the project **{0}** failed. \r\n\r\nDo you want to try to load it again?"), Path.GetFileName(initialSessionPath.ToOSPath()));
            var result = await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, buttons, MessageBoxImage.Warning);
            if (result != 1)
            {
                InternalSettings.LoadingStartupSession.SetValue(false);
                InternalSettings.SaveProfile();
                return false;
            }
        }

        // Safe-guard - this will warn that the startup session makes the editor crash while loading.
        InternalSettings.LoadingStartupSession.SetValue(true);
        InternalSettings.SaveProfile();

        var sessionResult = await OpenSession(initialSessionPath, token);

        InternalSettings.LoadingStartupSession.SetValue(false);
        InternalSettings.SaveProfile();

        return sessionResult;
    }

    /// <summary>
    /// Opens the session of a solution or package file, asking for one when no path is given.
    /// </summary>
    /// <returns><c>true</c> when the session is open, <c>false</c> when it failed, no file was chosen or the user kept an editor of the current session open, <c>null</c> when the user cancelled the loading.</returns>
    public async Task<bool?> OpenSession(UFile? filePath, CancellationToken token = default)
    {
        if (filePath is not null && !File.Exists(filePath.ToOSPath()))
        {
            RemoveRecentFile(filePath);
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(string.Format(Tr._p("Message", @"The file '{0}' does not exist."), filePath), MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        if (filePath is null)
        {
            filePath = await DialogService.OpenFilePickerAsync(InternalSettings.FileDialogLastOpenSessionDirectory.GetValue(), SessionFileFilters);
        }

        if (filePath is null) return false;

        // We have a session, or had one, let's restart cleanly
        if (session is not null || NeedsNewProcess)
            return await RestartAsync(filePath);

        // The session needs MSBuild to load the projects of the solution
        if (!await EnsureMSBuildAsync())
            return false;

        ProcessService?.NotifySessionLoading();
        var sessionResult = new PackageSessionResult();
        var loadedSession = await SessionViewModel.OpenSessionAsync(filePath, sessionResult, this, ServiceProvider, token);

        // Loading has failed
        if (loadedSession == null)
        {
            if (sessionResult.OperationCancelled)
            {
                // The cancelled session might have registered plugins or services, let's restart cleanly
                (Application.Current as App)?.Restart();

                // Null means the user has cancelled the loading operation.
                return null;
            }
            return false;
        }

        AttachSession(loadedSession, filePath);
        return true;
    }

    /// <summary>
    /// Creates a new session from a template (the project selection window), in an empty editor, and opens it.
    /// </summary>
    /// <returns><c>true</c> when the session is open, <c>false</c> when the generator failed or the user cancelled its parameters.</returns>
    public async Task<bool> CreateNewSessionAsync(NewSessionParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (session is not null)
            throw new InvalidOperationException("A session is open already.");

        // The generator loads the projects it creates
        if (!await EnsureMSBuildAsync())
            return false;

        ProcessService?.NotifySessionLoading();
        var createdSession = await SessionViewModel.CreateNewSessionAsync(parameters, this, ServiceProvider);
        if (createdSession is null)
        {
            // The generator might have registered plugins or services, let's restart cleanly
            (Application.Current as App)?.Restart();
            return false;
        }

        AttachSession(createdSession, createdSession.SolutionPath);
        return true;
    }

    /// <summary>
    /// Shows the project selection window: a new session from a template, or a recent project to open.
    /// </summary>
    /// <param name="exitOnCancel">
    /// Whether cancelling exits the editor (at startup, when nothing else can be done) instead of leaving it empty (after
    /// <see cref="NewSessionCommand"/>, when the user chose to close the previous session).
    /// </param>
    internal async Task SelectProjectAsync(bool exitOnCancel)
    {
        var templates = new NewOrOpenSessionTemplateCollectionViewModel(ServiceProvider, RecentFiles.Select(x => x.FilePath).ToList(), RemoveRecentFile);
        try
        {
            if (!await DialogService.ShowProjectSelectionDialogAsync(templates))
            {
                if (exitOnCancel)
                    DialogService.Exit();
                return;
            }

            if (templates.GetExistingSessionPath() is { } existingSession)
                await OpenSession(existingSession);
            else if (templates.GetNewSessionParameters() is { } newSession)
                await CreateNewSessionAsync(newSession);
        }
        finally
        {
            templates.Destroy();
        }
    }

    /// <summary>
    /// Makes an opened or created session the editor's: the recent files, the title, the plugins' session services and the preview.
    /// </summary>
    private void AttachSession(SessionViewModel loadedSession, UFile filePath)
    {
        MRU.AddFile(filePath, StrideGameStudio.EditorVersionMajor);
        InternalSettings.FileDialogLastOpenSessionDirectory.SetValue(filePath.GetFullDirectory());
        InternalSettings.SaveProfile();

        sessionPath = filePath;
        Session = loadedSession;
        // The solution's name, or the package's when a package file was opened directly
        var sessionFile = UPath.IsNullOrEmpty(Session.SolutionPath) ? filePath : Session.SolutionPath;
        sessionName = sessionFile.GetFileNameWithoutExtension();
        UpdateTitle();

        // The plugins initialize their session services (build, thumbnails, preview) and the property grid's commands and updaters
        // now that the session has its views
        ServiceProvider.Get<IAssetsPluginService>().InitializeSession(Session);
        Session.PluginsInitialized();
        // It owns the preview service that a plugin has just registered, and the preview game with it
        Preview = new PreviewViewModel(Session);

        // The build the session runs when it opens: thumbnails and previews wait for it
        _ = ShowShaderImportAsync(Session);
    }

    /// <summary>
    /// Follows the new session: <see cref="UndoCommand"/> and <see cref="RedoCommand"/> mirror the commands of its history, and the
    /// title shows its unsaved changes.
    /// </summary>
    private void OnSessionChanged()
    {
        SaveCommand.IsEnabled = session is not null;
        if (titleSession is not null)
            titleSession.PropertyChanged -= SessionPropertyChanged;
        titleSession = session;
        if (titleSession is not null)
            titleSession.PropertyChanged += SessionPropertyChanged;
        if (history is not null)
        {
            history.UndoCommand.CanExecuteChanged -= UpdateUndoRedo;
            history.RedoCommand.CanExecuteChanged -= UpdateUndoRedo;
        }
        history = session?.ActionHistory;
        if (history is not null)
        {
            history.UndoCommand.CanExecuteChanged += UpdateUndoRedo;
            history.RedoCommand.CanExecuteChanged += UpdateUndoRedo;
        }
        UpdateUndoRedo(null, EventArgs.Empty);
    }

    private void UpdateUndoRedo(object? sender, EventArgs e)
    {
        UndoCommand.IsEnabled = history?.UndoCommand.IsEnabled ?? false;
        RedoCommand.IsEnabled = history?.RedoCommand.IsEnabled ?? false;
    }

    private void SessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SessionViewModel.HasUnsavedChanges))
            UpdateTitle();
    }

    /// <summary>
    /// The name of the editor, then the name of the session with a <c>*</c> while it has unsaved changes.
    /// </summary>
    private void UpdateTitle()
    {
        Title = sessionName is null ? baseTitle : $"{baseTitle} - {sessionName}{(session?.HasUnsavedChanges == true ? "*" : string.Empty)}";
    }

    /// <summary>
    /// Shows the import of the system shaders in the notification bar until it is done; nothing when there is none to wait for.
    /// </summary>
    private async Task ShowShaderImportAsync(SessionViewModel openedSession)
    {
        if (Notifications is null || openedSession.ServiceProvider.TryGet<GameStudioBuilderService>() is not { AreShadersLoaded: false } builder)
            return;

        var logger = GlobalLogger.GetLogger("BuildOnOpen");
        var progress = new WorkProgressViewModel(ServiceProvider, logger)
        {
            Title = Tr._p("Notification", "Importing the system shaders…"),
            IsIndeterminate = true,
            KeepOpen = KeepOpen.Never,
        };
        // The import reports its steps from a thread of the builder: never block it on the UI thread (WorkProgressViewModel's own
        // RegisterProgressStatus dispatches synchronously), which may itself be waiting for the builder (when the session closes)
        void ImportProgressChanged(object? sender, ProgressStatusEventArgs e)
        {
            progress.Dispatcher.InvokeAsync(() =>
            {
                if (progress.WorkDone)
                    return;
                progress.IsIndeterminate = !e.HasKnownSteps;
                progress.Minimum = 0;
                progress.Maximum = e.StepCount;
                progress.ProgressValue = e.CurrentStep;
                progress.ProgressMessage = e.Message;
            });
        }

        var importProgress = builder.ShaderImportProgress;
        if (importProgress is not null)
            importProgress.ProgressChanged += ImportProgressChanged;
        ServiceProvider.Get<IEditorDialogService>().ShowNotification(progress);

        var error = false;
        try
        {
            await builder.WaitForShadersAsync();
        }
        catch (Exception e)
        {
            logger.Error("The import of the system shaders failed.", e);
            error = true;
        }
        finally
        {
            if (importProgress is not null)
                importProgress.ProgressChanged -= ImportProgressChanged;
            await progress.NotifyWorkFinished(false, error);
        }
    }

    private static IReadOnlyList<FilePickerFilter> SessionFileFilters =>
    [
        new(Tr._p("FileFilter", "Solution or package files")) { Patterns = [.. Core.Solutions.Solution.SolutionExtensions.Select(x => "*" + x), "*" + Package.PackageFileExtension] },
        new(Tr._p("FileFilter", "Solution file")) { Patterns = [.. Core.Solutions.Solution.SolutionExtensions.Select(x => "*" + x)] },
        new(Tr._p("FileFilter", "Package file")) { Patterns = ["*" + Package.PackageFileExtension] },
    ];

    /// <summary>
    /// Locates MSBuild once; a missing one is reported and stops the session from opening.
    /// </summary>
    private async Task<bool> EnsureMSBuildAsync()
    {
        if (msBuildFound)
            return true;
        try
        {
            PackageSessionPublicHelper.FindAndSetMSBuildVersion();
            msBuildFound = true;
            return true;
        }
        catch (Exception e)
        {
            var message = "Could not find a compatible version of MSBuild.\r\n\r\n" +
                          $"Check that you have the latest [.NET {PackageSessionPublicHelper.NetMajorVersion} SDK](https://dotnet.microsoft.com/) installed.\r\n\r\n" +
                          e.Message;
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void UpdateRecentFiles()
    {
        RecentFiles.Clear();
        var editorVersion = StrideGameStudio.EditorVersionMajor;
        foreach (var item in MRU.MostRecentlyUsedFiles.Where(x => string.Compare(x.Version, editorVersion, StringComparison.Ordinal) <= 0).Take(RecentFilesCount))
        {
            RecentFiles.Add(new RecentSessionViewModel(ServiceProvider, item, path => OpenSession(path)));
        }
    }

    private void RemoveRecentFile(UFile filePath)
    {
        foreach (var version in MRU.MostRecentlyUsedFiles.Where(x => x.FilePath == filePath).Select(x => x.Version).Distinct().ToList())
            MRU.RemoveFile(filePath, version);
    }

    private void ClearRecentFiles()
    {
        foreach (var version in MRU.MostRecentlyUsedFiles.Select(x => x.Version).Distinct().ToList())
            MRU.Clear(version);
    }

    private async Task OnAbout()
    {
        await DialogService.ShowAboutWindowAsync();
    }

    private async Task OnClose()
    {
        // We have a session, let's restart empty
        if (session is not null && Application.Current is App app && await CloseSessionAsync())
        {
            CloseSessionServices();
            app.Restart();
        }
    }

    private async Task OnNewSession()
    {
        if (session is null && !NeedsNewProcess)
        {
            await SelectProjectAsync(exitOnCancel: false);
            return;
        }

        // We have a session, or had one, let's restart empty with the project selection
        await RestartAsync(null);
    }

    private async Task OnSave()
    {
        if (session is not null)
            await session.SaveSessionAsync();
    }

    private async Task OnReload()
    {
        // The same session in a fresh editor, as WPF's Reload project does
        if (session is not null && sessionPath is { } path)
            await RestartAsync(path);
    }

    /// <summary>
    /// Closes the session, after asking whether to save it, and opens another one, or the project selection, in a fresh editor:
    /// a new process once this one has loaded a session (<see cref="EditorProcessService"/>), as WPF's New, Open and Reload
    /// restart the process; otherwise, or when the host cannot start one, in this process.
    /// </summary>
    /// <param name="filePath">The session to open; <c>null</c> for the project selection.</param>
    /// <returns><c>true</c> when the editor restarts, <c>false</c> when the user kept the session open.</returns>
    private async Task<bool> RestartAsync(UFile? filePath)
    {
        if (!await CloseSessionAsync())
            return false;

        var restartProcess = session is not null || NeedsNewProcess;
        CloseSessionServices();
        if (restartProcess && ProcessService is { } process)
            process.Restart(filePath);
        else
            (Application.Current as App)?.Restart(filePath, selectProject: filePath is null);
        return true;
    }

    /// <summary>
    /// Asks whether to save the session before it closes, as WPF does: one question (Save, Don't save, Cancel) covers the
    /// unsaved changes of the assets and those of the open editors (the text of a script), which make their asset dirty.
    /// The editors then close without asking.
    /// </summary>
    /// <returns><c>true</c> when the session can close or there is none, <c>false</c> when the user cancelled or the save failed.</returns>
    internal async Task<bool> CloseSessionAsync()
    {
        if (session is null)
            return true;

        if (!await session.CloseAsync())
            return false;

        // What the editors had unsaved is now saved, or dropped by the user
        return await session.EditorCollection.CloseAllEditorsAsync(false);
    }

    /// <summary>
    /// Forgets the session before the editor restarts, and exits its preview game: the next editor starts its own.
    /// </summary>
    private void CloseSessionServices()
    {
        Preview?.Dispose();
        Preview = null;
        session = null;
    }

    private void OnExit()
    {
        DialogService.Exit();
    }

    private async Task OnOpen(UFile? initialPath)
    {
        await OpenSession(initialPath);
    }

    private async Task OnOpenInitialSession(UFile? initialPath)
    {
        await OpenInitialSessionAsync(initialPath);
    }

    private async Task OnOpenDebugWindow()
    {
        await DialogService.ShowDebugWindowAsync();
    }

    private async Task OnOpenSettingsWindow()
    {
        await DialogService.ShowSettingsWindowAsync();
    }

    private async Task OnOpenWebPage(string url)
    {
        try
        {
            var process = new Process { StartInfo = new ProcessStartInfo(url) { UseShellExecute = true } };
            process.Start();
        }
        catch (Exception ex)
        {
            var message = $"{Tr._p("Message", "An error occurred while opening the file.")}{ex.FormatSummary(true)}";
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
