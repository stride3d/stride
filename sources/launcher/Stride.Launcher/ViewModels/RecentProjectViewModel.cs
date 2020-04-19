// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.LauncherApp.Resources;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;
using Stride.LauncherApp.Services;

namespace Stride.LauncherApp.ViewModels
{
    internal class RecentProjectViewModel : DispatcherViewModel
    {
        private readonly UFile fullPath;
        private string strideVersionName;
        private Version strideVersion;

        internal RecentProjectViewModel(LauncherViewModel launcher, UFile path)
            : base(launcher.SafeArgument(nameof(launcher)).ServiceProvider)
        {
            Name = path.GetFileNameWithoutExtension();
            Launcher = launcher;
            fullPath = path;
            StrideVersionName = Strings.ReportDiscovering;
            OpenCommand = new AnonymousTaskCommand(ServiceProvider, () => OpenWith(null)) { IsEnabled = false };
            OpenWithCommand = new AnonymousTaskCommand<StrideVersionViewModel>(ServiceProvider, OpenWith);
            ExploreCommand = new AnonymousCommand(ServiceProvider, Explore);
            RemoveCommand = new AnonymousCommand(ServiceProvider, Remove);
            CompatibleVersions = new ObservableList<StrideVersionViewModel>();
            DiscoverStrideVersion();
        }

        public string Name { get; private set; }

        public string FullPath => fullPath.ToWindowsPath();

        public string StrideVersionName { get { return strideVersionName; } private set { SetValue(ref strideVersionName, value); } }

        public Version StrideVersion { get { return strideVersion; } private set { SetValue(ref strideVersion, value); } }

        public LauncherViewModel Launcher { get; }

        public ObservableList<StrideVersionViewModel> CompatibleVersions { get; private set; }

        public ICommandBase ExploreCommand { get; }

        public ICommandBase OpenCommand { get; }

        public ICommandBase OpenWithCommand { get; }

        public ICommandBase RemoveCommand { get; }

        private void DiscoverStrideVersion()
        {
            Task.Run(async () =>
            {
                var packageVersion = await PackageSessionHelper.GetPackageVersion(fullPath);
                StrideVersion = packageVersion != null ? new Version(packageVersion.Version.Major, packageVersion.Version.Minor) : null;
                StrideVersionName = StrideVersion?.ToString();

                Dispatcher.Invoke(() => OpenCommand.IsEnabled = StrideVersionName != null);
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
            //Remove files that's was deleted or upgraded by stride versions <= 3.0
            if (string.IsNullOrEmpty(this.StrideVersionName) || string.Compare(this.StrideVersionName, "3.0", StringComparison.Ordinal) <= 0)
            {
                //Get all installed versions 
                var strideInstalledVersions = this.Launcher.StrideVersions.Where(x => x.CanDelete)
                    .Select(x => $"{x.Major}.{x.Minor}").ToList();

                //If original version of files is not in list get and to add it.
                if (!string.IsNullOrEmpty(this.StrideVersionName) && !strideInstalledVersions.Any(x => x.Equals(this.StrideVersionName)))
                    strideInstalledVersions.Add(this.StrideVersionName);

                foreach (var item in strideInstalledVersions)
                {
                    GameStudioSettings.RemoveMostRecentlyUsed(this.fullPath, item);
                }
            }
            else
            {
                GameStudioSettings.RemoveMostRecentlyUsed(this.fullPath, this.StrideVersionName);
            }
        }

        private async Task OpenWith(StrideVersionViewModel version)
        {
            string message;
            version = version ?? Launcher.StrideVersions.FirstOrDefault(x => new Version(x.Major, x.Minor) == StrideVersion);
            if (version == null)
            {
                message = string.Format(Strings.ErrorDoNotFindVersion, StrideVersion);
                await ServiceProvider.Get<IDialogService>().MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (version.IsProcessing)
            {
                message = string.Format(Strings.ErrorVersionBeingUpdated, StrideVersion);
                await ServiceProvider.Get<IDialogService>().MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!version.CanDelete)
            {
                message = string.Format(Strings.ErrorVersionNotInstalled, StrideVersion);
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
