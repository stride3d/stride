// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.View.DebugTools;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Serialization;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Presentation.Interop;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;
using AvalonDock.Layout;
#if DEBUG
using Stride.Assets.Presentation.Test;
#endif

namespace Stride.GameStudio
{
    /// <summary>
    /// Interaction logic for GameStudioWindow.xaml
    /// </summary>
    public partial class GameStudioWindow : IAsyncClosableWindow
    {
        private DebugWindow debugWindow;
        private bool forceClose;
        private readonly DockingLayoutManager dockingLayout;
        private readonly AssetEditorsManager assetEditorsManager;
        private TaskCompletionSource<bool> closingTask;

#if DEBUG
        private const bool TestMenuVisible = true;
#else
        private const bool TestMenuVisible = false;
#endif

        public GameStudioWindow(EditorViewModel editor)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (editor.Session == null) throw new ArgumentException($@"A valid session must exist before creating a {nameof(GameStudioWindow)}", nameof(editor));
            DataContext = editor; // Must be set before calling InitializeComponent

            dockingLayout = new DockingLayoutManager(this, editor.Session);
            assetEditorsManager = new AssetEditorsManager(dockingLayout, editor.Session);
            editor.ServiceProvider.Get<IEditorDialogService>().AssetEditorsManager = assetEditorsManager;

            OpenDebugWindowCommand = new AnonymousCommand(editor.ServiceProvider, OpenDebugWindow);
            CreateTestAssetCommand = new AnonymousCommand(editor.ServiceProvider, CreateTestAsset);
            CreateUnitTestAssetCommand = new AnonymousCommand(editor.ServiceProvider, CreateUnitTestAsset);
            BreakDebuggerCommand = new AnonymousCommand(editor.ServiceProvider, BreakDebugger);
            EditorSettings.ResetEditorLayout.Command = new AnonymousTaskCommand(editor.ServiceProvider, ResetAllLayouts);

            InitializeComponent();
            Application.Current.Activated += (s, e) => editor.ServiceProvider.Get<IEditorDialogService>().ShowDelayedNotifications();
            Loaded += GameStudioLoaded;

            OpenMetricsProjectSession(editor);
        }

        private async Task ResetAllLayouts()
        {
            var assets = assetEditorsManager.OpenedAssets.Select(x => x.Id).ToList();
            if (assets.Count > 0)
            {
                var message = Tr._p("Message", "To reset the layout, Game Studio needs to close and re-open all asset and document editors. You won't lose unsaved changes.");
                var buttons = DialogHelper.CreateButtons(new[] { "Reset layout", "Cancel" }, 1, 2);
                var result = await Editor.ServiceProvider.Get<IEditorDialogService>().MessageBox(message, buttons);
                if (result != 1)
                    return;
            }

            // Close all editors
            assetEditorsManager.CloseAllEditorWindows(null);

            // Check if user cancelled closing some of the editors.
            if (assetEditorsManager.OpenedAssets.Any())
                return;

            // Safely reset layout
            dockingLayout.ResetAllLayouts();

            // Reopen editors
            await ReopenAssetEditors(assets);
        }

