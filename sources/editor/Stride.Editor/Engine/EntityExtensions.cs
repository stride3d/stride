// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Navigation;
using Stride.Particles.Components;
using Stride.Rendering;
using Stride.SpriteStudio.Runtime;

namespace Stride.Editor.Engine
{
    public static class EntityExtensions
    {
        public static Entity FindSubEntity(this Entity entity, Guid subEntityId)
        {
            if (entity.Id == subEntityId)
                return entity;

            foreach (var child in entity.Transform.Children)
            {
                if (child.Entity.FindSubEntity(subEntityId) is Entity e)
                    return e;
            }

            return null;
        }

        public static Entity FindSubEntity(this Scene scene, Guid subEntityId)
        {
            if (scene.Entities.Count == 0)
                return null;
            
            if (scene.Entities[0].EntityManager is EntityManager manager)
            {
                // The entity manager contains all entities of all the scenes under the root scene,
                // it should be faster in most cases to iterate over that instead of going through the entity tree
                var e = manager.GetEnumerator();
                while (e.MoveNext())
                {
                    if (e.Current.Id == subEntityId && ReferenceEquals(scene, e.Current.Scene))
                        return e.Current;
                }
            }
            else // Slow path, go through tree recursively
            {
                foreach (Entity entity in scene.Entities)
                {
                    if (entity.FindSubEntity(subEntityId) is Entity e)
                        return e;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Calculate the bounding sphere of the entity's models.
        /// </summary>
        /// <param name="entity">The entity to measure</param>
        /// <param name="isRecursive">Indicate the child entities bounding spheres should be merged</param>
        /// <param name="meshSelector">Selects which meshes are considered for bounding box calculation.</param>
        /// <returns>The bounding sphere (world matrix included)</returns>
        public static BoundingSphere CalculateBoundSphere(this Entity entity, bool isRecursive = true, Func<Model, IEnumerable<Mesh>> meshSelector = null)
        {
            entity.Transform.UpdateWorldMatrix();
            var worldMatrix = entity.Transform.WorldMatrix;

            var boundingSphere = BoundingSphere.Empty;
            
            // calculate the bounding sphere of the model if any
            var modelComponent = entity.Get<ModelComponent>();
            var hasModel = modelComponent?.Model != null;
            if (hasModel)
            {
                var hierarchy = modelComponent.Skeleton;
                var nodeTransforms = new Matrix[hierarchy.Nodes.Length];

                // Calculate node transforms here, since there might not be a ModelProcessor running
                for (int i = 0; i < nodeTransforms.Length; i++)
                {
                    if (hierarchy.Nodes[i].ParentIndex == -1)
                    {
                        nodeTransforms[i] = worldMatrix;
                    }
                    else
                    {
                        Matrix localMatrix;

                        Matrix.Transformation(
                            ref hierarchy.Nodes[i].Transform.Scale,
                            ref hierarchy.Nodes[i].Transform.Rotation,
                            ref hierarchy.Nodes[i].Transform.Position, out localMatrix);

                        Matrix.Multiply(ref localMatrix, ref nodeTransforms[hierarchy.Nodes[i].ParentIndex], out nodeTransforms[i]);
                    }
                }

                // calculate the bounding sphere
                var boundingBox = BoundingBoxExt.Empty;
                    
                var meshes = modelComponent.Model.Meshes;
                var filteredMeshes = meshSelector == null ? meshes : meshSelector(modelComponent.Model);

                // Calculate skinned bounding boxes.
                // TODO: Cloned from ModelSkinningUpdater. Consolidate.
                foreach (var mesh in filteredMeshes)
                {
                    var skinning = mesh.Skinning;

                    if (skinning == null)
                    {
                        // For unskinned meshes, use the original bounding box
                        var boundingBoxExt = (BoundingBoxExt)mesh.BoundingBox;
                        boundingBoxExt.Transform(nodeTransforms[mesh.NodeIndex]);
                        BoundingBoxExt.Merge(ref boundingBox, ref boundingBoxExt, out boundingBox);
                    }
                    else
                    {
                        var bones = skinning.Bones;
                        var bindPoseBoundingBox = new BoundingBoxExt(mesh.BoundingBox);

                        for (var index = 0; index < bones.Length; index++)
                        {
                            var nodeIndex = bones[index].NodeIndex;
                            Matrix boneMatrix;

                            // Compute bone matrix
                            Matrix.Multiply(ref bones[index].LinkToMeshMatrix, ref nodeTransforms[nodeIndex], out boneMatrix);

                            // Fast AABB transform: http://zeuxcg.org/2010/10/17/aabb-from-obb-with-component-wise-abs/
                            // Compute transformed AABB (by world)
                            var boundingBoxExt = bindPoseBoundingBox;
                            boundingBoxExt.Transform(boneMatrix);
                            BoundingBoxExt.Merge(ref boundingBox, ref boundingBoxExt, out boundingBox);
                        }
                    }
                }
                var halfSize = boundingBox.Extent;
                var maxHalfSize = Math.Max(halfSize.X, Math.Max(halfSize.Y, halfSize.Z));
                boundingSphere = BoundingSphere.Merge(boundingSphere, new BoundingSphere(boundingBox.Center, maxHalfSize));
            }

            // Calculate the bounding sphere for the sprite component if any and merge the result
            var spriteComponent = entity.Get<SpriteComponent>();
            var hasSprite = spriteComponent?.CurrentSprite != null;
            if (hasSprite && !(hasModel && meshSelector != null))
            {
                var spriteSize = spriteComponent.CurrentSprite.Size;
                var spriteDiagonalSize = (float)Math.Sqrt(spriteSize.X * spriteSize.X + spriteSize.Y * spriteSize.Y);

                // Note: this is probably wrong, need to unify with SpriteComponentRenderer
                var center = worldMatrix.TranslationVector;
                var scales = new Vector3(worldMatrix.Row1.Length(), worldMatrix.Row2.Length(), worldMatrix.Row3.Length());
                var maxScale = Math.Max(scales.X, Math.Max(scales.Y, scales.Z));

                boundingSphere = BoundingSphere.Merge(boundingSphere, new BoundingSphere(center, maxScale * spriteDiagonalSize / 2f));
            }

            var spriteStudioComponent = entity.Get<SpriteStudioComponent>();
            if (spriteStudioComponent != null)
            {
                // Make sure nodes are prepared
                if (!SpriteStudioProcessor.PrepareNodes(spriteStudioComponent)) return new BoundingSphere();

                // Update root nodes
                foreach (var node in spriteStudioComponent.Nodes)
                {
                    node.UpdateTransformation();
                }

                // Compute bounding sphere for each node
                foreach (var node in spriteStudioComponent.Nodes.SelectDeep(x => x.ChildrenNodes))
                {
                    if (node.Sprite == null || node.Hide != 0) continue;

                    var nodeMatrix = node.ModelTransform * worldMatrix;

                    var spriteSize = node.Sprite.Size;
                    var spriteDiagonalSize = (float)Math.Sqrt(spriteSize.X * spriteSize.X + spriteSize.Y * spriteSize.Y);

                    Vector3 pos, scale;
                    nodeMatrix.Decompose(out scale, out pos);

                    var center = pos;
                    var maxScale = Math.Max(scale.X, scale.Y); //2d ignore Z

                    boundingSphere = BoundingSphere.Merge(boundingSphere, new BoundingSphere(center, maxScale * (spriteDiagonalSize / 2f)));
                }
            }

            var particleComponent = entity.Get<ParticleSystemComponent>();
            if (particleComponent != null)
            {
                var center = worldMatrix.TranslationVector;
                var sphere = particleComponent.ParticleSystem?.BoundingShape != null ? BoundingSphere.FromBox(particleComponent.ParticleSystem.BoundingShape.GetAABB(center, Quaternion.Identity, 1.0f)) : new BoundingSphere(center, 2.0f);
                boundingSphere = BoundingSphere.Merge(boundingSphere, sphere);
            }

            var boundingBoxComponent = entity.Get<NavigationBoundingBoxComponent>();
            if (boundingBoxComponent != null)
            {
                var center = worldMatrix.TranslationVector;
                var scales = new Vector3(worldMatrix.Row1.Length(), worldMatrix.Row2.Length(), worldMatrix.Row3.Length()) * boundingBoxComponent.Size;
                boundingSphere = BoundingSphere.FromBox(new BoundingBox(-scales + center, scales + center));
            }

            // Extend the bounding sphere to include the children
            if (isRecursive)
            {
                foreach (var child in entity.GetChildren())
                    boundingSphere = BoundingSphere.Merge(boundingSphere, child.CalculateBoundSphere(true, meshSelector));
            }

            // If the entity does not contain any components having an impact on the bounding sphere, create an empty bounding sphere centered on the entity position.
            if (boundingSphere == BoundingSphere.Empty)
                boundingSphere = new BoundingSphere(worldMatrix.TranslationVector, 0);

            return boundingSphere;
        }
    }
}
