// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

//#define SIMULATE_OFFLINE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

using Stride.Core.Extensions;
using Stride.PrivacyPolicy;
using Stride.LauncherApp.Resources;
using Stride.LauncherApp.Services;
using Stride.Core.Packages;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;
using Stride.Metrics;
using Stride.Core.VisualStudio;

namespace Stride.LauncherApp.ViewModels
{
    /// <summary>
    /// This class represents the root view model of the launcher.
    /// </summary>
    internal class LauncherViewModel : DispatcherViewModel, IPackagesLogger, IDisposable
    {
        private readonly NugetStore store;
        private readonly SortedObservableCollection<StrideVersionViewModel> strideVersions = new SortedObservableCollection<StrideVersionViewModel>();
        private readonly UninstallHelper uninstallHelper;
        private readonly object objectLock = new object();
        private ObservableList<NewsPageViewModel> newsPages;
        private ReleaseNotesViewModel activeReleaseNotes;
        private StrideVersionViewModel activeVersion;
        private bool isOffline;
        private bool isSynchronizing = true;
        private string currentToolTip;
        private List<(DateTime Time, MessageLevel Level, string Message)> logMessages = new List<(DateTime, MessageLevel, string)>();
        private bool autoCloseLauncher = LauncherSettings.CloseLauncherAutomatically;
        private bool lastActiveVersionRestored;
        private AnnouncementViewModel announcement;
        private bool isVisible;
        private bool showBetaVersions;

        internal LauncherViewModel(IViewModelServiceProvider serviceProvider, NugetStore store)
            : base(serviceProvider)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            DependentProperties.Add("ActiveVersion", new[] { "ActiveDocumentationPages" });
            this.store = store;
            store.Logger = this;

            DisplayReleaseAnnouncement();

            VsixPackage = new VsixVersionViewModel(this, store, store.VsixPluginId);
            VsixPackageXenko = new VsixVersionViewModel(this, store, store.VsixPluginId.Replace("Stride", "Xenko"));
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

            foreach (var devVersion in LauncherSettings.DeveloperVersions)
            {
                var version = new StrideDevVersionViewModel(this, store, null, devVersion, false);
                strideVersions.Add(version);
            }
            FetchOnlineData().Forget();
            LoadRecentProjects();
            uninstallHelper = new UninstallHelper(serviceProvider, store);
            GameStudioSettings.RecentProjectsUpdated += (sender, e) => Dispatcher.InvokeAsync(LoadRecentProjects).Forget();
        }

        public void Dispose()
        {
            uninstallHelper.Dispose();
        }

        public static IntPtr WindowHandle { get; set; }

        public IEnumerable<StrideVersionViewModel> StrideVersions => strideVersions;

        public bool ShowBetaVersions { get { return showBetaVersions; } set { SetValue(ref showBetaVersions, value); } }

        public VsixVersionViewModel VsixPackage { get; }

        public VsixVersionViewModel VsixPackageXenko { get; }

        public StrideVersionViewModel ActiveVersion { get { return activeVersion; } set { SetValue(ref activeVersion, value); Dispatcher.InvokeAsync(() => StartStudioCommand.IsEnabled = (value != null) && value.CanStart); } }

