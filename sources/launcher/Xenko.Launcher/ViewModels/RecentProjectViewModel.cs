// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.LauncherApp.Resources;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;
using Xenko.LauncherApp.Services;

namespace Xenko.LauncherApp.ViewModels
{
    internal class RecentProjectViewModel : DispatcherViewModel
    {
        private readonly UFile fullPath;
        private string xenkoVersionName;
        private Version xenkoVersion;

        internal RecentProjectViewModel(LauncherViewModel launcher, UFile path)
            : base(launcher.SafeArgument(nameof(launcher)).ServiceProvider)
        {
            Name = path.GetFileNameWithoutExtension();
            Launcher = launcher;
            fullPath = path;
            XenkoVersionName = Strings.ReportDiscovering;
            OpenCommand = new AnonymousTaskCommand(ServiceProvider, () => OpenWith(null)) { IsEnabled = false };
            OpenWithCommand = new AnonymousTaskCommand<XenkoVersionViewModel>(ServiceProvider, OpenWith);
            ExploreCommand = new AnonymousCommand(ServiceProvider, Explore);
            RemoveCommand = new AnonymousCommand(ServiceProvider, Remove);
            CompatibleVersions = new ObservableList<XenkoVersionViewModel>();
            DiscoverXenkoVersion();
        }

        public string Name { get; private set; }

        public string FullPath => fullPath.ToWindowsPath();

        public string XenkoVersionName { get { return xenkoVersionName; } private set { SetValue(ref xenkoVersionName, value); } }

        public Version XenkoVersion { get { return xenkoVersion; } private set { SetValue(ref xenkoVersion, value); } }

        public LauncherViewModel Launcher { get; }

        public ObservableList<XenkoVersionViewModel> CompatibleVersions { get; private set; }

        public ICommandBase ExploreCommand { get; }

        public ICommandBase OpenCommand { get; }

        public ICommandBase OpenWithCommand { get; }

        public ICommandBase RemoveCommand { get; }

        private void DiscoverXenkoVersion()
        {
            Task.Run(async () =>
            {
                var packageVersion = await PackageSessionHelper.GetPackageVersion(fullPath);
                XenkoVersion = new Version(packageVersion.Version.Major, packageVersion.Version.Minor);
                XenkoVersionName = XenkoVersion?.ToString();

                Dispatcher.Invoke(() => OpenCommand.IsEnabled = XenkoVersionName != null);
            });
        }

        private void Explore()
        {
            var startInfo = new ProcessStartInfo("explorer.exe", $"/select,{fullPath.ToWindowsPath()}") { UseShellExecute = true };
            var explorer = new Process { StartInfo = startInfo };
            explorer.Start();
        }

        private void Remove()
        {
            //Remove files that's was deleted or upgraded by xenko versions <= 3.0
            if (string.IsNullOrEmpty(this.XenkoVersionName) || string.Compare(this.XenkoVersionName, "3.0", StringComparison.Ordinal) <= 0)
            {
                //Get all installed versions 
                var xenkoInstalledVersions = this.Launcher.XenkoVersions.Where(x => x.CanDelete)
                    .Select(x => $"{x.Major}.{x.Minor}").ToList();

                //If original version of files is not in list get and to add it.
                if (!string.IsNullOrEmpty(this.XenkoVersionName) && !xenkoInstalledVersions.Any(x => x.Equals(this.XenkoVersionName)))
                    xenkoInstalledVersions.Add(this.XenkoVersionName);

                foreach (var item in xenkoInstalledVersions)
                {
                    GameStudioSettings.RemoveMostRecentlyUsed(this.fullPath, item);
                }
            }
            else
            {
                GameStudioSettings.RemoveMostRecentlyUsed(this.fullPath, this.XenkoVersionName);
            }
        }

        private async Task OpenWith(XenkoVersionViewModel version)
        {
            string message;
            version = version ?? Launcher.XenkoVersions.FirstOrDefault(x => x.Name == XenkoVersionName);
            if (version == null)
            {
                message = string.Format(Strings.ErrorDoNotFindVersion, XenkoVersion);
                await ServiceProvider.Get<IDialogService>().MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (version.IsProcessing)
            {
                message = string.Format(Strings.ErrorVersionBeingUpdated, XenkoVersion);
                await ServiceProvider.Get<IDialogService>().MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!version.CanDelete)
            {
                message = string.Format(Strings.ErrorVersionNotInstalled, XenkoVersion);
                var result = await ServiceProvider.Get<IDialogService>().MessageBox(message, MessageBoxButton.YesNoCancel, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    version.DownloadCommand.Execute();
                }
                return;
            }
            Launcher.ActiveVersion = version;
            Launcher.StartStudio($"\"{FullPath}\"");
        }
    }
}
