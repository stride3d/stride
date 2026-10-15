// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Yaml;
using Stride.Importer.Common;
using Stride.Rendering;

namespace Stride.Assets.Models
{
    [DataContract("Model")]
    [AssetDescription(FileExtension, AllowArchetype = true)]
    [AssetContentType(typeof(Model))]
    [Display((int)AssetDisplayPriority.Models + 80, "Model")]
    [AssetFormatVersion(StrideConfig.LogicalPackageName, CurrentVersion, "2.0.0.0")]
    [AssetUpgrader(StrideConfig.LogicalPackageName, "2.0.0.0", "2.1.0.0", typeof(DisableMorphTargetsUpgrader))]
    public sealed class ModelAsset : Asset, IAssetWithSource, IModelAsset
    {
        private const string CurrentVersion = "2.1.0.0";

        /// <summary>
        /// The default file extension used by the <see cref="ModelAsset"/>.
        /// </summary>
        public const string FileExtension = ".sdm3d";

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

        /// <summary>
        /// Gets or sets whether shaders can read the vertex and index buffers of the model.
        /// </summary>
        /// <userdoc>
        /// When checked, shaders can read the vertex and index buffers of the model as raw buffers (ByteAddressBuffer),
        /// for example to fetch vertices in a compute shader. Leave it unchecked unless a shader needs it.
        /// On Direct3D 11, it needs graphics profile Level 11.0 or higher.
        /// </userdoc>
        [DataMember(37)]
        [DefaultValue(false)]
        public bool ShaderReadableBuffers { get; set; }

        /// <inheritdoc/>
        [DataMember(40)]
        [MemberCollection(ReadOnly = true)]
        [Category]
        public List<ModelMaterial> Materials { get; } = new List<ModelMaterial>();

        [DataMember(45)]
        [DefaultValue(true)]
        [Display(Browsable = false)]
        public bool DeduplicateMaterials { get; set; } = true;

        [DataMember(50)]
        [Category]
        public List<IModelModifier> Modifiers { get; } = new List<IModelModifier>();

        /// <userdoc>
        /// When unchecked, morph targets (blend shapes) in the source file are ignored and the model is imported without them.
        /// After checking it on a model imported without morph targets, use Update from source to show their weights in the editor.
        /// </userdoc>
        [DataMember(54), DefaultValue(true)]
        public bool ImportMorphTargets { get; set; } = true;

        /// <userdoc>
        /// Where the normal deltas of morph targets come from: the source file, generated from the target shapes, or both.
        /// </userdoc>
        [DataMember(56), DefaultValue(MorphDeltaSource.ImportOrGenerate)]
        public MorphDeltaSource MorphNormals { get; set; } = MorphDeltaSource.ImportOrGenerate;

        /// <userdoc>
        /// Where the tangent deltas of morph targets come from: the source file, generated from the target shapes, or both.
        /// </userdoc>
        [DataMember(57), DefaultValue(MorphDeltaSource.ImportOrGenerate)]
        public MorphDeltaSource MorphTangents { get; set; } = MorphDeltaSource.ImportOrGenerate;

        // Editor metadata only; packed runtime targets are built from the source mesh.
        [DataMember(60), Display(Browsable = false)]
        public List<string> MorphTargetNames { get; } = new();

        /// <inheritdoc/>
        [DataMemberIgnore]
        public override UFile MainSource => Source;

        /// <summary>
        /// Models saved before morph target import keep their previous behavior: merged meshes and no compute deformation.
        /// </summary>
        private class DisableMorphTargetsUpgrader : AssetUpgraderBase
        {
            protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
            {
                asset.ImportMorphTargets = false;
            }
        }
    }
}
