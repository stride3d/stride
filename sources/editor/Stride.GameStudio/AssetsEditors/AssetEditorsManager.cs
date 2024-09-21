// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Animations;
using Stride.Assets.Presentation.CurveEditor.ViewModels;
using Stride.Assets.Presentation.CurveEditor.Views;
using AvalonDock.Layout;
using Stride.GameStudio.Helpers;
using Stride.GameStudio.Layout;

namespace Stride.GameStudio.AssetsEditors
{
    internal sealed class AssetEditorsManager : IAssetEditorsManager, IDestroyable
    {
        private readonly ConditionalWeakTable<IMultipleAssetEditorViewModel, NotifyCollectionChangedEventHandler> registeredHandlers = [];
        private readonly Dictionary<IAssetEditorViewModel, LayoutAnchorable> assetEditors = [];
        private readonly Dictionary<AssetViewModel, IAssetEditorViewModel> openedAssets = [];
        // TODO have a base interface for all editors and factorize to make curve editor not be a special case anymore
        private Tuple<CurveEditorViewModel, LayoutAnchorable> curveEditor;

        private readonly AsyncLock mutex = new();
        private readonly DockingLayoutManager dockingLayoutManager;
        private readonly SessionViewModel session;

        public AssetEditorsManager([NotNull] DockingLayoutManager dockingLayoutManager, [NotNull] SessionViewModel session)
        {
            this.dockingLayoutManager = dockingLayoutManager ?? throw new ArgumentNullException(nameof(dockingLayoutManager));
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            session.DeletedAssetsChanged += AssetsDeleted;
        }

        /// <summary>
        /// Gets the list of assets that are currently opened in an editor.
        /// </summary>
        /// <remarks>
        /// This does not include all assets in <see cref="IMultipleAssetEditorViewModel"/> but rather those that were explicitly opened.
        /// </remarks>
        public IReadOnlyCollection<AssetViewModel> OpenedAssets => openedAssets.Keys;

        /// <inheritdoc />
        void IDestroyable.Destroy()
        {
            session.DeletedAssetsChanged -= AssetsDeleted;
        }

        /// <inheritdoc/>
        public void OpenCurveEditorWindow([NotNull] object curve, string name)
        {
            if (curve == null) throw new ArgumentNullException(nameof(curve));
            if (dockingLayoutManager == null) throw new InvalidOperationException("This method can only be invoked on the IEditorDialogService that has the editor main window as parent.");

            CurveEditorViewModel editorViewModel = null;
            LayoutAnchorable editorPane = null;

            if (curveEditor != null)
            {
                // curve editor already exists
                editorViewModel = curveEditor.Item1;
                editorPane = curveEditor.Item2;
            }

            // Create the editor view model if needed
            editorViewModel ??= new CurveEditorViewModel(session.ServiceProvider, session);

            // Populate the editor view model
            if (curve is IComputeCurve<Color4> color4curve)
            {
                editorViewModel.AddCurve(color4curve, name);
            }
            else if (curve is IComputeCurve<float> floatCurve)
            {
                editorViewModel.AddCurve(floatCurve, name);
            }
            else if (curve is IComputeCurve<Quaternion> quaternionCurve)
            {
                editorViewModel.AddCurve(quaternionCurve, name);
            }
            else if (curve is IComputeCurve<Vector2> vec2curve)
            {
                editorViewModel.AddCurve(vec2curve, name);
            }
            else if (curve is IComputeCurve<Vector3> vec3curve)
            {
                editorViewModel.AddCurve(vec3curve, name);
            }
            else if (curve is IComputeCurve<Vector4> vec4curve)
            {
                editorViewModel.AddCurve(vec4curve, name);
            }

            editorViewModel.Focus();

            // Create the editor pane if needed
            if (editorPane == null)
            {
                editorPane = new LayoutAnchorable
                {
                    Content = new CurveEditorView { DataContext = editorViewModel },
                    Title = "Curve Editor",
                    CanClose = true,
                };

                editorPane.Closed += CurveEditorClosed;

                AvalonDockHelper.GetDocumentPane(dockingLayoutManager.DockingManager).Children.Add(editorPane);
            }

            curveEditor = Tuple.Create(editorViewModel, editorPane);

            MakeActiveVisible(editorPane);
        }

        /// <inheritdoc/>
        public void CloseCurveEditorWindow()
        {
            RemoveCurveEditor(true);
        }

        private void RemoveCurveEditor(bool removePane)
        {
            if (curveEditor == null)
                return;

            var editor = curveEditor.Item1;
            var pane = curveEditor.Item2;
            curveEditor = null;
            // clean view model
            editor.Destroy();

            CleanEditorPane(pane);
            if (removePane)
            {
                RemoveEditorPane(pane);
            }
        }

