// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Assets.Textures
{
    [CategoryOrder(40, "Transparency", Expand = ExpandRule.Never)]
    [DataContract("ColorTextureType")]
    [Display("Color")]
    public class ColorTextureType : ITextureType
    {
        /// <summary>
        /// Texture will be stored in sRGB format (standard for color textures) and converted to linear space when sampled. Only relevant when working in Linear color space.
        /// </summary>
        /// <userdoc>
        /// Store the texture in sRGB format and convert to linear space when sampled. We recommend you enable this for all color textures, unless they're explicitly in linear space.
        /// </userdoc>
        [DataMember(20)]
        [DefaultValue(true)]
        [Display("sRGB sampling")]
        public bool UseSRgbSampling { get; set; } = true;

        public bool IsSRgb(ColorSpace colorSpaceReference) => ((colorSpaceReference == ColorSpace.Linear) && UseSRgbSampling);

        /// <summary>
        /// Gets or sets a value indicating whether to enable color key. Default is false.
        /// </summary>
        /// <value><c>true</c> to enable color key; otherwise, <c>false</c>.</value>
        /// <userdoc>
        /// Replace all pixels of the color set in the **Color key color** property with transparent black
        /// </userdoc>
        [DataMember(43)]
        [DefaultValue(false)]
        [Display("Color key", "Transparency")]
        public bool ColorKeyEnabled { get; set; }

        /// <summary>
        /// Gets or sets the color key used when color keying for a texture is enabled. When color keying, all pixels of a specified color are replaced with transparent black.
        /// </summary>
        /// <value>The color key.</value>
        /// <userdoc>
        /// If **Color key** is enabled, replace all pixels of this color with transparent black
        /// </userdoc>
        [DataMember(45)]
        [Display("Color key color", "Transparency")]
        public Color ColorKeyColor { get; set; } = new Color(255, 0, 255);

        /// <summary>
        /// Gets or sets the alpha format.
        /// </summary>
        /// <value>The alpha format.</value>
        /// <userdoc>
        /// The format to use for alpha in the texture
        /// </userdoc>
        [DataMember(55)]
        [DefaultValue(AlphaFormat.Auto)]
        [Display(null, "Transparency")]
        public AlphaFormat Alpha { get; set; } = AlphaFormat.Auto;

        /// <summary>
        /// Gets or sets a value indicating whether to convert the texture in premultiply alpha.
        /// </summary>
        /// <value><c>true</c> to convert the texture in premultiply alpha.; otherwise, <c>false</c>.</value>
        /// <userdoc>
        /// Premultiply the color values by the **Alpha** value
        /// </userdoc>
        [DataMember(80)]
        [DefaultValue(true)]
        [Display("Premultiply alpha", "Transparency")]
        public bool PremultiplyAlpha { get; set; } = true;

        TextureHint ITextureType.Hint => TextureHint.Color;
    }
}
