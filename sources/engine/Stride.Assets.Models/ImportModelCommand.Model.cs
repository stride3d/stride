// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.BuildEngine;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Rendering;

namespace Stride.Assets.Models
{
    public partial class ImportModelCommand
    {
        public float ScaleImport { get; set; }

        public Vector3 PivotPosition { get; set; }

        public bool MergeMeshes { get; set; }

        public bool Allow32BitIndex { get; set; }
        public int MaxInputSlots { get; set; }
        public bool AllowUnsignedBlendIndices { get; set; }
        public bool DeduplicateMaterials { get; set; }
        public List<ModelMaterial> Materials { get; set; }
        public string EffectName { get; set; }

        public List<IModelModifier> ModelModifiers { get; set; }
        
        /// <summary>
        /// Checks if the vertex buffer input slots for the model are supported by the target graphics profile level
        /// </summary>
        /// <param name="commandContext">The context for this command, used to access the logger and parameters</param>
        /// <param name="model">The model to be verified</param>
        private bool CheckInputSlots(ICommandContext commandContext, Model model)
        {
            foreach (var mesh in model.Meshes)
            {
                foreach (var vertexBufferBinding in mesh.Draw.VertexBuffers)
                {
                    if (vertexBufferBinding.Declaration.VertexElements.Length > MaxInputSlots)
                    {
                        commandContext.Logger.Error($"The number of input vertex elements ({vertexBufferBinding.Declaration.VertexElements.Length}) " +
                                                    $"is more than the maximum supported slots for this graphics level ({MaxInputSlots}).");
                        return false;
                    }
                }
            }

            return true;
        }

        private object ExportModel(ICommandContext commandContext, ContentManager contentManager)
        {
            // Read from model file
            var modelSkeleton = LoadSkeleton(commandContext, contentManager); // we get model skeleton to compare it to real skeleton we need to map to
            AdjustSkeleton(modelSkeleton);
            var model = LoadModel(commandContext, contentManager);
            if (!CheckInputSlots(commandContext, model))
            {
                return null;
            }

            // Apply materials
            foreach (var modelMaterial in Materials)
            {
                if (modelMaterial.MaterialInstance?.Material == null)
                {
                    commandContext.Logger.Verbose($"The material [{modelMaterial.Name}] is null in the list of materials.");
                }
                model.Materials.Add(modelMaterial.MaterialInstance);
            }

            model.BoundingBox = BoundingBox.Empty;

            Skeleton skeleton;
            if (SkeletonUrl != null || !MergeMeshes)
            {
                if (SkeletonUrl != null)
                {
                    // Load the skeleton 
                    skeleton = contentManager.Load<Skeleton>(SkeletonUrl);
                }
                else
                {
                    skeleton = modelSkeleton;
                    SkeletonUrl = Location + "_Skeleton_" + Guid.NewGuid();
                    contentManager.Save(SkeletonUrl, skeleton);
                }

                // Assign skeleton to model
                model.Skeleton = AttachedReferenceManager.CreateProxyObject<Skeleton>(AssetId.Empty, SkeletonUrl);
            }
            else
            {
                skeleton = null;
            }

            var skeletonMapping = new SkeletonMapping(skeleton, modelSkeleton);

            // Refresh skeleton updater with model skeleton
            var hierarchyUpdater = new SkeletonUpdater(modelSkeleton);
            hierarchyUpdater.UpdateMatrices();

