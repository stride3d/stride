// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using Xenko.Core.Assets;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Serialization;
using Xenko.Rendering;
using Xenko.Rendering.Materials;

namespace Xenko.Assets.Materials
{
    /// <summary>
    /// The material asset.
    /// </summary>
    [DataContract("MaterialAsset")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(Material))]
    [AssetFormatVersion(XenkoConfig.PackageName, CurrentVersion, "2.0.0.0")]
    public sealed partial class MaterialAsset : Asset, IMaterialDescriptor
    {
        private const string CurrentVersion = "2.0.0.0";

        /// <summary>
        /// The default file extension used by the <see cref="MaterialAsset"/>.
        /// </summary>
        public const string FileExtension = ".xkmat";

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
