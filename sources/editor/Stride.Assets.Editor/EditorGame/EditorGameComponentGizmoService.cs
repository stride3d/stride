// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Assets.Presentation;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Core;
using Stride.Core.BuildEngine;
using Stride.Core.Extensions;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.EditorGame.ViewModels;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Engine.Gizmos;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Sprites;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// Shows the gizmos of the components of the entities (light, camera and the other icons), from the gizmo types the asset
/// assemblies register (<see cref="GizmoComponentAttribute"/>), plugins included, and picks them under the mouse.
/// </summary>
/// <remarks>
/// The cross-platform counterpart of the WPF <c>EditorGameComponentGizmoService</c>.
/// </remarks>
[EditorGameService(typeof(EntityHierarchyEditorController), Order = 190)]
public sealed class EditorGameComponentGizmoService : EditorGameServiceBase, IEditorGameComponentGizmoService, IEditorGameViewModelService
{
    private static readonly PropertyKey<Dictionary<EntityComponent, IEntityGizmo>> GizmoEntitiesKey = new("GizmoEntitiesProperty", typeof(EditorGameComponentGizmoService));
    private readonly IEditorGameController controller;
    private readonly HashSet<IEntityGizmo> sceneGizmos = [];
    private readonly Dictionary<Type, bool> gizmoVisibilities = [];
    private readonly HashSet<Entity> selectedEntities = [];
    private EntityHierarchyEditorGame? game;
    private Scene? editorScene;
    private SceneInstance? gameSceneInstance;
    private PickingSceneRenderer? gizmoEntityPicker;
    private float gizmoSize = 1.0f;

    public EditorGameComponentGizmoService(IEditorGameController controller)
    {
        this.controller = controller;

        // The default visibilities of the WPF scene settings: the gizmos hidden by default and the fallback gizmo stay hidden
        gizmoVisibilities[typeof(FallbackGizmo)] = false;
        foreach (var gizmoType in StrideEditorPlugin.GizmoTypeDictionary.Values)
        {
            if (gizmoType.GetCustomAttribute<GizmoComponentAttribute>(true) is { HiddenByDefault: true })
                gizmoVisibilities[gizmoType] = false;
        }
    }

    /// <inheritdoc />
    public float SceneUnit => Math.Max(1, Game.EditorServices.Get<IEditorGameCameraService>().SceneUnit);

    /// <summary>
    /// Gets or sets the size factor of the gizmos.
    /// </summary>
    public float GizmoSize
    {
        get => gizmoSize;
        set
        {
            gizmoSize = value;
            controller.InvokeAsync(() => sceneGizmos.ForEach(x => x.SizeFactor = value));
        }
    }

    /// <inheritdoc />
    public bool FixedSize { get; set; }

    /// <inheritdoc />
    public override IEnumerable<Type> Dependencies { get { yield return typeof(IEditorGameCameraService); } }

    private EntityHierarchyEditorGame Game => game ?? throw new InvalidOperationException("The service is not initialized.");

    /// <inheritdoc />
    public Entity? GetContentEntityUnderMouse()
    {
        if (gizmoEntityPicker is null)
            return null;

        var pickResult = gizmoEntityPicker.Pick();
        var pickedComponentId = new OpaqueComponentId(pickResult.ComponentId);
        foreach (var gizmo in sceneGizmos)
        {
            if (gizmo.HandlesComponentId(pickedComponentId, out var selection))
                return selection;
        }
        return null;
    }

    /// <inheritdoc />
    public void UpdateGizmoEntitiesSelection(Entity entity, bool isSelected)
    {
        var gizmoEntities = entity.Tags.Get(GizmoEntitiesKey);
        if (gizmoEntities == null)
            return;

        foreach (var gizmoEntity in gizmoEntities.Values)
        {
            gizmoEntity.IsSelected = isSelected;
        }

        if (isSelected)
            selectedEntities.Add(entity);
        else
            selectedEntities.Remove(entity);
    }

    /// <summary>
    /// Shows or hides the gizmos of a type of component (UI thread or game thread).
    /// </summary>
    /// <param name="componentType">The type of component; <see cref="TransformComponent"/> stands for the entities without gizmo.</param>
    /// <param name="isVisible">Whether the gizmos show.</param>
    public void ToggleGizmoVisibility(Type componentType, bool isVisible)
    {
        controller.InvokeAsync(() =>
        {
            var gizmoType = componentType != typeof(TransformComponent) ? GetGizmoType(componentType) : typeof(FallbackGizmo);
            if (gizmoType == null || gameSceneInstance is null)
                return;

            gizmoVisibilities[gizmoType] = isVisible;
            foreach (var gizmo in sceneGizmos)
            {
                if (gizmo.GetType() == gizmoType)
                    gizmo.IsEnabled = isVisible;
            }

            foreach (var entity in gameSceneInstance)
            {
                UpdateMainGizmo(entity);
            }

            if (isVisible)
            {
                foreach (var selectedEntity in selectedEntities)
                {
                    UpdateGizmoEntitiesSelection(selectedEntity, true);
                }
            }
        });
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        if (gameSceneInstance is not null)
        {
            gameSceneInstance.EntityAdded -= CreateGizmoEntities;
            gameSceneInstance.ComponentChanged -= UpdateGizmoEntities;
            gameSceneInstance.EntityRemoved -= RemoveGizmoEntities;
        }
        return base.DisposeAsync();
    }