        private void CurveEditorClosed(object sender, EventArgs eventArgs)
        {
            RemoveCurveEditor(true);
        }

        /// <inheritdoc/>
        [NotNull]
        public Task OpenAssetEditorWindow([NotNull] AssetViewModel asset)
        {
            return OpenAssetEditorWindow(asset, true);
        }

        /// <inheritdoc/>
        public bool CloseAllEditorWindows(bool? save)
        {
            // Attempt to close all opened assets
            if (!openedAssets.ToList().All(kv => CloseAssetEditorWindow(kv.Key, save)))
                return false;

            // Then check that they are no remaining editor
            if (assetEditors.Count > 0)
            {
                // Nicolas: this case should not happen. Please let me know if it happens to you.
                // Note: this likely means that some editors leaked (esp. in the case of multi-asset editors), but force removing should be enough.
                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Break();

                assetEditors.Keys.ToList().ForEach(RemoveEditor);
            }

            CloseCurveEditorWindow();

            return true;
        }

        /// <inheritdoc/>
        public void CloseAllHiddenWindows()
        {
            foreach (var pane in AvalonDockHelper.GetAllAnchorables(dockingLayoutManager.DockingManager).Where(p => string.IsNullOrEmpty(p.ContentId) && p.IsHidden).ToList())
            {
                CleanEditorPane(pane);
                RemoveEditorPane(pane);
            }
        }

        /// <inheritdoc/>
        public bool CloseAssetEditorWindow([NotNull] AssetViewModel asset, bool? save)
        {
            var canClose = !openedAssets.TryGetValue(asset, out var editor) || editor.PreviewClose(save);
            if (canClose)
                CloseEditorWindow(asset);

            return canClose;
        }

        /// <inheritdoc/>
        public void HideAllAssetEditorWindows()
        {
            foreach (var editorPane in assetEditors.Values)
            {
                editorPane.Hide();
            }
        }

        /// <inheritdoc/>
        public bool TryGetAssetEditor<TEditor>([NotNull] AssetViewModel asset, out TEditor assetEditor)
             where TEditor : IAssetEditorViewModel
        {
            if (openedAssets.TryGetValue(asset, out var found) && found is TEditor editor)
            {
                assetEditor = editor;
                return true;
            }

            assetEditor = default;
            return false;
        }

        /// <summary>
        /// Retrieves the list of all assets that are currently opened in an editor.
        /// </summary>
        /// <returns>A list of all assets currently opened.</returns>
        /// <remarks>
        /// This includes all assets in <see cref="IMultipleAssetEditorViewModel"/> even those that were not explicitly opened.
        /// </remarks>
        /// <seealso cref="OpenedAssets"/>
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IReadOnlyCollection<AssetViewModel> GetCurrentlyOpenedAssets()
        {
            var hashSet = new HashSet<AssetViewModel>(openedAssets.Keys);
            assetEditors.Keys.OfType<IMultipleAssetEditorViewModel>().ForEach(x => hashSet.AddRange(x.OpenedAssets));
            return hashSet;
        }

        /// <summary>
        /// Opens (and activates) an editor window for the given asset. If an editor window for this asset already exists, simply activates it.
        /// </summary>
        /// <param name="asset">The asset for which to show an editor window.</param>
        /// <param name="saveSettings">True if <see cref="MRUAdditionalData.OpenedAssets"/> should be updated, false otherwise. Note that if the editor fail to load it will not be updated.</param>
        /// <returns></returns>
        internal async Task OpenAssetEditorWindow([NotNull] AssetViewModel asset, bool saveSettings)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            if (dockingLayoutManager == null) throw new InvalidOperationException("This method can only be invoked on the IEditorDialogService that has the editor main window as parent.");

            // Switch to the editor layout before adding any Pane
            if (assetEditors.Count == 0)
            {
                dockingLayoutManager.SwitchToEditorLayout();
            }

