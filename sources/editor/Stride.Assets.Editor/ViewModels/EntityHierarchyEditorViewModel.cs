// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.EditorGame.Tools;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Assets.Presentation.ViewModel.CopyPasteProcessors;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Internal;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Translation;
using Stride.Editor.Engine;
using Stride.Engine;

namespace Stride.Assets.Editor.ViewModels;

public abstract class EntityHierarchyEditorViewModel : AssetCompositeHierarchyEditorViewModel<EntityDesign, Entity, EntityHierarchyViewModel, EntityHierarchyItemViewModel>
{
    private static readonly Logger Log = GlobalLogger.GetLogger("EntityHierarchyEditor");

    private readonly Dictionary<AbsoluteId, GameEditorChangePropagator> propagators = [];
    private readonly ModelComponentChangeHandler modelComponentChanges;
    // Latest dragged morph weights, waiting for the next game frame
    private readonly Dictionary<(AbsoluteId Entity, Guid Component, string Target), float> previewWeights = [];
    private bool previewPosted;
    private object? gameView;
    private EditorViewToolbarViewModel? toolbar;
    private bool isGameLoaded;
    private string filterPattern = string.Empty;

    protected EntityHierarchyEditorViewModel(EntityHierarchyViewModel asset)
        : base(asset)
    {
        HierarchyRoot.EntityAdded += EntityAdded;
        HierarchyRoot.EntityRemoved += EntityRemoved;
        HierarchyRoot.LockChanged += LockChanged;
        // The root is expanded when the editor opens, as in the WPF scene editor
        HierarchyRoot.IsExpanded = true;
        FocusOnEntityCommand = new AnonymousTaskCommand<EntityViewModel>(ServiceProvider, FocusOnEntityAsync) { IsEnabled = false };
        FocusOnSelectionCommand = new AnonymousTaskCommand(ServiceProvider, FocusOnSelectionAsync) { IsEnabled = false };
        // Whichever side the selection changes from (the tree or the game)
        SelectedItems.CollectionChanged += SelectedItemsChanged;
        CreateEntityInRootCommand = new AnonymousTaskCommand<IEntityFactory?>(ServiceProvider, x => CreateEntity(x, HierarchyRoot));
        CreateEntityInSelectionCommand = new AnonymousTaskCommand<IEntityFactory?>(ServiceProvider, x => CreateEntity(x, SelectedContent.FirstOrDefault() as EntityHierarchyItemViewModel ?? HierarchyRoot));
        CreateFolderInRootCommand = new AnonymousCommand(ServiceProvider, () => CreateFolder(HierarchyRoot, true));
        CreateFolderInSelectionCommand = new AnonymousCommand(ServiceProvider, () => CreateFolder(SelectedContent.FirstOrDefault() as EntityHierarchyItemViewModel ?? HierarchyRoot, true));
        RenameCommand = new AnonymousCommand(ServiceProvider, Rename);
        CreatePrefabFromSelectionCommand = new AnonymousCommand(ServiceProvider, CreatePrefabFromSelection);
        OpenPrefabEditorCommand = new AnonymousCommand(ServiceProvider, OpenPrefabEditor);
        SelectPrefabCommand = new AnonymousCommand(ServiceProvider, SelectPrefab);
        BreakLinkToPrefabCommand = new AnonymousCommand(ServiceProvider, () => BreakLinkToBase("prefab"));
        UpdateCommands();
        modelComponentChanges = new ModelComponentChangeHandler(asset);
    }

    public EntityHierarchyRootViewModel HierarchyRoot => (EntityHierarchyRootViewModel)RootPart;

    /// <summary>
    /// Gets the command that creates an entity at the root of the hierarchy, with the factory given as parameter, or an
    /// empty entity when the parameter is <c>null</c>.
    /// </summary>
    public ICommandBase CreateEntityInRootCommand { get; }

    /// <summary>
    /// Gets the command that creates an entity in the first selected item (an entity or a folder), or at the root of the
    /// hierarchy when nothing is selected, with the factory given as parameter, or an empty entity when the parameter is
    /// <c>null</c>.
    /// </summary>
    public ICommandBase CreateEntityInSelectionCommand { get; }

