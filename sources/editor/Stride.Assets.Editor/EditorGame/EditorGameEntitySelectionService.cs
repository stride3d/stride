// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.BuildEngine;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Sprites;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// Selects the entities of the scene editor in its game: the entities selected in the hierarchy show selected in the game
/// (their gizmos, the wireframe of their models), and a click in the game selects the entity under the mouse in the
/// hierarchy, Ctrl+click adds it to the selection or removes it.
/// </summary>
/// <remarks>
/// The cross-platform counterpart of the WPF <c>EditorGameEntitySelectionService</c>. The scene roots are not selected in
/// the game, and duplicating the selection is not supported yet.
/// </remarks>
[EditorGameService(typeof(EntityHierarchyEditorController), Order = 200)]
public sealed class EditorGameEntitySelectionService : EditorGameMouseServiceBase, IEditorGameEntitySelectionService, IEditorGameSelectionViewModelService
{
    private readonly EntityHierarchyEditorController controller;
    private readonly EntityHierarchyEditorViewModel editor;
    private readonly EntitySelection selection = new();
    private Vector2 mouseMoveAccumulator;
    private PickingSceneRenderer? entityPicker;
    private EntityHierarchyEditorGame? game;

    public EditorGameEntitySelectionService(EntityHierarchyEditorController controller, EntityHierarchyEditorViewModel editor)
    {
        this.controller = controller;
        this.editor = editor;
    }

    /// <inheritdoc/>
    public override bool IsControllingMouse { get; protected set; }

    /// <inheritdoc/>
    public int SelectedIdCount => selection.Count;

    /// <inheritdoc/>
    public int SelectedRootIdCount => selection.RootCount;

    /// <inheritdoc/>
    /// <remarks>
    /// Shown by default, as in the default WPF scene settings.
    /// </remarks>
    public bool DisplaySelectionMask { get; set; } = true;

    /// <inheritdoc/>
    public event EventHandler<EntitySelectionEventArgs>? SelectionUpdated;

    /// <inheritdoc/>
    public override IEnumerable<Type> Dependencies { get { yield return typeof(IEditorGameComponentGizmoService); } }

    private IEditorGameComponentGizmoService Gizmos => Services.Get<IEditorGameComponentGizmoService>();

    /// <inheritdoc/>
    public IReadOnlyCollection<AbsoluteId> GetSelectedIds() => selection.GetSelectedIds();

    /// <inheritdoc/>
    public IReadOnlyCollection<AbsoluteId> GetSelectedRootIds() => selection.GetSelectedRootIds();

    /// <inheritdoc/>
    public EntityPickingResult Pick()
    {
        return entityPicker?.Pick() ?? default;
    }

    /// <summary>
    /// Finds the entity under the mouse (game thread): the entity of a component gizmo, else the entity of the picked
    /// mesh or sprite, or the picked instance of an instancing entity.
    /// </summary>
    /// <param name="pickingResult">The result of the picking of the meshes and sprites.</param>
    /// <returns>The entity under the mouse, or <c>null</c>.</returns>
    public Entity? GetEntityUnderMouse(out EntityPickingResult pickingResult)
    {
        pickingResult = Pick();
        if (Gizmos.GetContentEntityUnderMouse() is { } gizmoEntity)
            return gizmoEntity;

        var entity = pickingResult.Entity;
        // Check for instancing
        if (entity?.Get<InstancingComponent>() is { Enabled: true, Type: InstancingEntityTransform instancing })
            entity = instancing.GetInstanceAt(pickingResult.InstanceId)?.Entity ?? entity;
        return entity;
    }

    /// <summary>
    /// Selects the clicked entity in the hierarchy (game thread), as a click in the WPF scene editor: a click on an entity
    /// selects it alone, a click elsewhere empties the selection, and Ctrl+click adds the entity to the selection or
    /// removes it.
    /// </summary>
    /// <param name="entity">The clicked entity, or <c>null</c> for a click on an empty area.</param>
    /// <param name="addToSelection">Whether Ctrl is down.</param>
    public void Select(Entity? entity, bool addToSelection)
    {
        switch (entity, addToSelection)
        {
            case (null, true):
                // Ctrl + click on an empty area: do nothing
                break;
            case (null, false):
                Clear();
                break;
            case (_, false):
                Set(entity);
                break;
            case (_, true) when selection.Contains(controller.GetAbsoluteId(entity)):
                Remove(entity);
                break;
            default:
                Add(entity);
                break;
        }
    }

