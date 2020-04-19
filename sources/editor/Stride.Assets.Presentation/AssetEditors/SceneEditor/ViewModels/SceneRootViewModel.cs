// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Threading;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Assets.Presentation.ViewModel;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels
{
    [DataSerializerGlobal(null, typeof(List<Entity>))]
    [DebuggerDisplay("SceneRoot = {" + nameof(Name) + "}")]
    public sealed class SceneRootViewModel : EntityHierarchyRootViewModel, IPropertyProviderViewModel
    {
        private readonly ObservableSet<SceneRootViewModel> childScenes = new ObservableSet<SceneRootViewModel>();
        private readonly AsyncLock loadMutex;
        private readonly MemberGraphNodeBinding<Vector3> offsetNodeBinding;
        /// <summary>
        /// Identifier of the game-side scene.
        /// </summary>
        private readonly Guid sceneId;

        public SceneRootViewModel([NotNull] SceneEditorViewModel editor, [NotNull] SceneViewModel scene, AsyncLock loadMutex)
            : base(editor, scene, "Scene root")
        {
            this.loadMutex = loadMutex;
            sceneId = Guid.NewGuid();

            foreach (var childScene in scene.Children)
            {
                var child = new SceneRootViewModel(editor, childScene, loadMutex) { Parent = this };
                childScenes.Add(child);
            }
            AddItems(ChildScenes);
            scene.Children.CollectionChanged += SceneChildrenCollectionChanged;

            var offsetNode = Editor.NodeContainer.GetNode(scene.Asset)[nameof(scene.Asset.Offset)];
            offsetNodeBinding = new MemberGraphNodeBinding<Vector3>(offsetNode, nameof(Offset), OnPropertyChanging, OnPropertyChanged, Editor.UndoRedoService);

            LoadSettings().Forget();
        }

        [ItemNotNull, NotNull]
        public IReadOnlyObservableList<SceneRootViewModel> ChildScenes => childScenes;

        /// <inheritdoc/>
        public override AbsoluteId Id => new AbsoluteId(Asset.Id, sceneId);

        /// <inheritdoc/>
        public override IEnumerable<EntityViewModel> InnerSubEntities => ChildrenExceptScenes.SelectMany(x => x.InnerSubEntities);

        /// <inheritdoc />
        public override string Name { get => Asset.Name; set => throw new NotSupportedException($"Cannot change the name of a {nameof(SceneRootViewModel)} object."); }

        public Vector3 Offset { get => offsetNodeBinding.Value; set => offsetNodeBinding.Value = value; }

        [CanBeNull]
        public SceneRootViewModel ParentScene => (SceneRootViewModel)Parent;

        [NotNull]
        public SceneViewModel SceneAsset => (SceneViewModel)Asset;

        /// <summary>
        /// Index of the first scene in the <see cref="EntityHierarchyRootViewModel.Children"/> property.
        /// </summary>
        /// <remarks>
        /// Scene are inserted after all entities and folders.
        /// </remarks>
        private int ChildScenesStartingIndex => Children.Count - ChildScenes.Count;

        [NotNull]
        private IEnumerable<EntityHierarchyItemViewModel> ChildrenExceptScenes => Children.Where(x => !(x is SceneRootViewModel));

        [NotNull]
        private new SceneEditorViewModel Editor => (SceneEditorViewModel)base.Editor;

        public void Delete()
        {
            // Reset parenting link
            ParentScene?.SceneAsset.Children.Remove(SceneAsset);
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(SceneRootViewModel));

            offsetNodeBinding.Dispose();
            SceneAsset.Children.CollectionChanged -= SceneChildrenCollectionChanged;
            base.Destroy();
        }

        /// <inheritdoc/>
        public override Task NotifyGameSidePartAdded()
        {
            // Manually notify the entities in the scene
            return Task.WhenAll(ChildrenExceptScenes.BreadthFirst(x => x.Children).Select(e => e.NotifyGameSidePartAdded()));
        }

        /// <inheritdoc />
        protected override int GetAddIndex(IReadOnlyCollection<object> children)
        {
            var sceneCount = children.OfType<SceneViewModel>().Count();
            var rootCount = children.OfType<SceneRootViewModel>().Count();
            if (sceneCount == children.Count || rootCount == children.Count)
            {
                // Scenes are added at the end
                return Owner.EntityCount + ChildScenes.Count;
            }
            // Cannot add scene/root along other types
            if (sceneCount != 0 || rootCount != 0)
                return -1;

            return base.GetAddIndex(children);
        }

        /// <inheritdoc />
        protected override int GetInsertionIndex(InsertPosition position)
        {
            var index = 0;
            var parentRoot = ParentScene;
            if (parentRoot != null)
            {
                // Note: scene are inserted after all entities
                index = parentRoot.EntityCount + parentRoot.ChildScenes.IndexOf(this);
            }
            if (position == InsertPosition.After)
                ++index;
            return index;
        }

        /// <inheritdoc />
        protected override async Task OnLoadingRequested(bool load, bool recursive)
        {
            var controller = Editor.Controller;
            if (load)
            {
                using (await loadMutex.LockAsync())
                {
                    // Compute the list of scenes that need loading
                    var stack = new Stack<SceneRootViewModel>();
                    var root = this;
                    while (root != null)
                    {
                        if (!root.IsLoaded)
                            stack.Push(root);
                        root = root.ParentScene;
                    }
                    var scenesToLoad = stack.ToList();
                    if (recursive)
                    {
                        // Also take child scenes, recursively
                        scenesToLoad.AddRange(ChildScenes.BreadthFirst(x => x.ChildScenes).Where(s => !s.IsLoaded));
                    }
                    // Set the IsLoading property immediately, so users have a clear feedback
                    foreach (var scene in scenesToLoad)
                        scene.IsLoading = true;
                    // Ensure the editor has finished its initialization
                    await Editor.Initialized;
                    // Load the scenes
                    await controller.LoadScenes(scenesToLoad);
                    await Task.WhenAll(scenesToLoad.Select(async scene =>
                    {
                        // Loading/unloading a scene root always applies to all its content
                        scene.SetEntityIsLoadedRecursively(true);
                        await scene.NotifyGameSidePartAdded();
                        scene.IsLoaded = true;
                    }));
                }
            }
            else
            {
                using (await loadMutex.LockAsync())
                {
                    if (!IsLoaded)
                        return;
                    // Compute the list of scenes that need unloading
                    var scenesToUnload = this.Yield().Concat(ChildScenes.BreadthFirst(r => r.ChildScenes).Where(r => r.IsLoaded)).Reverse().ToList();
                    // Set the IsLoading property immediately, so users have a clear feedback
                    foreach (var scene in scenesToUnload)
                        scene.IsLoading = true;
                    // Ensure the editor has finished its initialization
                    await Editor.Initialized;
                    foreach (var scene in scenesToUnload)
                    {
                        scene.IsLoaded = false;
                        // Loading/unloading a scene root always applies to all its content
                        scene.SetEntityIsLoadedRecursively(false);
                    }
                    // Unload the scenes
                    await controller.UnloadScenes(scenesToUnload);
                }
            }
        }

        /// <inheritdoc />
        protected override Task OnLockingRequested(bool @lock, bool recursive)
        {
            IsLocked = @lock;
            // Locking/unlocking a scene always applies to all its content
            SetEntityIsLockedRecursively(@lock);
            if (!recursive)
                return Task.CompletedTask;

            // Update IsLocked property recursively
            foreach (var scene in ChildScenes.BreadthFirst(x => x.ChildScenes))
            {
                scene.IsLocked = @lock;
                scene.SetEntityIsLockedRecursively(@lock);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(params string[] propertyNames)
        {
            base.OnPropertyChanged(propertyNames);

            if (propertyNames.Any(p => string.Equals(p, nameof(Offset))))
            {
                // Unloaded scenes don't exist on the editor game side
                if (IsLoaded || IsLoading)
                    Editor.Controller.UpdateSceneAnchorPosition(this).Forget();
            }
        }

        /// <summary>
        /// Saves specific settings of this <see cref="SceneAsset"/>.
        /// </summary>
        internal void SaveSettings()
        {
            var userSettings = Asset.Directory.Package.UserSettings;
            var sceneSettingsCollection = userSettings.GetValue(PackageSceneSettings.SceneSettings);
            SceneSettingsData settings;
            if (!sceneSettingsCollection.TryGetValue(Asset.Asset.Id, out settings))
            {
                // Create new settings
                settings = SceneSettingsData.CreateDefault();
                sceneSettingsCollection.Add(Asset.Asset.Id, settings);
            }
            settings.SceneLoaded = IsLoaded;

            // FIXME: it would be better to just set the one scene settings data instead of the whole collection.
            userSettings.SetValue(PackageSceneSettings.SceneSettings, sceneSettingsCollection);
            Asset.Directory.Package.UserSettings.Save();
        }

        /// <inheritdoc />
        protected override bool CanAddOrInsertChildren(IReadOnlyCollection<object> children, bool checkSameParent, AddChildModifiers modifiers, int index, out string message)
        {
            message = "Selection is empty";
            var roots = children.OfType<SceneRootViewModel>().ToList();
            if (roots.Count != children.Count)
                return base.CanAddOrInsertChildren(children, checkSameParent, modifiers, index, out message);

            // Note: scene are inserted after all entities
            if (index < EntityCount)
            {
                message = "Can only add a scene to another scene";
                return false;
            }
            var parentName = !string.IsNullOrWhiteSpace(Name) ? Name : "this location";
            foreach (var root in roots)
            {
                if (!SceneAsset.CanBeParentOf(root.SceneAsset, out message, checkSameParent))
                {
                    return false;
                }
                // Accepting child scenes
                message = $"Add as a child to {parentName}";
            }
            return true;
        }

        /// <inheritdoc />
        protected override void MoveChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, int index)
        {
            var roots = children.OfType<SceneRootViewModel>().ToList();
            if (roots.Count != children.Count)
            {
                base.MoveChildren(children, modifiers, index);
                return;
            }
            // Note: scene are inserted after all entities
            index -= EntityCount;
            using (var transaction = Editor.UndoRedoService.CreateTransaction())
            {
                // Move the roots in two steps: first remove all, then insert all
                foreach (var root in roots)
                {
                    // Some of the roots we're moving might already be children of this object, let's count for their removal in the insertion index.
                    var rootIndex = ChildScenes.IndexOf(root);
                    if (rootIndex >= 0 && rootIndex < index)
                        --index;

                    root.ParentScene?.SceneAsset.Children.Remove(root.SceneAsset);
                }
                foreach (var root in roots)
                {
                    SceneAsset.Children.Insert(index++, root.SceneAsset);
                }
                Editor.UndoRedoService.SetName(transaction, $"Move child scene{(roots.Count > 1 ? "s" : "")}");
            }
        }

        /// <summary>
        /// Loads specific settings of this <see cref="SceneAsset"/>.
        /// </summary>
        private async Task LoadSettings()
        {
            // Ensure the editor has finished its initialization
            await Editor.Initialized;

            var userSettings = Asset.Directory.Package.UserSettings;
            var sceneSettingsCollection = userSettings.GetValue(PackageSceneSettings.SceneSettings);
            SceneSettingsData settings;
            if (!sceneSettingsCollection.TryGetValue(Asset.Id, out settings))
            {
                // Fall back to default settings
                settings = SceneSettingsData.CreateDefault();
            }
            // Only force load
            if (settings.SceneLoaded)
                RequestLoading(true).Forget();
        }

        private void SceneChildrenCollectionChanged(object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems?.Count > 0)
                    {
                        foreach (var sceneRoot in e.OldItems.Cast<SceneViewModel>().Join(childScenes, s => s.Id, r => r.Asset.Id, (s, r) => r))
                        {
                            // Save settings first
                            sceneRoot.SaveSettings();
                            sceneRoot.ChildScenes.DepthFirst(s => s.ChildScenes).ForEach(s => s.SaveSettings());
                            // Unload the scene recursively
                            var unloadingTask = sceneRoot.RequestLoading(false);
                            // Remove the scene
                            RemoveItem(sceneRoot);
                            childScenes.Remove(sceneRoot);
                            // Update the active root
                            if (Editor.ActiveRoot == sceneRoot)
                                Editor.ActiveRoot = this;
                            // Wait for the unloading operation to finish, then destroy the scene and its content, recursively.
                            unloadingTask.ContinueWith(t =>
                            {
                                Dispatcher.InvokeAsync(() => sceneRoot.Destroy()).Forget();
                            }, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted).Forget();
                        }
                    }
                    if (e.NewItems?.Count > 0)
                    {
                        var sceneIndex = e.NewStartingIndex;
                        foreach (var sceneRoot in e.NewItems.Cast<SceneViewModel>().Select(s => new SceneRootViewModel(Editor, s, loadMutex) { Parent = this }))
                        {
                            InsertItem(sceneIndex + ChildScenesStartingIndex, sceneRoot);
                            childScenes.Insert(sceneIndex++, sceneRoot);
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

        /// <summary>
        /// Updates the <see cref="EntityHierarchyElementViewModel.IsLoaded"/> property on all entities of the scene.
        /// </summary>
        /// <param name="isLoaded"></param>
        private void SetEntityIsLoadedRecursively(bool isLoaded)
        {
            // This method is called from OnLoadingRequested, therefore we don't need to request the entities to load.
            // We just need to update their status.
            foreach (var entity in ChildrenExceptScenes.BreadthFirst(x => x.Children).OfType<EntityViewModel>())
            {
                entity.IsLoaded = isLoaded;
            }
        }

        /// <summary>
        /// Updates the <see cref="EntityHierarchyElementViewModel.IsLocked"/> property on all entities of the scene.
        /// </summary>
        /// <param name="isLocked"></param>
        private void SetEntityIsLockedRecursively(bool isLocked)
        {
            foreach (var entity in ChildrenExceptScenes.BreadthFirst(x => x.Children).OfType<EntityViewModel>())
            {
                // This method is called from OnLockingRequested, therefore we don't need to request the entities to lock.
                // We just need to update their status.
                entity.IsLocked = isLocked;
            }
        }

        /// <inheritdoc/>
        IObjectNode IPropertyProviderViewModel.GetRootNode()
        {
            // Display the asset properties
            return Editor.NodeContainer.GetNode(Asset.Asset);
        }
    }
}
