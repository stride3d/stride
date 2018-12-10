// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

using Xenko.Core.Extensions;
using Xenko.PrivacyPolicy;
using Xenko.LauncherApp.Resources;
using Xenko.LauncherApp.Services;
using Xenko.Core.Packages;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Metrics;

namespace Xenko.LauncherApp.ViewModels
{
    /// <summary>
    /// This class represents the root view model of the launcher.
    /// </summary>
    internal class LauncherViewModel : DispatcherViewModel, IPackagesLogger, IDisposable
    {
        private readonly NugetStore store;
        private readonly SortedObservableCollection<XenkoVersionViewModel> xenkoVersions = new SortedObservableCollection<XenkoVersionViewModel>();
        private readonly UninstallHelper uninstallHelper;
        private readonly object objectLock = new object();
        private ObservableList<NewsPageViewModel> newsPages;
        private ReleaseNotesViewModel activeReleaseNotes;
        private XenkoVersionViewModel activeVersion;
        private bool isOffline;
        private bool isSynchronizing = true;
        private string currentToolTip;
        private string lastErrorOrWarning;
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

            VsixPackage = new VsixVersionViewModel(this, store);
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
                var version = new XenkoDevVersionViewModel(this, store, null, devVersion, false);
                xenkoVersions.Add(version);
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

        public IEnumerable<XenkoVersionViewModel> XenkoVersions => xenkoVersions;

        public bool ShowBetaVersions { get { return showBetaVersions; } set { SetValue(ref showBetaVersions, value); } }

        public VsixVersionViewModel VsixPackage { get; }

        public XenkoVersionViewModel ActiveVersion { get { return activeVersion; } set { SetValue(ref activeVersion, value, () => Dispatcher.InvokeAsync(() => StartStudioCommand.IsEnabled = (value != null))); } }

        public ObservableList<RecentProjectViewModel> RecentProjects { get; } = new ObservableList<RecentProjectViewModel>();

        public ObservableList<NewsPageViewModel> NewsPages { get { return newsPages; } private set { SetValue(ref newsPages, value); } }

        public ReleaseNotesViewModel ActiveReleaseNotes { get { return activeReleaseNotes; } set { SetValue(ref activeReleaseNotes, value); } }

        public ObservableList<DocumentationPageViewModel> ActiveDocumentationPages => ActiveVersion.Yield().Concat(XenkoVersions).OfType<XenkoStoreVersionViewModel>().FirstOrDefault()?.DocumentationPages;

        public AnnouncementViewModel Announcement { get { return announcement; } set { SetValue(ref announcement, value); } }

        public bool IsOffline { get { return isOffline; } set { SetValue(ref isOffline, value); } }

        public bool IsSynchronizing { get { return isSynchronizing; } set { SetValue(ref isSynchronizing, value); } }

        public string CurrentToolTip { get { return currentToolTip; } set { SetValue(ref currentToolTip, value); } }

        public string LastErrorOrWarning { get { return lastErrorOrWarning; } set { SetValue(ref lastErrorOrWarning, value); } }

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
                await RetrieveLocalXenkoVersions();
                await RunLockTask(() => SelfUpdater.SelfUpdate(ServiceProvider, store));
                await RetrieveServerXenkoVersions();
                await VsixPackage.UpdateFromStore();
                await CheckForFirstInstall();
                await FetchNewsPages();
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

        public async Task RetrieveAllXenkoVersions()
        {
            Dispatcher.Invoke(() => IsSynchronizing = true);
            await RetrieveLocalXenkoVersions();
            await RetrieveServerXenkoVersions();
            Dispatcher.Invoke(() => IsSynchronizing = false);

        }

