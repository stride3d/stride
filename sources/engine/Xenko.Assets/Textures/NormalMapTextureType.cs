// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Assets.Textures
{
    [DataContract("NormalMapTextureType")]
    [Display("Normal Map")]
    public class NormapMapTextureType : ITextureType
    {
        public bool IsSRgb(ColorSpace colorSpaceReference) => false;

        /// <summary>
        /// Indicating whether the Y-component of normals should be inverted, to compensate for a flipped tangent-space.
        /// </summary>
        /// <userdoc>
        /// Indicates that a positive Y-component (green) faces up in tangent space. This options depends on your normal maps generation tools.
        /// </userdoc>
        [DataMember(10)]
        [DefaultValue(true)]
        public bool InvertY { get; set; } = true;

        bool ITextureType.ColorKeyEnabled => false;

        Color ITextureType.ColorKeyColor => new Color();

        AlphaFormat ITextureType.Alpha => AlphaFormat.None;

        bool ITextureType.PremultiplyAlpha => false;

        TextureHint ITextureType.Hint => TextureHint.NormalMap;
    }
}
