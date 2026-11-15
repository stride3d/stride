// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Assets.Textures
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
        /// Checked: the map is in OpenGL format (Y+). Unchecked: DirectX format (Y-). Tools such as Substance Painter let you choose this on export, so check your export settings. If details look lit from the wrong side vertically, change this setting.
        /// </userdoc>
        [DataMember(10)]
        [DefaultValue(true)]
        [Display("OpenGL format (Y+)")]
        public bool InvertY { get; set; } = true;

        bool ITextureType.ColorKeyEnabled => false;

        Color ITextureType.ColorKeyColor => new Color();

        AlphaFormat ITextureType.Alpha => AlphaFormat.None;

        bool ITextureType.PremultiplyAlpha => false;

        TextureHint ITextureType.Hint => TextureHint.NormalMap;
    }
}