            using (await mutex.LockAsync())
            {
                LayoutAnchorable editorPane = null;
                IEditorView view;
                // Asset already has an editor? Then, Look for the corresponding panel
                if (openedAssets.TryGetValue(asset, out var editor) && !assetEditors.TryGetValue(editor, out editorPane))
                {
                    // Inconsistency, clean leaking editor
                    RemoveAssetEditor(asset);
                    // Try to find if another editor currently has this asset
                    var otherEditor = assetEditors.Keys.OfType<IMultipleAssetEditorViewModel>().FirstOrDefault(x => x.OpenedAssets.Contains(asset));
                    if (otherEditor != null)
                    {
                        editorPane = assetEditors[otherEditor];
                    }
                }
                // Existing editor?
                if (editorPane != null)
                {
                    // Make the pane visible immediately
                    MakeActiveVisible(editorPane);
                    view = editorPane.Content as IEditorView;
                    if (view?.EditorInitialization != null)
                    {
                        // Wait for the end of the initialization
                        await view.EditorInitialization;
                    }
                    return;
                }

                var editorType = session.PluginService.GetEditorViewModelType(asset.GetType());
                if (editorType is not null)
                {
                    var viewType = session.PluginService.GetEditorViewType(editorType);
                    if (viewType is not null)
                    {
                        view = (IEditorView)Activator.CreateInstance(viewType);

                        // Pane may already exists (e.g. created from layout saving)
                        editorPane = AvalonDockHelper.GetAllAnchorables(dockingLayoutManager.DockingManager).FirstOrDefault(p => p.Title == asset.Url);
                        if (editorPane == null)
                        {
                            editorPane = new LayoutAnchorable { CanClose = true };
                            // Stack the asset in the dictionary of editor to prevent double-opening while double-clicking twice on the asset, since the initialization is async
                            AvalonDockHelper.GetDocumentPane(dockingLayoutManager.DockingManager).Children.Add(editorPane);
                        }
                        editorPane.IsActiveChanged += EditorPaneIsActiveChanged;
                        editorPane.IsSelectedChanged += EditorPaneIsSelectedChanged;
                        editorPane.Closing += EditorPaneClosing;
                        editorPane.Closed += EditorPaneClosed;
                        editorPane.Content = view;
                        // Make the pane visible immediately
                        MakeActiveVisible(editorPane);

                        // Create a binding for the title
                        var binding = new Binding(nameof(AssetViewModel.Url)) { Mode = BindingMode.OneWay, Source = asset };
                        BindingOperations.SetBinding(editorPane, LayoutContent.TitleProperty, binding);

                        editor = (AssetEditorViewModel)Activator.CreateInstance(editorType, asset);
                        // Initialize the editor view
                        view.DataContext = editor;
                        if (!await view.InitializeEditor(editor))
                        {
                            // Could not initialize editor
                            CleanEditorPane(editorPane);
                            RemoveEditorPane(editorPane);
                        }
                        else
                        {
                            assetEditors[editor] = editorPane;
                            if (editor is IMultipleAssetEditorViewModel multiEditor)
                            {
                                foreach (var item in multiEditor.OpenedAssets)
                                {
                                    // FIXME: do we still have this case after decoupling asset and editor?
                                    if (openedAssets.TryGetValue(asset, out var otherEditor))
                                    {
                                        // Note: this could happen in some case after undo/redo that involves parenting of scenes
                                        RemoveAssetEditor(item);
                                    }
                                }
                                NotifyCollectionChangedEventHandler handler = (_, e) => MultiEditorOpenAssetsChanged(multiEditor, e);
                                registeredHandlers.Add(multiEditor, handler);
                                multiEditor.OpenedAssets.CollectionChanged += handler;
                            }
                            openedAssets.Add(asset, editor);
                        }
                    }
                }
            }

            // If the opening of the editor failed, go back to normal layout
            if (assetEditors.Count == 0)
            {
                dockingLayoutManager.SwitchToNormalLayout();
                return;
            }

            if (saveSettings)
            {
                dockingLayoutManager.SaveOpenAssets(OpenedAssets);
            }
        }

        private void CloseEditorWindow([NotNull] AssetViewModel asset)
        {
            // make asset view active
            asset.Session.ActiveProperties = asset.Session.AssetViewProperties;
            // remove editor
            RemoveAssetEditor(asset);
            // if no more editor open, change layout
            if (assetEditors.Count == 0)
            {
                dockingLayoutManager.SwitchToNormalLayout();
            }
        }

