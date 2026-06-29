// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Graphics.Font;

namespace Stride.Graphics.Tests
{
    /// <summary>
    /// CPU-only tests for the kerning logic in <see cref="SpriteFont.GlyphEnumerator"/>.
    /// Kerning shifts a glyph's drawn X position (its <c>dx</c>), so the regression is
    /// observed on the per-glyph position rather than on the measured string width.
    /// </summary>
    public class TestSpriteFontKerning
    {
        private const char First = 'A';
        private const char Second = 'V';
        private const float Advance = 10f;
        private const float KerningOffset = -3f;

        private static SpriteFont CreateFont(bool withKerning)
        {
            var glyphs = new List<Glyph>
            {
                new Glyph { Character = First, XAdvance = Advance, Offset = Vector2.Zero, Subrect = new Rectangle(0, 0, 8, 8) },
                new Glyph { Character = Second, XAdvance = Advance, Offset = Vector2.Zero, Subrect = new Rectangle(0, 0, 8, 8) },
            };

            var kernings = withKerning
                ? new List<Kerning> { new Kerning { First = First, Second = Second, Offset = KerningOffset } }
                : null;

            return new OfflineRasterizedSpriteFont(
                size: 16, glyphs, textures: null, baseOffset: 0, defaultLineSpacing: 16,
                kernings, extraSpacing: 0, extraLineSpacing: 0, defaultCharacter: ' ');
        }

        private static List<SpriteFont.GlyphPosition> Enumerate(SpriteFont font, string text)
        {
            var proxy = new SpriteFont.StringProxy(text);
            var fontSize = new Vector2(16, 16);
            var result = new List<SpriteFont.GlyphPosition>();
            foreach (var glyphPosition in new SpriteFont.GlyphEnumerator(null, proxy, fontSize, false, 0, proxy.Length, font))
                result.Add(glyphPosition);
            return result;
        }

        /// <summary>
        /// Reproduces the regression where the kerning lookup key omitted the current character,
        /// so a kerning pair entry ((First &lt;&lt; 16) | Second) never matched and the offset was
        /// never applied. The second glyph must be shifted by the kerning offset.
        /// </summary>
        [Fact]
        public void KerningOffsetIsAppliedToGlyphPosition()
        {
            var positions = Enumerate(CreateFont(withKerning: true), "AV");

            Assert.Equal(2, positions.Count);
            // First glyph sits at the origin.
            Assert.Equal(0f, positions[0].X, 3);
            // Second glyph is advanced by the first glyph's advance, then shifted by the kerning offset.
            Assert.Equal(Advance + KerningOffset, positions[1].X, 3);
        }

        /// <summary>
        /// Without a kerning pair, the second glyph sits exactly at the previous glyph's advance.
        /// This is the value the buggy code produced even when a kerning pair existed.
        /// </summary>
        [Fact]
        public void NoKerningLeavesGlyphAtAdvance()
        {
            var positions = Enumerate(CreateFont(withKerning: false), "AV");

            Assert.Equal(2, positions.Count);
            Assert.Equal(0f, positions[0].X, 3);
            Assert.Equal(Advance, positions[1].X, 3);
        }
    }
}
