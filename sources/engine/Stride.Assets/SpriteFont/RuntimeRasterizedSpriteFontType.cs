// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics.Font;

namespace Stride.Assets.SpriteFont
{
    [DataContract("RuntimeRasterizedSpriteFontType")]
    [Display("Runtime Rasterized")]
    public class RuntimeRasterizedSpriteFontType : SpriteFontTypeBase
    {
        /// <inheritdoc/>
        [DataMember(30)]
        [DataMemberRange(MathUtil.ZeroTolerance, 2)]
        [DefaultValue(20)]
        [Display("Default Size")]
        public override float Size { get; set; } = 20;

        /// <inheritdoc/>
        [DataMember(110)]
        [DefaultValue(FontAntiAliasMode.Default)]
        [Display("Anti alias")]
        public override FontAntiAliasMode AntiAlias { get; set; } = FontAntiAliasMode.Default;
    }
}
