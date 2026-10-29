// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.EditorGame.Tools;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Editor.Engine;
using Stride.Engine;

namespace Stride.Assets.Editor.ViewModels;

public abstract class EntityHierarchyEditorViewModel : AssetCompositeHierarchyEditorViewModel<EntityDesign, Entity, EntityHierarchyViewModel, EntityHierarchyItemViewModel>
{
    private static readonly Logger Log = GlobalLogger.GetLogger("EntityHierarchyEditor");

    private readonly Dictionary<AbsoluteId, GameEditorChangePropagator> propagators = [];
    private object? gameView;
    private EditorViewToolbarViewModel? toolbar;
    private bool isGameLoaded;

    protected EntityHierarchyEditorViewModel(EntityHierarchyViewModel asset)
        : base(asset)
    {
        HierarchyRoot.EntityAdded += EntityAdded;
        HierarchyRoot.EntityRemoved += EntityRemoved;
        FocusOnEntityCommand = new AnonymousTaskCommand<EntityViewModel>(ServiceProvider, FocusOnEntityAsync) { IsEnabled = false };
        FocusOnSelectionCommand = new AnonymousTaskCommand(ServiceProvider, FocusOnSelectionAsync) { IsEnabled = false };
        // Whichever side the selection changes from (the tree or the game)
        SelectedItems.CollectionChanged += SelectedItemsChanged;
    }

    public EntityHierarchyRootViewModel HierarchyRoot => (EntityHierarchyRootViewModel)RootPart;

    /// <summary>
    /// Gets the control that shows the game of the editor, or <c>null</c> while the game has not started, or when the
    /// editor has no game (the session has no game viewport, the game could not start).
    /// </summary>
    public object? GameView { get => gameView; private set => SetValue(ref gameView, value); }

    /// <summary>
    /// Gets the toolbar of the view of the game, or <c>null</c> while the game has not loaded, or when the editor has no
    /// game.
    /// </summary>
    public EditorViewToolbarViewModel? Toolbar { get => toolbar; private set => SetValue(ref toolbar, value); }

    /// <summary>
    /// Gets the command that moves the camera of the view to frame the entity given as parameter.
    /// </summary>
    public ICommandBase FocusOnEntityCommand { get; }

    /// <summary>
    /// Gets the command that moves the camera of the view to frame the last selected entity (F).
    /// </summary>
    public ICommandBase FocusOnSelectionCommand { get; }

    /// <summary>
    /// Gets the controller of the game of the editor, or <c>null</c> when the editor has no game.
    /// </summary>
    protected EntityHierarchyEditorController? Controller { get; private set; }

