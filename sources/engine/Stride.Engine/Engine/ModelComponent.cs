// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Engine.Design;
using Xenko.Engine.Processors;
using Xenko.Rendering;
using Xenko.Updater;

namespace Xenko.Engine
{
    /// <summary>
    /// Add a <see cref="Model"/> to an <see cref="Entity"/>, that will be used during rendering.
    /// </summary>
    [DataContract("ModelComponent")]
    [Display("Model", Expand = ExpandRule.Once)]
    // TODO GRAPHICS REFACTOR
    [DefaultEntityComponentProcessor(typeof(ModelTransformProcessor))]
    [DefaultEntityComponentRenderer(typeof(ModelRenderProcessor))]
    [ComponentOrder(11000)]
    [ComponentCategory("Model")]
    public sealed class ModelComponent : ActivableEntityComponent, IModelInstance
    {
        private readonly List<MeshInfo> meshInfos = new List<MeshInfo>();
        private Model model;
        private SkeletonUpdater skeleton;
        private bool modelViewHierarchyDirty = true;

        /// <summary>
        /// Per-entity state of each individual mesh of a model.
        /// </summary>
        public class MeshInfo
        {
            /// <summary>
            /// The current blend matrices of a skinned meshes, transforming from mesh space to world space, for each bone.
            /// </summary>
            public Matrix[] BlendMatrices;

            /// <summary>
            /// The meshes current bounding box in world space.
            /// </summary>
            public BoundingBox BoundingBox;

            /// <summary>
            /// The meshes current sphere box in world space.
            /// </summary>
            public BoundingSphere BoundingSphere;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelComponent"/> class.
        /// </summary>
        public ModelComponent() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelComponent"/> class.
        /// </summary>
        /// <param name="model">The model.</param>
        public ModelComponent(Model model)
        {
            Model = model;
            IsShadowCaster = true;
        }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>
        /// The model.
        /// </value>
        /// <userdoc>The reference to the model asset to attach to this entity</userdoc>
        [DataMemberCustomSerializer]
        [DataMember(10)]
        public Model Model
        {
            get
            {
                return model;
            }
            set
            {
                if (model != value)
                    modelViewHierarchyDirty = true;
                model = value;
            }
        }

        /// <summary>
        /// Gets the materials; non-null ones will override materials from <see cref="Xenko.Rendering.Model.Materials"/> (same slots should be used).
        /// </summary>
        /// <value>
        /// The materials overriding <see cref="Xenko.Rendering.Model.Materials"/> ones.
        /// </value>
        /// <userdoc>The list of materials to use with the model. This list overrides the default materials of the model.</userdoc>
        [DataMember(40)]
        [Category]
        [MemberCollection(ReadOnly = true)]
        public IndexingDictionary<Material> Materials { get; } = new IndexingDictionary<Material>();

        [DataMemberIgnore, DataMemberUpdatable]
        [DataMember]
        public SkeletonUpdater Skeleton
        {
            get
            {
                CheckSkeleton();
                return skeleton;
            }
        }

        /// <summary>
        /// Gets the current per-entity state for each mesh in the associated model.
        /// </summary>
        [DataMemberIgnore]
        public IReadOnlyList<MeshInfo> MeshInfos => meshInfos;

        private void CheckSkeleton()
        {
            if (modelViewHierarchyDirty)
            {
                ModelUpdated();
                modelViewHierarchyDirty = false;
            }
        }

        /// <summary>
        /// Gets or sets a boolean indicating if this model component is casting shadows.
        /// </summary>
        /// <value>A boolean indicating if this model component is casting shadows.</value>
        /// <userdoc>Generate a shadow (when shadow maps are enabled)</userdoc>
        [DataMember(30)]
        [DefaultValue(true)]
        [Display("Cast shadows")]
        public bool IsShadowCaster { get; set; }

        /// <summary>
        /// The render group for this component.
        /// </summary>
        [DataMember(20)]
        [DefaultValue(RenderGroup.Group0)]
        [Display("Render group")]
        public RenderGroup RenderGroup { get; set; }

        /// <summary>
        /// Gets the bounding box in world space.
        /// </summary>
        /// <value>The bounding box.</value>
        [DataMemberIgnore]
        public BoundingBox BoundingBox;

        /// <summary>
        /// Gets the bounding sphere in world space.
        /// </summary>
        /// <value>The bounding sphere.</value>
        [DataMemberIgnore]
        public BoundingSphere BoundingSphere;

        /// <summary>
        /// Gets the material at the specified index. If the material is not overriden by this component, it will try to get it from <see cref="Xenko.Rendering.Model.Materials"/>
        /// </summary>
        /// <param name="index">The index of the material</param>
        /// <returns>The material at the specified index or null if not found</returns>
        public Material GetMaterial(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), @"index cannot be < 0");

            Material material;
            if (Materials.TryGetValue(index, out material))
            {
                return material;
            }
            // TODO: if Model is null, shouldn't we always return null?
            if (Model != null && index < Model.Materials.Count)
            {
                material = Model.Materials[index].Material;
            }
            return material;
        }

