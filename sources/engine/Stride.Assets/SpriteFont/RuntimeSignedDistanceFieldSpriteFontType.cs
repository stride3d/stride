// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;

namespace Stride.Assets.SpriteFont
{
    [DataContract("RuntimeSignedDistanceFieldSpriteFontType")]
    [Display("Runtime SDF")]
    public class RuntimeSignedDistanceFieldSpriteFontType : SpriteFontTypeBase
    {
        /// <inheritdoc/>
        [DataMember(30)]
        [DataMemberRange(MathUtil.ZeroTolerance, 2)]
        [DefaultValue(20)]
        [Display("Default Size")]
        public override float Size { get; set; } = 64;

        /// <summary>
        /// Distance field range/spread (in pixels) used during MSDF generation.
        /// </summary>
        [DataMember(40)]
        [DefaultValue(8)]
        [DataMemberRange(1, 64, 1, 4, 0)]
        [Display("Pixel Range")]
        public int PixelRange { get; set; } = 8;

        /// <summary>
        /// Extra padding around each glyph inside the atlas (in pixels).
        /// </summary>
        [DataMember(50)]
        [DefaultValue(2)]
        [DataMemberRange(0, 16, 1, 2, 0)]
        [Display("Padding")]
        public int Padding { get; set; } = 2;
    }
}
