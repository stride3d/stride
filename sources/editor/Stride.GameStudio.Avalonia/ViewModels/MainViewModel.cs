// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Avalonia;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.MostRecentlyUsedFiles;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;
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
        CloseCommand = new AnonymousCommand(serviceProvider, OnClose);
#if DEBUG
        CrashCommand = new AnonymousCommand(serviceProvider, () => throw new Exception("Boom!"));
#else
        CrashCommand = DisabledCommand.Instance;
#endif
        ExitCommand = new AnonymousCommand(serviceProvider, OnExit, () => DialogService.HasMainWindow);
        OpenCommand = new AnonymousTaskCommand<UFile?>(serviceProvider, OnOpen);
        OpenInitialSessionCommand = new AnonymousTaskCommand<UFile?>(serviceProvider, OnOpenInitialSession);
        ClearRecentFilesCommand = new AnonymousCommand(serviceProvider, ClearRecentFiles);
        OpenDebugWindowCommand = new AnonymousTaskCommand(serviceProvider, OnOpenDebugWindow, () => DialogService.HasMainWindow);
        OpenSettingsWindowCommand = new AnonymousTaskCommand(serviceProvider, OnOpenSettingsWindow, () => DialogService.HasMainWindow);
        OpenWebPageCommand = new AnonymousTaskCommand<string>(serviceProvider, OnOpenWebPage);

        Status = new StatusViewModel(ServiceProvider);
        Status.PushStatus("Ready");

        // The most recently used sessions of this and the earlier editor versions
        MRU = GameStudioInternalSettings.CreateMostRecentlyUsedFiles();
        MRU.MostRecentlyUsedFiles.CollectionChanged += (_, _) => UpdateRecentFiles();
        UpdateRecentFiles();
    }

    public SessionViewModel? Session
    {
        get => session;
        set => SetValue(ref session, value);
    }

    public StatusViewModel Status { get; }

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

    public ICommandBase OpenCommand { get; }

    /// <summary>
    /// Opens the session the editor starts with; a crash while it loads is remembered, and the next start asks before trying again.
    /// </summary>
    public ICommandBase OpenInitialSessionCommand { get; }

    public ICommandBase OpenDebugWindowCommand { get; }

    public ICommandBase OpenSettingsWindowCommand { get; }

    public ICommandBase OpenWebPageCommand { get; }

    private EditorDialogService DialogService => ServiceProvider.Get<EditorDialogService>();

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
    /// <returns><c>true</c> when the session is open, <c>false</c> when it failed or no file was chosen, <c>null</c> when the user cancelled the loading.</returns>
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

        // We have a session, let's restart cleanly
        if (session is not null)
        {
            session = null;
            (Application.Current as App)?.Restart(filePath);
            return true;
        }

        // The session needs MSBuild to load the projects of the solution
        if (!await EnsureMSBuildAsync())
            return false;

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

        MRU.AddFile(filePath, StrideGameStudio.EditorVersionMajor);
        InternalSettings.FileDialogLastOpenSessionDirectory.SetValue(filePath.GetFullDirectory());
        InternalSettings.SaveProfile();

        Session = loadedSession;
        // The solution's name, or the package's when a package file was opened directly
        var sessionFile = UPath.IsNullOrEmpty(Session.SolutionPath) ? filePath : Session.SolutionPath;
        Title = $"{baseTitle} - {sessionFile.GetFileNameWithoutExtension()}";

        // The plugins initialize their session services (build, thumbnails, preview) and the property grid's commands and updaters
        // now that the session has its views
        ServiceProvider.Get<IAssetsPluginService>().InitializeSession(Session);
        Session.PluginsInitialized();
        return true;
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

    private void OnClose()
    {
        // We have a session, let's restart empty
        if (session is not null && Application.Current is App app)
        {
            session = null;
            app.Restart();
        }
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
