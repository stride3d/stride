// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Physics.Engine;
using Stride.Rendering;

namespace Stride.Physics
{
    public class PhysicsProcessor : EntityProcessor<PhysicsComponent, PhysicsProcessor.AssociatedData>
    {
        public class AssociatedData
        {
            public PhysicsComponent PhysicsComponent;
            public TransformComponent TransformComponent;
            public ModelComponent ModelComponent; //not mandatory, could be null e.g. invisible triggers
            public bool BoneMatricesUpdated;
        }

        private readonly List<PhysicsComponent> elements = new List<PhysicsComponent>();
        private readonly List<PhysicsSkinnedComponentBase> boneElements = new List<PhysicsSkinnedComponentBase>();
        private readonly List<CharacterComponent> characters = new List<CharacterComponent>();

        private Bullet2PhysicsSystem physicsSystem;
        private Scene parentScene;
        private Scene debugScene;

        private bool colliderShapesRendering;

        private PhysicsShapesRenderingService debugShapeRendering;

        public PhysicsProcessor()
            : base(typeof(TransformComponent))
        {
            Order = 0xFFFF;
        }

        /// <summary>
        /// Gets or sets the associated parent scene to render the physics debug shapes. Assigned with default one on <see cref="OnSystemAdd"/>
        /// </summary>
        /// <value>
        /// The parent scene.
        /// </value>
        public Scene ParentScene
        {
            get => parentScene;
            set
            {
                if (value != parentScene)
                {
                    if (parentScene != null && debugShapeRendering.Enabled)
                    {
                        // If debug rendering is running, disable it and re-enable for new scene system
                        RenderColliderShapes(false);
                        parentScene = value;
                        RenderColliderShapes(true);
                    }
                    else
                    {
                        parentScene = value;
                    }
                }
            }
        }

        public Simulation Simulation { get; private set; }

        internal void RenderColliderShapes(bool enabled)
        {
            debugShapeRendering.Enabled = enabled;

            colliderShapesRendering = enabled;

            if (!colliderShapesRendering)
            {
                if (debugScene != null)
                {
                    debugScene.Dispose();

                    foreach (var element in elements)
                    {
                        element.RemoveDebugEntity(debugScene);
                    }
                    
                    // Remove from parent scene
                    debugScene.Parent = null;
                }
            }
            else
            {
                debugScene = new Scene();

                foreach (var element in elements)
                {
                    if (element.Enabled)
                    {
                        element.AddDebugEntity(debugScene, Simulation.ColliderShapesRenderGroup);
                    }
                }

                debugScene.Parent = parentScene;
            }
        }

        protected override AssociatedData GenerateComponentData(Entity entity, PhysicsComponent component)
        {
            var data = new AssociatedData
            {
                PhysicsComponent = component,
                TransformComponent = entity.Transform,
                ModelComponent = entity.Get<ModelComponent>(),
            };

            data.PhysicsComponent.Simulation = Simulation;
            data.PhysicsComponent.DebugShapeRendering = debugShapeRendering;

            return data;
        }

        protected override bool IsAssociatedDataValid(Entity entity, PhysicsComponent physicsComponent, AssociatedData associatedData)
        {
            return
                physicsComponent == associatedData.PhysicsComponent &&
                entity.Transform == associatedData.TransformComponent &&
                entity.Get<ModelComponent>() == associatedData.ModelComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, PhysicsComponent component, AssociatedData data)
        {
            // Tagged for removal? If yes, cancel it
            if (currentFrameRemovals.Remove(component))
                return;

            component.Attach(data);

            var character = component as CharacterComponent;
            if (character != null)
            {
                characters.Add(character);
            }

            if (colliderShapesRendering)
            {
                component.AddDebugEntity(debugScene, Simulation.ColliderShapesRenderGroup);
            }

            elements.Add(component);

            if (component.BoneIndex != -1)
            {
                boneElements.Add((PhysicsSkinnedComponentBase)component);
            }
        }

