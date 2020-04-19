// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

using Stride.Core.Mathematics;

namespace Stride.Graphics.Font
{
    /// <summary>
    /// A character of a specific font with a specific size.
    /// </summary>
    internal class CharacterSpecification
    {
        public CharacterSpecification(char character, string fontName, Vector2 size, FontStyle style, FontAntiAliasMode antiAliasMode)
        {
            Character = character;
            FontName = fontName;
            Size = size;
            Style = style;
            AntiAlias = antiAliasMode;
            ListNode = new LinkedListNode<CharacterSpecification>(this);
        }

        /// <summary>
        /// Name of a system (TrueType) font.
        /// </summary>
        public readonly string FontName;

        /// <summary>
        ///  Size of the TrueType fonts in pixels
        /// </summary>
        public readonly Vector2 Size;

        /// <summary>
        /// Style for the font. 'regular', 'bold', 'italic', 'underline', 'strikeout'. Default is 'regular
        /// </summary>
        public readonly FontStyle Style;

        /// <summary>
        /// The alias mode of the font
        /// </summary>
        public readonly FontAntiAliasMode AntiAlias;

        /// <summary>
        /// The bitmap of the character
        /// </summary>
        public CharacterBitmap Bitmap;

        /// <summary>
        /// The glyph of the character
        /// </summary>
        public readonly Glyph Glyph = new Glyph();

        /// <summary>
        /// Indicate if the current <see cref="Font.Glyph.Subrect"/> and <see cref="Font.Glyph.BitmapIndex"/> data has been uploaded to the GPU or not.
        /// </summary>
        public bool IsBitmapUploaded;

        /// <summary>
        /// The node of the least recently used (LRU) list.
        /// </summary>
        public LinkedListNode<CharacterSpecification> ListNode;

        /// <summary>
        /// The index of the frame where the character has been used for the last time.
        /// </summary>
        public int LastUsedFrame;

        public char Character
        {
            get { return (char)Glyph.Character; }
            set { Glyph.Character = value; }
        }

        public override bool Equals(object obj)
        {
            return Equals(this, (CharacterSpecification)obj);
        }

        public static bool Equals(CharacterSpecification left, CharacterSpecification right)
        {
            return left.Character == right.Character && left.FontName == right.FontName && left.Size == right.Size && left.Style == right.Style && left.AntiAlias == right.AntiAlias;
        }

        public override int GetHashCode()
        {
            return Character.GetHashCode();
        }
    }
}
