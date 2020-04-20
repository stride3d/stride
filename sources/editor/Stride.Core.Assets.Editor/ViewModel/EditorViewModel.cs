// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.MostRecentlyUsedFiles;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public abstract class EditorViewModel : ViewModelBase
    {
        public const string PackageFileExtension = Package.PackageFileExtension;
        public const string SolutionFileExtension = ".sln";
        private SessionViewModel session;

        protected EditorViewModel(IViewModelServiceProvider serviceProvider, MostRecentlyUsedFileCollection mru, string editorName, string editorVersionMajor)
            : base(serviceProvider)
        {
            AssetsPlugin.RegisterPlugin(typeof(AssetsEditorPlugin));
            serviceProvider.Get<IEditorDialogService>();

            ClearMRUCommand = new AnonymousCommand(serviceProvider, () => ClearRecentFiles());
            OpenSettingsWindowCommand = new AnonymousCommand(serviceProvider, OpenSettingsWindow);
            OpenWebPageCommand = new AnonymousTaskCommand<string>(serviceProvider, OpenWebPage);
#if DEBUG
            DebugCommand = new AnonymousCommand(serviceProvider, DebugFunction);
#endif

            MRU = mru;
            MRU.MostRecentlyUsedFiles.CollectionChanged += MostRecentlyUsedFiles_CollectionChanged;

            serviceProvider.Get<IEditorDialogService>().RegisterDefaultTemplateProviders();

            EditorName = editorName;
            EditorVersionMajor = editorVersionMajor;
            UpdateRecentFiles();
            if (Instance != null)
                throw new InvalidOperationException("The EditorViewModel class can be instanced only once.");

            Status = new StatusViewModel(ServiceProvider);
            Status.PushStatus("Ready");

            Instance = this;
        }

        private void MostRecentlyUsedFiles_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateRecentFiles();
        }

        /// <summary>
        /// Gets the current instance of <see cref="EditorViewModel"/>.
        /// </summary>
        public static EditorViewModel Instance { get; private set; }

        /// <summary>
        /// Gets the name of this editor.
        /// </summary>
        public string EditorName { get; set; }

        /// <summary>
        /// Gets the version major of this editor.
        /// </summary>
        public string EditorVersionMajor { get; set; }

        /// <summary>
        /// Gets the current active session.
        /// </summary>
        public SessionViewModel Session { get { return session; } private set { SetValue(ref session, value); } }

        public StatusViewModel Status { get; }

        public MostRecentlyUsedFileCollection MRU { get; }

        public ObservableList<MostRecentlyUsedFile> RecentFiles { get; } = new ObservableList<MostRecentlyUsedFile>();

        public ICommandBase ClearMRUCommand { get; }

        public ICommandBase OpenSettingsWindowCommand { get; }

        public ICommandBase OpenWebPageCommand { get; }

#if DEBUG
        public ICommandBase DebugCommand { get; }
#endif

        // Temporary, until we integrate a proper text editor in the studio
        protected internal virtual IEnumerable<string> TextAssetTypes => Enumerable.Empty<string>();

#if DEBUG
        public void DebugFunction()
        {
            Console.WriteLine(@"DebugFunction invoked");
        }
