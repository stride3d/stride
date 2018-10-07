// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.MostRecentlyUsedFiles;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Core.Translation;
using Xenko.Core.VisualStudio;
using Xenko.Assets.Effect;
using Xenko.Assets.Presentation.ViewModel;
using Xenko.GameStudio.Services;

namespace Xenko.GameStudio
{
    public class GameStudioViewModel : EditorViewModel
    {
        private PreviewViewModel preview;
        private DebuggingViewModel debugging;
        private string restartArguments;
        private readonly List<IDEInfo> availableIDEs;

        public GameStudioViewModel([NotNull] IViewModelServiceProvider serviceProvider, MostRecentlyUsedFileCollection mru)
            : base(serviceProvider, mru, XenkoGameStudio.EditorName, XenkoGameStudio.EditorVersionMajor)
        {
            Panels = new EditionPanelViewModel(ServiceProvider);
            availableIDEs = new List<IDEInfo> { VisualStudioVersions.DefaultIDE };
            availableIDEs.AddRange(VisualStudioVersions.AvailableVisualStudioInstances);
            NewSessionCommand = new AnonymousCommand(serviceProvider, RestartAndCreateNewSession);
            OpenAboutPageCommand = new AnonymousCommand(serviceProvider, OpenAboutPage);
            OpenSessionCommand = new AnonymousTaskCommand<UFile>(serviceProvider, RestartAndOpenSession);
            ReloadSessionCommand = new AnonymousTaskCommand(serviceProvider, () => RestartAndOpenSession(Session.SessionFilePath));
        }

        public static GameStudioViewModel GameStudio => (GameStudioViewModel)Instance;

        [NotNull]
        public EditionPanelViewModel Panels { get; }

        public XenkoAssetsViewModel XenkoAssets => XenkoAssetsViewModel.Instance;

        public PreviewViewModel Preview { get => preview; set => SetValue(ref preview, value); }

        public DebuggingViewModel Debugging { get => debugging; set => SetValue(ref debugging, value); }

        [NotNull]
        public IReadOnlyList<IDEInfo> AvailableIDEs => availableIDEs;

        [NotNull]
        public ICommandBase NewSessionCommand { get; }

        [NotNull]
        public ICommandBase OpenAboutPageCommand { get; }

        [NotNull]
        public ICommandBase OpenSessionCommand { get; }

        [NotNull]
        public ICommandBase ReloadSessionCommand { get; }

        protected internal override IEnumerable<string> TextAssetTypes
        {
            get
            {
                yield return nameof(EffectShaderAsset);
                yield return nameof(EffectCompositorAsset);
            }
        }

        protected override void RestartAndCreateNewSession()
        {
            restartArguments = "/NewProject" + GetCommonArguments();
            CloseAndRestart();
        }

        protected override async Task RestartAndOpenSession(UFile sessionPath)
        {
            if (sessionPath != null && !File.Exists(sessionPath.ToWindowsPath()))
            {
                await ServiceProvider.Get<IDialogService>().MessageBox(Tr._p("Message", "The file {0} does not exist.").ToFormat(sessionPath.ToWindowsPath()));
                return;
            }
            if (sessionPath == null)
            {
                sessionPath = await EditorDialogHelper.BrowseForExistingProject(ServiceProvider);
            }

            // Operation cancelled
            if (sessionPath == null)
                return;

            restartArguments = $"\"{sessionPath.ToWindowsPath()}\"" + GetCommonArguments();
            await CloseAndRestart();
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            Preview?.Destroy();
            base.Destroy();
        }

        /// <summary>
        /// Attempts to close the window and then restarts if closing succeeded.
        /// </summary>
        [NotNull]
        private Task CloseAndRestart()
        {
            return ServiceProvider.Get<IDialogService>().CloseMainWindow(RestartOnClosed);
        }

        private void OpenAboutPage()
        {
            ServiceProvider.Get<IXenkoDialogService>().ShowAboutPage();
        }

        private void RestartOnClosed()
        {
            try
            {
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = Assembly.GetExecutingAssembly().Location,
                        Arguments = restartArguments,
                    }
                };
                process.Start();
            }
            catch (Exception e)
            {
                e.Ignore();
            }
        }

        private static string GetCommonArguments()
        {
            var arguments = "";

            using (var debugger = VisualStudioDebugger.GetAttached())
            {
                if (debugger != null)
                {
                    arguments += $" /Reattach {debugger.ProcessId}";
                }
            }

            return arguments;
        }
    }
}
