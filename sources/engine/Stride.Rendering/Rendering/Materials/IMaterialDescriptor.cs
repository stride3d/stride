// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Assets;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Common interface for the description of a material.
    /// </summary>
    public interface IMaterialDescriptor : IMaterialShaderGenerator
    {
        /// <summary>
        /// Gets the material identifier used only internaly to match material instance by id (when cloning an asset for example)
        /// to provide an error when defining a material that is recursively referencing itself.
        /// </summary>
        /// <value>The material identifier.</value>
        [DataMemberIgnore]
        AssetId MaterialId { get; }

        /// <summary>
        /// Gets or sets the material attributes.
        /// </summary>
        /// <value>The material attributes.</value>
        [DataMember(10)]
        [NotNull]
        [Display("Attributes", Expand = ExpandRule.Always)]
        MaterialAttributes Attributes { get; set; }

        /// <summary>
        /// Gets or sets the material compositor.
        /// </summary>
        /// <value>The material compositor.</value>
        [DefaultValue(null)]
        [DataMember(20)]
        [NotNull]
        [MemberCollection(NotNullItems = true)]
        MaterialBlendLayers Layers { get; set; }
    }
}
