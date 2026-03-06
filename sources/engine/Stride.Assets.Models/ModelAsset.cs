// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Rendering;

namespace Stride.Assets.Models
{
    [DataContract("Model")]
    [AssetDescription(FileExtension, AllowArchetype = true)]
    [AssetContentType(typeof(Model))]
    [Display((int)AssetDisplayPriority.Models + 80, "Model")]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.0.0.0")]
    public sealed class ModelAsset : Asset, IAssetWithSource, IModelAsset
    {
        private const string CurrentVersion = "2.0.0.0";

        /// <summary>
        /// The default file extension used by the <see cref="ModelAsset"/>.
        /// </summary>
        public const string FileExtension = ".sdm3d;pdxm3d";

        /// <summary>
        /// Gets or sets the source file of this asset.
        /// </summary>
        /// <value>The source.</value>
        /// <userdoc>
        /// The source file of this asset.
        /// </userdoc>
        [DataMember(-50)]
        [DefaultValue(null)]
        [SourceFileMember(true)]
        public UFile Source { get; set; } = new UFile("");

        /// <summary>
        /// Gets or sets the Skeleton.
        /// </summary>
        /// <userdoc>
        /// Describes the node hierarchy that will be active at runtime.
        /// </userdoc>
        [DataMember(10)]
        public Skeleton Skeleton { get; set; }

        /// <summary>
        /// Gets or sets the pivot position, that will be used as center of object. If a Skeleton is set, its value will be used instead.
        /// </summary>
        /// <userdoc>
        /// The root (pivot) of the animation will be offset by this distance. If a Skeleton is set, its value will be used instead.
        /// </userdoc>
        [DataMember(20)]
        public Vector3 PivotPosition { get; set; }

        /// <summary>
        /// Gets or sets the scale import. If a Skeleton is set, its value will be used instead.
        /// </summary>
        /// <userdoc>The scale applied when importing a model. If a Skeleton is set, its value will be used instead.</userdoc>
        [DataMember(30)]
        [DefaultValue(1.0f)]
        public float ScaleImport { get; set; } = 1.0f;


        /// <summary>
        /// Gets or sets the model meshes merge property. When set to true to a model without skeleton, the meshes of the model are merged together by material.
        /// </summary>
        /// <userdoc>
        /// When checked and the model has no skeleton, the meshes of the model are merged together by material.
        /// In most cases this improves the performances but prevents the meshes to be culled independently.
        /// </userdoc>
        [DataMember(35)]
        [DefaultValue(true)]
        public bool MergeMeshes { get; set; } = true;

        /// <inheritdoc/>
        [DataMember(40)]
        [MemberCollection(ReadOnly = true)]
        [Category]
        public List<ModelMaterial> Materials { get; } = new List<ModelMaterial>();

        [DataMember(45)]
        [DefaultValue(true)]
        [Display(Browsable = false)]
        public bool DeduplicateMaterials { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to split the imported model into multiple model assets based on the
        /// source file's internal node hierarchy, and automatically generate a prefab that mirrors that hierarchy.
        /// When false (default), the model is imported as a single asset, preserving the existing behavior.
        /// </summary>
        /// <userdoc>
        /// When checked, the model file is split into separate model assets for each node in the source
        /// hierarchy, and a prefab is generated that mirrors the original scene tree structure.
        /// This is useful for files authored with a meaningful hierarchy (e.g., vehicle parts, building pieces).
        /// </userdoc>
        [DataMember(46)]
        [DefaultValue(false)]
        public bool SplitModelByHierarchy { get; set; } = false;

        /// <summary>
        /// When set, only meshes attached to the listed node indices are included in this model asset.
        /// Used internally by the hierarchy splitter to produce per-node sub-models from a single source file.
        /// An empty or null list means "include all meshes" (default behavior).
        /// </summary>
        [DataMember(47)]
        [Display(Browsable = false)]
        public List<int> NodeFilter { get; set; } = new List<int>();

        [DataMember(50)]
        [Category]
        public List<IModelModifier> Modifiers { get; } = new List<IModelModifier>();

        /// <inheritdoc/>
        [DataMemberIgnore]
        public override UFile MainSource => Source;
    }
}