    /// <summary>
    /// Gets the command that creates a prefab from the selected entities, with their children, next to the edited asset;
    /// the selected entities become instances of the prefab.
    /// </summary>
    public ICommandBase CreatePrefabFromSelectionCommand { get; }

    /// <summary>
    /// Gets the command that opens the editor of the prefabs of the selected entities.
    /// </summary>
    public ICommandBase OpenPrefabEditorCommand { get; }

    /// <summary>
    /// Gets the command that selects in the asset view the prefab of the selected entity.
    /// </summary>
    public ICommandBase SelectPrefabCommand { get; }

    /// <summary>
    /// Gets the command that makes the selected entities plain entities, instead of instances of their prefab.
    /// </summary>
    public ICommandBase BreakLinkToPrefabCommand { get; }

    /// <summary>
    /// Gets the command that begins the renaming in place (F2) of the last selected entity or folder.
    /// </summary>
    public ICommandBase RenameCommand { get; }

    /// <summary>
    /// Gets or sets the words the names of the shown items contain, separated by spaces, case ignored. An item that
    /// doesn't match shows when one of its children matches. Empty, every item shows.
    /// </summary>
    public string FilterPattern { get => filterPattern; set => SetValue(ref filterPattern, value ?? string.Empty, UpdateVisibilities); }

    /// <summary>
    /// Gets the command that creates a folder at the root of the hierarchy.
    /// </summary>
    public ICommandBase CreateFolderInRootCommand { get; }

