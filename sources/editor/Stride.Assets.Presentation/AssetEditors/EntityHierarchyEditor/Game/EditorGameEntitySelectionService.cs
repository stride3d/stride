// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Sprites;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
{
    /// <summary>
    /// A class that manages selection in the entity hierarchy editor. It provides methods to modify the selection from the game thread
    /// and handles changes in the selection that occurs in the view model.
    /// </summary>
    public class EditorGameEntitySelectionService : EditorGameMouseServiceBase, IEditorGameEntitySelectionService, IEditorGameSelectionViewModelService
    {
        private Vector2 mouseMoveAccumulator;
        private PickingSceneRenderer entityPicker;
        private EntityHierarchyEditorGame game;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorGameEntitySelectionService"/> class.
        /// </summary>
        /// <param name="editor">The <see cref="EntityHierarchyEditorViewModel"/> related to the current instance of the scene editor.</param>
        public EditorGameEntitySelectionService([NotNull] EntityHierarchyEditorViewModel editor)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            Editor = editor;
        }

        /// <inheritdoc/>
        public override bool IsControllingMouse { get; protected set; }

        /// <summary>
        /// Gets the number of currently selected entities.
        /// </summary>
        public int SelectedIdCount => SelectedIds.Count;

        /// <summary>
        /// Gets the number of currently selected root entities.
        /// </summary>
        /// <remarks>
        /// An entity is selected as a root if it is currently selected and none of its parent is currently selected.
        /// </remarks>
        public int SelectedRootIdCount => SelectedRootIds.Count;

        /// <summary>
        /// Raised in the scene game thread after the selection has been updated.
        /// </summary>
        public event EventHandler<EntitySelectionEventArgs> SelectionUpdated;

        /// <inheritdoc/>
        public override IEnumerable<Type> Dependencies { get { yield return typeof(IEditorGameComponentGizmoService); } }

        protected EntityHierarchyEditorViewModel Editor { get; }

        /// <summary>
        /// A lock that should be taken when accessing <see cref="SelectedIds"/> and <see cref="SelectedRootIds"/> properties.
        /// </summary>
        protected object LockObject { get; } = new object();

        protected ISet<AbsoluteId> SelectableIds { get; } = new HashSet<AbsoluteId>();

        /// <summary>
        /// The set of <see cref="Guid"/> corresponding to the currently selected entities.
        /// </summary>
        protected ISet<AbsoluteId> SelectedIds { get; } = new HashSet<AbsoluteId>();

        /// <summary>
        /// The set of <see cref="Guid"/> corresponding to the currently selected root entities.
        /// </summary>
        /// <remarks>
        /// An entity is selected as a root if it is currently selected and none of its parent is currently selected.
        /// </remarks>
        protected ISet<AbsoluteId> SelectedRootIds { get; } = new HashSet<AbsoluteId>();

        /// <inheritdoc/>
        bool IEditorGameSelectionViewModelService.DisplaySelectionMask { get; set; }

        /// <inheritdoc/>
        bool IEditorGameEntitySelectionService.DisplaySelectionMask => ((IEditorGameSelectionViewModelService)this).DisplaySelectionMask;

        private IEditorGameComponentGizmoService Gizmos => Services.Get<IEditorGameComponentGizmoService>();

        /// <inheritdoc />
        public override Task DisposeAsync()
        {
            EnsureNotDestroyed(nameof(EditorGameEntitySelectionService));

            Editor.SelectedContent.CollectionChanged -= SelectedContentChanged;
            SelectionUpdated -= SelectionSelectionUpdated;
            return base.DisposeAsync();
        }

        /// <inheritdoc/>
        public override void RegisterScene(Scene scene)
        {
            base.RegisterScene(scene);
            entityPicker.CacheScene(scene, true);

            game.SceneSystem.SceneInstance.EntityAdded += (sender, entity) => entityPicker.CacheEntity(entity, false);
            game.SceneSystem.SceneInstance.EntityRemoved += (sender, entity) => entityPicker.UncacheEntity(entity, false);
            game.SceneSystem.SceneInstance.ComponentChanged += (sender, e) =>
            {
                if (e.PreviousComponent != null)
                    entityPicker.UncacheEntityComponent(e.PreviousComponent);
                if (e.NewComponent != null)
                    entityPicker.CacheEntityComponent(e.NewComponent);
            };

            SelectionUpdated += SelectionSelectionUpdated;
        }

        /// <summary>
        /// Gets a copy of the set of <see cref="Guid"/> corresponding to the currently selected entities.
        /// </summary>
        [NotNull]
        public IReadOnlyCollection<AbsoluteId> GetSelectedIds()
        {
            lock (LockObject)
            {
                return SelectedIds.ToList();
            }
        }

        /// <summary>
        /// Gets a copy of the set of <see cref="Guid"/> corresponding to the currently selected root entities.
        /// </summary>
        /// <remarks>
        /// An entity is selected as a root if it is currently selected and none of its parent is currently selected.
        /// </remarks>
        [NotNull]
        public IReadOnlyCollection<AbsoluteId> GetSelectedRootIds()
        {
            lock (LockObject)
            {
                return SelectedRootIds.ToList();
            }
        }

        /// <summary>
        /// Clears the selection.
        /// </summary>
        private void Clear()
        {
            if (SelectedIds.Count == 0)
                return;

            IsControllingMouse = true;
            Editor.Dispatcher.InvokeAsync(() =>
            {
                Editor.ClearSelection();
                Editor.Controller.InvokeAsync(() => IsControllingMouse = false);
            });
        }

        /// <summary>
        /// Resets the selection to the given entity.
        /// </summary>
        /// <param name="entity">The entity that must be selected.</param>
        private void Set([NotNull] Entity entity)
        {
            var entityId = Editor.Controller.GetAbsoluteId(entity);
            if (!SelectableIds.Contains(entityId) || SelectedIds.Count == 1 && SelectedIds.Contains(entityId))
                return;

            IsControllingMouse = true;
            Editor.Dispatcher.InvokeAsync(() =>
            {
                var viewModel = (EntityHierarchyElementViewModel)Editor.FindPartViewModel(entityId);
                Editor.ClearSelection();
                if (viewModel != null)
                    Editor.SelectedContent.Add(viewModel);
                Editor.Controller.InvokeAsync(() => IsControllingMouse = false);
            });
        }

        /// <summary>
        /// Adds the given entity to the selection.
        /// </summary>
        /// <param name="entity">The entity that must be added to selection.</param>
        private void Add([NotNull] Entity entity)
        {
            var entityId = Editor.Controller.GetAbsoluteId(entity);
            if (!SelectableIds.Contains(entityId) || SelectedIds.Contains(entityId))
                return;

            IsControllingMouse = true;
            Editor.Dispatcher.InvokeAsync(() =>
            {
                var viewModel = (EntityHierarchyElementViewModel)Editor.FindPartViewModel(entityId);
                if (viewModel?.IsSelectable == true)
                    Editor.SelectedContent.Add(viewModel);
                Editor.Controller.InvokeAsync(() => IsControllingMouse = false);
            });
        }

        /// <summary>
        /// Adds the given entity from the selection.
        /// </summary>
        /// <param name="entity">The entity that must be removed from selection.</param>
        private void Remove([NotNull] Entity entity)
        {
            var entityId = Editor.Controller.GetAbsoluteId(entity);
            if (!SelectedIds.Contains(entityId))
                return;

            IsControllingMouse = true;
            Editor.Dispatcher.InvokeAsync(() =>
            {
                var viewModel = (EntityHierarchyElementViewModel)Editor.FindPartViewModel(entityId);
                if (viewModel != null)
                    Editor.SelectedContent.Remove(viewModel);
                Editor.Controller.InvokeAsync(() => IsControllingMouse = false);
            });
        }

        /// <summary>
        /// Indicates whether the selection currently contains the given entity.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <returns><c>True</c> if the entity is currently selected, <c>False</c> otherwise.</returns>
        private bool Contains([NotNull] Entity entity)
        {
            var entityId = Editor.Controller.GetAbsoluteId(entity);
            return SelectedIds.Contains(entityId);
        }

        public EntityPickingResult Pick()
        {
            return entityPicker?.Pick() ?? default(EntityPickingResult);
        }

        public async Task<bool> DuplicateSelection()
        {
            var duplicatedIds = new HashSet<AbsoluteId>();
            var tcs = new TaskCompletionSource<bool>();
            var selectedEntitiesId = new HashSet<AbsoluteId>();

            EventHandler<EntitySelectionEventArgs> callback = (sender, e) =>
            {
                // Ignore first call (clear list)
                selectedEntitiesId.Clear();
                selectedEntitiesId.AddRange(e.NewSelection.Select(Editor.Controller.GetAbsoluteId));

                if (selectedEntitiesId.SetEquals(duplicatedIds))
                {
                    tcs.TrySetResult(selectedEntitiesId.SetEquals(duplicatedIds));
                }
            };
            SelectionUpdated += callback;

            var result = await Editor.Dispatcher.InvokeAsync(() => Editor.DuplicateSelectedEntities()?.Select(x => x.Id));
            if (result == null)
            {
                SelectionUpdated -= callback;
                return false;
            }
            duplicatedIds.AddRange(result);

            await tcs.Task;
            SelectionUpdated -= callback;
            return true;
        }

        private void AddToSelection([NotNull] EntityHierarchyElementViewModel element)
        {
            lock (LockObject)
            {
                if (!element.IsSelectable)
                    return;

                // Add the entity id to the selected ids
                SelectedIds.Add(element.Id);

                // Check if one of its parents is in the selection
                var parent = element.TransformParent;
                while (parent != null)
                {
                    if (SelectedIds.Contains(parent.Id))
                        break;

                    parent = parent.TransformParent;
                }

                // If so, the SelectedRootIds collection does not need to be updated.
                if (parent != null)
                    return;

                // Otherwise, it's a new root entity in the selection.
                SelectedRootIds.Add(element.Id);

                // Remove its children that were previously root entities in the selection.
                foreach (var child in element.TransformChildren.SelectDeep(x => x.TransformChildren))
                {
                    SelectedRootIds.Remove(child.Id);
                }
            }
        }

        private void RemoveFromSelection([NotNull] EntityHierarchyElementViewModel element)
        {
            lock (LockObject)
            {
                SelectedIds.Remove(element.Id);

                // Remove the root entity from the selected root entities
                if (SelectedRootIds.Remove(element.Id) && element.IsLoaded)
                {
                    // Ensure all children that are selected are properly added to the selected root collection
                    foreach (var child in element.TransformChildren.SelectDeep(x => x.TransformChildren).Where(x => SelectedIds.Contains(x.Id)))
                    {
                        // Check if one of its parents is in the selection
                        var parent = child.TransformParent;
                        while (parent != element && parent != null)
                        {
                            if (SelectedIds.Contains(parent.Id))
                                break;

                            parent = parent.TransformParent;
                        }

                        // If so, the SelectedRootIds collection does not need to be updated.
                        if (parent != element)
                            return;

                        // Otherwise, it's a new root entity in the selection.
                        SelectedRootIds.Add(child.Id);
                    }
                }
            }
        }

        private void SelectedContentChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!Editor.SceneInitialized)
                return;

            lock (LockObject)
            {
                // Retrieve old selection to pass it to the event
                var oldSelectionIds = GetSelectedRootIds();

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (EntityHierarchyItemViewModel newItem in e.NewItems)
                        {
                            var root = newItem as SceneRootViewModel;
                            if (root != null)
                                AddToSelection(root);
                            else
                                foreach (var entity in newItem.InnerSubEntities)
                                    AddToSelection(entity);
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (EntityHierarchyItemViewModel oldItem in e.OldItems)
                        {
                            var root = oldItem as SceneRootViewModel;
                            if (root != null)
                                RemoveFromSelection(root);
                            else
                                foreach (var entity in oldItem.InnerSubEntities)
                                    RemoveFromSelection(entity);
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        SelectedIds.Clear();
                        SelectedRootIds.Clear();
                        break;
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Move:
                        throw new NotSupportedException("This operation is not supported.");
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                RaiseSelectionUpdated(oldSelectionIds);
            }
        }

        private void RaiseSelectionUpdated(IReadOnlyCollection<AbsoluteId> oldSelectionIds)
        {
            var newSelectionIds = GetSelectedRootIds();
            Editor.Controller.InvokeAsync(() =>
            {
                var oldSelection = oldSelectionIds.Select(x => Editor.Controller.FindGameSidePart(x)).Cast<Entity>().NotNull().ToList();
                var newSelection = newSelectionIds.Select(x => Editor.Controller.FindGameSidePart(x)).Cast<Entity>().NotNull().ToList();
                SelectionUpdated?.Invoke(this, new EntitySelectionEventArgs(oldSelection, newSelection));
            });
        }

        private void SelectionSelectionUpdated(object sender, [NotNull] EntitySelectionEventArgs e)
        {
            var previousSelection = new HashSet<Entity>(e.OldSelection);
            foreach (var childEntity in e.OldSelection.SelectDeep(x => x.Transform.Children.Select(y => y.Entity)))
            {
                previousSelection.Add(childEntity);
            }
            var newSelection = new HashSet<Entity>(e.NewSelection);
            foreach (var childEntity in e.NewSelection.SelectDeep(x => x.Transform.Children.Select(y => y.Entity)))
            {
                newSelection.Add(childEntity);
            }

            previousSelection.ExceptWith(newSelection);

            // update the selection on the gizmo entities.
            foreach (var previousEntity in previousSelection)
            {
                UpdateGizmoEntitiesSelection(previousEntity, false);
            }

            foreach (var newEntity in newSelection)
            {
                UpdateGizmoEntitiesSelection(newEntity, true);
            }
        }

        private void UpdateGizmoEntitiesSelection([NotNull] Entity entity, bool isSelected)
        {
            Gizmos.UpdateGizmoEntitiesSelection(entity, isSelected);
            foreach (var child in entity.Transform.Children.SelectDeep(x => x.Children).Select(x => x.Entity).NotNull())
            {
                Gizmos.UpdateGizmoEntitiesSelection(child, isSelected);
            }
        }

        private async Task Execute()
        {
            MicrothreadLocalDatabases.MountCommonDatabase();

            while (game.IsRunning)
            {
                await game.Script.NextFrame();

                if (IsActive)
                {
                    // TODO: code largely duplicated in EditorGameMaterialHighlightService. Factorize!

                    var screenSize = new Vector2(game.GraphicsDevice.Presenter.BackBuffer.Width, game.GraphicsDevice.Presenter.BackBuffer.Height);

                    if (game.Input.IsMouseButtonPressed(MouseButton.Left))
                    {
                        mouseMoveAccumulator = Vector2.Zero;
                    }
                    mouseMoveAccumulator += new Vector2(Math.Abs(game.Input.MouseDelta.X * screenSize.X), Math.Abs(game.Input.MouseDelta.Y * screenSize.Y));

                    if (IsMouseAvailable && game.Input.IsMouseButtonReleased(MouseButton.Left) && !game.Input.IsMouseButtonDown(MouseButton.Right))
                    {
                        if (mouseMoveAccumulator.Length() >= TransformationGizmo.TransformationStartPixelThreshold)
                            continue;

                        var addToSelection = game.Input.IsKeyDown(Keys.LeftCtrl) || game.Input.IsKeyDown(Keys.RightCtrl);

                        var entityUnderMouse = Gizmos.GetContentEntityUnderMouse();

                        if (entityUnderMouse == null)
                        {
                            var entityPicked = Pick();
                            entityUnderMouse = entityPicked.Entity;

                            // Check for instancing
                            var instancingComponent = entityUnderMouse?.Get<InstancingComponent>();
                            if (instancingComponent != null && instancingComponent.Enabled && instancingComponent.Type is InstancingEntityTransform instancing)
                            {
                                entityUnderMouse = instancing.GetInstanceAt(entityPicked.InstanceId)?.Entity ?? entityUnderMouse;
                            }
                        }

                        // Ctrl + click on an empty area: do nothing
                        if (entityUnderMouse == null && addToSelection)
                            continue;

                        // Click on an empty area: clear selection
                        if (entityUnderMouse == null)
                            Clear();
                        // Click on an entity: select this entity
                        else if (!addToSelection)
                            Set(entityUnderMouse);
                        // Ctrl + click on an already selected entity: unselect this entity
                        else if (Contains(entityUnderMouse))
                            Remove(entityUnderMouse);
                        // Ctrl + click on an entity: add this entity to the selection
                        else
                            Add(entityUnderMouse);
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            game = (EntityHierarchyEditorGame)editorGame;

            Editor.SelectedContent.CollectionChanged += SelectedContentChanged;
            game.Script.AddTask(Execute);
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public override void UpdateGraphicsCompositor(EditorServiceGame game)
        {
            base.UpdateGraphicsCompositor(game);

            var pickingRenderStage = new RenderStage("Picking", "Picking");
            game.SceneSystem.GraphicsCompositor.RenderStages.Add(pickingRenderStage);

            pickingRenderStage.Filter = new PickingFilter(this);

            // Meshes
            var meshRenderFeature = game.SceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().FirstOrDefault();
            // TODO: Complain (log) if there is no MeshRenderFeature
            if (meshRenderFeature != null)
            {
                meshRenderFeature.RenderFeatures.Add(new PickingRenderFeature());
                meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
                {
                    EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect + ".Picking",
                    RenderStage = pickingRenderStage,
                    RenderGroup = RenderGroupMask.All
                });
            }

            // Sprites
            var spriteRenderFeature = game.SceneSystem.GraphicsCompositor.RenderFeatures.OfType<SpriteRenderFeature>().FirstOrDefault();
            // TODO: Complain (log) if there is no SpriteRenderFeature
            if (spriteRenderFeature != null)
            {
                spriteRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
                {
                    EffectName = "Test",
                    RenderStage = pickingRenderStage,
                    RenderGroup = RenderGroupMask.All
                });
            }

            // TODO: SpriteStudio (not here but as a plugin)

            var editorCompositor = (EditorTopLevelCompositor)game.SceneSystem.GraphicsCompositor.Game;
            editorCompositor.PostGizmoCompositors.Add(entityPicker = new PickingSceneRenderer { PickingRenderStage = pickingRenderStage });

            var contentScene = ((EntityHierarchyEditorGame)game).ContentScene;
            if (contentScene != null) entityPicker.CacheScene(contentScene, true);
        }

        /// <inheritdoc/>
        void IEditorGameSelectionViewModelService.AddSelectable(AbsoluteId id)
        {
            Editor.Controller.EnsureAssetAccess();
            Editor.Controller.InvokeAsync(() => SelectableIds.Add(id));
            // Add to game selection, in case it is already selected
            var element = (EntityHierarchyElementViewModel)Editor.FindPartViewModel(id);
            if (element == null)
                return;
            if (Editor.SelectedContent.Contains(element))
            {
                // Retrieve old selection to pass it to the event
                var oldSelectionIds = GetSelectedRootIds();
                AddToSelection(element);
                RaiseSelectionUpdated(oldSelectionIds);
            }
        }

        /// <inheritdoc/>
        void IEditorGameSelectionViewModelService.RemoveSelectable(AbsoluteId id)
        {
            Editor.Controller.EnsureAssetAccess();
            Editor.Controller.InvokeAsync(() => SelectableIds.Remove(id));
            // Remove from game selection, in case it was selected
            var element = (EntityHierarchyElementViewModel)Editor.FindPartViewModel(id);
            if (element == null)
                return;
            // Retrieve old selection to pass it to the event
            var oldSelectionIds = GetSelectedRootIds();
            RemoveFromSelection(element);
            RaiseSelectionUpdated(oldSelectionIds);
        }

        private class PickingFilter : RenderStageFilter
        {
            private readonly EditorGameEntitySelectionService service;

            public PickingFilter(EditorGameEntitySelectionService service)
            {
                this.service = service;
            }

            public override bool IsVisible(RenderObject renderObject, RenderView renderView, RenderViewStage renderViewStage)
            {
                var entity = (renderObject.Source as EntityComponent)?.Entity;
                if (entity != null)
                {
                    var entityId = service.Editor.Controller.GetAbsoluteId(entity);
                    return service.SelectableIds.Contains(entityId);
                }

                return false;
            }
        }
    }
}