        public async Task RetrieveLocalXenkoVersions()
        {
            List<RecentProjectViewModel> currentRecentProjects;
            lock (RecentProjects)
            {
                currentRecentProjects = new List<RecentProjectViewModel>(RecentProjects);
            }
            try
            {
                var localPackages = await RunLockTask(() => store.GetPackagesInstalled(store.MainPackageIds).FilterXenkoMainPackages().OrderByDescending(p => p.Version).ToList());
                lock (objectLock)
                {
                    // Retrieve all local packages
                    var packages = localPackages.Where(p => !store.IsDevRedirectPackage(p)).GroupBy(p => $"{p.Version.Version.Major}.{p.Version.Version.Minor}", p => p);
                    var updatedLocalPackages = new HashSet<XenkoStoreVersionViewModel>();
                    foreach (var package in packages)
                    {
                        var localPackage = package.FirstOrDefault();
                        if (localPackage != null)
                        {
                            // Find if we already have this package in our list
                            int index = xenkoVersions.BinarySearch(Tuple.Create(localPackage.Version.Version.Major, localPackage.Version.Version.Minor));
                            XenkoStoreVersionViewModel version;
                            if (index < 0)
                            {
                                // If not, add it
                                version = new XenkoStoreVersionViewModel(this, store, localPackage, localPackage.Version.Version.Major, localPackage.Version.Version.Minor);
                                Dispatcher.Invoke(() => xenkoVersions.Add(version));
                            }
                            else
                            {
                                version = (XenkoStoreVersionViewModel)xenkoVersions[index];
                            }
                            version.UpdateLocalPackage(localPackage);
                            updatedLocalPackages.Add(version);
                        }
                    }

                    // Update versions that are not installed locally anymore
                    Dispatcher.Invoke(() =>
                    {
                        foreach (var xenkoUninstalledVersion in xenkoVersions.OfType<XenkoStoreVersionViewModel>().Where(x => !updatedLocalPackages.Contains(x)))
                            xenkoUninstalledVersion.UpdateLocalPackage(null);
                    });

                    // Update the active version if it is now invalid.
                    if (ActiveVersion == null || !xenkoVersions.Contains(ActiveVersion) || !ActiveVersion.CanDelete)
                        ActiveVersion = XenkoVersions.FirstOrDefault(x => x.CanDelete);

                    if (!lastActiveVersionRestored)
                    {
                        var restoredVersion = XenkoVersions.FirstOrDefault(x => x.CanDelete && x.Name == LauncherSettings.ActiveVersion);
                        if (restoredVersion != null)
                        {
                            ActiveVersion = restoredVersion;
                            lastActiveVersionRestored = true;
                        }
                    }
                }

                var devPackages = localPackages.Where(store.IsDevRedirectPackage);
                Dispatcher.Invoke(() => xenkoVersions.RemoveWhere(x => x is XenkoDevVersionViewModel));
                foreach (var package in devPackages)
                {
                    try
                    {
                        var realPath = store.GetRealPath(package);
                        var version = new XenkoDevVersionViewModel(this, store, package, realPath, true);
                        Dispatcher.Invoke(() => xenkoVersions.Add(version));
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
                        if (project.XenkoVersionName == "1.0")
                            continue;

                        project.CompatibleVersions.Clear();
                        foreach (var version in XenkoVersions)
                        {
                            // We suppose all dev versions are compatible with any project.
                            if (version is XenkoDevVersionViewModel)
                                project.CompatibleVersions.Add(version);

                            var storeVersion = version as XenkoStoreVersionViewModel;
                            if (storeVersion != null && storeVersion.CanDelete)
                            {
                                // Discard the version that matches the recent project version
                                if (project.XenkoVersion == new Version(storeVersion.Version.Version.Major, storeVersion.Version.Version.Minor))
                                    continue;

                                // Discard the versions that are anterior to the recent project version
                                if (project.XenkoVersion > storeVersion.Version.Version)
                                    continue;

                                project.CompatibleVersions.Add(version);
                            }
                        }
                    }
                });
            }
        }