    /// <inheritdoc />
    protected override Task<bool> Initialize(EditorServiceGame editorGame)
    {
        game = (EntityHierarchyEditorGame)editorGame;
        editorScene = game.EditorScene;
        gameSceneInstance = game.SceneSystem.SceneInstance;
        gameSceneInstance.EntityAdded += CreateGizmoEntities;
        gameSceneInstance.ComponentChanged += UpdateGizmoEntities;
        gameSceneInstance.EntityRemoved += RemoveGizmoEntities;

        var pickingRenderStage = new RenderStage("Picking", "Picking");
        game.EditorSceneSystem.GraphicsCompositor.RenderStages.Add(pickingRenderStage);

        // Meshes
        var meshRenderFeature = game.EditorSceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().First();
        meshRenderFeature.RenderFeatures.Add(new PickingRenderFeature());
        meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
        {
            EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect + ".Picking",
            RenderGroup = GizmoBase.DefaultGroupMask,
            RenderStage = pickingRenderStage,
        });

        // Sprites
        var spriteRenderFeature = game.EditorSceneSystem.GraphicsCompositor.RenderFeatures.OfType<SpriteRenderFeature>().First();
        spriteRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
        {
            EffectName = "Test",
            RenderGroup = GizmoBase.DefaultGroupMask,
            RenderStage = pickingRenderStage,
        });

        var editorCompositor = (EditorTopLevelCompositor)game.EditorSceneSystem.GraphicsCompositor.Game;
        editorCompositor.PostGizmoCompositors.Add(gizmoEntityPicker = new PickingSceneRenderer { PickingRenderStage = pickingRenderStage });

        // Initialize and add the gizmo entities to the gizmo scene
        MicrothreadLocalDatabases.MountCommonDatabase();
        foreach (var gizmo in sceneGizmos)
        {
            gizmo.Initialize(editorGame.Services, editorScene);
        }

        editorGame.Script.AddTask(Update);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Updates all the gizmos of the scene, every frame.
    /// </summary>
    private async Task Update()
    {
        while (!IsDisposed)
        {
            await Game.Script.NextFrame();
            sceneGizmos.ForEach(x => x.Update());
        }
    }

    private void RemoveGizmoEntities(object? sender, Entity entity)
    {
        var gizmoEntities = entity.Tags.Get(GizmoEntitiesKey);
        if (gizmoEntities == null)
            return;

        // Reset the gizmo entities
        entity.Tags.Set(GizmoEntitiesKey, null);

        // Remove the gizmo entities from the scene
        foreach (var gizmoEntity in gizmoEntities.Values)
        {
            sceneGizmos.Remove(gizmoEntity);
            gizmoEntity.Dispose();
        }

        selectedEntities.Remove(entity);
    }

    private void UpdateGizmoEntities(object? sender, EntityComponentEventArgs e)
    {
        var entity = e.Entity;
        var oldComponent = e.PreviousComponent;
        var newComponent = e.NewComponent;
        var gizmoEntities = entity.Tags.Get(GizmoEntitiesKey);

        // Remove the gizmo of the old component
        if (gizmoEntities != null && oldComponent != null && gizmoEntities.TryGetValue(oldComponent, out var gizmo))
        {
            RemoveGizmo(gizmoEntities, gizmo, oldComponent);
            UpdateMainGizmo(entity);
        }

        // Create the gizmo of the new component
        if (newComponent != null)
        {
            var isMainGizmo = false;
            var gizmoType = GetGizmoType(newComponent.GetType());
            if (gizmoType != null)
            {
                var attribute = gizmoType.GetCustomAttribute<GizmoComponentAttribute>(true);
                isMainGizmo = attribute?.IsMainGizmo ?? false;
                // Remove the fallback gizmo when the entity gets a component with a gizmo
                if (gizmoEntities != null && gizmoEntities.TryGetValue(entity.Transform, out var removeGizmo))
                {
                    RemoveGizmo(gizmoEntities, removeGizmo, entity.Transform);
                }
            }
            if (isMainGizmo)
            {
                UpdateMainGizmo(entity);
            }
            else
            {
                CreateGizmoEntity(entity, newComponent);
            }
        }
    }

    private void UpdateMainGizmo(Entity entity)
    {
        var mainComponent = GetMainGizmoComponent(entity);
        var gizmoEntities = entity.Tags.Get(GizmoEntitiesKey);

        // Remove any main gizmo that does not correspond to the current main component
        if (gizmoEntities != null)
        {
            foreach (var component in entity.Components)
            {
                var attribute = GetGizmoType(component.GetType())?.GetCustomAttribute<GizmoComponentAttribute>(true);
                if (attribute != null && attribute.IsMainGizmo && component != mainComponent && gizmoEntities.TryGetValue(component, out var gizmo))
                {
                    RemoveGizmo(gizmoEntities, gizmo, component);
                }
            }
        }

        // Create the main gizmo if it does not exist
        if (mainComponent != null && (gizmoEntities == null || !gizmoEntities.ContainsKey(mainComponent)))
        {
            CreateGizmoEntity(entity, mainComponent);
            gizmoEntities = entity.Tags.Get(GizmoEntitiesKey);
        }
        if (gizmoEntities == null || gizmoEntities.Count == 0)
        {
            CreateGizmoEntity(entity, entity.Transform, typeof(FallbackGizmo));
        }
    }

    private EntityComponent? GetMainGizmoComponent(Entity entity)
    {
        EntityComponent? mainComponent = null;
        var mainComponentOrder = int.MinValue;
        foreach (var component in entity.Components)
        {
            var gizmoType = GetGizmoType(component.GetType());
            if (gizmoType == null)
                continue;

            if (gizmoVisibilities.TryGetValue(gizmoType, out var isVisible) && !isVisible)
                continue;

            var attribute = gizmoType.GetCustomAttribute<GizmoComponentAttribute>(true);
            if (attribute == null || !attribute.IsMainGizmo)
                continue;

            var componentIndex = StrideDefaultAssetsPlugin.ComponentOrders.IndexOf(t => t.type == component.GetType());
            var order = componentIndex >= 0 ? StrideDefaultAssetsPlugin.ComponentOrders[componentIndex].order : default;
            if (order > mainComponentOrder)
            {
                mainComponentOrder = order;
                mainComponent = component;
            }
        }

        return mainComponent;
    }

    private List<EntityComponent> GetGizmosToCreate(Entity entity)
    {
        var list = new List<EntityComponent>();
        foreach (var component in entity.Components)
        {
            var attribute = GetGizmoType(component.GetType())?.GetCustomAttribute<GizmoComponentAttribute>(true);
            if (attribute != null && !attribute.IsMainGizmo)
                list.Add(component);
        }
        if (GetMainGizmoComponent(entity) is { } mainComponent)
        {
            list.Add(mainComponent);
        }
        return list;
    }

    private void CreateGizmoEntities(object? sender, Entity entity)
    {
        var gizmoCreated = false;
        foreach (var component in GetGizmosToCreate(entity))
        {
            gizmoCreated = CreateGizmoEntity(entity, component) || gizmoCreated;
        }
        if (!gizmoCreated)
        {
            CreateGizmoEntity(entity, entity.Transform, typeof(FallbackGizmo));
        }
    }

    private bool CreateGizmoEntity(Entity entity, EntityComponent component)
    {
        var gizmoType = GetGizmoType(component.GetType());
        if (gizmoType == null)
            return false;

        CreateGizmoEntity(entity, component, gizmoType);
        return true;
    }

    private void CreateGizmoEntity(Entity entity, EntityComponent component, Type gizmoType)
    {
        var gizmoEntities = entity.Tags.Get(GizmoEntitiesKey);
        if (gizmoEntities == null)
        {
            gizmoEntities = [];
            entity.Tags.Set(GizmoEntitiesKey, gizmoEntities);
        }

        entity.Tags.TryGetValue(GizmoBase.NoGizmoKey, out var noGizmo);
        if (noGizmo)
            return;

        IEntityGizmo gizmo;
        try
        {
            gizmo = (IEntityGizmo)Activator.CreateInstance(gizmoType, component)!;
        }
        catch (MissingMethodException e)
        {
            throw new MissingMethodException($"{nameof(IEntityGizmo)} '{gizmoType}' must have a constructor with exactly one parameter of type '{component.GetType()}'", e);
        }
        gizmo.Initialize(Game.Services, editorScene);

        gizmo.SizeFactor = GizmoSize;
        gizmo.Update();

        // Register the gizmo into the scene entity and vice versa
        gizmoEntities[component] = gizmo;
        sceneGizmos.Add(gizmo);
        if (!gizmoVisibilities.TryGetValue(gizmoType, out var isVisible))
            isVisible = true;

        gizmo.IsEnabled = isVisible;
    }

    private void RemoveGizmo(Dictionary<EntityComponent, IEntityGizmo> gizmoEntities, IEntityGizmo gizmo, EntityComponent component)
    {
        sceneGizmos.Remove(gizmo);
        gizmoEntities.Remove(component);
        gizmo.Dispose();
    }

    private static Type? GetGizmoType(Type componentType)
    {
        Type? gizmoType;
        var type = componentType;
        while (!StrideEditorPlugin.GizmoTypeDictionary.TryGetValue(type, out gizmoType))
        {
            type = type.BaseType ?? throw new ArgumentException(@"The given type is not an EntityComponent type", nameof(componentType));
            if (type == typeof(EntityComponent))
                return null;
        }
        return gizmoType;
    }
}