        private void ComponentRemoval(PhysicsComponent component)
        {
            Simulation.CleanContacts(component);

            if (component.BoneIndex != -1)
            {
                boneElements.Remove((PhysicsSkinnedComponentBase)component);
            }

            elements.Remove(component);

            if (colliderShapesRendering)
            {
                component.RemoveDebugEntity(debugScene);
            }

            var character = component as CharacterComponent;
            if (character != null)
            {
                characters.Remove(character);
            }

            component.Detach();
        }

        private readonly HashSet<PhysicsComponent> currentFrameRemovals = new HashSet<PhysicsComponent>();

        protected override void OnEntityComponentRemoved(Entity entity, PhysicsComponent component, AssociatedData data)
        {
            currentFrameRemovals.Add(component);
        }

        protected override void OnSystemAdd()
        {
            physicsSystem = (Bullet2PhysicsSystem)Services.GetService<IPhysicsSystem>();
            if (physicsSystem == null)
            {
                physicsSystem = new Bullet2PhysicsSystem(Services);
                Services.AddService<IPhysicsSystem>(physicsSystem);
                var gameSystems = Services.GetSafeServiceAs<IGameSystemCollection>();
                gameSystems.Add(physicsSystem);
            }

            ((IReferencable)physicsSystem).AddReference();

            // Check if PhysicsShapesRenderingService is created (and check if rendering is enabled with IGraphicsDeviceService)
            if (Services.GetService<Graphics.IGraphicsDeviceService>() != null && Services.GetService<PhysicsShapesRenderingService>() == null)
            {
                debugShapeRendering = new PhysicsShapesRenderingService(Services);
                var gameSystems = Services.GetSafeServiceAs<IGameSystemCollection>();
                gameSystems.Add(debugShapeRendering);
            }

            Simulation = physicsSystem.Create(this);

            parentScene = Services.GetSafeServiceAs<SceneSystem>()?.SceneInstance?.RootScene;
        }

        protected override void OnSystemRemove()
        {
            physicsSystem.Release(this);
            ((IReferencable)physicsSystem).Release();
        }

        internal void UpdateCharacters()
        {
            var charactersProfilingState = Profiler.Begin(PhysicsProfilingKeys.CharactersProfilingKey);
            var activeCharacters = 0;
            //characters need manual updating
            foreach (var element in characters)
            {
                if (!element.Enabled || element.ColliderShape == null) continue;

                var worldTransform = Matrix.RotationQuaternion(element.Orientation) * element.PhysicsWorldTransform;
                element.UpdateTransformationComponent(ref worldTransform);

                if (element.DebugEntity != null)
                {
                    Vector3 scale, pos;
                    Quaternion rot;
                    worldTransform.Decompose(out scale, out rot, out pos);
                    element.DebugEntity.Transform.Position = pos;
                    element.DebugEntity.Transform.Rotation = rot;
                }

                charactersProfilingState.Mark();
                activeCharacters++;
            }
            charactersProfilingState.End("Active characters: {0}", activeCharacters);
        }

        public override void Draw(RenderContext context)
        {
            if (Simulation.DisableSimulation) return;

            foreach (var element in boneElements)
            {
                element.UpdateDraw();
            }
        }

        internal void UpdateBones()
        {
            foreach (var element in boneElements)
            {
                element.UpdateBones();
            }
        }

        public void UpdateContacts()
        {
            foreach (var dataPair in ComponentDatas)
            {
                var data = dataPair.Value;
                var shouldProcess = data.PhysicsComponent.ProcessCollisions || ((data.PhysicsComponent as PhysicsTriggerComponentBase)?.IsTrigger ?? false);
                if (data.PhysicsComponent.Enabled && shouldProcess && data.PhysicsComponent.ColliderShape != null)
                {
                    Simulation.ContactTest(data.PhysicsComponent);
                }
            }
        }

        public void UpdateRemovals()
        {
            foreach (var currentFrameRemoval in currentFrameRemovals)
            {
                ComponentRemoval(currentFrameRemoval);
            }

            currentFrameRemovals.Clear();
        }
    }
}
