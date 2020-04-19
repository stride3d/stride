// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// A descriptor of a <see cref="Material"/>.
    /// </summary>
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MaterialDescriptor>), Profile = "Content")]
    [ContentSerializer(typeof(DataContentSerializer<MaterialDescriptor>))]
    [DataContract("MaterialDescriptor")]
    public class MaterialDescriptor : IMaterialDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialDescriptor"/> class.
        /// </summary>
        public MaterialDescriptor()
        {
            Attributes = new MaterialAttributes();
            Layers = new MaterialBlendLayers();
            // An instance id, only used to match descriptor
            MaterialId = AssetId.New();
        }

        [DataMemberIgnore]
        public AssetId MaterialId { get; set; }

        /// <summary>
        /// Gets or sets the material attributes.
        /// </summary>
        /// <value>The material attributes.</value>
        [DataMember(10)]
        [NotNull]
        [Display("Attributes", Expand = ExpandRule.Always)]
        public MaterialAttributes Attributes { get; set; }

        /// <summary>
        /// Gets or sets the material compositor.
        /// </summary>
        /// <value>The material compositor.</value>
        [DefaultValue(null)]
        [DataMember(20)]
        [NotNull]
        [MemberCollection(NotNullItems = true)]
        public MaterialBlendLayers Layers { get; set; }

        public void Visit(MaterialGeneratorContext context)
        {
            if (Attributes != null)
            {
                Attributes.Visit(context);
            }

            if (Layers != null)
            {
                Layers.Visit(context);
            }
        }
    }
}
