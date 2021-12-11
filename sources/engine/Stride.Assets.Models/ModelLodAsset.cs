// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Core.Serialization;

namespace Stride.Assets.Models
{
    [DataContract("ModelLod")]
    [AssetDescription(FileExtension, AllowArchetype = false)]
    [AssetContentType(typeof(Model))]
    [Display((int)AssetDisplayPriority.Models + 90, "LOD Model")]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.0.0.0")]
    public sealed class ModelLodAsset : Asset, IAssetWithSource
    {
        private const string CurrentVersion = "2.0.0.0";

        /// <summary>
        /// The default file extension used by the <see cref="ModelLod"/>.
        /// </summary>
        public const string FileExtension = ".sdmlod3d;pdxmlod3d";

        /// <summary>
        /// Gets or sets the lod level,
        /// </summary>
        /// <value>The lod level.</value>
        /// <userdoc>The lod level is the index for the lod list inside the source model.</userdoc>
        [DataMember(20)]
        [DefaultValue(1)]
        public int Level { get; set; } = 1;

        /// <summary>
        /// Gets or sets the lod quality level.
        /// </summary>
        /// <value>The lod quality level..</value>
        /// <userdoc>The quality applied when importing a model.</userdoc>
        [DataMember(25)]
        [DefaultValue(0.5f)]
        public float Quality { get; set; } = 0.5f;

        /// <inheritdoc/>
        [DataMember(10)]
        [Display(Browsable = false)]
        public Skeleton Skeleton { get; set; }

        /// <inheritdoc/>
        [DataMember(-50)]
        [DefaultValue(null)]
        [SourceFileMember(true)]
        public UFile Source { get; set; } = new UFile("");

        /// <inheritdoc/>
        [DataMember(35)]
        [DefaultValue(true)]
        [Display(Browsable = false)]
        public bool MergeMeshes { get; set; } = true;

        /// <inheritdoc/>
        [DataMember(10)]
        [Display(Browsable = false)]
        public Vector3 PivotPosition { get; set; }

        /// <inheritdoc/>
        [DataMember(15)]
        [DefaultValue(1.0f)]
        [Display(Browsable = false)]
        public float ScaleImport { get; set; } = 1.0f;

        /// <inheritdoc/>
        [DataMember(30)]
        [Display(Browsable = false)]
        public List<ModelMaterial> Materials { get; } = new List<ModelMaterial>();

        /// <inheritdoc/>
        [DataMemberIgnore]
        public override UFile MainSource => Source;

        /// <inheritdoc/>
        [DataMember(45)]
        [DefaultValue(true)]
        [Display(Browsable = false)]
        public bool DeduplicateMaterials { get; set; } = true;
    }
}