            // Move meshes in the new nodes
            foreach (var mesh in model.Meshes)
            {
                // Apply scale import on meshes
                if (!MathUtil.NearEqual(ScaleImport, 1.0f))
                {
                    var transformationMatrix = Matrix.Scaling(ScaleImport);
                    for (int vbIdx = 0; vbIdx < mesh.Draw.VertexBuffers.Length; vbIdx++)
                    {
                        mesh.Draw.VertexBuffers[vbIdx].TransformBuffer(ref transformationMatrix);
                    }
                }

                var skinning = mesh.Skinning;
                if (skinning != null)
                {
                    // Update node mapping
                    // Note: we only remap skinning matrices, but we could directly remap skinning bones instead
                    for (int i = 0; i < skinning.Bones.Length; ++i)
                    {
                        var linkNodeIndex = skinning.Bones[i].NodeIndex;
                        var newLinkNodeIndex = skeletonMapping.SourceToSource[linkNodeIndex];

                        var nodeIndex = mesh.NodeIndex;
                        var newNodeIndex = skeletonMapping.SourceToSource[mesh.NodeIndex];

                        skinning.Bones[i].NodeIndex = skeletonMapping.SourceToTarget[linkNodeIndex];

                        // Adjust scale import
                        if (!MathUtil.NearEqual(ScaleImport, 1.0f))
                        {
                            skinning.Bones[i].LinkToMeshMatrix.TranslationVector = skinning.Bones[i].LinkToMeshMatrix.TranslationVector * ScaleImport;
                        }

                        // If it was remapped, we also need to update matrix
                        if (nodeIndex != newNodeIndex)
                        {
                            // Update mesh part
                            var transformMatrix = CombineMatricesFromNodeIndices(hierarchyUpdater.NodeTransformations, newNodeIndex, nodeIndex);
                            transformMatrix.Invert();
                            skinning.Bones[i].LinkToMeshMatrix = Matrix.Multiply(transformMatrix, skinning.Bones[i].LinkToMeshMatrix);
                        }

                        if (newLinkNodeIndex != linkNodeIndex)
                        {
                            // Update link part
                            var transformLinkMatrix = CombineMatricesFromNodeIndices(hierarchyUpdater.NodeTransformations, newLinkNodeIndex, linkNodeIndex);
                            skinning.Bones[i].LinkToMeshMatrix = Matrix.Multiply(skinning.Bones[i].LinkToMeshMatrix, transformLinkMatrix);
                        }
                    }
                }

                // Check if there was a remap using model skeleton
                if (skeletonMapping.SourceToSource[mesh.NodeIndex] != mesh.NodeIndex)
                {
                    // Transform vertices
                    var transformationMatrix = CombineMatricesFromNodeIndices(hierarchyUpdater.NodeTransformations, skeletonMapping.SourceToSource[mesh.NodeIndex], mesh.NodeIndex);
                    for (int vbIdx = 0; vbIdx < mesh.Draw.VertexBuffers.Length; vbIdx++)
                    {
                        mesh.Draw.VertexBuffers[vbIdx].TransformBuffer(ref transformationMatrix);
                    }

                    // Check if geometry is inverted, to know if we need to reverse winding order
                    // TODO: What to do if there is no index buffer? We should create one... (not happening yet)
                    if (mesh.Draw.IndexBuffer == null)
                        throw new InvalidOperationException();

                    Matrix rotation;
                    Vector3 scale, translation;
                    if (transformationMatrix.Decompose(out scale, out rotation, out translation)
                        && scale.X * scale.Y * scale.Z < 0)
                    {
                        mesh.Draw.ReverseWindingOrder();
                    }
                }

                // Update new node index using real asset skeleton
                mesh.NodeIndex = skeletonMapping.SourceToTarget[mesh.NodeIndex];
            }

            // Apply custom model modifiers
            if (ModelModifiers != null)
            {
                foreach (var modifier in ModelModifiers)
                {
                    modifier.Apply(commandContext, model);
                }
            }

            // Merge meshes with same parent nodes, material and skinning
            var meshesByNodes = model.Meshes.GroupBy(x => x.NodeIndex).ToList();

            foreach (var meshesByNode in meshesByNodes)
            {
                // This logic to detect similar material is kept from old code; this should be reviewed/improved at some point
                foreach (var meshesPerDrawCall in meshesByNode.GroupBy(x => x,
                    new AnonymousEqualityComparer<Mesh>((x, y) =>
                    x.MaterialIndex == y.MaterialIndex // Same material
                    && ArrayExtensions.ArraysEqual(x.Skinning?.Bones, y.Skinning?.Bones) // Same bones
                    && CompareParameters(model, x, y) // Same parameters
                    && CompareShadowOptions(model, x, y), // Same shadow parameters
                    x => 0)).ToList())
                {
                    if (meshesPerDrawCall.Count() == 1)
                    {
                        // Nothing to group, skip to next entry
                        continue;
                    }

                    // Remove old meshes
                    foreach (var mesh in meshesPerDrawCall)
                    {
                        model.Meshes.Remove(mesh);
                    }

                    // Add new combined mesh(es)
                    var baseMesh = meshesPerDrawCall.First();
                    var newMeshList = meshesPerDrawCall.Select(x => x.Draw).ToList().GroupDrawData(Allow32BitIndex);

                    foreach (var generatedMesh in newMeshList)
                    {
                        model.Meshes.Add(new Mesh(generatedMesh, baseMesh.Parameters)
                        {
                            MaterialIndex = baseMesh.MaterialIndex,
                            Name = baseMesh.Name,
                            Draw = generatedMesh,
                            NodeIndex = baseMesh.NodeIndex,
                            Skinning = baseMesh.Skinning,
                        });
                    }
                }
            }

            // split the meshes if necessary
            model.Meshes = SplitExtensions.SplitMeshes(model.Meshes, Allow32BitIndex);

            // Refresh skeleton updater with asset skeleton
            hierarchyUpdater = new SkeletonUpdater(skeleton);
            hierarchyUpdater.UpdateMatrices();