    /// <summary>
    /// Starts the game of the editor and loads the hierarchy into it, when the session can show a game.
    /// </summary>
    /// <returns><c>false</c> when the game could not start; the editor still works without it.</returns>
    public override async Task<bool> Initialize()
    {
        if (!await base.Initialize())
            return false;

        // Without a viewport (no graphics device, previews turned off) the editor has no game view
        if (ServiceProvider.TryGet<IEditorGameViewportService>() is not { } viewportService)
            return true;

        try
        {
            if (CreateController(viewportService) is not { } controller)
                return true;

            Controller = controller;
            if (!await controller.StartGame())
                return false;
            GameView = controller.View;

            // Wait for the game to be fully initialized (including services).
            await controller.GameContentLoaded;
            if (IsDestroyed)
                return false;

            if (!await controller.CreateScene())
                return false;

            // From now on, the entities the asset gets or loses are added to or removed from the game
            isGameLoaded = true;
            await OnGameContentLoaded();
            UpdateFocusCommands();

            if (controller.GetService<EditorGameToolService>() is { } toolService)
            {
                var gameToolbar = new EditorViewToolbarViewModel(ServiceProvider, toolService, controller.GetService<IEditorGameSelectionViewModelService>(), controller.GetService<EditorGameMaterialHighlightService>());
                await gameToolbar.InitializeAsync();
                if (IsDestroyed)
                    gameToolbar.Destroy();
                else
                    Toolbar = gameToolbar;
            }
            return true;
        }
        catch (Exception e) when (!IsDestroyed)
        {
            Log.Error($"The game of the editor of {Asset.Url} could not start.", e);
            GameView = null;
            return false;
        }
        catch (Exception) when (IsDestroyed)
        {
            // The editor was closed while its game started
            return false;
        }
    }

    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(EntityHierarchyEditorViewModel));

        HierarchyRoot.EntityAdded -= EntityAdded;
        HierarchyRoot.EntityRemoved -= EntityRemoved;
        SelectedItems.CollectionChanged -= SelectedItemsChanged;
        foreach (var propagator in propagators.Values)
            propagator.Destroy();
        propagators.Clear();
        Toolbar?.Destroy();
        Toolbar = null;
        GameView = null;
        Controller?.Destroy();

        base.Destroy();
    }

    /// <summary>
    /// Lets the game render: its view is shown.
    /// </summary>
    public void ShowGame()
    {
        Controller?.OnShowGame();
    }

    /// <summary>
    /// Throttles the game: its view is hidden.
    /// </summary>
    public void HideGame()
    {
        Controller?.OnHideGame();
    }

    /// <summary>
    /// Writes the transformations of entities moved in the game (by a transformation gizmo) back to the asset, as one
    /// undoable operation.
    /// </summary>
    /// <param name="transformations">The new transformations, by entity.</param>
    public void UpdateTransformations(IReadOnlyDictionary<AbsoluteId, TransformationTRS> transformations)
    {
        var undoRedoService = Session.UndoRedoService;
        using var transaction = undoRedoService.CreateTransaction();
        foreach (var (id, transformation) in transformations)
        {
            if (FindPartViewModel(id) is not EntityViewModel entity)
                continue;

            var node = Session.AssetNodeContainer.GetNode(entity.AssetSideEntity.Transform);
            // Update properties only when they actually changed
            if ((Vector3)node[nameof(TransformComponent.Position)].Retrieve()! != transformation.Position)
                node[nameof(TransformComponent.Position)].Update(transformation.Position);
            if ((Quaternion)node[nameof(TransformComponent.Rotation)].Retrieve()! != transformation.Rotation)
                node[nameof(TransformComponent.Rotation)].Update(transformation.Rotation);
            if ((Vector3)node[nameof(TransformComponent.Scale)].Retrieve()! != transformation.Scale)
                node[nameof(TransformComponent.Scale)].Update(transformation.Scale);
        }
        undoRedoService.SetName(transaction, "Update transformation");
    }

    /// <summary>
    /// Moves the camera of the view to frame an entity, keeping the direction of the camera (UI thread). In the material
    /// selection mode, the camera frames the mesh of the highlighted material instead, as in the WPF scene editor.
    /// </summary>
    /// <returns>A task that completes once the camera has moved, or at once when the editor has no game.</returns>
    public Task FocusOnEntityAsync(EntityViewModel entity)
    {
        if (!isGameLoaded || Controller is not { } controller || controller.GetService<EditorGameEntityCameraService>() is not { } camera)
            return Task.CompletedTask;

        var materialHighlight = controller.GetService<EditorGameMaterialHighlightService>();
        var id = entity.Id;
        return controller.InvokeAsync(() =>
        {
            if (materialHighlight?.HighlightedMesh is { } mesh)
                camera.SetTarget(mesh.Entity, false, mesh.MeshIndex);
            else if (controller.FindGameSidePart(id) is Entity target)
                camera.SetTarget(target, false);
        });
    }

    /// <summary>
    /// Creates the controller of the game of the editor (UI thread).
    /// </summary>
    /// <returns>The controller, or <c>null</c> when this editor has no game.</returns>
    protected virtual EntityHierarchyEditorController? CreateController(IEditorGameViewportService viewportService)
    {
        return null;
    }

    /// <summary>
    /// Called once the game has loaded its content and created its scene: the hierarchy can be loaded into it.
    /// </summary>
    protected virtual Task OnGameContentLoaded()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Links the given entities, whose game-side clones were just added to the game, to the game side, and loads the
    /// content they reference.
    /// </summary>
    /// <returns>A task that completes once every entity is linked.</returns>
    protected Task LinkToGameSide(IEnumerable<EntityViewModel> entities)
    {
        if (Controller is not { } controller)
            return Task.CompletedTask;

        var selection = controller.GetService<IEditorGameSelectionViewModelService>();
        var added = new List<GameEditorChangePropagator>();
        foreach (var entity in entities)
        {
            var propagator = new EntityHierarchyElementChangePropagator(this, controller, entity.Asset, entity.Id, entity.AssetSideEntity);
            propagators.Add(entity.Id, propagator);
            added.Add(propagator);
            propagator.NotifyGameSidePartAdded();
            // The clone of the entity is in the game: it can be selected there
            selection?.AddSelectable(entity.Id);
        }
        return Task.WhenAll(added.Select(x => x.Initialized));
    }

    /// <summary>
    /// Adds to the game an entity the asset just got, with its children.
    /// </summary>
    private async void EntityAdded(object? sender, EntityHierarchyChangedEventArgs e)
    {
        if (!isGameLoaded || Controller is not { } controller)
            return;

        try
        {
            await controller.AddPart(e.Parent, e.Entity.AssetSideEntity);
            // The entity was removed again while its clone was added: the removal already took the clone out
            if (IsDestroyed || e.Entity.Parent is null)
                return;

            await LinkToGameSide(SelfAndDescendants(e.Entity));
        }
        catch (Exception ex) when (!IsDestroyed && e.Entity.Parent is not null)
        {
            Log.Error($"The entity {e.Entity.Id} could not be added to the game.", ex);
        }
        catch (Exception) when (IsDestroyed || e.Entity.Parent is null)
        {
            // The editor was closed, or the entity removed, while the entity was added
        }
    }

    /// <summary>
    /// Removes from the game an entity the asset just lost, with its children.
    /// </summary>
    private async void EntityRemoved(object? sender, EntityHierarchyChangedEventArgs e)
    {
        var entities = SelfAndDescendants(e.Entity).ToList();
        var selection = isGameLoaded ? Controller?.GetService<IEditorGameSelectionViewModelService>() : null;
        foreach (var entity in entities)
        {
            if (propagators.Remove(entity.Id, out var propagator))
                propagator.Destroy();
            selection?.RemoveSelectable(entity.Id);
        }
        foreach (var entity in entities)
        {
            SelectedContent.Remove(entity);
        }

        if (!isGameLoaded || Controller is not { } controller)
            return;

        try
        {
            await controller.RemovePart(e.Parent, e.Entity.AssetSideEntity);
        }
        catch (Exception ex) when (!IsDestroyed)
        {
            Log.Error($"The entity {e.Entity.Id} could not be removed from the game.", ex);
        }
        catch (Exception) when (IsDestroyed)
        {
            // The editor was closed while the entity was removed
        }
    }

    private static IEnumerable<EntityViewModel> SelfAndDescendants(EntityViewModel entity)
    {
        return new EntityHierarchyItemViewModel[] { entity }.BreadthFirst(x => x.Children).OfType<EntityViewModel>();
    }

    protected override async Task RefreshEditorProperties()
    {
        EditorProperties.UpdateTypeAndName(SelectedItems, x => "Entity", x => x.Name, "entities");
        await EditorProperties.GenerateSelectionPropertiesAsync(SelectedItems.OfType<EntityViewModel>());
    }

    protected override void SelectedContentCollectionChanged(NotifyCollectionChangedAction action)
    {
        SelectedItems.Clear();
        SelectedItems.AddRange(SelectedContent.Cast<EntityHierarchyItemViewModel>().SelectMany(x => x.InnerSubEntities));
    }

    private Task FocusOnSelectionAsync()
    {
        // The entity with the transformation gizmo, as in the WPF scene editor
        return SelectedItems.LastOrDefault() is EntityViewModel entity ? FocusOnEntityAsync(entity) : Task.CompletedTask;
    }

    private void SelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateFocusCommands();

    private void UpdateFocusCommands()
    {
        FocusOnEntityCommand.IsEnabled = isGameLoaded;
        FocusOnSelectionCommand.IsEnabled = isGameLoaded && SelectedItems.LastOrDefault() is EntityViewModel;
    }
}