    /// <summary>
    /// Gets the command that creates a folder in the first selected item, or at the root of the hierarchy when nothing is
    /// selected.
    /// </summary>
    public ICommandBase CreateFolderInSelectionCommand { get; }

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
        HierarchyRoot.LockChanged -= LockChanged;
        SelectedItems.CollectionChanged -= SelectedItemsChanged;
        modelComponentChanges.Dispose();
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
    /// Shows a morph weight dragged in the property grid in the game on its next frame, without editing the asset: the
    /// editor sends asset changes to the game one after another, which lags behind a drag. Only the latest weight of each
    /// target is sent.
    /// </summary>
    /// <param name="entityId">The entity of the model component.</param>
    /// <param name="componentId">The model component.</param>
    /// <param name="target">The name of the morph target.</param>
    /// <param name="weight">The weight to show.</param>
    public void PreviewMorphWeight(AbsoluteId entityId, Guid componentId, string target, float weight)
    {
        if (!float.IsFinite(weight) || !isGameLoaded || Controller is not { } controller)
            return;

        lock (previewWeights)
        {
            previewWeights[(entityId, componentId, target)] = weight;
            if (previewPosted)
                return;
            previewPosted = true;
        }
        _ = controller.InvokeAsync(() => ApplyPreviewWeights(controller));
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
            // The clone of the entity is in the game: it can be selected there, unless it is locked
            if (!entity.IsLocked)
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
    /// A locked entity cannot be selected in the game, as in the WPF scene editor: it leaves the selection of the game, and
    /// the gizmos cannot move it. It can still be selected in the hierarchy.
    /// </summary>
    private void LockChanged(object? sender, EntityHierarchyElementEventArgs e)
    {
        if (e.Element is not EntityViewModel entity || !propagators.ContainsKey(entity.Id))
            return;

        var selection = Controller?.GetService<IEditorGameSelectionViewModelService>();
        if (entity.IsLocked)
            selection?.RemoveSelectable(entity.Id);
        else
            selection?.AddSelectable(entity.Id);
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

    // Game thread
    private void ApplyPreviewWeights(EntityHierarchyEditorController controller)
    {
        KeyValuePair<(AbsoluteId Entity, Guid Component, string Target), float>[] weights;
        lock (previewWeights)
        {
            weights = previewWeights.ToArray();
            previewWeights.Clear();
            previewPosted = false;
        }
        // Runtime weights only: the authored weights replace them once the asset change reaches the scene.
        foreach (var ((entityId, componentId, target), weight) in weights)
        {
            if (controller.FindGameSidePart(entityId) is not Entity entity || entity.Components.FirstOrDefault(x => x.Id == componentId) is not ModelComponent component)
                continue;
            var slot = component.FindMorphTarget(target);
            if (slot >= 0)
                component.SetMorphWeight(slot, weight);
        }
    }

    private static IEnumerable<EntityViewModel> SelfAndDescendants(EntityViewModel entity)
    {
        return new EntityHierarchyItemViewModel[] { entity }.BreadthFirst(x => x.Children).OfType<EntityViewModel>();
    }

    /// <summary>
    /// Creates an entity with the given factory, or an empty entity, as the last entity of the given item, selects it and
    /// begins its renaming, as one undoable operation.
    /// </summary>
    /// <param name="factory">The factory of the entity, or <c>null</c> for an empty entity.</param>
    /// <param name="parent">The root of the hierarchy, an entity or a folder.</param>
    /// <returns>The view model of the new entity, or <c>null</c> when the factory created nothing (the user cancelled).</returns>
    public async Task<EntityViewModel?> CreateEntity(IEntityFactory? factory, EntityHierarchyItemViewModel parent)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var entity = factory != null ? await factory.CreateEntity(new EntityHierarchyItemAdapter(parent)) : new Entity { Name = "Entity" };
        if (entity is null)
            return null;

        // The item ids of the collections of the new entity exist before it goes into the asset
        AssetCollectionItemIdHelper.GenerateMissingItemIds(entity);

        if (entity.Transform.Children.Count > 0)
            throw new InvalidOperationException("Entity factories should create single entity. Creating a hierarchy is not supported.");

        var asset = (EntityHierarchyViewModel)parent.Asset;
        var collection = new AssetPartCollection<EntityDesign, Entity> { new EntityDesign(entity, (parent as EntityFolderViewModel)?.Path ?? "") };
        var undoRedo = Session.UndoRedoService;
        using (var transaction = undoRedo.CreateTransaction())
        {
            asset.AssetHierarchyPropertyGraph.AddPartToAsset(collection, collection.Single().Value, (parent.Owner as EntityViewModel)?.AssetSideEntity, parent.Owner.EntityCount);
            undoRedo.SetName(transaction, $"Create entity '{entity.Name}'");
        }

        // The new entity is selected, and its name edited
        var newEntity = (EntityViewModel)FindPartViewModel(new AbsoluteId(asset.Id, entity.Id))!;
        ClearSelection();
        SelectedContent.Add(newEntity);
        newEntity.IsEditing = true;
        return newEntity;
    }

    /// <summary>
    /// Creates a folder in the given item, with a name no other folder of the item has, and selects it, as one undoable
    /// operation.
    /// </summary>
    /// <param name="parent">The root of the hierarchy, an entity or a folder.</param>
    /// <param name="editName">Whether to begin the renaming of the folder.</param>
    /// <returns>The new folder.</returns>
    public EntityFolderViewModel CreateFolder(EntityHierarchyItemViewModel parent, bool editName)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var asset = (EntityHierarchyViewModel)parent.Asset;
        var undoRedo = Session.UndoRedoService;
        EntityFolderViewModel folder;
        var transaction = undoRedo.UndoRedoInProgress ? null : undoRedo.CreateTransaction();
        try
        {
            folder = new EntityFolderViewModel(asset, NamingHelper.ComputeNewName("Folder", parent.Folders, x => x.Name), []);
            parent.Folders.Add(folder);
            parent.IsExpanded = true;

            if (transaction != null)
            {
                undoRedo.PushOperation(new EntityFolderOperation(asset, EntityFolderOperation.Action.FolderCreated, folder.Path, parent.Owner.Id));
                undoRedo.SetName(transaction, $"Create folder '{folder.Name}'");
            }
        }
        finally
        {
            transaction?.Dispose();
        }

        // The new folder is selected
        ClearSelection();
        SelectedContent.Add(folder);
        folder.IsEditing = editName;
        return folder;
    }

