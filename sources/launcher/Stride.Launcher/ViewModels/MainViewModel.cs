// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;
using Stride.Core.CodeEditorSupport.VisualStudio;
using Stride.Core.Extensions;
using Stride.Core.Packages;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Launcher.Assets.Localization;
using Stride.Launcher.Services;

namespace Stride.Launcher.ViewModels;

/// <summary>
/// This class represents the root view model of the launcher.
/// </summary>
public sealed class MainViewModel : DispatcherViewModel, IPackagesLogger, IDisposable
{
    private readonly NugetStore store;
    private readonly SortedObservableCollection<StrideVersionViewModel> strideVersions = [];
    private readonly UninstallHelper uninstallHelper;
    private readonly object objectLock = new();
    private ObservableList<NewsPageViewModel> newsPages;
    private ReleaseNotesViewModel activeReleaseNotes;
    private StrideVersionViewModel? activeVersion;
    private bool isOffline;
    private bool isSynchronizing = true;
    private string currentToolTip;
    private readonly List<(DateTime Time, MessageLevel Level, string Message)> logMessages = [];
    private bool autoCloseLauncher = LauncherSettings.CloseLauncherAutomatically;
    private bool lastActiveVersionRestored;
    private AnnouncementViewModel announcement;
    private bool isVisible;
    private bool showBetaVersions;

    public MainViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        DependentProperties.Add("ActiveVersion", ["ActiveDocumentationPages"]);
        store = Launcher.InitializeNugetStore();
        store.Logger = this;

        DisplayReleaseAnnouncement();

        VsixPackage2019 = new(this, store, store.VsixPackageId, NugetStore.VsixSupportedVsVersion.VS2019);
        VsixPackage2022 = new(this, store, store.VsixPackageId, NugetStore.VsixSupportedVsVersion.VS2022AndNext);
        // Commands
        InstallLatestVersionCommand = new AnonymousTaskCommand(ServiceProvider, InstallLatestVersion) { IsEnabled = false };
        OpenUrlCommand = new AnonymousTaskCommand<string>(ServiceProvider, OpenUrl);
        ReconnectCommand = new AnonymousTaskCommand(ServiceProvider, async () =>
        {
            // We are back online (or so we think)
            IsOffline = false;
            await FetchOnlineData();
        });
        StartStudioCommand = new AnonymousTaskCommand(ServiceProvider, StartStudio) { IsEnabled = false };
        CheckDeprecatedSourcesCommand = new AnonymousTaskCommand(ServiceProvider, async () =>
        {
            var settings = NuGet.Configuration.Settings.LoadDefaultSettings(null);
            if (NugetStore.CheckPackageSource(settings, "Stride"))
            {
                return;
            }
            // Add Stride package store (still used for Xenko up to 3.0)
            if (await ServiceProvider.Get<IDialogService>().MessageBoxAsync(Strings.AskAddNugetDeprecatedSource, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                NugetStore.UpdatePackageSource(settings, "Stride", "https://packages.stride3d.net/nuget");
                settings.SaveToDisk();

                SelfUpdater.RestartApplication();
            }
        });

        foreach (var devVersion in LauncherSettings.DeveloperVersions)
        {
            var version = new StrideDevVersionViewModel(this, store, null, devVersion, false);
            strideVersions.Add(version);
        }
        FetchOnlineData().Forget();
        LoadRecentProjects();
        uninstallHelper = new(serviceProvider, store);
        GameStudioSettings.RecentProjectsUpdated += (sender, e) => Dispatcher.InvokeAsync(LoadRecentProjects).Forget();
    }

    public void Dispose()
    {
        uninstallHelper.Dispose();
    }

    public static IntPtr WindowHandle { get; set; }

    public IEnumerable<StrideVersionViewModel> StrideVersions => strideVersions;

    public bool ShowBetaVersions { get { return showBetaVersions; } set { SetValue(ref showBetaVersions, value); } }

    public VsixVersionViewModel VsixPackage2019 { get; }

