// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Mathematics;

namespace Stride.Assets.SpriteFont
{
    [DataContract("SignedDistanceFieldSpriteFontType")]
    [Display("Signed Distance Field")]
    public class SignedDistanceFieldSpriteFontType : SpriteFontTypeBase
    {
        /// <inheritdoc/>
        [DataMember(30)]
        [DataMemberRange(MathUtil.ZeroTolerance, 2)]
        [DefaultValue(20)]
        public override float Size { get; set; } = 20;

        /// <summary>
        ///  Gets or sets the text file referencing which characters to include when generating the static fonts (eg. "ABCDEF...")
        /// </summary>
        /// <userdoc>
        /// The path to a file containing the characters to import from the font source file. This property is ignored when 'IsDynamic' is checked.
        /// </userdoc>
        [DataMember(70)]
        public UFile CharacterSet { get; set; } = new UFile("");

        /// <summary>
        /// Gets or set the additional character ranges to include when generating the static fonts (eg. "/CharacterRegion:0x20-0x7F /CharacterRegion:0x123")
        /// </summary>
        /// <userdoc>
        /// The list of series of character to import from the font source file. This property is ignored when 'IsDynamic' is checked.
        /// Note that this property only represents an alternative way of indicating character to import, the result is the same as using the 'CharacterSet' property.
        /// </userdoc>
        [DataMember(80)]
        [MemberCollection(NotNullItems = true)]
        public List<CharacterRegion> CharacterRegions { get; set; } = new List<CharacterRegion>();
    }
}
