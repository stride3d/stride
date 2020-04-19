// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Graphics.Font;

namespace Stride.Assets.SpriteFont
{
    [DataContract("SpriteFontTypeBase")]
    public abstract class SpriteFontTypeBase
    {
        /// <summary>
        ///  Gets or sets the size in virtual pixels of the font (ignored when converting a bitmap font).
        /// </summary>
        /// <userdoc>
        /// The size of the font in virtual pixels for static fonts, the default size for dynamic fonts. This property is ignored when the font source is a bitmap.
        /// </userdoc>
        [DataMemberIgnore]
        public virtual float Size { get; set; }

        /// <summary>
        /// Gets or sets the font anti-aliasing mode. By default, levels of grays are used.
        /// </summary>
        /// <userdoc>
        /// The type of anti-aliasing to use when rendering the font. 
        /// </userdoc>
        [DataMemberIgnore]
        public virtual FontAntiAliasMode AntiAlias { get { return FontAntiAliasMode.Aliased; } set { } }

        /// <summary>
        /// Gets or sets the value indicating if the font texture should be generated pre-multiplied by alpha component. 
        /// </summary>
        /// <userdoc>
        /// If checked, the texture generated for this font is not pre-multiplied by the alpha component.
        /// Check this property if you prefer to use interpolative alpha blending when rendering the font.
        /// </userdoc>
        [DataMemberIgnore]
        public virtual bool IsPremultiplied { get { return true; } set { } }
    }
}
