// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering;

namespace Xenko.Physics.Engine
{
    public class PhysicsShapesRenderingService : GameSystem
    {
        private GraphicsDevice graphicsDevice;

        private enum ComponentType
        {
            Trigger,
            Static,
            Dynamic,
            Kinematic,
            Character,
        }

        private readonly Dictionary<ComponentType, Color> componentTypeColor = new Dictionary<ComponentType, Color>
        {
            { ComponentType.Trigger, Color.Purple },
            { ComponentType.Static, Color.Red },
            { ComponentType.Dynamic, Color.Green },
            { ComponentType.Kinematic, Color.Blue },
            { ComponentType.Character, Color.LightPink },
        };

        private readonly Dictionary<ComponentType, Material> componentTypeDefaultMaterial = new Dictionary<ComponentType, Material>();
        private readonly Dictionary<ComponentType, Material> componentTypeStaticPlaneMaterial = new Dictionary<ComponentType, Material>();

        private readonly Dictionary<Type, MeshDraw> debugMeshCache = new Dictionary<Type, MeshDraw>();
        private readonly Dictionary<ColliderShape, MeshDraw> debugMeshCache2 = new Dictionary<ColliderShape, MeshDraw>();

        public override void Initialize()
        {
            graphicsDevice = Services.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;

            foreach (var typeObject in Enum.GetValues(typeof(ComponentType)))
            {
                var type = (ComponentType)typeObject;
                componentTypeDefaultMaterial[type] = PhysicsDebugShapeMaterial.CreateDefault(graphicsDevice, Color.AdjustSaturation(componentTypeColor[type], 0.77f), 1);
                componentTypeStaticPlaneMaterial[type] = componentTypeDefaultMaterial[type];
                // TODO enable this once material is implemented.
                // ComponentTypeStaticPlaneMaterial[type] = PhysicsDebugShapeMaterial.CreateStaticPlane(graphicsDevice, Color.AdjustSaturation(ComponentTypeColor[type], 0.77f), 1); 
            }
        }

        public PhysicsShapesRenderingService(IServiceRegistry registry) : base(registry)
        {
        }

        public Entity CreateDebugEntity(PhysicsComponent component, RenderGroup renderGroup, bool alwaysAddOffset = false)
        {
            if (component?.ColliderShape == null) return null;

            if (component.DebugEntity != null) return null;

            var debugEntity = new Entity();

            var skinnedElement = component as PhysicsSkinnedComponentBase;
            if (skinnedElement != null && skinnedElement.BoneIndex != -1)
            {
                Vector3 scale, pos;
                Quaternion rot;
                skinnedElement.BoneWorldMatrixOut.Decompose(out scale, out rot, out pos);
                debugEntity.Transform.Position = pos;
                debugEntity.Transform.Rotation = rot;
            }
            else
            {
                Vector3 scale, pos;
                Quaternion rot;
                component.Entity.Transform.WorldMatrix.Decompose(out scale, out rot, out pos);
                debugEntity.Transform.Position = pos;
                debugEntity.Transform.Rotation = rot;
            }

            var shouldNotAddOffset = component is RigidbodyComponent || component is CharacterComponent;

            //don't add offset for non bone dynamic and kinematic as it is added already in the updates
            var colliderEntity = CreateChildEntity(component, component.ColliderShape, renderGroup, alwaysAddOffset || !shouldNotAddOffset);
            if (colliderEntity != null) debugEntity.AddChild(colliderEntity);

            return debugEntity;
        }

        private Entity CreateChildEntity(PhysicsComponent component, ColliderShape shape, RenderGroup renderGroup, bool addOffset)
        {
            if (shape == null)
                return null;

            switch (shape.Type)
            {
                case ColliderShapeTypes.Compound:
                    {
                        var entity = new Entity();

                        //We got to recurse
                        var compound = (CompoundColliderShape)shape;
                        for (var i = 0; i < compound.Count; i++)
                        {
                            var subShape = compound[i];
                            var subEntity = CreateChildEntity(component, subShape, renderGroup, true); //always add offsets to compounds
                            if (subEntity != null)
                            {
                                entity.AddChild(subEntity);
                            }
                        }

                        entity.Transform.LocalMatrix = Matrix.Identity;
                        entity.Transform.UseTRS = false;

                        compound.DebugEntity = entity;

                        return entity;
                    }
                case ColliderShapeTypes.Box:
                case ColliderShapeTypes.Capsule:
                case ColliderShapeTypes.ConvexHull:
                case ColliderShapeTypes.Cylinder:
                case ColliderShapeTypes.Sphere:
                case ColliderShapeTypes.Cone:
                case ColliderShapeTypes.StaticPlane:
                    {
                        MeshDraw draw;
                        var type = shape.GetType();
                        if (type == typeof(CapsuleColliderShape) || type == typeof(ConvexHullColliderShape))
                        {
                            if (!debugMeshCache2.TryGetValue(shape, out draw))
                            {
                                draw = shape.CreateDebugPrimitive(graphicsDevice);
                                debugMeshCache2[shape] = draw;
                            }
                        }
                        else
                        {
                            if (!debugMeshCache.TryGetValue(shape.GetType(), out draw))
                            {
                                draw = shape.CreateDebugPrimitive(graphicsDevice);
                                debugMeshCache[shape.GetType()] = draw;
                            }
                        }

                        var entity = new Entity
                        {
                            new ModelComponent
                            {
                                Model = new Model
                                {
                                    GetMaterial(component, shape),
                                    new Mesh
                                    {
                                        Draw = draw,
                                    },
                                },
                                RenderGroup = renderGroup,
                            },
                        };

                        var offset = addOffset ? Matrix.RotationQuaternion(shape.LocalRotation) * Matrix.Translation(shape.LocalOffset) : Matrix.Identity;

                        entity.Transform.LocalMatrix = shape.DebugPrimitiveMatrix * offset * Matrix.Scaling(shape.Scaling);

                        entity.Transform.UseTRS = false;

                        shape.DebugEntity = entity;

                        return entity;
                    }
                default:
                    return null;
            }
        }

        private Material GetMaterial(EntityComponent component, ColliderShape shape)
        {
            var componentType = ComponentType.Trigger;

            var rigidbodyComponent = component as RigidbodyComponent;
            if (rigidbodyComponent != null)
            {
                componentType = rigidbodyComponent.IsTrigger ? ComponentType.Trigger : 
                    rigidbodyComponent.IsKinematic ? ComponentType.Kinematic : ComponentType.Dynamic;
            }
            else if (component is CharacterComponent)
            {
                componentType = ComponentType.Character;
            }
            else if (component is StaticColliderComponent)
            {
                var staticCollider = (StaticColliderComponent)component;
                componentType = staticCollider.IsTrigger ? ComponentType.Trigger : ComponentType.Static;
            }

            return shape is StaticPlaneColliderShape
                ? componentTypeStaticPlaneMaterial[componentType]
                : componentTypeDefaultMaterial[componentType];
        }
    }
}
