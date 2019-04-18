// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Extensions;
using Xenko.LauncherApp.Resources;
using Xenko.Core.Packages;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Services;

namespace Xenko.LauncherApp.ViewModels
{
    /// <summary>
    /// An implementation of the <see cref="XenkoVersionViewModel"/> that represents an official release coming from the store.
    /// </summary>
    internal sealed class XenkoStoreVersionViewModel : XenkoVersionViewModel
    {
        private ReleaseNotesViewModel releaseNotes;

        /// <summary>
        /// Initializes a new instance of the <see cref="XenkoStoreVersionViewModel"/>
        /// </summary>
        /// <param name="launcher"></param>
        /// <param name="store"></param>
        /// <param name="localPackage"></param>
        /// <param name="major"></param>
        /// <param name="minor"></param>
        internal XenkoStoreVersionViewModel(LauncherViewModel launcher, NugetStore store, NugetLocalPackage localPackage, int major, int minor)
            : base(launcher, store, localPackage, major, minor)
        {
            FetchReleaseNotes();
            FetchDocumentation();
        }

        /// <summary>
        /// Gets the full name of this version, including revision number and special revision string.
        /// </summary>
        /// <remarks>If this version is installed, it will use the name of the installed version. Otherwise, it will use the name of the latest version available on the server.</remarks>
        public override string FullName
        {
            get
            {
                var result = Version?.ToString() ?? "Unknown";
                //if (ServerPackage != null)
                //    result += $" ({ServerPackage.Source})";
                return result;
            }
        }

        /// <summary>
        /// Gets the full name of this version on the server.
        /// </summary>
        public string ServerVersionFullName => ServerPackage?.Version?.ToString() ?? "";

        /// <summary>
        /// Gets the release notes associated to this version.
        /// </summary>
        public ReleaseNotesViewModel ReleaseNotes { get { return releaseNotes; } private set { SetValue(ref releaseNotes, value); } }

        /// <summary>
        /// Gets the collection of <see cref="DocumentationPageViewModel"/> associated with this version.
        /// </summary>
        public ObservableList<DocumentationPageViewModel> DocumentationPages { get; } = new ObservableList<DocumentationPageViewModel>();

        /// <summary>
        /// Gets the full version of the local package if it exists, or the server package.
        /// </summary>
        /// <value>The version.</value>
        public PackageVersion Version => LocalPackage != null ? LocalPackage.Version : ServerPackage?.Version;

        /// <summary>
        /// Updates the local package of this version.
        /// </summary>
        /// <param name="package">The local package corresponding to this version.</param>
        internal void UpdateLocalPackage(NugetLocalPackage package)
        {
            OnPropertyChanging(nameof(FullName), nameof(Version));
            LocalPackage = package;
            OnPropertyChanged(nameof(FullName), nameof(Version));
            Dispatcher.Invoke(UpdateStatus);
        }

        /// <summary>
        /// Updates the server package of this version.
        /// </summary>
        /// <param name="package">The server package corresponding to this version.</param>
        internal void UpdateServerPackage(NugetServerPackage package)
        {
            ServerPackage = package;
            Dispatcher.Invoke(UpdateStatus);
        }

        internal async Task RunPrerequisitesInstaller()
        {
            // Only used for older packages
            if (ServerPackage.Version.Version >= new Version(1, 11, 2, 0))
            {
                return;
            }

            // Run prerequisites installer (if it exists)
            var prerequisitesInstaller = Store.GetPrerequisitesInstaller();
            if (!string.IsNullOrEmpty(prerequisitesInstaller))
            {
                var packagePath = Store.GetInstalledPath(ServerPackage.Id, ServerPackage.Version);
                var prerequisitesInstallerPath = Path.Combine(packagePath, prerequisitesInstaller);
                if (File.Exists(prerequisitesInstallerPath))
                {
                    CurrentProcessStatus = Strings.ReportInstallingPrerequisites;
                    var prerequisitesInstalled = false;
                    while (!prerequisitesInstalled)
                    {
                        try
                        {
                            var prerequisitesInstallerProcess = Process.Start(prerequisitesInstallerPath);
                            prerequisitesInstallerProcess?.WaitForExit();
                            prerequisitesInstalled = true;
                        }
                        catch
                        {
                            // We'll enter this if UAC has been declined, but also if it timed out (which is a frequent case
                            // if you don't stay in front of your computer during the installation.
                            var result = await ServiceProvider.Get<IDialogService>().MessageBox("The installation of prerequisites has been canceled by user or failed to run. Do you want to run it again?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                            if (result != MessageBoxResult.Yes)
                                break;
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override string InstallErrorMessage => string.Format(Strings.ErrorInstallingVersion, ServerVersionFullName);

        /// <inheritdoc/>
        protected override string UninstallErrorMessage => string.Format(Strings.ErrorUninstallingVersion, FullName);

        /// <inheritdoc/>
        protected override Task UpdateVersionsFromStore()
        {
            return Launcher.RetrieveAllXenkoVersions();
        }

        /// <inheritdoc/>
        protected override void UpdateStatus()
        {
            base.UpdateStatus();
            OnPropertyChanging(nameof(ServerVersionFullName));
            OnPropertyChanged(nameof(ServerVersionFullName));
        }

        /// <inheritdoc/>
        protected override void UpdateInstallStatus()
        {
            switch (CurrentProgressAction)
            {
                case ProgressAction.Download:
                    CurrentProcessStatus = string.Format(Strings.ReportDownloadingVersion, ServerVersionFullName, CurrentProgress);
                    break;
                case ProgressAction.Install:
                    CurrentProcessStatus = string.Format(Strings.ReportInstallingVersion, ServerVersionFullName, CurrentProgress);
                    break;
                case ProgressAction.Delete:
                    CurrentProcessStatus = string.Format(Strings.ReportDeletingVersion, FullName, CurrentProgress);
                    break;
            }
        }

        /// <inheritdoc/>
        protected override void BeforeDownload()
        {
            base.BeforeDownload();
            ReleaseNotes.Show();
        }

        /// <inheritdoc/>
        protected override void AfterDownload()
        {
            base.AfterDownload();
            RunPrerequisitesInstaller().Forget();
        }

        /// <summary>
        /// Fetches the documentation pages corresponding to this version from the server.
        /// </summary>
        internal async void FetchDocumentation()
        {
            var pages = await DocumentationPageViewModel.FetchGettingStartedPages(ServiceProvider, $"{Major}.{Minor}");
            Dispatcher.Invoke(() => { DocumentationPages.Clear(); DocumentationPages.AddRange(pages); });
        }

        internal void FetchReleaseNotes()
        {
            Dispatcher.Invoke(() =>
            {
                ReleaseNotes = new ReleaseNotesViewModel(Launcher, Name);
                ReleaseNotes.FetchReleaseNotes();
            });
        }
    }
}
