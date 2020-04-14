// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Shaders.Ast;
using Stride.Engine;
using Stride.Extensions;
using Stride.Physics.Engine;
using Stride.Graphics;
using Stride.Physics;
using Stride.Rendering;
using Stride.Core.Assets;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    [GizmoComponent(typeof(PhysicsComponent), false)]
    public class PhysicsGizmo : EntityGizmo<PhysicsComponent>
    {
        private readonly List<Entity> spawnedEntities = new List<Entity>();

        private bool rendering;

        public PhysicsGizmo(PhysicsComponent component) : base(component)
        {
            RenderGroup = PhysicsShapesGroup;
        }

        protected override Entity Create()
        {
            return new Entity("Physics Gizmo Root Entity (id={0})".ToFormat(ContentEntity.Id));
        }

        protected override void Destroy()
        {
            foreach (var spawnedEntity in spawnedEntities)
            {
                EditorScene.Entities.Remove(spawnedEntity);
            }
        }

        private struct PhysicsElementInfo
        {
            private readonly ColliderShape shape;
            private readonly bool isKinematic;
            private readonly bool isTrigger;
            private readonly string boneName;
            private readonly List<IInlineColliderShapeDesc> colliderShapes;
            private readonly SkeletonUpdater skeleton;
            private readonly int boneIndex;

            public PhysicsElementInfo(PhysicsComponent component, SkeletonUpdater skeleton)
            {
                shape = component.ColliderShape;
                var rigidbodyComponent = component as RigidbodyComponent;
                isKinematic = rigidbodyComponent != null && rigidbodyComponent.IsKinematic;
                colliderShapes = component.ColliderShapes != null ? CloneDescs(component.ColliderShapes) : null;
                var componentBase = component as PhysicsSkinnedComponentBase;
                boneName = componentBase?.NodeName;
                this.skeleton = skeleton;
                boneIndex = componentBase?.BoneIndex ?? -1;
                var triggerBase = component as PhysicsTriggerComponentBase;
                isTrigger = triggerBase != null && triggerBase.IsTrigger;
            }

            public bool HasChanged(PhysicsComponent component, SkeletonUpdater skeletonUpdater)
            {
                var componentBase = component as PhysicsSkinnedComponentBase;
                var triggerBase = component as PhysicsTriggerComponentBase;
                var newBoneName = componentBase?.NodeName;
                var newIndex = componentBase?.BoneIndex ?? -1;
                var rb = component as RigidbodyComponent;

                return shape != component.ColliderShape ||
                (colliderShapes == null && component.ColliderShapes != null) ||
                (colliderShapes != null && component.ColliderShapes == null) ||
                DescsAreDifferent(colliderShapes, component.ColliderShapes) ||
                component.ColliderShapeChanged ||
                (rb != null && isKinematic != rb.IsKinematic) || 
                skeleton != skeletonUpdater ||
                boneIndex != newIndex ||
                boneIndex == -1 && skeletonUpdater != null && !string.IsNullOrEmpty(boneName) || //force recreation if we have a skeleton?.. wrong name tho is also possible...
                triggerBase != null && triggerBase.IsTrigger != isTrigger ||
                shape != null && component.DebugEntity == null || //force recreation in this case as well
                boneName != newBoneName;
            }

            private static List<IInlineColliderShapeDesc> CloneDescs(IEnumerable<IInlineColliderShapeDesc> descs)
            {
                var res = new List<IInlineColliderShapeDesc>();
                foreach (var desc in descs)
                {
                    if (desc == null)
                    {
                        res.Add(null);
                    }
                    else
                    {
                        if (desc.GetType() == typeof(ColliderShapeAssetDesc))
                        {
                            var sourceDesc = (ColliderShapeAssetDesc)desc;
                            var assetDesc = new ColliderShapeAssetDesc
                            {
                                Shape = sourceDesc.Shape
                            };
                            res.Add(assetDesc);
                        }
                        else
                        {
                            var cloned = AssetCloner.Clone(desc, AssetClonerFlags.KeepReferences);
                            res.Add(cloned);
                        }
                    }
                }
                return res;
            }
        }

        private readonly Dictionary<PhysicsComponent, PhysicsElementInfo> elementToEntity = new Dictionary<PhysicsComponent, PhysicsElementInfo>();

        private static bool DescsAreDifferent(IList<IInlineColliderShapeDesc> left, IList<IInlineColliderShapeDesc> right)
        {
            if (left == null && right != null || right == null && left != null) return true;

            if (left == null) return false;

            if (left.Count != right.Count) return true;

            for (var i = 0; i < left.Count; i++)
            {
                var leftDesc = left[i];
                var rightDesc = right[i];
                if (leftDesc != null && !leftDesc.Match(rightDesc)) return true;
            }

            return false;
        }

        public override void Update()
        {
            if (ContentEntity == null)
                return;

            if ((!IsEnabled || !Component.Enabled) && Component.DebugEntity != null)
            {
                if (!rendering) return;

                Component.DebugEntity.Enable<ModelComponent>(false, true);
                rendering = false;
                return;
            }

            // Create and add the element missing
            PhysicsElementInfo entityInfo;

            var skinnedElement = Component as PhysicsSkinnedComponentBase;
            var modelComponent = ContentEntity.Get<ModelComponent>();
            var skeleton = modelComponent?.Skeleton;

            if (!elementToEntity.TryGetValue(Component, out entityInfo) || entityInfo.HasChanged(Component, skeleton))
            {
                //remove and clean up the old debug entity
                if (Component.DebugEntity != null)
                {
                    spawnedEntities.Remove(Component.DebugEntity);
                    Component.RemoveDebugEntity(EditorScene);
                    Component.DebugEntity = null;
                }

                //compose shape and fill data as data is not being filled by the processor when we run from the editor
                Component.ComposeShape();

                // check for bones
                if (skinnedElement?.ColliderShape != null)
                {                       
                    if (skeleton != null)
                    {
                        var boneIndex = skeleton.Nodes.IndexOf(x => x.Name == skinnedElement.NodeName);
                        if (boneIndex != -1)
                        {
                            skinnedElement.BoneIndex = boneIndex;

                            Vector3 position, scale;
                            Quaternion rotation;

                            ContentEntity.Transform.UpdateWorldMatrix();
                            var isScalingNegative = false;
                            if (ContentEntity.Transform.WorldMatrix.Decompose(out scale, out rotation, out position))
                            {
                                isScalingNegative = scale.X * scale.Y * scale.Z < 0.0f;
                            }
                            skeleton.NodeTransformations[0].LocalMatrix = ContentEntity.Transform.WorldMatrix;
                            skeleton.NodeTransformations[0].IsScalingNegative = isScalingNegative;
                            skeleton.UpdateMatrices();

                            skinnedElement.BoneWorldMatrixOut = skeleton.NodeTransformations[boneIndex].WorldMatrix;

                            if (skinnedElement.ColliderShape.LocalOffset != Vector3.Zero || skinnedElement.ColliderShape.LocalRotation != Quaternion.Identity)
                            {
                                skinnedElement.BoneWorldMatrixOut = Matrix.Multiply(skinnedElement.ColliderShape.PositiveCenterMatrix, skinnedElement.BoneWorldMatrixOut);
                            }
                        }
                    }
                }

                Component.AddDebugEntity(EditorScene, RenderGroup, Component.BoneIndex == -1);
                if (Component.DebugEntity != null)
                {
                    spawnedEntities.Add(Component.DebugEntity);
                    Component.DebugEntity?.Enable<ModelComponent>(false, true);
                    rendering = false; //make sure we refresh enabled flags?
                }

                elementToEntity[Component] = new PhysicsElementInfo(Component, skeleton);
            }

            if (Component.DebugEntity != null)
            {
                if (IsEnabled && Component.Enabled && !rendering)
                {
                    Component.DebugEntity?.Enable<ModelComponent>(true, true);
                    rendering = true;
                }

                if (skinnedElement != null && skinnedElement.BoneIndex != -1 && skinnedElement.ColliderShape != null)
                {
                    Vector3 pos, scale;
                    Quaternion rot;

                    if (skeleton != null)
                    {
                        ContentEntity.Transform.UpdateWorldMatrix();
                        var isScalingNegative = false;
                        if (ContentEntity.Transform.WorldMatrix.Decompose(out scale, out rot, out pos))
                        {
                            isScalingNegative = scale.X*scale.Y*scale.Z < 0.0f;
                        }
                        skeleton.NodeTransformations[0].LocalMatrix = ContentEntity.Transform.WorldMatrix;
                        skeleton.NodeTransformations[0].IsScalingNegative = isScalingNegative;
                        skeleton.UpdateMatrices();

                        skinnedElement.BoneWorldMatrixOut = skeleton.NodeTransformations[skinnedElement.BoneIndex].WorldMatrix;

                        if (skinnedElement.ColliderShape.LocalOffset != Vector3.Zero || skinnedElement.ColliderShape.LocalRotation != Quaternion.Identity)
                        {
                            skinnedElement.BoneWorldMatrixOut = Matrix.Multiply(skinnedElement.ColliderShape.PositiveCenterMatrix, skinnedElement.BoneWorldMatrixOut);
                        }
                    }

                    skinnedElement.BoneWorldMatrixOut.Decompose(out scale, out rot, out pos);
                    Component.DebugEntity.Transform.Position = pos;
                    Component.DebugEntity.Transform.Rotation = rot;

                    if (Component.CanScaleShape && Component.DebugEntity.Transform.Scale != scale)
                    {
                        Component.DebugEntity.Transform.Scale = scale;
                    }
                }
                else
                {
                    Vector3 scale, pos;
                    Quaternion rot;
                    ContentEntity.Transform.WorldMatrix.Decompose(out scale, out rot, out pos);
                    Component.DebugEntity.Transform.Position = pos;
                    Component.DebugEntity.Transform.Rotation = rot;

                    if (Component.CanScaleShape && Component.DebugEntity.Transform.Scale != scale)
                    {
                        Component.DebugEntity.Transform.Scale = scale;
                    }
                }
            }

            GizmoRootEntity.Transform.LocalMatrix = ContentEntity.Transform.WorldMatrix;
            GizmoRootEntity.Transform.UseTRS = false;
        }

        public PhysicsGizmo(EntityComponent component) : base(component)
        {
        }
    }
}
