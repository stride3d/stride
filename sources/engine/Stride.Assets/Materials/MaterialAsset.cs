// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Serialization;
using Stride.Rendering;
using Stride.Rendering.Materials;

namespace Stride.Assets.Materials
{
    /// <summary>
    /// The material asset.
    /// </summary>
    [DataContract("MaterialAsset")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(Material))]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.0.0.0")]
    public sealed partial class MaterialAsset : Asset, IMaterialDescriptor
    {
        private const string CurrentVersion = "2.0.0.0";

        /// <summary>
        /// The default file extension used by the <see cref="MaterialAsset"/>.
        /// </summary>
        public const string FileExtension = ".sdmat";

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialAsset"/> class.
        /// </summary>
        public MaterialAsset()
        {
            Attributes = new MaterialAttributes();
            Layers = new MaterialBlendLayers();
        }

        [DataMemberIgnore]
        public AssetId MaterialId => Id;

        /// <summary>
        /// Gets or sets the material attributes.
        /// </summary>
        /// <value>The material attributes.</value>
        /// <userdoc>The base attributes of the material.</userdoc>
        [DataMember(10)]
        [Display("Attributes", Expand = ExpandRule.Always)]
        public MaterialAttributes Attributes { get; set; }


        /// <summary>
        /// Gets or sets the material compositor.
        /// </summary>
        /// <value>The material compositor.</value>
        /// <userdoc>The layers overriding the base attributes of the material. Layers are displayed from bottom to top.</userdoc>
        [DefaultValue(null)]
        [DataMember(20)]
        [Category]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public MaterialBlendLayers Layers { get; set; }

        public IEnumerable<AssetReference> FindMaterialReferences()
        {
            foreach (var layer in Layers)
            {
                if (layer.Material != null)
                {
                    var reference = AttachedReferenceManager.GetAttachedReference(layer.Material);
                    yield return new AssetReference(reference.Id, reference.Url);
                }
            }
        }

        public void Visit(MaterialGeneratorContext context)
        {
            Attributes.Visit(context);
            Layers.Visit(context);
        }
    }
}