    public VsixVersionViewModel VsixPackage2022 { get; }

    public StrideVersionViewModel? ActiveVersion
    {
        get { return activeVersion; }
        set
        {
            if (SetValue(ref activeVersion, value))
            {
                Dispatcher.InvokeAsync(() => StartStudioCommand.IsEnabled = value?.CanStart ?? false);
            }
        }
    }

    public ObservableList<RecentProjectViewModel> RecentProjects { get; } = [];

    public ObservableList<NewsPageViewModel> NewsPages { get { return newsPages; } private set { SetValue(ref newsPages, value); } }

    public ReleaseNotesViewModel ActiveReleaseNotes { get { return activeReleaseNotes; } set { SetValue(ref activeReleaseNotes, value); } }

    public ObservableList<DocumentationPageViewModel> ActiveDocumentationPages => ActiveVersion.Yield().Concat(StrideVersions).OfType<StrideStoreVersionViewModel>().FirstOrDefault()?.DocumentationPages;

    public AnnouncementViewModel Announcement { get { return announcement; } set { SetValue(ref announcement, value); } }

    public bool IsOffline { get { return isOffline; } set { SetValue(ref isOffline, value); } }

    public bool IsSynchronizing { get { return isSynchronizing; } set { SetValue(ref isSynchronizing, value); } }

    public string CurrentToolTip { get { return currentToolTip; } set { SetValue(ref currentToolTip, value); } }

    public string LogMessages
    {
        get
        {
            lock (logMessages)
            {
                if (logMessages.Count == 0)
                    return "Empty";
                return string.Join(Environment.NewLine, logMessages.Select(x => $"[{x.Time:HH:mm:ss}] {x.Level}: {x.Message}"));
            }
        }
    }

    public bool AutoCloseLauncher { get { return autoCloseLauncher; } set { SetValue(ref autoCloseLauncher, value, () => LauncherSettings.CloseLauncherAutomatically = value); } }

    /// <summary>
    /// Gets or Sets the visibility status of this instance.
    /// </summary>
    public bool IsVisible { get { return isVisible; } set { SetValue(ref isVisible, value); } }

    public CommandBase InstallLatestVersionCommand { get; }

    public CommandBase OpenUrlCommand { get; }

    public CommandBase ReconnectCommand { get; }

    public CommandBase StartStudioCommand { get; }

    public CommandBase CheckDeprecatedSourcesCommand { get; }

