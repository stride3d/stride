// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Stride.Core.Mathematics;

namespace Stride.Graphics.Font
{
    /// <summary>
    /// A character of a specific font with a specific size.
    /// </summary>
    internal class CharacterSpecification : IEquatable<CharacterSpecification>
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


        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is CharacterSpecification characterSpecification && Equals(characterSpecification);
        }

        /// <inheritdoc/>
        public bool Equals(CharacterSpecification? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Character == other.Character
                && FontName == other.FontName
                && Size == other.Size
                && Style == other.Style
                && AntiAlias == other.AntiAlias;
        }

        /// <summary>
        ///   Determines whether two <see cref="CharacterSpecification"/> instances are equal.
        /// </summary>
        /// <param name="left">The first <see cref="CharacterSpecification"/> instance to compare.</param>
        /// <param name="right">The second <see cref="CharacterSpecification"/> instance to compare.</param>
        /// <returns><see langword="true"/> if the specified instances are equal; otherwise, <see langword="false"/>.</returns>
        public static bool Equals(CharacterSpecification? left, CharacterSpecification? right)
        {
            if ((left is null) != (right is null))
                return false;

            if (left is not null)
                return left.Equals(right);

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Character.GetHashCode();
        }
    }
}