        private static void OpenMetricsProjectSession(EditorViewModel editor)
        {
            var projectUid = editor.Session.CurrentProject?.Project.Id ?? Guid.Empty;

            var execProfiles = editor.Session.LocalPackages.OfType<ProjectViewModel>().Where(x => x.Type == ProjectType.Executable);
            var sessionPlatforms = new HashSet<PlatformType>();
            foreach (var execProfile in execProfiles)
            {
                if (execProfile.Platform != PlatformType.Shared)
                {
                    sessionPlatforms.Add(execProfile.Platform);
                }
            }
            if (sessionPlatforms.Count > 0)
            {
                var metricData = new StringBuilder();
                foreach (var sessionPlatform in sessionPlatforms)
                {
                    metricData.Append($"#platform:{sessionPlatform}|");
                }
                metricData.Remove(metricData.Length - 1, 1);

                StrideGameStudio.MetricsClient?.OpenProjectSession($"#projectUid:{projectUid}|{metricData}");
            }
            else
            {
                StrideGameStudio.MetricsClient?.OpenProjectSession($"#projectUid:{projectUid}|#platform:None");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            CloseMetricsProjectSession();
        }

        private static void CloseMetricsProjectSession()
        {
            StrideGameStudio.MetricsClient?.CloseProjectSession();
        }

        public EditorViewModel Editor => (EditorViewModel)DataContext;

        public string EditorTitle => Editor.Session.SolutionPath != null ? $"{Editor.Session.SolutionPath.GetFileName()} - {StrideGameStudio.EditorName}" : StrideGameStudio.EditorName;

        public ICommandBase OpenDebugWindowCommand { get; }

        public ICommandBase CreateTestAssetCommand { get; }

        public ICommandBase CreateUnitTestAssetCommand { get; }

        public ICommandBase BreakDebuggerCommand { get; }

        public bool IsTestMenuVisible => TestMenuVisible;

        /// <inheritdoc />
        public Task<bool> TryClose()
        {
            Editor.Session.Dispatcher.EnsureAccess();

            if (closingTask == null)
            {
                closingTask = new TaskCompletionSource<bool>();
                Close();
            }
            return closingTask.Task;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (forceClose)
            {
                // Stop listening to clipboard
                ClipboardMonitor.UnregisterListener(this);
                return;
            }
            // We need to run async stuff before closing, so let's always cancel the close at first.
            e.Cancel = true;
            // This method will shutdown the application if the session has been successfully closed.
            SaveAndClose().Forget();
        }

        internal void RegisterAssetPreview(LayoutAnchorable assetPreviewAnchorable)
        {
            // We listen to the events here instead of via xaml because DockingLayoutManager essentially breaks
            // the entire docking control and OnEventCommandBehavior.CommandParameter binding would not
            // get restored properly, due to not being able to rebind to a control.
            assetPreviewAnchorable.IsSelectedChanged += OnAssetPreviewAnchorable_IsSelectedChanged;
            UpdateAssetPreviewAnchorable(assetPreviewAnchorable);
        }

        internal void UnregisterAssetPreview(LayoutAnchorable assetPreviewAnchorable)
        {
            assetPreviewAnchorable.IsSelectedChanged -= OnAssetPreviewAnchorable_IsSelectedChanged;
        }

        private void OnAssetPreviewAnchorable_IsSelectedChanged(object sender, EventArgs e)
        {
            if (sender is LayoutAnchorable anchorable)
            {
                UpdateAssetPreviewAnchorable(anchorable);
            }
        }

        private void UpdateAssetPreviewAnchorable(LayoutAnchorable anchorable)
        {
            (Editor as GameStudioViewModel)?.Preview?.RenderPreviewCommand?.Execute(anchorable.IsSelected);
        }

        private void InitializeWindowSize()
        {
            var previousWorkAreaWidth = GameStudioInternalSettings.WorkAreaWidth.GetValue();
            var previousWorkAreaHeight = GameStudioInternalSettings.WorkAreaHeight.GetValue();
            var wasWindowMaximized = GameStudioInternalSettings.WindowMaximized.GetValue();
            var workArea = this.GetWorkArea();

            AdjustMaxSizeWithTaskbar();
            
            if (wasWindowMaximized || previousWorkAreaWidth > (int)workArea.Width || previousWorkAreaHeight > (int)workArea.Height)
            {
                // Resolution has changed (and is now smaller), let's make the window fill all available space.
                this.FillArea(workArea);
                WindowState = WindowState.Maximized;
            }
            else
            {
                // Load state
                var previousWindowWidth = GameStudioInternalSettings.WindowWidth.GetValue();
                var previousWindowHeight = GameStudioInternalSettings.WindowHeight.GetValue();
                // Set window size
                Width = Math.Min(previousWindowWidth, workArea.Width);
                Height = Math.Min(previousWindowHeight, workArea.Height);
                // Window is centered by default
                this.CenterToArea(workArea);
                WindowState = WindowState.Normal;
            }
        }

        private void GameStudioLoaded(object sender, RoutedEventArgs e)
        {
            if (!Editor.Session.IsEditorInitialized)
            {
                // Size the window to best fit the current screen size
                InitializeWindowSize();
                // Load the docking layout
                dockingLayout.ReloadCurrentLayout();
                // Restore visible/hidden status of panes
                GameStudioViewModel.GameStudio.Panels.LoadFromSettings();
                // Initialize plugins
                Editor.Session.ServiceProvider.Get<IAssetsPluginService>().Plugins.ForEach(x => x.InitializeSession(Editor.Session));
                // Open assets that were being edited in the previous session
                ReopenAssetEditors(dockingLayout.LoadOpenAssets().ToList()).Forget();

                // Listen to clipboard
                ClipboardMonitor.RegisterListener(this);
                // Notify start
                Program.NotifyGameStudioStarted();

                Editor.Session.PluginsInitialized();

                foreach (var window in Application.Current.Windows.Cast<Window>().Where(x => !Equals(x, this)))
                {
                    var childHwnd = new WindowInteropHelper(window).Handle;
                    var parentHwnd = new WindowInteropHelper(this).Handle;
                    var handleRef = new HandleRef(window, childHwnd);
                    NativeHelper.SetWindowLong(handleRef, NativeHelper.WindowLongType.HwndParent, parentHwnd);
                }
            }
        }

        private async Task SaveAndClose()
        {
            try
            {
                // Save MRUs
                if (Editor.Session != null)
                {
                    var openedAssets = assetEditorsManager.OpenedAssets.ToList();
                    if (!await Editor.Session.Close())
                    {
                        closingTask?.SetResult(false);
                        return;
                    }

                    // Make sure the curve editor is closed
                    assetEditorsManager.CloseCurveEditorWindow();

                    // Close all windows (except if the user interrupt the flow)
                    // Since all dirty assets must have been saved before, we don't need to ask for any user confirmation
                    assetEditorsManager.CloseAllEditorWindows(false);

                    Editor.Session.Destroy();

                    // Save opened assets
                    dockingLayout.SaveOpenAssets(openedAssets);
                    // Save layout
                    dockingLayout.SaveCurrentLayout();
                }

                var workArea = this.GetWorkArea();
                // Save state
                GameStudioInternalSettings.WorkAreaWidth.SetValue((int)workArea.Width);
                GameStudioInternalSettings.WorkAreaHeight.SetValue((int)workArea.Height);
                GameStudioInternalSettings.WindowWidth.SetValue((int)Math.Max(320, (WindowState == WindowState.Maximized ? RestoreBounds.Width : ActualWidth)));
                GameStudioInternalSettings.WindowHeight.SetValue((int)Math.Max(240, (WindowState == WindowState.Maximized ? RestoreBounds.Height : ActualHeight)));
                GameStudioInternalSettings.WindowMaximized.SetValue(WindowState == WindowState.Maximized);

                // Write the settings file
                InternalSettings.Save();

                forceClose = true;
                var studio = Editor as GameStudioViewModel;
                studio?.Destroy();

                closingTask?.SetResult(true);
                // Shutdown after all other operations have completed
                await Application.Current.Dispatcher.InvokeAsync(Application.Current.Shutdown, DispatcherPriority.ContextIdle);
            }
            finally
            {
                closingTask = null;
            }
        }

        /// <summary>
        /// Opens assets that were being edited in the previous session.
        /// </summary>
        /// <param name="assetIds">The list of asset ids for which to reopen the editor.</param>
        private async Task ReopenAssetEditors(IReadOnlyCollection<AssetId> assetIds)
        {
            if (assetIds.Count == 0)
            {
                // If no data, try to open the default scene
                OpenDefaultScene(Editor.Session);
                return;
            }
            // Open assets
            var assets = assetIds.Select(x => Editor.Session.GetAssetById(x)).NotNull().ToList();
            Editor.Session.ActiveAssetView.SelectAssets(assets.Last().Yield());
            foreach (var asset in assets)
            {
                // HACK: temporary open and await asset editor sequentially
                await assetEditorsManager.OpenAssetEditorWindow(asset, false);
            }
            // Close remaining hidden windows (i.e. windows that failed to load, or which asset is not available anymore)
            assetEditorsManager.CloseAllHiddenWindows();
            // Save list of opened asset editors
            dockingLayout.SaveOpenAssets(assetEditorsManager.OpenedAssets);
        }

        private async void OpenDefaultScene(SessionViewModel session)
        {
            var startupPackage = session.LocalPackages.OfType<ProjectViewModel>().SingleOrDefault(x => x.IsCurrentProject);
            if (startupPackage == null)
                return;

            var gameSettingsAsset = startupPackage.Assets.FirstOrDefault(x => x.Url == Assets.GameSettingsAsset.GameSettingsLocation);
            if (gameSettingsAsset == null)
            {
                // Scan dependencies for game settings
                // TODO: Scanning order? (direct dependencies first)
                // TODO: Switch to using startupPackage.Dependencies view model instead
                foreach (var dependency in startupPackage.PackageContainer.FlattenedDependencies)
                {
                    if (dependency.Package == null)
                        continue;

                    var dependencyPackageViewModel = session.AllPackages.First(x => x.Package == dependency.Package);
                    if (dependencyPackageViewModel == null)
                        continue;

                    gameSettingsAsset = dependencyPackageViewModel.Assets.FirstOrDefault(x => x.Url == Assets.GameSettingsAsset.GameSettingsLocation);
                    if (gameSettingsAsset != null)
                        break;
                }
            }

            if (gameSettingsAsset == null)
                return;

            var defaultScene = ((Assets.GameSettingsAsset)gameSettingsAsset?.Asset)?.DefaultScene;
            if (defaultScene == null)
                return;

            var defaultSceneReference = AttachedReferenceManager.GetAttachedReference(defaultScene);
            if (defaultSceneReference == null)
                return;

            var asset = session.GetAssetById(defaultSceneReference.Id);
            if (asset == null)
                return;

            Editor.Session.ActiveAssetView.SelectAssets(asset.Yield());

            await assetEditorsManager.OpenAssetEditorWindow(asset);
        }

        private void OpenDebugWindow()
        {
            if (debugWindow == null)
            {
                debugWindow = new DebugWindow();
                debugWindow.Show();
                debugWindow.Closed += (s, e) => debugWindow = null;
            }
        }

        private void CreateTestAsset()
        {
#if DEBUG
            var package = Editor.Session.CurrentProject;
            if (package != null)
            {
                using (var transaction = Editor.Session.UndoRedoService.CreateTransaction())
                {
                    var dir = package.AssetMountPoint;
                    var name = NamingHelper.ComputeNewName("TestAsset", x => dir.Assets.Any(y => string.Equals(x, y.Name, StringComparison.OrdinalIgnoreCase)));
                    var asset = TestAsset.CreateNew();
                    var assetItem = new AssetItem(name, asset);
                    var assetViewModel = package.CreateAsset(dir, assetItem, true, null);
                    Editor.Session.NotifyAssetPropertiesChanged(new[] { assetViewModel });
                    Editor.Session.ActiveAssetView.SelectAssets(new[] { assetViewModel });
                    Editor.Session.UndoRedoService.SetName(transaction, $"Create test asset '{name}'");
                }
            }
#endif
        }

        private void CreateUnitTestAsset()
        {
#if DEBUG
            var package = Editor.Session.CurrentProject;
            if (package != null)
            {
                using (var transaction = Editor.Session.UndoRedoService.CreateTransaction())
                {
                    var dir = package.AssetMountPoint;
                    var name = NamingHelper.ComputeNewName("UnitTestAsset", x => dir.Assets.Any(y => string.Equals(x, y.Name, StringComparison.OrdinalIgnoreCase)));
                    var asset = UnitTestAsset.CreateNew();
                    var assetItem = new AssetItem(name, asset);
                    var assetViewModel = package.CreateAsset(dir, assetItem, true, null);
                    Editor.Session.NotifyAssetPropertiesChanged(new[] { assetViewModel });
                    Editor.Session.ActiveAssetView.SelectAssets(new[] { assetViewModel });
                    Editor.Session.UndoRedoService.SetName(transaction, $"Create test asset '{name}'");
                }
            }
#endif
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void BreakDebugger()
        {
            // You can access SessionViewModel.Instance from here to debug
            System.Diagnostics.Debugger.Break();
        }

        private void EditorWindowPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ((EditorViewModel)DataContext).Status.DiscardStatus();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            // To handle window changing screen
            AdjustMaxSizeWithTaskbar();
        }

        void AdjustMaxSizeWithTaskbar()
        {
            // There's an issue were auto-hide taskbars cannot be focused while WPF windows are maximized
            // decreasing, even slightly, the maximum size fixes that issue
            var v = this.GetWorkArea();
            MaxWidth = v.Width;
            // Yes, works even when the taskbar is on the left and right of the screen, somehow
            MaxHeight = v.Height - 0.1d;
        }
    }
}
