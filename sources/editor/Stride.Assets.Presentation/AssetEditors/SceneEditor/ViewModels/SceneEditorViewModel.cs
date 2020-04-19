// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Threading;
using Stride.Core.Presentation.Collections;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.Services;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.Views;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels
{
    /// <summary>
    /// View model of a <see cref="SceneViewModel"/> editor.
    /// </summary>
    [AssetEditorViewModel(typeof(SceneAsset), typeof(SceneEditorView))]
    public sealed class SceneEditorViewModel : EntityHierarchyEditorViewModel, IMultipleAssetEditorViewModel
    {
        private readonly ObservableSet<SceneViewModel> allScenes = new ObservableSet<SceneViewModel>();
        /// <summary>
        /// The scene that was initially opened by this editor.
        /// </summary>
        private SceneViewModel mainScene;
        private readonly AsyncLock loadMutex = new AsyncLock();
        private readonly TaskCompletionSource<int> initialized = new TaskCompletionSource<int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneEditorViewModel"/> class.
        /// </summary>
        /// <param name="asset">The asset related to this editor.</param>
        /// <param name="controllerFactory">A factory to create the associated <see cref="IEditorGameController"/>.</param>
        /// <seealso cref="Create(SceneViewModel)"/>
        private SceneEditorViewModel([NotNull] SceneViewModel asset, [NotNull] Func<GameEditorViewModel, IEditorGameController> controllerFactory)
            : base(asset, controllerFactory)
        {

        }

        public bool DisplayCameraPreview { get => Preview.IsActive; set { SetValue(Preview.IsActive != value, () => Preview.IsActive = value); } }

        public Task Initialized => initialized.Task;

        /// <summary>
        /// A collection of all scenes opened in this editor.
        /// </summary>
        /// <remarks>
        /// The order of items in this collection is not guarenteed to be consistent with the hierarchy of scenes.
        /// </remarks>
        public IReadOnlyObservableList<SceneViewModel> AllScenes => allScenes;

        /// <inheritdoc />
        IReadOnlyObservableList<AssetViewModel> IMultipleAssetEditorViewModel.OpenedAssets => allScenes;

        private IEditorGameCameraPreviewViewModelService Preview => Controller.GetService<IEditorGameCameraPreviewViewModelService>();

        [NotNull]
        internal new SceneEditorController Controller => (SceneEditorController)base.Controller;

        [NotNull]
        public static SceneEditorViewModel Create([NotNull] SceneViewModel asset)
        {
            var rootScene = asset.GetRoot();
            return new SceneEditorViewModel(rootScene, x => new SceneEditorController(rootScene, (SceneEditorViewModel)x)) { mainScene = asset };
        }

        /// <inheritdoc />
        protected override AssetCompositeItemViewModel CreateRootPartViewModel()
        {
            return new SceneRootViewModel(this, (SceneViewModel)Asset, loadMutex);
        }

        /// <inheritdoc />
        protected override Task Delete()
        {
            var sceneRoots = SelectedContent.OfType<SceneRootViewModel>().ToList();
            // Mix of scene roots and entities selected
            if (sceneRoots.Count != SelectedContent.Count)
                return base.Delete();

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                ClearSelection();
                foreach (var sceneRoot in GetCommonRoots(sceneRoots))
                {
                    sceneRoot.Delete();
                }

                UndoRedoService.SetName(transaction, "Remove selected child scenes");
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(SceneEditorViewModel));
            var root = (SceneRootViewModel)HierarchyRoot;
            // Note: settings of the root scene are saved in EntityHierarchyEditorViewModel.Destroy()
            foreach (var scene in root.ChildScenes.BreadthFirst(x => x.ChildScenes))
            {
                // Save settings first
                scene.SaveSettings();
                // Unregister scene
                scene.ChildScenes.CollectionChanged -= ChildScenesCollectionChanged;
            }
            // Unregister root
            root.ChildScenes.CollectionChanged -= ChildScenesCollectionChanged;
            base.Destroy();
        }

        /// <inheritdoc />
        protected override async Task<bool> InitializeEditor()
        {
            if (!await base.InitializeEditor())
                return false;

            var root = (SceneRootViewModel)RootPart;
            SceneRootViewModel mainSceneRoot = null;
            // Register all scenes
            foreach (var sceneRoot in root.Yield().Concat(root.ChildScenes.BreadthFirst(x => x.ChildScenes)))
            {
                var asset = sceneRoot.SceneAsset;
                if (asset == mainScene)
                    mainSceneRoot = sceneRoot;
                allScenes.Add(asset);
                sceneRoot.ChildScenes.CollectionChanged += ChildScenesCollectionChanged;
            }
            // Load the main scene (the one that was initially opened by this editor, not necessarily the root scene)
            mainSceneRoot?.RequestLoading(true).Forget();
            // Notify initialization completed
            initialized.SetResult(0);
            // Expand the root by default
            root.IsExpanded = true;
            // Set the main scene as currently active (fallback to root)
            ActiveRoot = mainSceneRoot ?? root;
            return true;
        }

        /// <inheritdoc />
        protected override void LoadSettings(SceneSettingsData settings)
        {
            base.LoadSettings(settings);
            DisplayCameraPreview = settings.CameraPreviewVisible;
        }

        /// <inheritdoc />
        protected override void SaveSettings(SceneSettingsData settings)
        {
            base.SaveSettings(settings);
            settings.CameraPreviewVisible = DisplayCameraPreview;
        }

        /// <inheritdoc />
        protected override async Task RefreshEditorProperties()
        {
            // Single element selected (could be a scene)
            if (SelectedContent.Count == 1)
            {
                var sceneRoot = SelectedContent[0] as SceneRootViewModel;
                if (sceneRoot != null)
                {
                    EditorProperties.Name = sceneRoot.Name;
                    EditorProperties.TypeDescription = "Scene";
                    await EditorProperties.GenerateSelectionPropertiesAsync(sceneRoot.Yield());
                    return;
                }
            }
            await base.RefreshEditorProperties();
        }

        protected override void UpdateTransformations(EntityHierarchyElementViewModel element, TransformationTRS transformation)
        {
            if (element is EntityViewModel)
            {
                base.UpdateTransformations(element, transformation);
                return;
            }

            var root = element as SceneRootViewModel;
            if (root == null)
                return;

            // Update properties only when they actually changed
            var oldOffset = root.Offset;
            if (oldOffset != transformation.Position)
                root.Offset = transformation.Position;
        }

        private void ChildScenesCollectionChanged(object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems?.Count > 0)
                    {
                        foreach (var root in e.OldItems?.Cast<SceneRootViewModel>().BreadthFirst(x => x.ChildScenes))
                        {
                            root.ChildScenes.CollectionChanged -= ChildScenesCollectionChanged;
                            allScenes.Remove(root.SceneAsset);
                        }
                    }
                    if (e.NewItems?.Count > 0)
                    {
                        foreach (var root in e.NewItems?.Cast<SceneRootViewModel>().BreadthFirst(x => x.ChildScenes))
                        {
                            allScenes.Add(root.SceneAsset);
                            root.ChildScenes.CollectionChanged += ChildScenesCollectionChanged;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