        private void MultiEditorOpenAssetsChanged([NotNull] IMultipleAssetEditorViewModel multiEditor, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems?.Count > 0)
                    {
                        // nothing to do?
                        //foreach (AssetViewModel item in e.OldItems)
                        //{
                        //    item.Editor = null;
                        //}
                    }
                    if (e.NewItems?.Count > 0)
                    {
                        foreach (AssetViewModel item in e.NewItems)
                        {
                            if (openedAssets.TryGetValue(item, out var editor) && assetEditors.ContainsKey(editor))
                            {
                                RemoveAssetEditor(item);
                            }
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    // nothing to do
                    break;
                case NotifyCollectionChangedAction.Reset:
                    throw new InvalidOperationException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Removes the editor for the given <paramref name="asset"/>.
        /// </summary>
        /// <param name="asset">The asset.</param>
        private void RemoveAssetEditor([NotNull] AssetViewModel asset)
        {
            if (openedAssets.Remove(asset, out var editor))
            {
                RemoveEditor(editor);
            }
        }

        private void RemoveEditor([NotNull] IAssetEditorViewModel editor)
        {
            assetEditors.TryGetValue(editor, out var editorPane);

            if (editor is IMultipleAssetEditorViewModel multiEditor)
            {
                if (registeredHandlers.TryGetValue(multiEditor, out var handler))
                {
                    multiEditor.OpenedAssets.CollectionChanged -= handler;
                    registeredHandlers.Remove(multiEditor);
                }
                else
                {
                    throw new InvalidOperationException($"Expected {multiEditor} to have a handler set up");
                }
            }
            // Remove editor
            assetEditors.Remove(editor);
            // Attempt to destroy the editor
            try
            {
                editor.Destroy();
            }
            catch (ObjectDisposedException)
            {
            }
            // Clean and remove editor pane
            if (editorPane != null)
            {
                CleanEditorPane(editorPane);
                RemoveEditorPane(editorPane);
            }
        }

        private void AssetsDeleted(object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            e.NewItems?.Cast<AssetViewModel>().Where(x => openedAssets.ContainsKey(x)).ForEach(CloseEditorWindow);
        }

        /// <summary>
        /// Cleans the editor pane.
        /// </summary>
        /// <param name="editorPane">The editor pane.</param>
        /// <seealso cref="RemoveEditorPane"/>
        private static void CleanEditorPane([NotNull] LayoutAnchorable editorPane)
        {
            // Destroy the editor view
            (editorPane.Content as IDestroyable)?.Destroy();
            editorPane.Content = null;
            editorPane.Title = null;
        }

        /// <summary>
        /// Removes the editor pane.
        /// </summary>
        /// <param name="editorPane">The editor pane.</param>
        /// <seealso cref="CleanEditorPane"/>
        private void RemoveEditorPane([NotNull] LayoutAnchorable editorPane)
        {
            editorPane.IsActiveChanged -= EditorPaneIsActiveChanged;
            editorPane.IsSelectedChanged -= EditorPaneIsSelectedChanged;
            editorPane.Closing -= EditorPaneClosing;
            editorPane.Closed -= EditorPaneClosed;

            // If this editor pane was closed by user, no need to do that; it is necessary for closing programmatically
            if (editorPane.Root != null)
                editorPane.Close();
        }

        private void EditorPaneClosing(object sender, CancelEventArgs e)
        {
            var editorPane = (LayoutAnchorable)sender;

            var element = editorPane.Content as FrameworkElement;

            // If any editor couldn't close, cancel the sequence
            if (element?.DataContext is AssetViewModel asset && !(openedAssets.TryGetValue(asset, out var editor) && editor.PreviewClose(null)))
            {
                e.Cancel = true;
            }
        }

        private void EditorPaneClosed(object sender, EventArgs eventArgs)
        {
            var editorPane = (LayoutAnchorable)sender;

            var element = editorPane.Content as FrameworkElement;
            if (element?.DataContext is AssetEditorViewModel editor)
            {
                CloseEditorWindow(editor.Asset);
            }
        }

        private static void EditorPaneContentLoaded(object sender, RoutedEventArgs e)
        {
            // Give focus to element
            var element = (FrameworkElement)sender;
            if (!element.IsKeyboardFocusWithin)
                Keyboard.Focus(element);
        }

        private static void EditorPaneIsActiveChanged(object sender, EventArgs e)
        {
            var editorPane = (LayoutAnchorable)sender;

            if (editorPane.Content is FrameworkElement element)
            {
                if (editorPane.IsActive)
                {
                    if (element.IsLoaded)
                    {
                        // Give focus to element
                        if (!element.IsKeyboardFocusWithin)
                            Keyboard.Focus(element);
                    }
                    else
                    {
                        // Not loaded yet, let's defer the focus until loaded
                        element.Loaded += EditorPaneContentLoaded;
                    }
                }
                else
                {
                    element.Loaded -= EditorPaneContentLoaded;
                }
            }
        }

        private void EditorPaneIsSelectedChanged(object sender, EventArgs e)
        {
            var editorPane = (LayoutAnchorable)sender;

            if (editorPane.Content is FrameworkElement element && element?.DataContext is AssetViewModel asset)
            {
                if (openedAssets.TryGetValue(asset, out var editor) && editor is Assets.Presentation.AssetEditors.GameEditor.ViewModels.GameEditorViewModel gameEditor)
                {
                    // A tab/sub-window is visible via IsSelected, not IsVisible
                    if (editorPane.IsSelected)
                    {
                        gameEditor.ShowGame();
                    }
                    else
                    {
                        gameEditor.HideGame();
                    }
                }
            }
        }

        /// <summary>
        /// Makes the editor pane active and visible.
        /// </summary>
        /// <param name="editorPane"></param>
        private static void MakeActiveVisible([NotNull] LayoutAnchorable editorPane)
        {
            editorPane.IsActive = true;
            editorPane.Show();
        }
    }
}