        /// <summary>
        /// Gets the number of materials (computed from <see cref="Xenko.Rendering.Model.Materials"/>)
        /// </summary>
        /// <returns></returns>
        public int GetMaterialCount()
        {
            if (Model != null)
            {
                return Model.Materials.Count;
            }
            return 0;
        }

        private void ModelUpdated()
        {
            if (model != null)
            {
                // Create mesh-per-entity state
                meshInfos.Clear();
                foreach (var mesh in model.Meshes)
                {
                    var meshData = new MeshInfo();
                    meshInfos.Add(meshData);

                    if (mesh.Skinning != null)
                        meshData.BlendMatrices = new Matrix[mesh.Skinning.Bones.Length];
                }

                if (skeleton != null)
                {
                    // Reuse previous ModelViewHierarchy
                    skeleton.Initialize(model.Skeleton);
                }
                else
                {
                    skeleton = new SkeletonUpdater(model.Skeleton);
                }
            }
        }

        internal void Update(TransformComponent transformComponent)
        {
            if (!Enabled || model == null)
                return;

            ref Matrix worldMatrix = ref transformComponent.WorldMatrix;

            // Check if scaling is negative
            var up = Vector3.Cross(worldMatrix.Right, worldMatrix.Forward);
            bool isScalingNegative = Vector3.Dot(worldMatrix.Up, up) < 0.0f;

            // Make sure skeleton is up to date
            CheckSkeleton();
            if (skeleton != null)
            {
                // Update model view hierarchy node matrices
                skeleton.NodeTransformations[0].LocalMatrix = worldMatrix;
                skeleton.NodeTransformations[0].IsScalingNegative = isScalingNegative;
                skeleton.UpdateMatrices();
            }

            // Update the bounding sphere / bounding box in world space
            BoundingSphere = BoundingSphere.Empty;
            BoundingBox = BoundingBox.Empty;
            bool modelHasBoundingBox = false;

            for (int meshIndex = 0; meshIndex < Model.Meshes.Count; meshIndex++)
            {
                var mesh = Model.Meshes[meshIndex];
                var meshInfo = meshInfos[meshIndex];
                meshInfo.BoundingSphere = BoundingSphere.Empty;
                meshInfo.BoundingBox = BoundingBox.Empty;

                if (mesh.Skinning != null && skeleton != null)
                {
                    bool meshHasBoundingBox = false;
                    var bones = mesh.Skinning.Bones;

                    // For skinned meshes, bounding box is union of the bounding boxes of the unskinned mesh, transformed by each affecting bone.
                    for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
                    {
                        var nodeIndex = bones[boneIndex].NodeIndex;
                        Matrix.Multiply(ref bones[boneIndex].LinkToMeshMatrix, ref skeleton.NodeTransformations[nodeIndex].WorldMatrix, out meshInfo.BlendMatrices[boneIndex]);

                        BoundingBox skinnedBoundingBox;
                        BoundingBox.Transform(ref mesh.BoundingBox, ref meshInfo.BlendMatrices[boneIndex], out skinnedBoundingBox);
                        BoundingSphere skinnedBoundingSphere;
                        BoundingSphere.Transform(ref mesh.BoundingSphere, ref meshInfo.BlendMatrices[boneIndex], out skinnedBoundingSphere);

                        if (meshHasBoundingBox)
                        {
                            BoundingBox.Merge(ref meshInfo.BoundingBox, ref skinnedBoundingBox, out meshInfo.BoundingBox);
                            BoundingSphere.Merge(ref meshInfo.BoundingSphere, ref skinnedBoundingSphere, out meshInfo.BoundingSphere);
                        }
                        else
                        {
                            meshHasBoundingBox = true;
                            meshInfo.BoundingSphere = skinnedBoundingSphere;
                            meshInfo.BoundingBox = skinnedBoundingBox;
                        }
                    }
                }
                else
                {
                    // If there is a skeleton, use the corresponding node's transform. Otherwise, fall back to the model transform.
                    var transform = skeleton != null ? skeleton.NodeTransformations[mesh.NodeIndex].WorldMatrix : worldMatrix;
                    BoundingBox.Transform(ref mesh.BoundingBox, ref transform, out meshInfo.BoundingBox);
                    BoundingSphere.Transform(ref mesh.BoundingSphere, ref transform, out meshInfo.BoundingSphere);
                }

                if (modelHasBoundingBox)
                {
                    BoundingBox.Merge(ref BoundingBox, ref meshInfo.BoundingBox, out BoundingBox);
                    BoundingSphere.Merge(ref BoundingSphere, ref meshInfo.BoundingSphere, out BoundingSphere);
                }
                else
                {
                    BoundingBox = meshInfo.BoundingBox;
                    BoundingSphere = meshInfo.BoundingSphere;
                    modelHasBoundingBox = true;
                }
            }
        }
    }
}