    /// <inheritdoc/>
    public Task<bool> DuplicateSelection()
    {
        // The scene editor can't duplicate entities yet
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public override ValueTask DisposeAsync()
    {
        EnsureNotDestroyed(nameof(EditorGameEntitySelectionService));

        editor.SelectedContent.CollectionChanged -= SelectedContentChanged;
        SelectionUpdated -= UpdateGizmoSelection;
        if (game?.SceneSystem.SceneInstance is { } sceneInstance)
        {
            sceneInstance.EntityAdded -= CacheEntity;
            sceneInstance.EntityRemoved -= UncacheEntity;
            sceneInstance.ComponentChanged -= UpdateCachedComponent;
        }
        return base.DisposeAsync();
    }

    /// <inheritdoc/>
    public override void RegisterScene(Scene scene)
    {
        base.RegisterScene(scene);
        entityPicker?.CacheScene(scene, true);

        var sceneInstance = Game.SceneSystem.SceneInstance;
        sceneInstance.EntityAdded += CacheEntity;
        sceneInstance.EntityRemoved += UncacheEntity;
        sceneInstance.ComponentChanged += UpdateCachedComponent;
        SelectionUpdated += UpdateGizmoSelection;
    }

    /// <inheritdoc/>
    public override void UpdateGraphicsCompositor(EditorServiceGame editorGame)
    {
        base.UpdateGraphicsCompositor(editorGame);

        var pickingRenderStage = new RenderStage("Picking", "Picking") { Filter = new PickingFilter(this) };
        editorGame.SceneSystem.GraphicsCompositor.RenderStages.Add(pickingRenderStage);

        // Meshes
        if (editorGame.SceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().FirstOrDefault() is { } meshRenderFeature)
        {
            meshRenderFeature.RenderFeatures.Add(new PickingRenderFeature());
            meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect + ".Picking",
                RenderStage = pickingRenderStage,
                RenderGroup = RenderGroupMask.All,
            });
        }