#endif

        public async Task<bool> NewSession(NewSessionParameters newSessionParameters)
        {
            if (Session != null)
                throw new InvalidOperationException("A session is already open in this instance.");

            var newSession = await SessionViewModel.CreateNewSession(this, ServiceProvider, newSessionParameters);
            if (newSession != null)
            {
                Session = newSession;
                MRU.AddFile(Session.SolutionPath, EditorVersionMajor);
            }

            return Session != null;
        }

        public async Task<bool?> OpenInitialSession(string initialSessionPath)
        {
            if (string.IsNullOrWhiteSpace(initialSessionPath))
                return false;

            // The LoadingStartupSession settings is true, which means that the editor crashed while loading the startup project last time.
            // Lets give the user a chance to fix the startup session.
            if (InternalSettings.LoadingStartupSession.GetValue())
            {
                var buttons = DialogHelper.CreateButtons(new[]
                {
                    Tr._p("Button", "Try again"),
                    Tr._p("Button", "Cancel")
                }, 1, 2);
                string message = string.Format(Tr._p("Message", "The last attempt to load the project **{0}** failed. \r\n\r\nDo you want to try to load it again?"), Path.GetFileName(initialSessionPath));
                var result = await ServiceProvider.Get<IDialogService>().MessageBox(message, buttons, MessageBoxImage.Warning);
                if (result != 1)
                    return false;
            }

            // Safe-guard - this will warn that the startup session makes the editor crash while loading.
            InternalSettings.LoadingStartupSession.SetValue(true);
            InternalSettings.Save();

            var sessionResult = await OpenSession(initialSessionPath);

            InternalSettings.LoadingStartupSession.SetValue(false);
            InternalSettings.Save();

            return sessionResult;
        }

        /// <summary>
        /// Attempts to load the session corresponding to the given path.
        /// </summary>
        /// <param name="filePath">The path to a solution or package file.</param>
        /// <returns><c>True</c> if the session was successfully loaded, <c>False</c> if an error occurred, <c>Null</c> if the operation was cancelled by user.</returns>
        public async Task<bool?> OpenSession(UFile filePath)
        {
            if (Session != null)
                throw new InvalidOperationException("A session is already open in this instance.");

            if (filePath != null && !File.Exists(filePath))
            {
                RemoveRecentFile(filePath);

                await ServiceProvider.Get<IDialogService>().MessageBox(string.Format(Tr._p("Message", @"The file '{0}' does not exist."), filePath), MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var sessionResult = new PackageSessionResult();
            var loadedSession = await SessionViewModel.OpenSession(filePath, ServiceProvider, this, sessionResult);

            // Loading has failed
            if (loadedSession == null)
            {
                // Null means the user has cancelled the loading operation.
                return sessionResult.OperationCancelled ? (bool?)null : false;
            }

            RemoveRecentFile(filePath);
            MRU.AddFile(filePath, EditorVersionMajor);
            Session = loadedSession;

            InternalSettings.FileDialogLastOpenSessionDirectory.SetValue(new UFile(filePath).GetFullDirectory());
            InternalSettings.Save();
            return true;
        }

        public async Task OpenFile(string filePath, bool tryEdit)
        {
            try
            {
                filePath = filePath.Replace('/', '\\');
                if (!File.Exists(filePath))
                {
                    await ServiceProvider.Get<IDialogService>().MessageBox(Tr._p("Message", "You need to save the file before you can open it."), MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var process = new Process { StartInfo = new ProcessStartInfo(filePath) { UseShellExecute = true } };
                if (tryEdit)
                {
                    process.StartInfo.Verb = process.StartInfo.Verbs.FirstOrDefault(x => x.ToLowerInvariant() == "edit");
                }
                process.Start();
            }
            catch (Exception ex)
            {
                var message = $"{Tr._p("Message", "An error occurred while opening the file.")}{ex.FormatSummary(true)}";
                await ServiceProvider.Get<IDialogService>().MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected abstract void RestartAndCreateNewSession();

        protected abstract Task RestartAndOpenSession(UFile sessionPath);

        private async Task OpenWebPage(string url)
        {
            try
            {
                var process = new Process { StartInfo = new ProcessStartInfo(url) { UseShellExecute = true } };
                process.Start();
            }
            catch (Exception ex)
            {
                var message = $"{Tr._p("Message", "An error occurred while opening the file.")}{ex.FormatSummary(true)}";
                await ServiceProvider.Get<IDialogService>().MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RemoveRecentFile(UFile filePath)
        {
            // Get all versions of showing on recent files
            var strideVersions = RecentFiles?.Select(x => x.Version).Distinct().ToList();
            if (strideVersions != null)
            {
                foreach (var item in strideVersions)
                {
                    MRU.RemoveFile(filePath, item);
                }
            }
        }

        private void ClearRecentFiles()
        {
            // Clear considering old projects that have been deleted or upgraded from older versions
            var strideVersions = RecentFiles?.Select(x => x.Version).ToList();
            if (strideVersions != null)
            {
                foreach (var item in strideVersions)
                {
                    MRU.Clear(item);
                }
            }
        }

        private void UpdateRecentFiles()
        {
            RecentFiles.Clear();

            // Get only files that is current version or older
            foreach (var item in MRU.MostRecentlyUsedFiles.Where(x => string.Compare(x.Version, EditorVersionMajor, StringComparison.Ordinal) <= 0).Take(10))
            {
                RecentFiles.Add(new MostRecentlyUsedFile() { FilePath = item.FilePath, Timestamp = item.Timestamp, Version = item.Version });
            }
        }

        private void OpenSettingsWindow()
        {
            ServiceProvider.Get<IEditorDialogService>().ShowSettingsWindow(ServiceProvider);
        }
    }
}