    /// <summary>
    /// Adds a copy of each selected entity, with its children, right after it, and selects the copies, as one undoable
    /// operation (Ctrl+D, or Ctrl+drag in the view).
    /// </summary>
    /// <returns>The copies.</returns>
    public ISet<EntityViewModel> DuplicateSelectedEntities()
    {
        // The selected entities without a selected ancestor
        var entitiesToDuplicate = GetCommonRoots(SelectedItems.OfType<EntityViewModel>().ToList());
        if (entitiesToDuplicate.Count == 0)
            return entitiesToDuplicate;

        SelectedItems.Clear();

        HashSet<EntityViewModel> duplicatedEntities;
        var undoRedo = Session.UndoRedoService;
        using (var transaction = undoRedo.CreateTransaction())
        {
            duplicatedEntities = [.. entitiesToDuplicate.Select(x => x.Duplicate())];
            undoRedo.SetName(transaction, "Duplicate entities");
        }

        // The copies are selected
        SelectedItems.AddRange(duplicatedEntities);
        return duplicatedEntities;
    }

    /// <inheritdoc/>
    protected override EntityDesign? GetPartDesign(EntityHierarchyItemViewModel item)
    {
        return (item as EntityViewModel)?.EntityDesign;
    }

    /// <inheritdoc/>
    protected override (Type TargetMemberType, Type[] ExpectedTypes) GetPasteTypes(bool asRoot)
    {
        // Entities go into the root or into an entity, and components into an entity
        return (asRoot ? typeof(AssetCompositeHierarchyData<EntityDesign, Entity>) : typeof(Entity), [typeof(AssetCompositeHierarchyData<EntityDesign, Entity>), typeof(EntityComponent)]);
    }

    /// <inheritdoc/>
    protected override bool CanDelete()
    {
        return SelectedContent.Count > 0 && !SelectedContent.Contains(HierarchyRoot);
    }

    /// <inheritdoc/>
    protected override async Task Delete()
    {
        if (!CanDelete())
            return;

        var entitiesToDelete = GetCommonRoots(SelectedItems.OfType<EntityViewModel>().ToList());
        if (SceneEditorSettings.AskBeforeDeletingEntities.GetValue())
        {
            var confirmMessage = Tr._p("Message", "Are you sure you want to delete this entity?");
            // TODO: we should compute the actual total number of entities to be deleted here (children recursively, etc.)
            if (entitiesToDelete.Count > 1)
                confirmMessage = string.Format(Tr._p("Message", "Are you sure you want to delete these {0} entities?"), entitiesToDelete.Count);
            var checkedMessage = string.Format(Core.Assets.Editor.Settings.EditorSettings.AlwaysDeleteWithoutAsking, "entities");
            var buttons = DialogButtons.Create([Tr._p("Button", "Delete"), Tr._p("Button", "Cancel")], 1, 2);
            var result = await ServiceProvider.Get<IDialogService>().CheckedMessageBoxAsync(confirmMessage, false, checkedMessage, buttons, MessageBoxImage.Question);
            if (result.Result != 1)
                return;
            if (result.IsChecked == true)
            {
                SceneEditorSettings.AskBeforeDeletingEntities.SetValue(false);
                SceneEditorSettings.Save();
            }
        }

        var undoRedo = Session.UndoRedoService;
        using var transaction = undoRedo.CreateTransaction();
        var foldersToDelete = SelectedContent.OfType<EntityFolderViewModel>().ToList();
        ClearSelection();

        // The entities first
        foreach (var entities in entitiesToDelete.GroupBy(x => (EntityHierarchyViewModel)x.Asset))
        {
            entities.Key.AssetHierarchyPropertyGraph.DeleteParts(entities.Select(x => x.EntityDesign), out var mapping);
            undoRedo.PushOperation(new DeletedPartsTrackingOperation<EntityDesign, Entity>(entities.Key, mapping));
        }

        // Then the folders, empty now; a folder inside another deleted folder goes with it
        foreach (var folder in GetCommonRoots(foldersToDelete))
        {
            folder.Delete();
        }

        undoRedo.SetName(transaction, "Delete selected entities");
    }

    /// <inheritdoc/>
    protected override ISet<EntityHierarchyItemViewModel> DuplicateSelection()
    {
        return DuplicateSelectedEntities().Cast<EntityHierarchyItemViewModel>().ToHashSet();
    }