        private async Task RetrieveServerXenkoVersions()
        {
            try
            {
#if SIMULATE_OFFLINE
                var serverPackages = new List<IPackage>();
#else
                var serverPackages = await RunLockTask(() => store.FindSourcePackages(store.MainPackageIds, CancellationToken.None).Result.FilterXenkoMainPackages().Where(p => !store.IsDevRedirectPackage(p)).OrderByDescending(p => p.Version).ToList());
#endif
                // Check if we could connect to the server
                var wasOffline = IsOffline;
                IsOffline = serverPackages.Count == 0;

                // Inform the user if we just switched offline
                if (IsOffline && !wasOffline)
                {
                    var message = Strings.ErrorOfflineMode;
                    if (!string.IsNullOrEmpty(LastErrorOrWarning))
                    {
                        message += Environment.NewLine + Environment.NewLine + Strings.Details + Environment.NewLine + LastErrorOrWarning;
                    }
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
                            int index = xenkoVersions.BinarySearch(Tuple.Create(serverPackage.Version.Version.Major, serverPackage.Version.Version.Minor));
                            XenkoStoreVersionViewModel version;
                            if (index < 0)
                            {
                                // If not, add it
                                version = new XenkoStoreVersionViewModel(this, store, null, serverPackage.Version.Version.Major, serverPackage.Version.Version.Minor);
                                Dispatcher.Invoke(() => xenkoVersions.Add(version));
                            }
                            else
                            {
                                // If yes, update it and remove it from the list of old version
                                version = (XenkoStoreVersionViewModel)xenkoVersions[index];
                            }
                            version.UpdateServerPackage(serverPackage);
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
                    var latestVersion = xenkoVersions.FirstOrDefault();
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
                foreach (var version in XenkoVersions.OfType<XenkoStoreVersionViewModel>().Where(x => x.CanDelete))
                {
                    await version.RunPrerequisitesInstaller();
                }
                SaveTaskAsDone(prerequisitesRunTaskName);
            }

            bool firstInstall = XenkoVersions.All(x => !x.CanDelete) && XenkoVersions.Any(x => x.CanBeDownloaded);
            //var surveyTaskName = CultureInfo.InstalledUICulture.IetfLanguageTag != "ja-JP" ? askedForSurveyTaskName : askedForJapaneseSurveyTaskName;
            //bool surveyAsked = HasDoneTask(surveyTaskName);

            await Dispatcher.InvokeTask(async () =>
            {
                if (firstInstall)
                {
                    var result = await ServiceProvider.Get<IDialogService>().MessageBox(Strings.AskInstallVersion, MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        var versionToInstall = XenkoVersions.First(x => x.CanBeDownloaded);
                        versionToInstall.DownloadCommand.Execute();
                    }
                    if (VsixPackage != null && !VsixPackage.IsLatestVersionInstalled)
                    {
                        result = await ServiceProvider.Get<IDialogService>().MessageBox(Strings.AskInstallVSIX, MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            VsixPackage.ExecuteActionCommand.Execute();
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
                var packagePath = ActiveVersion.InstallPath;
                var mainExecutable = store.LocateMainExecutable(packagePath);

                // If version is older than 1.2.0, than we need to log the usage of older version
                var activeStoreVersion = ActiveVersion as XenkoStoreVersionViewModel;
                if (activeStoreVersion != null && activeStoreVersion.Version.Version < new Version(1, 2, 0, 0))
                {
                    metricsForEditorBefore120 = new MetricsClient(CommonApps.XenkoEditorAppId, versionOverride: activeStoreVersion.Version.ToString());
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
                StartStudioCommand.IsEnabled = ActiveVersion != null;
                //Save settings because launcher maybe have not been closed
                LauncherSettings.ActiveVersion = ActiveVersion != null ? ActiveVersion.Name : "";
                LauncherSettings.Save();
            });
        }

        private async Task InstallLatestVersion()
        {
            var latestVersion = xenkoVersions.FirstOrDefault();
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
            using (var subkey = localMachine32.OpenSubKey(@"SOFTWARE\Xenko\"))
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
            using (var subkey = localMachine32.CreateSubKey(@"SOFTWARE\Xenko\"))
            {
                subkey?.SetValue(taskName, "True");
            }
        }

        private void DisplayReleaseAnnouncement()
        {
            const string announcementName = "Release30";
            if (!PrivacyPolicyHelper.Xenko30Accepted)
            {
                // If the user is a beta user, it will have already accepted Privacy Policy 1.2 before the first chance to display this announcement.
                // If he didn't, we don't need to display the message so we mark this announcement as done.
                var taskName = AnnouncementViewModel.GetTaskName(announcementName);
                SaveTaskAsDone(taskName);
            }
            var viewModel = new AnnouncementViewModel(this, announcementName);
            Announcement = viewModel.MarkdownAnnouncement != null ? viewModel : null;
        }

        void IPackagesLogger.Log(MessageLevel level, string message)
        {
            if (level == MessageLevel.Warning || level == MessageLevel.Error)
            {
                LastErrorOrWarning = string.Format(message);
            }
        }

        Task IPackagesLogger.LogAsync(MessageLevel level, string message)
        {
            ((IPackagesLogger)this).Log(level, message);
            return Task.CompletedTask;
        }
    }
}