    private async Task FetchOnlineData()
    {
        // We ensure that the self-updater task starts once the app is running because it might invoke dialogs.
        IsSynchronizing = true;
        await Task.Run(async () =>
        {
            await RetrieveLocalStrideVersions();
            await RunLockTask(async () =>
            {
                try
                {
                    await SelfUpdater.SelfUpdate(ServiceProvider, store);
                }
                catch (Exception e)
                {
                    var message = $@"**An error occurred while updating the launcher. If the problem persists, please reinstall this application.**
### Log
```
{LogMessages}
```

### Exception
```
{e.FormatSummary(false).TrimEnd(Environment.NewLine.ToCharArray())}
```";
                    await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Error);
                    // We do not want our users to use the old launcher when a new one is available.
                    if (e is not HttpRequestException) // Prevent launcher closing when the user does not have internet access
                        Environment.Exit(1);
                }
            });
            // Run news task early so that it can run while we fetch package versions
            var newsTask = FetchNewsPages();

            await RetrieveServerStrideVersions();
            await VsixPackage2019.UpdateFromStore();
            await VsixPackage2022.UpdateFromStore();
            await CheckForFirstInstall();

            await newsTask;
        });
        IsSynchronizing = false;
    }

    internal void LoadRecentProjects()
    {
        lock (RecentProjects)
        {
            RecentProjects.Clear();
            foreach (var mruFile in GameStudioSettings.GetMostRecentlyUsed())
            {
                RecentProjects.Add(new(this, mruFile));
            }
        }
    }

    public async Task RetrieveAllStrideVersions()
    {
        Dispatcher.Invoke(() => IsSynchronizing = true);
        await RetrieveLocalStrideVersions();
        await RetrieveServerStrideVersions();
        Dispatcher.Invoke(() => IsSynchronizing = false);
    }

    private class ReferencedPackageEqualityComparer : IEqualityComparer<NugetLocalPackage>
    {
        public static readonly ReferencedPackageEqualityComparer Instance = new();

        private ReferencedPackageEqualityComparer() { }

        public bool Equals(NugetLocalPackage x, NugetLocalPackage y)
            => (ReferenceEquals(x, y)) || ((!ReferenceEquals(x, null)) && (!ReferenceEquals(y, null)) && (x.Id == y.Id) && (x.Version.ToString() == y.Version.ToString()));

        public int GetHashCode([DisallowNull] NugetLocalPackage obj)
            => (obj.Id.GetHashCode() ^ obj.Version.ToString().GetHashCode());
    }

    private HashSet<NugetLocalPackage> referencedPackages = new(ReferencedPackageEqualityComparer.Instance);

    private async Task RemoveUnusedPackages(IEnumerable<NugetLocalPackage> mainPackages)
    {
        var previousReferencedPackages = referencedPackages;
        referencedPackages = new(ReferencedPackageEqualityComparer.Instance);
        foreach (var mainPackage in mainPackages)
        {
            await FindReferencedPackages(mainPackage);
        }
        foreach (var package in previousReferencedPackages.Where(package => !referencedPackages.Contains(package)))
        {
            await store.UninstallPackage(package, null);
        }
    }

    private async Task FindReferencedPackages(NugetLocalPackage package)
    {
        foreach (var dependency in package.Dependencies)
        {
            string prefix = dependency.Item1.Split('.', 2)[0];
            if (prefix is not "Stride" and not "Xenko")
            {
                continue;
            }
            NugetLocalPackage dependencyPackage = store.FindLocalPackage(dependency.Item1, dependency.Item2);
            if (dependencyPackage is null || !referencedPackages.Add(dependencyPackage))
            {
                continue;
            }

            await FindReferencedPackages(dependencyPackage);
        }
    }

    public async Task RetrieveLocalStrideVersions()
    {
        List<RecentProjectViewModel> currentRecentProjects;
        lock (RecentProjects)
        {
            currentRecentProjects = new(RecentProjects);
        }
        try
        {
            var localPackages = await RunLockTask(() => store.GetPackagesInstalled(store.MainPackageIds).FilterStrideMainPackages().OrderByDescending(p => p.Version).ToList());
            lock (objectLock)
            {
                // Try to remove unused Stride/Xenko packages after uninstall or update
                try
                {
                    Task.WaitAll(RemoveUnusedPackages(localPackages));
                }
                catch (Exception e)
                {
                    var message = $@"**Failed to remove unused NuGet package(s).**

### Exception
```
{e.FormatSummary(false).TrimEnd(Environment.NewLine.ToCharArray())}
```";
                    Task.WaitAll(ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Warning));
                }

                // Retrieve all local packages
                var packages = localPackages.Where(p => !store.IsDevRedirectPackage(p)).GroupBy(p => $"{p.Version.Version.Major}.{p.Version.Version.Minor}", p => p);
                var updatedLocalPackages = new HashSet<StrideStoreVersionViewModel>();
                foreach (var package in packages)
                {
                    var localPackage = package.FirstOrDefault();
                    if (localPackage is not null)
                    {
                        // Find if we already have this package in our list
                        int index = strideVersions.BinarySearch(Tuple.Create(localPackage.Version.Version.Major, localPackage.Version.Version.Minor));
                        StrideStoreVersionViewModel version;
                        if (index < 0)
                        {
                            // If not, add it
                            version = new(this, store, localPackage, localPackage.Id, localPackage.Version.Version.Major, localPackage.Version.Version.Minor);
                            Dispatcher.Invoke(() => strideVersions.Add(version));
                        }
                        else
                        {
                            version = (StrideStoreVersionViewModel)strideVersions[index];
                        }
                        version.UpdateLocalPackage(localPackage, package);
                        updatedLocalPackages.Add(version);
                    }
                }

                // Update versions that are not installed locally anymore
                Dispatcher.Invoke(() =>
                {
                    foreach (var strideUninstalledVersion in strideVersions.OfType<StrideStoreVersionViewModel>().Where(x => !updatedLocalPackages.Contains(x)))
                        strideUninstalledVersion.UpdateLocalPackage(null, Array.Empty<NugetLocalPackage>());
                });

                // Update the active version if it is now invalid.
                if (ActiveVersion is null || !strideVersions.Contains(ActiveVersion) || !ActiveVersion.CanDelete)
                    ActiveVersion = StrideVersions.FirstOrDefault(x => x.CanDelete);

                if (!lastActiveVersionRestored)
                {
                    var restoredVersion = StrideVersions.FirstOrDefault(x => x.CanDelete && x.Name == LauncherSettings.ActiveVersion);
                    if (restoredVersion is not null)
                    {
                        ActiveVersion = restoredVersion;
                        lastActiveVersionRestored = true;
                    }
                }
            }

            var devPackages = localPackages.Where(store.IsDevRedirectPackage);
            Dispatcher.Invoke(() => strideVersions.RemoveWhere(x => x is StrideDevVersionViewModel));
            foreach (var package in devPackages)
            {
                try
                {
                    var realPath = store.GetRealPath(package);
                    var version = new StrideDevVersionViewModel(this, store, package, realPath, true);
                    await Dispatcher.InvokeAsync(() => strideVersions.Add(version));
                }
                catch (Exception e)
                {
                    await ServiceProvider.Get<IDialogService>().MessageBoxAsync(string.Format(Strings.ErrorDevRedirect, e), MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception e)
        {
            // TODO: error
            e.Ignore();
        }
        finally
        {
            await Dispatcher.InvokeAsync(() =>
            {
                foreach (var project in currentRecentProjects)
                {
                    // Manually discarding the possibility to upgrade from 1.0
                    if (project.StrideVersionName == "1.0")
                        continue;

                    project.CompatibleVersions.Clear();
                    foreach (var version in StrideVersions)
                    {
                        // We suppose all dev versions are compatible with any project.
                        if (version is StrideDevVersionViewModel)
                            project.CompatibleVersions.Add(version);

                        if (version is StrideStoreVersionViewModel { CanDelete: true } storeVersion)
                        {
                            // Discard the version that matches the recent project version
                            if (project.StrideVersion == new Version(storeVersion.Version.Version.Major, storeVersion.Version.Version.Minor))
                                continue;

                            // Discard the versions that are anterior to the recent project version
                            if (project.StrideVersion > storeVersion.Version.Version)
                                continue;

                            project.CompatibleVersions.Add(version);
                        }
                    }
                }
            });
        }
    }

    private async Task RetrieveServerStrideVersions()
    {
        try
        {
            var serverPackages = await RunLockTask(() => store
                .FindSourcePackages(store.MainPackageIds, CancellationToken.None).Result
                .FilterStrideMainPackages()
                .Where(p => !store.IsDevRedirectPackage(p))
                .OrderByDescending(p => p.Version)
                .ToList());

            // Check if we could connect to the server
            var wasOffline = IsOffline;
            IsOffline = serverPackages.Count == 0;

            // Inform the user if we just switched offline
            if (IsOffline && !wasOffline)
            {
                var message =
                    $"""
                    **{Strings.ErrorOfflineMode}**
                    ### Log
                    ```
                    {LogMessages}
                    ```
                    """;
                await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // We are offline, let's stop here
            if (IsOffline)
                return;

            lock (objectLock)
            {
                // Retrieve all server packages (ignoring dev ones)
                var packages = serverPackages
                    //.Where(x => !string.Equals(x.Source, Environment.ExpandEnvironmentVariables(store.DevSource), StringComparison.OrdinalIgnoreCase))
                    .GroupBy(p => $"{p.Version.Version.Major}.{p.Version.Version.Minor}", p => p);
                foreach (var package in packages)
                {
                    var serverPackage = package.FirstOrDefault();
                    if (serverPackage is not null)
                    {
                        // Find if we already have this package in our list
                        int index = strideVersions.BinarySearch(Tuple.Create(serverPackage.Version.Version.Major, serverPackage.Version.Version.Minor));
                        StrideStoreVersionViewModel version;
                        if (index < 0)
                        {
                            // If not, add it
                            version = new(this, store, null, serverPackage.Id, serverPackage.Version.Version.Major, serverPackage.Version.Version.Minor);
                            Dispatcher.Invoke(() => strideVersions.Add(version));
                        }
                        else
                        {
                            // If yes, update it and remove it from the list of old version
                            version = (StrideStoreVersionViewModel)strideVersions[index];
                        }
                        version.UpdateServerPackage(serverPackage, package);
                    }
                }
            }
        }
        catch (Exception e)
        {
            // TODO: error
            e.Ignore();
        }
        finally
        {
            await Dispatcher.InvokeAsync(() =>
            {
                // Allow to install the latest version if any version is found
                var latestVersion = strideVersions.FirstOrDefault();
                if (latestVersion is not null)
                {
                    // Latest version not installed and can be downloaded
                    if (latestVersion.CanBeDownloaded)
                        InstallLatestVersionCommand.IsEnabled = latestVersion is { CanDelete: false, CanBeDownloaded: true };
                }

                OnPropertyChanging(nameof(ActiveDocumentationPages));
                OnPropertyChanged(nameof(ActiveDocumentationPages));
            });
        }
    }

    public async Task CheckForFirstInstall()
    {
        const string prerequisitesRunTaskName = "PrerequisitesRun";

        if (!HasDoneTask(prerequisitesRunTaskName))
        {
            foreach (var version in StrideVersions.OfType<StrideStoreVersionViewModel>().Where(x => x.CanDelete))
            {
                await version.RunPrerequisitesInstaller();
            }
            SaveTaskAsDone(prerequisitesRunTaskName);
        }

        bool firstInstall = StrideVersions.All(x => !x.CanDelete) && StrideVersions.Any(x => x.CanBeDownloaded);

        await Dispatcher.InvokeTask(async () =>
        {
            if (firstInstall)
            {
                var result = await ServiceProvider.Get<IDialogService>().MessageBoxAsync(Strings.AskInstallVersion, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    var versionToInstall = StrideVersions.First(x => x.CanBeDownloaded);
                    await versionToInstall.Download(true);

                    // if VS2022+ is installed (version 17.x+)
                    if (VsixPackage2022 is { IsLatestVersionInstalled: false, CanBeDownloaded: true } && VisualStudioVersions.AvailableInstances.Any(ide => ide.InstallationVersion.Major >= 17))
                    {
                        result = await ServiceProvider.Get<IDialogService>().MessageBoxAsync(string.Format(Strings.AskInstallVSIX, "2022"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            await VsixPackage2022.ExecuteAction();
                        }
                    }

                    // if VS2019 is installed (version 16.x)
                    if (VsixPackage2019 is { IsLatestVersionInstalled: false, CanBeDownloaded: true } && VisualStudioVersions.AvailableInstances.Any(ide => ide.InstallationVersion.Major == 16))
                    {
                        result = await ServiceProvider.Get<IDialogService>().MessageBoxAsync(string.Format(Strings.AskInstallVSIX, "2019"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            await VsixPackage2019.ExecuteAction();
                        }
                    }
                }
            }
        });
    }

    /// <summary>
    /// Execute action <paramref name="action"/> under the exclusive lock <see cref="objectLock"/>.
    /// </summary>
    /// <typeparam name="T">Return type of action.</typeparam>
    /// <param name="action">Action to be executed.</param>
    /// <returns>Result of executing <paramref name="action"/>.</returns>
    internal Task<T> RunLockTask<T>(Func<T> action)
    {
        return Task.Run(() =>
        {
            lock (objectLock)
            {
                return action();
            }
        });
    }

    public Task StartStudio()
    {
        return StartStudio("");
    }

    public async Task StartStudio(string argument)
    {
        ArgumentNullException.ThrowIfNull(argument);

        if (ActiveVersion is null)
            return;

        if (AutoCloseLauncher)
        {
            argument = $"/LauncherWindowHandle {WindowHandle} {argument}";
        }

        try
        {
            Dispatcher.Invoke(() => StartStudioCommand.IsEnabled = false);
            var mainExecutable = ActiveVersion.LocateMainExecutable();

            // We set the WorkingDirectory so that global.json is properly resolved
            switch (Path.GetExtension(mainExecutable))
            {
                case ".dll":
                    argument = $"{mainExecutable} {argument}";
                    Process.Start(new ProcessStartInfo("dotnet", argument)
                    {
                        WorkingDirectory = Path.GetDirectoryName(mainExecutable)
                    });
                    break;

                default:
                    Process.Start(new ProcessStartInfo(mainExecutable, argument)
                    {
                        WorkingDirectory = Path.GetDirectoryName(mainExecutable)
                    });
                    break;
            }
        }
        catch (Exception e)
        {
            var message = string.Format(Strings.ErrorStartingProcess, e.FormatSummary(true));
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        await Task.Delay(5000);
        await Dispatcher.InvokeAsync(() =>
        {
            StartStudioCommand.IsEnabled = ActiveVersion is not null && ActiveVersion.CanStart;
            //Save settings because launcher maybe have not been closed
            LauncherSettings.ActiveVersion = ActiveVersion is not null ? ActiveVersion.Name : "";
            LauncherSettings.Save();
        });
    }

    private async Task InstallLatestVersion()
    {
        var latestVersion = strideVersions.FirstOrDefault();
        // Should never happen
        if (latestVersion is null || !latestVersion.CanBeDownloaded)
            return;

        if (latestVersion.IsProcessing)
        {
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(Strings.InstallAlreadyInProgress, MessageBoxButton.OK, MessageBoxImage.Information);
            InstallLatestVersionCommand.IsEnabled = false;
        }

        latestVersion.DownloadCommand.Execute();
    }

    private async Task OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        // FIXME: catch only specific exceptions?
        catch (Exception)
        {
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(Strings.ErrorOpeningBrowser, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task FetchNewsPages()
    {
        var pages = await NewsPageViewModel.FetchNewsPages(ServiceProvider, 30);
        var sortedPages = pages.OrderBy(x => x.Date).Reverse().ToList();
        Dispatcher.Invoke(() => NewsPages = new(sortedPages));
    }

    public static bool HasDoneTask(string taskName)
    {
        // FIXME xplat-editor get that information from a config file on Linux (e.g. under /etc) and MacOS
        if (OperatingSystem.IsWindows())
        {
            var localMachine32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            using var subkey = localMachine32.OpenSubKey(@"SOFTWARE\Stride\");
            if (subkey is not null)
            {
                var value = (string?)subkey.GetValue(taskName);
                return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        return true;
    }

    public static void SaveTaskAsDone(string taskName)
    {
        // FIXME xplat-editor store that information to a config file on Linux (e.g. under /etc) and MacOS
        if (OperatingSystem.IsWindows())
        {
            var localMachine32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            using var subkey = localMachine32.CreateSubKey(@"SOFTWARE\Stride\");
            subkey?.SetValue(taskName, "True");
        }
    }

    private void DisplayReleaseAnnouncement()
    {
    }

    void IPackagesLogger.Log(MessageLevel level, string message)
    {
        lock (logMessages)
        {
            logMessages.Add((DateTime.Now, level, message));
        }
    }

    Task IPackagesLogger.LogAsync(MessageLevel level, string message)
    {
        ((IPackagesLogger)this).Log(level, message);
        return Task.CompletedTask;
    }
}
