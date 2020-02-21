// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xenko.Core.BuildEngine;
using Xenko.Core;
using Xenko.Core.Extensions;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Game;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Services;
using Xenko.Assets.Presentation.AssetEditors.Gizmos;
using Xenko.Assets.Presentation.SceneEditor;
using Xenko.Editor.EditorGame.Game;
using Xenko.Engine;
using Xenko.Engine.Design;
using Xenko.Rendering;
using Xenko.Rendering.Compositing;
using Xenko.Rendering.Sprites;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
{
    public class EditorGameComponentGizmoService : EditorGameServiceBase, IEditorGameComponentGizmoService, IEditorGameComponentGizmoViewModelService
    {
        internal static readonly Dictionary<GizmoTransformationAxes, int> PlaneToIndex = new Dictionary<GizmoTransformationAxes, int> { { GizmoTransformationAxes.YZ, 0 }, { GizmoTransformationAxes.XZ, 1 }, { GizmoTransformationAxes.XY, 2 } };
        internal static readonly PropertyKey<Entity> ReferencedEntityKey = new PropertyKey<Entity>("ReferencedEntityProperty", typeof(EntityHierarchyEditorGame));
        private readonly IEditorGameController controller;
        private static readonly PropertyKey<Dictionary<EntityComponent, IEntityGizmo>> GizmoEntitiesKey = new PropertyKey<Dictionary<EntityComponent, IEntityGizmo>>("GizmoEntitiesProperty", typeof(EditorGameComponentGizmoService));
        public static readonly PropertyKey<bool> SelectedKey = new PropertyKey<bool>("SelectedProperty", typeof(EditorGameComponentGizmoService));
        private readonly HashSet<IEntityGizmo> sceneGizmos = new HashSet<IEntityGizmo>();
        private readonly Dictionary<Type, bool> gizmoVisibilities = new Dictionary<Type, bool>();
        private EntityHierarchyEditorGame game;
        private Scene editorScene;
        private SceneInstance gameSceneInstance;
        private PickingSceneRenderer gizmoEntityPicker;
        private float gizmoSize = 1.0f;

        private readonly HashSet<Entity> selectedEntities = new HashSet<Entity>();

        public EditorGameComponentGizmoService(IEditorGameController controller)
        {
            this.controller = controller;
        }

        /// <summary>
        /// Gets the scene unit.
        /// </summary>
        public float SceneUnit => Math.Max(1, game.EditorServices.Get<IEditorGameCameraService>().SceneUnit);

        public float GizmoSize { get => gizmoSize; set { gizmoSize = value; controller.InvokeAsync(() => sceneGizmos.ForEach(x => x.SizeFactor = value)); } }

        public bool FixedSize { get; set; }

        public override IEnumerable<Type> Dependencies { get { yield return typeof(IEditorGameCameraService); } }

        public Entity GetContentEntityUnderMouse()
        {
            var pickResult = gizmoEntityPicker.Pick();
            var pickedComponentId = pickResult.ComponentId;
            var selectedGizmo = sceneGizmos.FirstOrDefault(x => x.IsUnderMouse(pickedComponentId));
            return selectedGizmo?.ContentEntity;
        }

        /// <summary>
        /// Update all the gizmo of the scene
        /// </summary>
        public async Task Update()
        {
            // update all gizmo of the scene.
            while (!IsDisposed)
            {
                await game.Script.NextFrame();
                sceneGizmos.ForEach(x => x.Update());
            }
        }


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

            // Initialize and add the Gizmo entities to the gizmo scene
            MicrothreadLocalDatabases.MountCommonDatabase();

            // initialize the gizmo
            foreach (var gizmo in sceneGizmos)
                gizmo.Initialize(editorGame.Services, editorScene);

            editorGame.Script.AddTask(Update);
            return Task.FromResult(true);
        }

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

        private void RemoveGizmoEntities(object sender, Entity entity)
        {
            var gizmoEntities = entity.Tags.Get(GizmoEntitiesKey);
            if (gizmoEntities == null)
                return;

            // reset the gizmo entities
            entity.Tags.Set(GizmoEntitiesKey, null);

            // remove the gizmo entities from the scene
            foreach (var gizmoEntity in gizmoEntities.Values)
            {
                sceneGizmos.Remove(gizmoEntity);
                gizmoEntity.Dispose();
            }

            selectedEntities.Remove(entity);
        }

        private void UpdateGizmoEntities(object sender, EntityComponentEventArgs e)
        {
            var entity = e.Entity;
            var oldComponent = e.PreviousComponent;
            var newComponent = e.NewComponent;
            var gizmoEntities = entity.Tags.Get(GizmoEntitiesKey);

            // remove the old component gizmo
            if (gizmoEntities != null && oldComponent != null && gizmoEntities.TryGetValue(oldComponent, out IEntityGizmo gizmo))
            {
                RemoveGizmo(gizmoEntities, gizmo, oldComponent);
                UpdateMainGizmo(entity);
            }

            // create the new component gizmo
            if (newComponent != null)
            {
                var isMainGizmo = false;
                var gizmoType = GetGizmoType(newComponent.GetType());
                if (gizmoType != null)
                {
                    var attribute = gizmoType.GetCustomAttribute<GizmoComponentAttribute>(true);
                    isMainGizmo = attribute.IsMainGizmo;
                    // Remove the fallback gizmo if we got a new component.
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
            IEntityGizmo gizmo;

            // Remove any main gizmo that is not corresponding to the current main component
            foreach (var component in entity.Components)
            {
                var attribute = GetGizmoType(component.GetType())?.GetCustomAttribute<GizmoComponentAttribute>(true);
                if (attribute != null && attribute.IsMainGizmo && component != mainComponent)
                {
                    gizmo = gizmoEntities.TryGetValue(component);
                    if (gizmo != null)
                        RemoveGizmo(gizmoEntities, gizmo, component);
                }

            }

            // Creates the main gizmo if it does not exist
            if (mainComponent != null && !gizmoEntities.TryGetValue(mainComponent, out gizmo))
            {
                CreateGizmoEntity(entity, mainComponent);
            }
            if (gizmoEntities == null || gizmoEntities.Count == 0)
            {
                CreateGizmoEntity(entity, entity.Transform, typeof(FallbackGizmo));
            }
        }

        private EntityComponent GetMainGizmoComponent(Entity entity)
        {
            EntityComponent mainComponent = null;
            var mainComponentOrder = int.MinValue;
            foreach (var component in entity.Components)
            {
                var gizmoType = GetGizmoType(component.GetType());
                if (gizmoType == null)
                    continue;

                if (gizmoVisibilities.TryGetValue(gizmoType, out bool isVisible) && !isVisible)
                    continue;

                var attribute = gizmoType.GetCustomAttribute<GizmoComponentAttribute>(true);
                if (attribute == null || !attribute.IsMainGizmo)
                    continue;

                var compIndex = XenkoDefaultAssetsPlugin.ComponentOrders.IndexOf(t => t.type == component.GetType());
                var order = compIndex >= 0 ? XenkoDefaultAssetsPlugin.ComponentOrders[compIndex].order : default(int);
                if (order > mainComponentOrder)
                {
                    mainComponentOrder = order;
                    mainComponent = component;
                }
            }

            return mainComponent;
        }

        private IEnumerable<EntityComponent> GetGizmosToCreate(Entity entity)
        {
            var list = new List<EntityComponent>();

            foreach (var component in entity.Components)
            {
                var attribute = GetGizmoType(component.GetType())?.GetCustomAttribute<GizmoComponentAttribute>(true);
                if (attribute != null && !attribute.IsMainGizmo)
                    list.Add(component);
            }
            var mainComponent = GetMainGizmoComponent(entity);
            if (mainComponent != null)
            {
                list.Add(mainComponent);
            }
            return list;
        }

        private void CreateGizmoEntities(object sender, Entity entity)
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
            // create the gizmo
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
                gizmoEntities = new Dictionary<EntityComponent, IEntityGizmo>();
                entity.Tags.Set(GizmoEntitiesKey, gizmoEntities);
            }

            entity.Tags.TryGetValue(GizmoBase.NoGizmoKey, out bool noGizmo);
            if (noGizmo)
                return;

            // initialize the gizmo
            var gizmo = (EntityGizmo)Activator.CreateInstance(gizmoType, component);
            gizmo.InitializeContentEntity(entity);
            gizmo.Initialize(game.Services, editorScene);

            gizmo.SizeFactor = GizmoSize;
            gizmo.Update();

            // register the gizmo into the scene entity and vice-versa
            gizmoEntities[component] = gizmo;
            sceneGizmos.Add(gizmo);
            if (!gizmoVisibilities.TryGetValue(gizmoType, out bool isVisible))
                isVisible = true;

            gizmo.IsEnabled = isVisible;
        }

        private void RemoveGizmo(IDictionary<EntityComponent, IEntityGizmo> gizmoEntities, IEntityGizmo gizmo, EntityComponent component)
        {
            sceneGizmos.Remove(gizmo);
            gizmoEntities.Remove(component);
            gizmo.Dispose();
        }

        private Type GetGizmoType(Type componentType)
        {
            if (componentType == null) throw new ArgumentNullException(nameof(componentType));
            Type gizmoType;
            while (!XenkoDefaultAssetsPlugin.GizmoTypeDictionary.TryGetValue(componentType, out gizmoType))
            {
                componentType = componentType.BaseType;
                if (componentType == null) throw new ArgumentException(@"The given type is not an EntityComponent type", nameof(componentType));
                if (componentType == typeof(EntityComponent))
                    return null;
            }
            return gizmoType;
        }

        void IEditorGameComponentGizmoViewModelService.ToggleGizmoVisibility(Type componentType, bool isVisible)
        {
            controller.InvokeAsync(() =>
            {
                var gizmoType = componentType != typeof(TransformComponent) ? GetGizmoType(componentType) : typeof(FallbackGizmo);
                if (gizmoType == null)
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

    }
}
