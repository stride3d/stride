// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.EntityFactories;
using Stride.Assets.Entities;
using Stride.Assets.Models;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Internal;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Serialization;
using Stride.Core.Translation;
using Stride.Editor.EditorGame.ContentLoader;
using Stride.Editor.Engine;
using Stride.Engine;
using Stride.Assets.Editor.AssetEditors.GameEditor;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.SceneEditor;
using Stride.Assets.Editor.ViewModels.CopyPasteProcessors;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The editor of a scene or a prefab: the hierarchy of its entities and the game that shows them.
/// </summary>
public abstract class EntityHierarchyEditorViewModel : AssetCompositeHierarchyEditorViewModel<EntityDesign, Entity, EntityHierarchyViewModel, EntityViewModel>,
    IEntityHierarchyEditorViewModel, IGameEditorViewModel, IAddChildViewModel
{
    private static readonly Logger Log = GlobalLogger.GetLogger("EntityHierarchyEditor");

    private readonly Dictionary<AbsoluteId, GameEditorChangePropagator> propagators = [];
    // Completes once the game is loaded, or once the editor knows it has none
    private readonly TaskCompletionSource initialized = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private IReadOnlyList<IAddAssetPolicy>? addAssetPolicies;
    private EntityHierarchyRootViewModel? activeRoot;
    private readonly ModelComponentChangeHandler modelComponentChanges;
    // Latest dragged morph weights, waiting for the next game frame
    private readonly Dictionary<(AbsoluteId Entity, Guid Component, string Target), float> previewWeights = [];
    private bool previewPosted;
    private IEditorViewOptions? viewOptions;
    private SceneViewWidgetsViewModel? sceneViewWidgets;
    private bool isGameLoaded;
    private bool compilingAssets;
    private string filterPattern = string.Empty;

    protected EntityHierarchyEditorViewModel(EntityHierarchyViewModel asset)
        : base(asset)
    {
        GameHost = new EditorGameHost(ServiceProvider);
        InitializeRootPart();
        HierarchyRoot.EntityAdded += EntityAdded;
        HierarchyRoot.EntityRemoved += EntityRemoved;
        HierarchyRoot.SelectableChanged += SelectableChanged;
        // The root is expanded when the editor opens, as in the WPF scene editor
        HierarchyRoot.IsExpanded = true;
        FocusOnEntityCommand = new AnonymousTaskCommand<EntityViewModel>(ServiceProvider, FocusOnEntityAsync) { IsEnabled = false };
        FocusOnSelectionCommand = new AnonymousTaskCommand(ServiceProvider, FocusOnSelectionAsync) { IsEnabled = false };
        // Whichever side the selection changes from (the tree or the game)
        SelectedItems.CollectionChanged += SelectedItemsChanged;
        // The entities and folders created without a parent go into the active root, as in the WPF scene editor
        CreateEntityCommand = new AnonymousTaskCommand<IEntityFactory?>(ServiceProvider, x => CreateEntity(x, ActiveRoot, true));
        CreateEntityInRootCommand = new AnonymousTaskCommand<IEntityFactory?>(ServiceProvider, x => CreateEntity(x, ActiveRoot));
        CreateEntityInSelectionCommand = new AnonymousTaskCommand<IEntityFactory?>(ServiceProvider, x => CreateEntity(x, SelectedContent.FirstOrDefault() as EntityHierarchyItemViewModel ?? ActiveRoot));
        CreateFolderInRootCommand = new AnonymousCommand(ServiceProvider, () => CreateFolder(ActiveRoot, true));
        CreateFolderInSelectionCommand = new AnonymousCommand(ServiceProvider, () => CreateFolder(SelectedContent.FirstOrDefault() as EntityHierarchyItemViewModel ?? ActiveRoot, true));
        SetActiveRootCommand = new AnonymousCommand(ServiceProvider, SetActiveRoot);
        RenameCommand = new AnonymousCommand(ServiceProvider, Rename);
        CreatePrefabFromSelectionCommand = new AnonymousCommand(ServiceProvider, CreatePrefabFromSelection);
        OpenPrefabEditorCommand = new AnonymousCommand(ServiceProvider, OpenPrefabEditor);
        SelectPrefabCommand = new AnonymousCommand(ServiceProvider, SelectPrefab);
        BreakLinkToPrefabCommand = new AnonymousCommand(ServiceProvider, () => BreakLinkToBase("prefab"));
        UpdateCommands();
        modelComponentChanges = new ModelComponentChangeHandler(asset);
        // The properties of a selected entity show the target names and import settings of its model asset
        Session.AssetPropertiesChanged += ReferencedModelChanged;
    }

    /// <summary>
    /// Gets the root of the hierarchy: the edited scene, with its child scenes, or the edited prefab.
    /// </summary>
    public EntityHierarchyRootViewModel HierarchyRoot => (EntityHierarchyRootViewModel)RootPart!;

    /// <inheritdoc/>
    public EditorGameHost GameHost { get; }

    /// <inheritdoc/>
    /// <remarks>The edited scene or prefab by default.</remarks>
    public EntityHierarchyRootViewModel ActiveRoot { get => activeRoot ?? HierarchyRoot; set => SetValue(ref activeRoot, value); }

    /// <inheritdoc/>
    public IReadOnlyList<IAddAssetPolicy> AddAssetPolicies => addAssetPolicies ??= AddAssetPolicyRegistry.CreatePolicies();

    /// <summary>
    /// Gets the selected entity that has the transformation gizmo: the last selected one, or <c>null</c>.
    /// </summary>
    public EntityViewModel? EntityWithGizmo => SelectedItems.LastOrDefault();

    /// <summary>
    /// Gets whether the game is compiling assets the entities reference.
    /// </summary>
    public bool CompilingAssets { get => compilingAssets; private set => SetValue(ref compilingAssets, value); }

    /// <summary>
    /// Gets the command that creates an entity in the active root, at the point of the scene under the last right click of
    /// the view, with the factory given as parameter, or an empty entity when the parameter is <c>null</c>.
    /// </summary>
    public ICommandBase CreateEntityCommand { get; }

    /// <summary>
    /// Gets the command that creates an entity at the root of the active root, with the factory given as parameter, or an
    /// empty entity when the parameter is <c>null</c>.
    /// </summary>
    public ICommandBase CreateEntityInRootCommand { get; }

    /// <summary>
    /// Gets the command that makes the last selected root (a scene) the active root: the entities created without a
    /// parent, pasted at the root or dropped on the view go into it.
    /// </summary>
    public ICommandBase SetActiveRootCommand { get; }

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
    /// Gets the view options of the game, created by the <see cref="IEditorViewOptionsFactory"/> of the session, or
    /// <c>null</c> while the game has not loaded, or when the editor has no game.
    /// </summary>
    public IEditorViewOptions? ViewOptions { get => viewOptions; internal set => SetValue(ref viewOptions, value); }

    /// <summary>
    /// The widgets of the view, once the view asked for them (<see cref="GetOrCreateSceneViewWidgets"/>), or null.
    /// </summary>
    [Experimental("STREXP005")]
    public SceneViewWidgetsViewModel? SceneViewWidgets { get => sceneViewWidgets; private set => SetValue(ref sceneViewWidgets, value); }

    /// <summary>
    /// The widgets of the view that the plugins register (<see cref="AssetEditors.EntityHierarchyEditor.Widgets.SceneViewWidgetDescriptor"/>),
    /// created the first time a view that shows them asks: an editor whose view shows no widget (WPF) runs none of them.
    /// </summary>
    /// <returns>The widgets, or null once the editor is destroyed.</returns>
    [Experimental("STREXP005")]
    public SceneViewWidgetsViewModel? GetOrCreateSceneViewWidgets()
    {
        if (IsDestroyed)
            return null;
        return SceneViewWidgets ??= new SceneViewWidgetsViewModel(this, ServiceProvider.TryGet<IAssetsPluginService>() as EditorPluginHost);
    }

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
    public EntityHierarchyEditorController? Controller => GameHost.Controller as EntityHierarchyEditorController;

    /// <inheritdoc/>
    /// <remarks>The game reads the scene editor settings, which the plugins register, on every frame.</remarks>
    public override Task Stopped => GameHost.Stopped;

    /// <summary>
    /// Starts the game of the editor and loads the hierarchy into it, when the session can show a game.
    /// </summary>
    /// <returns><c>false</c> when the game could not start; the editor still works without it.</returns>
    public override async Task<bool> Initialize()
    {
        try
        {
            return await InitializeGame();
        }
        finally
        {
            // Whatever happened, the hierarchy stops waiting for the game
            initialized.TrySetResult();
        }
    }

    private async Task<bool> InitializeGame()
    {
        if (!await base.Initialize())
            return false;

        // Without a viewport (no graphics device, previews turned off) the editor has no game view: the hierarchy is
        // loaded without a game
        if (ServiceProvider.TryGet<IEditorGameViewportService>() is not { } viewportService)
        {
            initialized.TrySetResult();
            await LoadHierarchyAsync();
            return true;
        }

        try
        {
            if (CreateController(viewportService) is not { } controller)
            {
                initialized.TrySetResult();
                await LoadHierarchyAsync();
                return true;
            }

            controller.Loader.AssetLoading += AssetLoadingChanged;
            controller.Loader.AssetLoaded += AssetLoadingChanged;
            if (!await GameHost.StartAsync(controller) || IsDestroyed)
                return false;

            // From now on, the entities the asset gets or loses are added to or removed from the game
            isGameLoaded = true;
            initialized.TrySetResult();
            await LoadHierarchyAsync();
            UpdateFocusCommands();

            if (ServiceProvider.TryGet<IEditorViewOptionsFactory>()?.CreateViewOptions(this, controller) is { } options)
            {
                await options.InitializeAsync();
                if (IsDestroyed)
                    options.Destroy();
                else
                {
                    LoadSettings(options);
                    ViewOptions = options;
                    // The entities already in the game
                    foreach (var entity in propagators.Keys.Select(FindPartViewModel).OfType<EntityViewModel>())
                        options.OnEntityLinked(entity);
                }
            }
            return true;
        }
        catch (Exception e) when (!IsDestroyed)
        {
            Log.Error($"The game of the editor of {Asset.Url} could not start.", e);
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

        // As WPF, the settings first, before the toolbar and the game go
        SaveSettings();
        initialized.TrySetResult();
        HierarchyRoot.EntityAdded -= EntityAdded;
        HierarchyRoot.EntityRemoved -= EntityRemoved;
        HierarchyRoot.SelectableChanged -= SelectableChanged;
        SelectedItems.CollectionChanged -= SelectedItemsChanged;
        Session.AssetPropertiesChanged -= ReferencedModelChanged;
        if (Controller is { } controller)
        {
            controller.Loader.AssetLoading -= AssetLoadingChanged;
            controller.Loader.AssetLoaded -= AssetLoadingChanged;
        }
        modelComponentChanges.Dispose();
        foreach (var propagator in propagators.Values)
            propagator.Destroy();
        propagators.Clear();
        ViewOptions?.Destroy();
        ViewOptions = null;
        SceneViewWidgets?.Destroy();
        SceneViewWidgets = null;
        GameHost.Destroy();

        base.Destroy();
    }

    /// <summary>
    /// Shows the view options saved for this scene or prefab in the user settings of its package, shared by both editors,
    /// and moves the camera where it was.
    /// </summary>
    private void LoadSettings(IEditorViewOptions options)
    {
        try
        {
            var sceneSettings = Asset.Directory.Package.UserSettings.GetValue(PackageSceneSettings.SceneSettings);
            var settings = sceneSettings.TryGetValue(Asset.Asset.Id, out var saved) ? saved : SceneSettingsData.CreateDefault();
            options.LoadSettings(settings);
        }
        catch (Exception e)
        {
            Log.Warning($"The view settings of {Asset.Url} could not be read.", e);
        }
    }

    /// <summary>
    /// Saves the view options of this scene or prefab in the user settings of its package, keeping the members the view
    /// options of this user interface don't show as they were.
    /// </summary>
    private void SaveSettings()
    {
        // Without a game, nothing was shown that could have changed
        if (ViewOptions is not { } options)
            return;

        try
        {
            var userSettings = Asset.Directory.Package.UserSettings;
            // A copy: without settings yet, the value is the default dictionary of the key, shared by every package
            var sceneSettings = new Dictionary<AssetId, SceneSettingsData>(userSettings.GetValue(PackageSceneSettings.SceneSettings));
            if (!sceneSettings.TryGetValue(Asset.Asset.Id, out var settings))
            {
                settings = SceneSettingsData.CreateDefault();
                sceneSettings.Add(Asset.Asset.Id, settings);
            }
            SaveSettings(settings);
            userSettings.SetValue(PackageSceneSettings.SceneSettings, sceneSettings);
            userSettings.Save();
        }
        catch (Exception e)
        {
            Log.Warning($"The view settings of {Asset.Url} could not be saved.", e);
        }
    }

    /// <summary>
    /// Writes the view options into the given settings: the editor of a scene adds its own.
    /// </summary>
    protected virtual void SaveSettings(SceneSettingsData settings)
    {
        ViewOptions?.SaveSettings(settings);
    }

    /// <summary>
    /// Lets the game render: its view is shown.
    /// </summary>
    public void ShowGame()
    {
        GameHost.ShowGame();
    }

    /// <summary>
    /// Throttles the game: its view is hidden.
    /// </summary>
    public void HideGame()
    {
        GameHost.HideGame();
    }

    /// <summary>
    /// Writes the transformations of elements moved in the game (by a transformation gizmo) back to the asset, as one
    /// undoable operation.
    /// </summary>
    /// <param name="transformations">The new transformations, by entity or root.</param>
    public void UpdateTransformations(IReadOnlyDictionary<AbsoluteId, TransformationTRS> transformations)
    {
        var undoRedoService = Session.UndoRedoService;
        using var transaction = undoRedoService.CreateTransaction();
        foreach (var (id, transformation) in transformations)
        {
            if (GetObjectToSelect(id) is EntityHierarchyElementViewModel element)
                UpdateTransformations(element, transformation);
        }
        undoRedoService.SetName(transaction, "Update transformation");
    }

    /// <summary>
    /// Writes the transformation of an element moved in the game back to the asset: the transformation of an entity by
    /// default. The editor of a scene moves its child scenes.
    /// </summary>
    protected virtual void UpdateTransformations(EntityHierarchyElementViewModel element, TransformationTRS transformation)
    {
        if (element is not EntityViewModel entity)
            return;

        var node = Session.AssetNodeContainer.GetNode(entity.AssetSideEntity.Transform);
        // Update properties only when they actually changed
        if ((Vector3)node[nameof(TransformComponent.Position)].Retrieve()! != transformation.Position)
            node[nameof(TransformComponent.Position)].Update(transformation.Position);
        if ((Quaternion)node[nameof(TransformComponent.Rotation)].Retrieve()! != transformation.Rotation)
            node[nameof(TransformComponent.Rotation)].Update(transformation.Rotation);
        if ((Vector3)node[nameof(TransformComponent.Scale)].Retrieve()! != transformation.Scale)
            node[nameof(TransformComponent.Scale)].Update(transformation.Scale);
    }

    /// <summary>
    /// Gets the root of the hierarchy that holds the given entity: the scene or prefab, or the child scene.
    /// </summary>
    public static EntityHierarchyRootViewModel GetRoot(EntityViewModel entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var rootEntity = entity;
        while (rootEntity.TransformParent is EntityViewModel parentEntity)
            rootEntity = parentEntity;
        return rootEntity.TransformParent as EntityHierarchyRootViewModel ?? throw new InvalidOperationException($"{entity} is not contained in a hierarchy.");
    }

    /// <summary>
    /// Moves the camera of the view to frame an entity, keeping the direction of the camera (UI thread). In the material
    /// selection mode, the camera frames the mesh of the highlighted material instead, as in the WPF scene editor.
    /// </summary>
    /// <returns>A task that completes once the camera has moved, or at once when the editor has no game.</returns>
    public Task FocusOnEntityAsync(EntityViewModel entity)
    {
        if (!entity.IsLoaded || !isGameLoaded || Controller is not { } controller || controller.GetService<EditorGameEntityCameraService>() is not { } camera)
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
    /// Called once the game has loaded its content and created its scene, or once the editor knows it has no game: the
    /// edited scene or prefab can be loaded (<see cref="EntityHierarchyElementViewModel.RequestLoading"/>).
    /// </summary>
    protected virtual Task LoadHierarchyAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task LoadRootsAsync(IReadOnlyCollection<EntityHierarchyRootViewModel> roots)
    {
        await initialized.Task;
        if (IsDestroyed || !isGameLoaded || Controller is null)
            return;

        await LoadRootsIntoGameAsync(roots);
        if (IsDestroyed)
            return;
        await LinkToGameSide(roots.SelectMany(x => x.InnerSubEntities.BreadthFirst(e => e.TransformChildren)));
    }

    /// <inheritdoc/>
    public async Task UnloadRootsAsync(IReadOnlyCollection<EntityHierarchyRootViewModel> roots)
    {
        await initialized.Task;
        if (IsDestroyed || !isGameLoaded || Controller is not { } controller)
            return;

        var selection = controller.GetService<IEditorGameSelectionViewModelService>();
        foreach (var entity in roots.SelectMany(x => x.InnerSubEntities.BreadthFirst(e => e.TransformChildren)))
        {
            UnlinkFromGameSide(entity);
            selection?.RemoveSelectable(entity.Id);
        }
        await UnloadRootsFromGameAsync(roots);
    }

    /// <inheritdoc/>
    public virtual Task UpdateSceneOffsetAsync(SceneRootViewModel root)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<bool> IsSceneLoadedOnOpenAsync(SceneRootViewModel root)
    {
        // The edited scene is always loaded (LoadHierarchyAsync)
        if (root.ParentScene is null)
            return false;

        await initialized.Task;
        if (IsDestroyed)
            return false;

        try
        {
            var sceneSettings = root.Asset.Directory.Package.UserSettings.GetValue(PackageSceneSettings.SceneSettings);
            return (sceneSettings.TryGetValue(root.Asset.Id, out var settings) ? settings : SceneSettingsData.CreateDefault()).SceneLoaded;
        }
        catch (Exception e)
        {
            Log.Warning($"The settings of {root.Asset.Url} could not be read.", e);
            return false;
        }
    }

    /// <inheritdoc/>
    public void SaveSceneLoaded(SceneRootViewModel root)
    {
        try
        {
            var userSettings = root.Asset.Directory.Package.UserSettings;
            // A copy: without settings yet, the value is the default dictionary of the key, shared by every package
            var sceneSettings = new Dictionary<AssetId, SceneSettingsData>(userSettings.GetValue(PackageSceneSettings.SceneSettings));
            if (!sceneSettings.TryGetValue(root.Asset.Id, out var settings))
            {
                settings = SceneSettingsData.CreateDefault();
                sceneSettings.Add(root.Asset.Id, settings);
            }
            settings.SceneLoaded = root.IsLoaded;
            userSettings.SetValue(PackageSceneSettings.SceneSettings, sceneSettings);
            userSettings.Save();
        }
        catch (Exception e)
        {
            Log.Warning($"The settings of {root.Asset.Url} could not be saved.", e);
        }
    }

    /// <inheritdoc/>
    ISet<AssetId> IEntityHierarchyEditorViewModel.GatherAllBasePartAssets(EntityViewModel entity, bool includeChildren)
    {
        return GatherAllBasePartAssets(entity, includeChildren);
    }

    /// <inheritdoc/>
    bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        // Only assets can be dropped on the view
        if (children.Any(x => x is not AssetViewModel))
        {
            message = "Only assets can be dropped here";
            return false;
        }
        return ((IAddChildViewModel)ActiveRoot).CanAddChildren(children, modifiers, out message);
    }

    /// <inheritdoc/>
    void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
    {
        // The entities of the dropped assets go under the mouse, in the active root
        var root = ActiveRoot;
        var position = GetMousePositionInRoot(root, false);
        IReadOnlyCollection<EntityViewModel> newEntities;
        var undoRedo = Session.UndoRedoService;
        using (var transaction = undoRedo.CreateTransaction())
        {
            newEntities = root.AddEntitiesFromAssets(children, root.Asset.Asset.Hierarchy.RootParts.Count, modifiers, position);
            undoRedo.SetName(transaction, "Create entities");
        }

        // The new entities are selected
        ClearSelection();
        SelectedContent.AddRange(newEntities);
    }

    /// <summary>
    /// Loads the given roots into the game (UI thread).
    /// </summary>
    protected abstract Task LoadRootsIntoGameAsync(IReadOnlyCollection<EntityHierarchyRootViewModel> roots);

    /// <summary>
    /// Unloads the given roots from the game, in this order (UI thread).
    /// </summary>
    protected abstract Task UnloadRootsFromGameAsync(IReadOnlyCollection<EntityHierarchyRootViewModel> roots);

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
        var added = new List<Task>();
        foreach (var entity in entities)
        {
            var propagator = new EntityHierarchyElementChangePropagator(this, controller, entity.Asset, entity.Id, entity.AssetSideEntity);
            propagators.Add(entity.Id, propagator);
            added.Add(NotifyLinkedAsync(entity, propagator));
            propagator.NotifyGameSidePartAdded();
            // The clone of the entity is in the game: it can be selected there, unless it is locked
            if (!entity.IsLocked)
                selection?.AddSelectable(entity.Id);
        }
        return Task.WhenAll(added);
    }

    /// <summary>
    /// Tells the view options that an entity is in the game, once its game-side clone is initialized.
    /// </summary>
    private async Task NotifyLinkedAsync(EntityViewModel entity, GameEditorChangePropagator propagator)
    {
        await propagator.Initialized;
        // The entity left the game while its propagator initialized
        if (!IsDestroyed && propagators.TryGetValue(entity.Id, out var current) && current == propagator)
            ViewOptions?.OnEntityLinked(entity);
    }

    /// <summary>
    /// Destroys the propagator of an entity that leaves the game, and tells the view options.
    /// </summary>
    private void UnlinkFromGameSide(EntityViewModel entity)
    {
        if (propagators.Remove(entity.Id, out var propagator))
        {
            propagator.Destroy();
            ViewOptions?.OnEntityUnlinked(entity);
        }
    }

    /// <summary>
    /// Adds to the game an entity the asset just got, with its children.
    /// </summary>
    private async void EntityAdded(object? sender, EntityHierarchyChangedEventArgs e)
    {
        // An entity of an unloaded scene is not in the game
        if (!isGameLoaded || Controller is not { } controller || !e.Entity.IsLoaded)
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
    /// A locked or unloaded entity cannot be selected in the game, as in the WPF scene editor: it leaves the selection of
    /// the game, and the gizmos cannot move it. It can still be selected in the hierarchy.
    /// </summary>
    private void SelectableChanged(object? sender, EntityHierarchyElementEventArgs e)
    {
        // An entity not in the game yet becomes selectable once it is linked to it; a scene is in the game when loaded
        if (e.Element is EntityViewModel entity && !propagators.ContainsKey(entity.Id))
            return;

        var selection = Controller?.GetService<IEditorGameSelectionViewModelService>();
        if (e.Element.IsSelectable)
            selection?.AddSelectable(e.Element.Id);
        else
            selection?.RemoveSelectable(e.Element.Id);
    }

    /// <summary>
    /// Removes from the game an entity the asset just lost, with its children.
    /// </summary>
    private async void EntityRemoved(object? sender, EntityHierarchyChangedEventArgs e)
    {
        var entities = SelfAndDescendants(e.Entity).ToList();
        // Only an entity linked to the game (of a loaded scene) has a clone in the game
        var inGame = propagators.ContainsKey(e.Entity.Id);
        var selection = isGameLoaded ? Controller?.GetService<IEditorGameSelectionViewModelService>() : null;
        foreach (var entity in entities)
        {
            UnlinkFromGameSide(entity);
            selection?.RemoveSelectable(entity.Id);
        }
        foreach (var entity in entities)
        {
            SelectedContent.Remove(entity);
        }

        if (!isGameLoaded || Controller is not { } controller || !inGame)
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
    /// <param name="atMousePosition">Whether the entity goes at the point of the scene under the last right click of the
    /// view, as the entity menu of the view.</param>
    /// <returns>The view model of the new entity, or <c>null</c> when the factory created nothing (the user cancelled).</returns>
    public async Task<EntityViewModel?> CreateEntity(IEntityFactory? factory, EntityHierarchyItemViewModel parent, bool atMousePosition = false)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var entity = factory != null ? await factory.CreateEntity(parent) : new Entity { Name = EntityFactory.ComputeNewName(parent, "Entity") };
        if (entity is null)
            return null;

        if (atMousePosition && parent is EntityHierarchyRootViewModel root)
            entity.Transform.Position = GetMousePositionInRoot(root, true);

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
        var entitiesToDuplicate = GetCommonRoots(SelectedItems.ToList());
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
    protected override EntityDesign GetPartDesign(EntityViewModel item)
    {
        return item.EntityDesign;
    }

    /// <inheritdoc/>
    /// <remarks>The root, the entities and the folders can be selected, and the child scenes.</remarks>
    protected override object? GetObjectToSelect(AbsoluteId id)
    {
        if (id == HierarchyRoot.Id)
            return HierarchyRoot;

        return HierarchyRoot.Children.BreadthFirst(x => x.Children).FirstOrDefault(x => x switch
        {
            EntityHierarchyElementViewModel element => element.Id == id,
            EntityFolderViewModel folder => folder.Id == id,
            _ => false,
        });
    }

    /// <inheritdoc/>
    protected override AbsoluteId? GetSelectedObjectId(object obj)
    {
        return (obj as EntityHierarchyElementViewModel)?.Id ?? (obj as EntityFolderViewModel)?.Id;
    }

    /// <inheritdoc/>
    /// <remarks>Into the active root, as in the WPF scene editor.</remarks>
    protected override AssetCompositeItemViewModel GetPasteRoot()
    {
        return ActiveRoot;
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

        var entitiesToDelete = GetCommonRoots(SelectedItems.ToList());
        if (SceneEditorSettings.AskBeforeDeletingEntities.GetValue())
        {
            var confirmMessage = Tr._p("Message", "Are you sure you want to delete this entity?");
            // TODO: we should compute the actual total number of entities to be deleted here (children recursively, etc.)
            if (entitiesToDelete.Count > 1)
                confirmMessage = string.Format(Tr._p("Message", "Are you sure you want to delete these {0} entities?"), entitiesToDelete.Count);
            // A whole sentence, so that each language can translate it
            var checkedMessage = Tr._p("Settings", "Always delete entities without asking");
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
    protected override ISet<EntityViewModel> DuplicateSelection()
    {
        return DuplicateSelectedEntities();
    }

    /// <inheritdoc/>
    protected override void PrepareToCopy(AssetCompositeHierarchyData<EntityDesign, Entity> clonedHierarchy, ICollection<AssetCompositeItemViewModel> commonRoots, ICollection<EntityViewModel> commonParts)
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
    protected override void AttachPropertiesForPaste(ref PropertyContainer propertyContainer, AssetCompositeItemViewModel pasteTarget)
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

        var prefabInstanceSelected = SelectedItems.Any(x => x.SourcePrefab != null);
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
        foreach (var prefab in SelectedItems.Select(x => x.SourcePrefab).NotNull().Distinct())
        {
            Session.EditorCollection.OpenAssetEditor(prefab);
        }
    }

    private void SelectPrefab()
    {
        // The entity with the transformation gizmo, as in the WPF scene editor
        if (EntityWithGizmo?.SourcePrefab is { } prefab)
            Session.AssetCollection.SelectAssets(prefab.Yield());
    }

    private void SetActiveRoot()
    {
        if (SelectedContent.LastOrDefault() is EntityHierarchyRootViewModel root)
            ActiveRoot = root;
    }

    /// <summary>
    /// Gets the point of the scene under the mouse, in the space of the given root (a child scene has an offset), snapped
    /// to the translation snap of the view, or the origin when the editor has no game.
    /// </summary>
    private Vector3 GetMousePositionInRoot(EntityHierarchyRootViewModel root, bool lastRightClick)
    {
        if (!isGameLoaded || Controller?.GetService<IEditorGameMousePositionViewModelService>() is not { } mouse)
            return Vector3.Zero;

        var position = mouse.GetMousePositionInScene(lastRightClick);
        if (ViewOptions is { } options)
            position = options.SnapPosition(position);
        for (var scene = root as SceneRootViewModel; scene is not null; scene = scene.ParentScene)
            position -= scene.Offset;
        return position;
    }

    /// <summary>
    /// Shows whether the game compiles assets (game thread or UI thread).
    /// </summary>
    private void AssetLoadingChanged(object? sender, ContentLoadEventArgs e)
    {
        Dispatcher.InvokeAsync(() => CompilingAssets = !IsDestroyed && e.ContentLoadingCount > 0).Forget();
    }

    /// <summary>
    /// Refreshes the properties of the selected entities when the model asset of one of them changed: they show its target
    /// names and import settings, including when the change is undone or redone.
    /// </summary>
    private void ReferencedModelChanged(object? sender, AssetChangedEventArgs e)
    {
        var changedModelIds = e.Assets.Where(x => x.Asset is ModelAsset).Select(x => x.Id).ToHashSet();
        if (changedModelIds.Count == 0)
            return;

        Dispatcher.InvokeAsync(() =>
        {
            if (IsDestroyed || Session.ActiveProperties != EditorProperties)
                return;

            var modelReferences = SelectedItems.SelectMany(x => x.AssetSideEntity.Components.OfType<ModelComponent>()).Select(x => AttachedReferenceManager.GetAttachedReference(x.Model));
            if (modelReferences.Any(x => x is not null && changedModelIds.Contains(x.Id)))
                EditorProperties.RefreshSelectedPropertiesAsync().Forget();
        }).Forget();
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
        OnPropertyChanging(nameof(EntityWithGizmo));
        OnPropertyChanged(nameof(EntityWithGizmo));
    }

    protected override void SelectedItemsCollectionChanged(NotifyCollectionChangedAction action)
    {
        base.SelectedItemsCollectionChanged(action);
        OnPropertyChanging(nameof(EntityWithGizmo));
        OnPropertyChanged(nameof(EntityWithGizmo));
    }

    private Task FocusOnSelectionAsync()
    {
        // The entity with the transformation gizmo, as in the WPF scene editor
        return EntityWithGizmo is { } entity ? FocusOnEntityAsync(entity) : Task.CompletedTask;
    }

    private void SelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateFocusCommands();

    private void UpdateFocusCommands()
    {
        FocusOnEntityCommand.IsEnabled = isGameLoaded;
        FocusOnSelectionCommand.IsEnabled = isGameLoaded && EntityWithGizmo is not null;
    }
}
