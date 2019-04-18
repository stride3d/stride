// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Extensions;
using Xenko.LauncherApp.Resources;
using Xenko.Core.Packages;
using Xenko.LauncherApp.Services;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.LauncherApp.ViewModels
{
    /// <summary>
    /// A view model class that represents a Nuget package, as it exists both locally and on a remote server.
    /// </summary>
    internal abstract class PackageVersionViewModel : DispatcherViewModel
    {
        protected NugetLocalPackage LocalPackage;
        protected NugetServerPackage ServerPackage;
        private ProgressAction currentProgressAction;
        private int currentProgress;
        private bool isProcessing;
        private bool canBeDownloaded;
        private bool canDelete;
        private string currentProcessStatus;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageVersionViewModel"/> class.
        /// </summary>
        /// <param name="launcher">The parent <see cref="LauncherViewModel"/> instance.</param>
        /// <param name="store">The related <see cref="NugetStore"/> instance.</param>
        /// <param name="localPackage">The local package of this version, if a local package exists.</param>
        internal PackageVersionViewModel(LauncherViewModel launcher, NugetStore store, NugetLocalPackage localPackage)
            : base(launcher.SafeArgument("launcher").ServiceProvider)
        {
            if (launcher == null) throw new ArgumentNullException(nameof(launcher));
            if (store == null) throw new ArgumentNullException(nameof(store));
            Launcher = launcher;
            Store = store;
            LocalPackage = localPackage;
            DownloadCommand = new AnonymousCommand(ServiceProvider, () => Download(true));
            DeleteCommand = new AnonymousTaskCommand(ServiceProvider, () => Delete(true, true)) { IsEnabled = CanDelete };
            UpdateStatusInternal();
        }

        /// <summary>
        /// Gets the short name of this version.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the full name of this version.
        /// </summary>
        public abstract string FullName { get; }

        /// <summary>
        /// Gets the installation path of this version, or <c>null</c> if it is not installed.
        /// </summary>
        public virtual string InstallPath => LocalPackage?.Path;

        /// <summary>
        /// Gets whether a download is available for this version, being an update or a first install.
        /// </summary>
        public virtual bool CanBeDownloaded { get { return canBeDownloaded; } private set { SetValue(ref canBeDownloaded, value); } }

        /// <summary>
        /// Gets whether this package is installed and can be deleted.
        /// </summary>
        public virtual bool CanDelete { get { return canDelete; } private set { SetValue(ref canDelete, value); } }

        /// <summary>
        /// Gets the progress of the current download, in percents.
        /// </summary>
        public ProgressAction CurrentProgressAction { get { return currentProgressAction; } private set { SetValue(ref currentProgressAction, value); } }

        /// <summary>
        /// Gets the progress of the current download, in percents.
        /// </summary>
        public int CurrentProgress { get { return currentProgress; } private set { SetValue(ref currentProgress, value); } }

        /// <summary>
        /// Gets whether this version is being processed, being installed, upgraded or deleted.
        /// </summary>
        public bool IsProcessing { get { return isProcessing; } protected set { SetValue(ref isProcessing, value); } }

        /// <summary>
        /// Gets a string representing the current status while this version is being installed, upgraded or deleted.
        /// </summary>
        public string CurrentProcessStatus { get { return currentProcessStatus; } protected set { SetValue(ref currentProcessStatus, value); } }

        /// <summary>
        /// Gets the command that will download the latest version of the associated package and deploy it.
        /// </summary>
        public ICommandBase DownloadCommand { get; }

        /// <summary>
        /// Gets the command that will delete the associated package.
        /// </summary>
        public CommandBase DeleteCommand { get; }

        public LauncherViewModel Launcher { get; }

        /// <summary>
        /// Gets the related <see cref="NugetStore"/> instance.
        /// </summary>
        protected NugetStore Store { get; }

        /// <summary>
        /// Gets the message to display when an error occurs during the install of this package.
        /// </summary>
        protected abstract string InstallErrorMessage { get; }

        /// <summary>
        /// Gets the message to display when an error occurs during the uninstall of this package.
        /// </summary>
        protected abstract string UninstallErrorMessage { get; }

        /// <summary>
        /// Updates all the versions of this type from the store. This method should update the <see cref="LocalPackage"/> and <see cref="ServerPackage"/>
        /// for each version of the same type, remove versions that do not exist anymore, and add new versions.
        /// </summary>
        /// <returns>A task that completes when the versions are updated.</returns>
        protected abstract Task UpdateVersionsFromStore();

        /// <summary>
        /// Updates the status of this version, synchronizing the different properties and command state of the view model with the local and server packages status.
        /// </summary>
        protected virtual void UpdateStatus()
        {
            UpdateStatusInternal();
        }

        protected void UpdateProgress(ProgressAction action, int progress)
        {
            CurrentProgressAction = action;
            CurrentProgress = progress;
            UpdateInstallStatus();
        }

        /// <summary>
        /// Updates the <see cref="CurrentProcessStatus"/> property according to the <see cref="CurrentProgress"/> value.
        /// </summary>
        protected abstract void UpdateInstallStatus();

        /// <summary>
        /// Executes some actions before starting to download this version.
        /// </summary>
        protected virtual void BeforeDownload()
        {
            // Intentionally does nothing.
        }

        /// <summary>
        /// Executes some actions after downloading and installing this version.
        /// </summary>
        protected virtual void AfterDownload()
        {
            // Intentionally does nothing.
        }

        /// <summary>
        /// Downloads the latest version of this package. If a version is already in the local store, it will be deleted first.
        /// </summary>
        /// <param name="displayErrorMessage">Indicates whether to display error message boxes when an error occurs.</param>
        /// <returns>A task that completes when the latest version has been downloaded.</returns>
        /// <remarks>
        /// This method will invoke, from a worker thread, <see cref="BeforeDownload"/> before doing anything, and <see cref="AfterDownload"/>
        /// if the download successfully completed without exception. In every case, it will also invoke <see cref="UpdateVersionsFromStore"/>
        /// before completing.
        /// </remarks>
        protected Task Download(bool displayErrorMessage)
        {
            BeforeDownload();

            return Task.Run(async () =>
            {
                IsProcessing = true;

                // Uninstall previous version first, if it exists
                if (LocalPackage != null)
                {
                    try
                    {
                        CurrentProcessStatus = null;
                        using (var progressReport = new ProgressReport(Store, ServerPackage))
                        {
                            progressReport.ProgressChanged += (action, progress) => { Dispatcher.InvokeAsync(() => { UpdateProgress(action, progress); }).Forget(); };
                            progressReport.UpdateProgress(ProgressAction.Delete, -1);
                            await Store.UninstallPackage(LocalPackage, progressReport);
                            CurrentProcessStatus = null;
                        }
                    }
                    catch (Exception e)
                    {
                        if (displayErrorMessage)
                        {
                            var message = $"{UninstallErrorMessage}{e.FormatSummary(true)}";
                            await ServiceProvider.Get<IDialogService>().MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Error);
                            await UpdateVersionsFromStore();
                            IsProcessing = false;
                            return;
                        }

                        IsProcessing = false;
                        throw;
                    }
                }

                // Then download and install the latest version.
                bool downloadCompleted = false;
                try
                {
                    using (var progressReport = new ProgressReport(Store, ServerPackage))
                    {
                        progressReport.ProgressChanged += (action, progress) => { Dispatcher.InvokeAsync(() => { UpdateProgress(action, progress); }).Forget(); };
                        progressReport.UpdateProgress(ProgressAction.Install, -1);
                        MetricsHelper.NotifyDownloadStarting(ServerPackage.Id, ServerPackage.Version.ToString());
                        await Store.InstallPackage(ServerPackage.Id, ServerPackage.Version, progressReport);
                        downloadCompleted = true;
                        MetricsHelper.NotifyDownloadCompleted(ServerPackage.Id, ServerPackage.Version.ToString());
                    }

                    AfterDownload();
                }
                catch (Exception e)
                {
                    if (!downloadCompleted)
                        MetricsHelper.NotifyDownloadFailed(ServerPackage.Id, ServerPackage.Version.ToString());

                    // Rollback: try to delete the broken package (i.e. if it is installed with NuGet but had a failure during Install scripts)
                    try
                    {
                        var localPackage = Store.FindLocalPackage(ServerPackage.Id, ServerPackage.Version);
                        if (localPackage != null)
                        {
                            await Store.UninstallPackage(localPackage, null);
                        }
                    }
                    catch
                    {
                        // Note: quite a bad state: rollback (uninstall) failed
                        //       we don't display the message to not confuse the user even more with an intermediate uninstall error message before the install error message
                    }

                    if (displayErrorMessage)
                    {
                        var message = $"{InstallErrorMessage}{Environment.NewLine}{Environment.NewLine}{Launcher.LastErrorOrWarning?.Replace(Environment.NewLine, Environment.NewLine + Environment.NewLine) ?? string.Empty}{Environment.NewLine}{e.FormatSummary(true)}";
                        await ServiceProvider.Get<IDialogService>().MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    throw;
                }
                finally
                {
                    await UpdateVersionsFromStore();
                    IsProcessing = false;
                }
            });
        }

        protected async Task Delete(bool displayErrorMessage, bool askConfirmation)
        {
            bool proceed = !askConfirmation;
            if (askConfirmation)
            {
                var message = string.Format(Strings.ConfirmUninstall, FullName);
                var confirmResult = await ServiceProvider.Get<IDialogService>().MessageBox(message, MessageBoxButton.YesNo);
                proceed = confirmResult == MessageBoxResult.Yes;
            }
            if (proceed)
            {
                await Task.Run(() => DeleteInternal(displayErrorMessage));
            }
        }

        private async Task DeleteInternal(bool displayErrorMessage)
        {
            IsProcessing = true;
            try
            {
                using (var progressReport = new ProgressReport(Store, ServerPackage))
                {
                    progressReport.ProgressChanged += (action, progress) => { Dispatcher.InvokeAsync(() => { UpdateProgress(action, progress); }).Forget(); };
                    progressReport.UpdateProgress(ProgressAction.Delete, -1);
                    CurrentProcessStatus = string.Format(Strings.ReportDeletingVersion, FullName);
                    await Store.UninstallPackage(LocalPackage, progressReport);
                    CurrentProcessStatus = null;
                }
            }
            catch (Exception e)
            {
                if (displayErrorMessage)
                {
                    var message = $"{UninstallErrorMessage}{e.FormatSummary(true)}";
                    await ServiceProvider.Get<IDialogService>().MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                throw;
            }
            finally
            {
                await UpdateVersionsFromStore();
                IsProcessing = false;
            }
        }

        private void UpdateStatusInternal()
        {
            CanBeDownloaded = LocalPackage == null || (LocalPackage != null && ServerPackage != null && LocalPackage.Version < ServerPackage.Version);
            CanDelete = LocalPackage != null;
            DownloadCommand.IsEnabled = CanBeDownloaded;
        }
    }
}