            // bounding boxes
            var modelBoundingBox = model.BoundingBox;
            var modelBoundingSphere = model.BoundingSphere;
            foreach (var mesh in model.Meshes)
            {
                var vertexBuffers = mesh.Draw.VertexBuffers;
                for (int vbIdx = 0; vbIdx < vertexBuffers.Length; vbIdx++)
                {
                    // Compute local mesh bounding box (no node transformation)
                    Matrix matrix = Matrix.Identity;
                    mesh.BoundingBox = vertexBuffers[vbIdx].ComputeBounds(ref matrix, out mesh.BoundingSphere);

                    // Compute model bounding box (includes node transformation)
                    hierarchyUpdater.GetWorldMatrix(mesh.NodeIndex, out matrix);
                    BoundingSphere meshBoundingSphere;
                    var meshBoundingBox = vertexBuffers[vbIdx].ComputeBounds(ref matrix, out meshBoundingSphere);
                    BoundingBox.Merge(ref modelBoundingBox, ref meshBoundingBox, out modelBoundingBox);
                    BoundingSphere.Merge(ref modelBoundingSphere, ref meshBoundingSphere, out modelBoundingSphere);
                }

                // TODO: temporary Always try to compact
                mesh.Draw.CompactIndexBuffer();
            }
            model.BoundingBox = modelBoundingBox;
            model.BoundingSphere = modelBoundingSphere;

            // Count unique meshes (they can be shared)
            var uniqueDrawMeshes = model.Meshes.Select(x => x.Draw).Distinct();

            // Count unique vertex buffers and squish them together in a single buffer
            var uniqueVB = uniqueDrawMeshes.SelectMany(x => x.VertexBuffers).Distinct().ToList();

            var vbMap = new Dictionary<VertexBufferBinding, VertexBufferBinding>();
            var sizeVertexBuffer = uniqueVB.Select(x => x.Buffer.GetSerializationData().Content.Length).Sum();
            var vertexBuffer = new BufferData(BufferFlags.VertexBuffer, new byte[sizeVertexBuffer]);
            var vertexBufferSerializable = vertexBuffer.ToSerializableVersion();

            var vertexBufferNextIndex = 0;
            foreach (var vbBinding in uniqueVB)
            {
                var oldVertexBuffer = vbBinding.Buffer.GetSerializationData().Content;
                Array.Copy(oldVertexBuffer, 0, vertexBuffer.Content, vertexBufferNextIndex, oldVertexBuffer.Length);

                vbMap.Add(vbBinding, new VertexBufferBinding(vertexBufferSerializable, vbBinding.Declaration, vbBinding.Count, vbBinding.Stride, vertexBufferNextIndex));

                vertexBufferNextIndex += oldVertexBuffer.Length;
            }

            // Count unique index buffers and squish them together in a single buffer
            var uniqueIB = uniqueDrawMeshes.Select(x => x.IndexBuffer).NotNull().Distinct().ToList();
            var sizeIndexBuffer = 0;
            foreach (var ibBinding in uniqueIB)
            {
                // Make sure 32bit indices are properly aligned to 4 bytes in case the last alignment was 2 bytes
                if (ibBinding.Is32Bit && sizeIndexBuffer % 4 != 0)
                    sizeIndexBuffer += 2;

                sizeIndexBuffer += ibBinding.Buffer.GetSerializationData().Content.Length;
            }

            var ibMap = new Dictionary<IndexBufferBinding, IndexBufferBinding>();

            if (uniqueIB.Count > 0)
            {
                var indexBuffer = new BufferData(BufferFlags.IndexBuffer, new byte[sizeIndexBuffer]);
                var indexBufferSerializable = indexBuffer.ToSerializableVersion();
                var indexBufferNextIndex = 0;

                foreach (var ibBinding in uniqueIB)
                {
                    var oldIndexBuffer = ibBinding.Buffer.GetSerializationData().Content;

                    // Make sure 32bit indices are properly aligned to 4 bytes in case the last alignment was 2 bytes
                    if (ibBinding.Is32Bit && indexBufferNextIndex % 4 != 0)
                        indexBufferNextIndex += 2;

                    Array.Copy(oldIndexBuffer, 0, indexBuffer.Content, indexBufferNextIndex, oldIndexBuffer.Length);

                    ibMap.Add(ibBinding, new IndexBufferBinding(indexBufferSerializable, ibBinding.Is32Bit, ibBinding.Count, indexBufferNextIndex));

                    indexBufferNextIndex += oldIndexBuffer.Length;
                }
            }

            // Assign new vertex and index buffer bindings
            foreach (var drawMesh in uniqueDrawMeshes)
            {
                for (int i = 0; i < drawMesh.VertexBuffers.Length; i++)
                    drawMesh.VertexBuffers[i] = vbMap[drawMesh.VertexBuffers[i]];

                if (drawMesh.IndexBuffer != null)
                    drawMesh.IndexBuffer = ibMap[drawMesh.IndexBuffer];
            }

            vbMap.Clear();
            ibMap.Clear();

            // Convert to Entity
            return model;
        }

    }
}