    /// <inheritdoc/>
    protected override void PrepareToCopy(AssetCompositeHierarchyData<EntityDesign, Entity> clonedHierarchy, ICollection<EntityHierarchyItemViewModel> commonRoots, ICollection<EntityHierarchyItemViewModel> commonParts)
    {
        // No folder by default
        foreach (var entity in clonedHierarchy.Parts.Values)
        {
            entity.Folder = null;
        }

        // The folders of the copied folders, relative to them
        foreach (var item in commonRoots.OfType<EntityFolderViewModel>())
        {
            var parentFolderPathLength = 1 + ((item.Parent as EntityFolderViewModel)?.Path.Length ?? -1);
            foreach (var entity in item.InnerSubEntities)
            {
                clonedHierarchy.Parts[entity.Id.ObjectId].Folder = entity.EntityDesign.Folder[parentFolderPathLength..];
            }
        }
        base.PrepareToCopy(clonedHierarchy, commonRoots, commonParts);
    }

    /// <inheritdoc/>
    protected override void AttachPropertiesForPaste(ref PropertyContainer propertyContainer, EntityHierarchyItemViewModel pasteTarget)
    {
        // Into a folder: the pasted entities go to that folder
        if ((pasteTarget as EntityFolderViewModel)?.Path is { Length: > 0 } folder)
        {
            propertyContainer.Add(EntityHierarchyPasteProcessor.TargetFolderKey, folder);
        }
        base.AttachPropertiesForPaste(ref propertyContainer, pasteTarget);
    }

    /// <inheritdoc/>
    protected override void UpdateCommands()
    {
        base.UpdateCommands();

        DuplicateSelectionCommand.IsEnabled = GetCommonRoots(SelectedContent.OfType<EntityHierarchyItemViewModel>().ToList()).Count > 0;
        RenameCommand.IsEnabled = SelectedContent.LastOrDefault() is EntityHierarchyItemViewModel { IsEditable: true };

        var prefabInstanceSelected = SelectedItems.OfType<EntityViewModel>().Any(x => x.SourcePrefab != null);
        CreatePrefabFromSelectionCommand.IsEnabled = SelectedItems.Count >= 1;
        BreakLinkToPrefabCommand.IsEnabled = prefabInstanceSelected;
        OpenPrefabEditorCommand.IsEnabled = prefabInstanceSelected;
        SelectPrefabCommand.IsEnabled = prefabInstanceSelected && SelectedItems.Count == 1;
    }

    private void CreatePrefabFromSelection()
    {
        CreateAssetFromSelectedParts(() => new PrefabAsset(), e => e?.Name ?? "Prefab", true, out _);
    }

    private void OpenPrefabEditor()
    {
        foreach (var prefab in SelectedItems.OfType<EntityViewModel>().Select(x => x.SourcePrefab).NotNull().Distinct())
        {
            Session.EditorCollection.OpenAssetEditor(prefab);
        }
    }

    private void SelectPrefab()
    {
        // The entity with the transformation gizmo, as in the WPF scene editor
        if ((SelectedItems.LastOrDefault() as EntityViewModel)?.SourcePrefab is { } prefab)
            Session.AssetCollection.SelectAssets(prefab.Yield());
    }

    private void Rename()
    {
        if (SelectedContent.LastOrDefault() is EntityHierarchyItemViewModel { IsEditable: true } item)
            item.IsEditing = true;
    }

    private void UpdateVisibilities()
    {
        var filterTokens = filterPattern.Split(" \t\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in HierarchyRoot.Children)
        {
            UpdateVisibilities(item, filterTokens);
        }
    }

    private static void UpdateVisibilities(EntityHierarchyItemViewModel item, string[] filterTokens)
    {
        // An item shows when one of its children shows, or when its name contains every word of the filter
        var isVisible = false;
        foreach (var child in item.Children)
        {
            UpdateVisibilities(child, filterTokens);
            isVisible |= child.IsVisible;
        }
        item.IsVisible = isVisible || filterTokens.All(x => item.Name?.Contains(x, StringComparison.OrdinalIgnoreCase) == true);
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