        // Sprites
        if (editorGame.SceneSystem.GraphicsCompositor.RenderFeatures.OfType<SpriteRenderFeature>().FirstOrDefault() is { } spriteRenderFeature)
        {
            spriteRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                EffectName = "Test",
                RenderStage = pickingRenderStage,
                RenderGroup = RenderGroupMask.All,
            });
        }

        var editorCompositor = (EditorTopLevelCompositor)editorGame.SceneSystem.GraphicsCompositor.Game;
        editorCompositor.PostGizmoCompositors.Add(entityPicker = new PickingSceneRenderer { PickingRenderStage = pickingRenderStage });

        if (((EntityHierarchyEditorGame)editorGame).ContentScene is { } contentScene)
            entityPicker.CacheScene(contentScene, true);
    }

    /// <inheritdoc/>
    public void AddSelectable(AbsoluteId id)
    {
        controller.EnsureAssetAccess();
        selection.AddSelectable(id);
        // The entity may already be selected in the hierarchy
        if (editor.FindPartViewModel(id) is EntityViewModel entity && editor.SelectedContent.Contains(entity))
        {
            var oldSelectionIds = selection.GetSelectedRootIds();
            if (selection.Add(entity))
                RaiseSelectionUpdated(oldSelectionIds);
        }
    }

    /// <inheritdoc/>
    public void RemoveSelectable(AbsoluteId id)
    {
        controller.EnsureAssetAccess();
        selection.RemoveSelectable(id);
        // The entity may be selected
        if (editor.FindPartViewModel(id) is EntityViewModel entity)
        {
            var oldSelectionIds = selection.GetSelectedRootIds();
            if (selection.Remove(entity))
                RaiseSelectionUpdated(oldSelectionIds);
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> Initialize(EditorServiceGame editorGame)
    {
        game = (EntityHierarchyEditorGame)editorGame;

        editor.SelectedContent.CollectionChanged += SelectedContentChanged;
        game.Script.AddTask(Execute);
        return Task.FromResult(true);
    }

    private EntityHierarchyEditorGame Game => game ?? throw new InvalidOperationException("The service is not initialized.");

    private void CacheEntity(object? sender, Entity entity) => entityPicker?.CacheEntity(entity, false);

    private void UncacheEntity(object? sender, Entity entity) => entityPicker?.UncacheEntity(entity, false);

    private void UpdateCachedComponent(object? sender, EntityComponentEventArgs e)
    {
        if (e.PreviousComponent != null)
            entityPicker?.UncacheEntityComponent(e.PreviousComponent);
        if (e.NewComponent != null)
            entityPicker?.CacheEntityComponent(e.NewComponent);
    }

    /// <summary>
    /// Empties the selection of the hierarchy.
    /// </summary>
    private void Clear()
    {
        if (selection.Count == 0)
            return;

        UpdateHierarchySelection(selectedContent => selectedContent.Clear());
    }

    /// <summary>
    /// Selects the given entity alone in the hierarchy.
    /// </summary>
    private void Set(Entity entity)
    {
        var entityId = controller.GetAbsoluteId(entity);
        if (!selection.IsSelectable(entityId) || selection.Count == 1 && selection.Contains(entityId))
            return;

        UpdateHierarchySelection(selectedContent =>
        {
            selectedContent.Clear();
            if (editor.FindPartViewModel(entityId) is EntityViewModel viewModel)
                selectedContent.Add(viewModel);
        });
    }

    /// <summary>
    /// Adds the given entity to the selection of the hierarchy.
    /// </summary>
    private void Add(Entity entity)
    {
        var entityId = controller.GetAbsoluteId(entity);
        if (!selection.IsSelectable(entityId) || selection.Contains(entityId))
            return;

        UpdateHierarchySelection(selectedContent =>
        {
            if (editor.FindPartViewModel(entityId) is EntityViewModel viewModel)
                selectedContent.Add(viewModel);
        });
    }

    /// <summary>
    /// Removes the given entity from the selection of the hierarchy.
    /// </summary>
    private void Remove(Entity entity)
    {
        var entityId = controller.GetAbsoluteId(entity);
        if (!selection.Contains(entityId))
            return;

        UpdateHierarchySelection(selectedContent =>
        {
            if (editor.FindPartViewModel(entityId) is EntityViewModel viewModel)
                selectedContent.Remove(viewModel);
        });
    }

    /// <summary>
    /// Changes the selection of the hierarchy (UI thread), keeping the mouse until it is done.
    /// </summary>
    private void UpdateHierarchySelection(Action<ICollection<object>> update)
    {
        IsControllingMouse = true;
        editor.Dispatcher.InvokeAsync(() =>
        {
            update(editor.SelectedContent);
            controller.InvokeAsync(() => IsControllingMouse = false);
        });
    }

    private void SelectedContentChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var oldSelectionIds = selection.GetSelectedRootIds();
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (var entity in e.NewItems!.OfType<EntityViewModel>())
                    selection.Add(entity);
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (var entity in e.OldItems!.OfType<EntityViewModel>())
                    selection.Remove(entity);
                break;
            case NotifyCollectionChangedAction.Reset:
                selection.Clear();
                break;
            default:
                throw new NotSupportedException($"The selection doesn't support the {e.Action} change.");
        }
        RaiseSelectionUpdated(oldSelectionIds);
    }

    private void RaiseSelectionUpdated(IReadOnlyCollection<AbsoluteId> oldSelectionIds)
    {
        var newSelectionIds = selection.GetSelectedRootIds();
        controller.InvokeAsync(() =>
        {
            var oldSelection = oldSelectionIds.Select(controller.FindGameSidePart).OfType<Entity>().ToList();
            var newSelection = newSelectionIds.Select(controller.FindGameSidePart).OfType<Entity>().ToList();
            SelectionUpdated?.Invoke(this, new EntitySelectionEventArgs(oldSelection, newSelection));
        });
    }

    /// <summary>
    /// Shows the gizmos of the selected entities and of their children selected (game thread).
    /// </summary>
    private void UpdateGizmoSelection(object? sender, EntitySelectionEventArgs e)
    {
        var previousSelection = SelfAndDescendants(e.OldSelection).ToHashSet();
        var newSelection = SelfAndDescendants(e.NewSelection).ToHashSet();
        previousSelection.ExceptWith(newSelection);

        foreach (var entity in previousSelection)
        {
            Gizmos.UpdateGizmoEntitiesSelection(entity, false);
        }
        foreach (var entity in newSelection)
        {
            Gizmos.UpdateGizmoEntitiesSelection(entity, true);
        }
    }

    private static IEnumerable<Entity> SelfAndDescendants(IEnumerable<Entity> entities)
    {
        return entities.BreadthFirst(x => x.Transform.Children.Select(y => y.Entity));
    }

    /// <summary>
    /// Selects the entity under the mouse on a click: the left button released without moving the mouse.
    /// </summary>
    private async Task Execute()
    {
        MicrothreadLocalDatabases.MountCommonDatabase();

        while (Game.IsRunning)
        {
            await Game.Script.NextFrame();
            if (!IsActive)
                continue;

            var backBuffer = Game.GraphicsDevice.Presenter.BackBuffer;
            var screenSize = new Vector2(backBuffer.Width, backBuffer.Height);
            // The moves of the frame of the press can come before it: they don't count
            if (Game.Input.IsMouseButtonPressed(MouseButton.Left))
                mouseMoveAccumulator = Vector2.Zero;
            else
                mouseMoveAccumulator += new Vector2(Math.Abs(Game.Input.MouseDelta.X * screenSize.X), Math.Abs(Game.Input.MouseDelta.Y * screenSize.Y));

            if (!IsMouseAvailable || !Game.Input.IsMouseButtonReleased(MouseButton.Left) || Game.Input.IsMouseButtonDown(MouseButton.Right))
                continue;

            // A drag is not a click
            if (mouseMoveAccumulator.Length() >= TransformationGizmo.TransformationStartPixelThreshold)
                continue;

            var addToSelection = Game.Input.IsKeyDown(Keys.LeftCtrl) || Game.Input.IsKeyDown(Keys.RightCtrl);
            Select(GetEntityUnderMouse(out _), addToSelection);
        }
    }

    /// <summary>
    /// Picks only the selectable entities.
    /// </summary>
    private sealed class PickingFilter(EditorGameEntitySelectionService service) : RenderStageFilter
    {
        public override bool IsVisible(RenderObject renderObject, RenderView renderView, RenderViewStage renderViewStage)
        {
            return (renderObject.Source as EntityComponent)?.Entity is { } entity && service.selection.IsSelectable(service.controller.GetAbsoluteId(entity));
        }
    }
}