        public ObservableList<RecentProjectViewModel> RecentProjects { get; } = new ObservableList<RecentProjectViewModel>();

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
                    return string.Join(Environment.NewLine, logMessages.Select(x => $"[{x.Time.ToString("HH:mm:ss")}] {x.Level}: {x.Message}"));
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
                        await ServiceProvider.Get<IDialogService>().MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Error);
                        // We do not want our users to use the old launcher when a new one is available.
                        Environment.Exit(1);
                    }
                });
                // Run news task early so that it can run while we fetch package versions
                var newsTask = FetchNewsPages();

                await RetrieveServerStrideVersions();
                await VsixPackage.UpdateFromStore();
                await VsixPackageXenko.UpdateFromStore();
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
                    RecentProjects.Add(new RecentProjectViewModel(this, mruFile));
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

        public async Task RetrieveLocalStrideVersions()
        {
            List<RecentProjectViewModel> currentRecentProjects;
            lock (RecentProjects)
            {
                currentRecentProjects = new List<RecentProjectViewModel>(RecentProjects);
            }
            try
            {
                var localPackages = await RunLockTask(() => store.GetPackagesInstalled(store.MainPackageIds).FilterStrideMainPackages().OrderByDescending(p => p.Version).ToList());
                lock (objectLock)
                {
                    // Retrieve all local packages
                    var packages = localPackages.Where(p => !store.IsDevRedirectPackage(p)).GroupBy(p => $"{p.Version.Version.Major}.{p.Version.Version.Minor}", p => p);
                    var updatedLocalPackages = new HashSet<StrideStoreVersionViewModel>();
                    foreach (var package in packages)
                    {
                        var localPackage = package.FirstOrDefault();
                        if (localPackage != null)
                        {
                            // Find if we already have this package in our list
                            int index = strideVersions.BinarySearch(Tuple.Create(localPackage.Version.Version.Major, localPackage.Version.Version.Minor));
                            StrideStoreVersionViewModel version;
                            if (index < 0)
                            {
                                // If not, add it
                                version = new StrideStoreVersionViewModel(this, store, localPackage, localPackage.Id, localPackage.Version.Version.Major, localPackage.Version.Version.Minor);
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
                            strideUninstalledVersion.UpdateLocalPackage(null, new NugetLocalPackage[0]);
                    });

                    // Update the active version if it is now invalid.
                    if (ActiveVersion == null || !strideVersions.Contains(ActiveVersion) || !ActiveVersion.CanDelete)
                        ActiveVersion = StrideVersions.FirstOrDefault(x => x.CanDelete);

                    if (!lastActiveVersionRestored)
                    {
                        var restoredVersion = StrideVersions.FirstOrDefault(x => x.CanDelete && x.Name == LauncherSettings.ActiveVersion);
                        if (restoredVersion != null)
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
                        Dispatcher.Invoke(() => strideVersions.Add(version));
                    }
                    catch (Exception e)
                    {
                        await ServiceProvider.Get<IDialogService>().MessageBox(string.Format(Strings.ErrorDevRedirect, e), MessageBoxButton.OK, MessageBoxImage.Information);
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
                Dispatcher.Invoke(() =>
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

                            var storeVersion = version as StrideStoreVersionViewModel;
                            if (storeVersion != null && storeVersion.CanDelete)
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
#if SIMULATE_OFFLINE
                var serverPackages = new List<IPackage>();
#else
                var serverPackages = await RunLockTask(() => store.FindSourcePackages(store.MainPackageIds, CancellationToken.None).Result.FilterStrideMainPackages().Where(p => !store.IsDevRedirectPackage(p)).OrderByDescending(p => p.Version).ToList());
#endif
                // Check if we could connect to the server
                var wasOffline = IsOffline;
                IsOffline = serverPackages.Count == 0;

                // Inform the user if we just switched offline
                if (IsOffline && !wasOffline)
                {
                    var message = $@"**{Strings.ErrorOfflineMode}**
### Log
```
{LogMessages}
```";
                    await ServiceProvider.Get<IDialogService>().MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Information);
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
                        if (serverPackage != null)
                        {
                            // Find if we already have this package in our list
                            int index = strideVersions.BinarySearch(Tuple.Create(serverPackage.Version.Version.Major, serverPackage.Version.Version.Minor));
                            StrideStoreVersionViewModel version;
                            if (index < 0)
                            {
                                // If not, add it
                                version = new StrideStoreVersionViewModel(this, store, null, serverPackage.Id, serverPackage.Version.Version.Major, serverPackage.Version.Version.Minor);
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
                Dispatcher.Invoke(() =>
                {
                    // Allow to install the latest version if any version is found
                    var latestVersion = strideVersions.FirstOrDefault();
                    if (latestVersion != null)
                    {
                        // Latest version not installed and can be downloaded
                        if (latestVersion.CanBeDownloaded)
                            InstallLatestVersionCommand.IsEnabled = !latestVersion.CanDelete && latestVersion.CanBeDownloaded;
                    }

                    OnPropertyChanging(nameof(ActiveDocumentationPages));
                    OnPropertyChanged(nameof(ActiveDocumentationPages));
                });
            }
        }

        public async Task CheckForFirstInstall()
        {
            const string prerequisitesRunTaskName = "PrerequisitesRun";
            //const string askedForJapaneseSurveyTaskName = "AskedForJapaneseSurvey";
            //const string askedForSurveyTaskName = "AskedForSurvey";

            if (!HasDoneTask(prerequisitesRunTaskName))
            {
                foreach (var version in StrideVersions.OfType<StrideStoreVersionViewModel>().Where(x => x.CanDelete))
                {
                    await version.RunPrerequisitesInstaller();
                }
                SaveTaskAsDone(prerequisitesRunTaskName);
            }

            bool firstInstall = StrideVersions.All(x => !x.CanDelete) && StrideVersions.Any(x => x.CanBeDownloaded);
            //var surveyTaskName = CultureInfo.InstalledUICulture.IetfLanguageTag != "ja-JP" ? askedForSurveyTaskName : askedForJapaneseSurveyTaskName;
            //bool surveyAsked = HasDoneTask(surveyTaskName);

            await Dispatcher.InvokeTask(async () =>
            {
                if (firstInstall)
                {
                    var result = await ServiceProvider.Get<IDialogService>().MessageBox(Strings.AskInstallVersion, MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        var versionToInstall = StrideVersions.First(x => x.CanBeDownloaded);
                        await versionToInstall.Download(true);
                    }
                    if (!VsixPackage.IsLatestVersionInstalled && VisualStudioVersions.AvailableVisualStudioInstances.Any())
                    {
                        result = await ServiceProvider.Get<IDialogService>().MessageBox(Strings.AskInstallVSIX, MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            await VsixPackage.ExecuteAction();
                        }
                    }
                }
                // Disable dialog for the survey
                //else if (!surveyAsked)
                //{
                //    var result = ShowMessage(ServiceProvider, Strings.AskSurvey, MessageBoxButton.YesNo, MessageBoxImage.Question);
                //    if (result == MessageBoxResult.Yes)
                //    {
                //        try
                //        {
                //            Process.Start(Urls.Survey1);
                //        }
                //        catch
                //        {
                //            ShowMessage(ServiceProvider, Strings.ErrorOpeningBrowser, MessageBoxButton.OK, MessageBoxImage.Error);
                //        }
                //    }
                //    SaveTaskAsDone(surveyTaskName);
                //}
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
            if (argument == null) throw new ArgumentNullException(nameof(argument));
            if (ActiveVersion == null)
                return;

            if (AutoCloseLauncher)
            {
                argument = $"/LauncherWindowHandle {WindowHandle} {argument}";
            }

            MetricsClient metricsForEditorBefore120 = null;
            try
            {
                Dispatcher.Invoke(() => StartStudioCommand.IsEnabled = false);
                var mainExecutable = ActiveVersion.LocateMainExecutable();

                // If version is older than 1.2.0, than we need to log the usage of older version
                var activeStoreVersion = ActiveVersion as StrideStoreVersionViewModel;
                if (activeStoreVersion != null && activeStoreVersion.Version.Version < new Version(1, 2, 0, 0))
                {
                    metricsForEditorBefore120 = new MetricsClient(CommonApps.StrideEditorAppId, versionOverride: activeStoreVersion.Version.ToString());
                }

                Process.Start(mainExecutable, argument);
            }
            catch (Exception e)
            {
                var message = string.Format(Strings.ErrorStartingProcess, e.FormatSummary(true));
                await ServiceProvider.Get<IDialogService>().MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                metricsForEditorBefore120?.Dispose();
            }
            await Task.Delay(5000);
            Dispatcher.Invoke(() =>
            {
                StartStudioCommand.IsEnabled = ActiveVersion != null && ActiveVersion.CanStart;
                //Save settings because launcher maybe have not been closed
                LauncherSettings.ActiveVersion = ActiveVersion != null ? ActiveVersion.Name : "";
                LauncherSettings.Save();
            });
        }

        private async Task InstallLatestVersion()
        {
            var latestVersion = strideVersions.FirstOrDefault();
            // Should never happen
            if (latestVersion == null || !latestVersion.CanBeDownloaded)
                return;

            if (latestVersion.IsProcessing)
            {
                await ServiceProvider.Get<IDialogService>().MessageBox(Strings.InstallAlreadyInProgress, MessageBoxButton.OK, MessageBoxImage.Information);
                InstallLatestVersionCommand.IsEnabled = false;
            }

            latestVersion.DownloadCommand.Execute();
        }

        private async Task OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            // FIXME: catch only specific exceptions?
            catch (Exception)
            {
                await ServiceProvider.Get<IDialogService>().MessageBox(Strings.ErrorOpeningBrowser, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task FetchNewsPages()
        {
            var pages = await NewsPageViewModel.FetchNewsPages(ServiceProvider, 30);
            var sortedPages = pages.OrderBy(x => x.Date).Reverse().ToList();
            Dispatcher.Invoke(() => NewsPages = new ObservableList<NewsPageViewModel>(sortedPages));
        }

        public static bool HasDoneTask(string taskName)
        {
            var localMachine32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            using (var subkey = localMachine32.OpenSubKey(@"SOFTWARE\Stride\"))
            {
                if (subkey != null)
                {
                    var value = (string)subkey.GetValue(taskName);
                    return value != null && value.ToLowerInvariant() == "true";
                }
            }
            return false;
        }

        public static void SaveTaskAsDone(string taskName)
        {
            var localMachine32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            using (var subkey = localMachine32.CreateSubKey(@"SOFTWARE\Stride\"))
            {
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
}
