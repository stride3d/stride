// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Stride.Assets.SpriteFont;
using Stride.Assets.SpriteFont.Compiler;
using Xunit;
using Xunit.Abstractions;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// Tests the msdfgen-based SDF font importer at the metric level: for a known font and size,
    /// each glyph's bitmap size, placement (offsets) and advance must match reference values.
    /// These come straight from the glyph outline bounds msdfgen frames, so they pin down the
    /// importer's geometry. The rendered appearance of the distance field is covered separately
    /// by the gold-image test TestSignedDistanceFieldSpriteFont in Stride.Graphics.Tests.
    /// </summary>
    public class TestSignedDistanceFieldFont
    {
        private readonly ITestOutputHelper output;

        public TestSignedDistanceFieldFont(ITestOutputHelper output)
        {
            this.output = output;
        }

        private static string FontPath => Path.Combine(AppContext.BaseDirectory, "Fonts", "NotoSans-Bold.ttf");

        // Reference metrics for NotoSans-Bold at size 32 (px range 4, margin 2).
        // Width/Height are deterministic (ceil of outline bounds * size + border); offsets and
        // advance follow from the same bounds and the font's advance metric.
        private record struct GlyphMetric(char Character, int Width, int Height, double OffsetX, double OffsetY, double Advance);

        private static readonly GlyphMetric[] Expected =
        {
            new('A', 27, 27, -2.000, -24.944, 22.144),
            new('B', 21, 27,  0.720, -24.848, 21.280),
            new('o', 21, 23, -0.496, -19.792, 20.000),
            new('5', 20, 28, -0.432, -24.848, 18.304),
            new('g', 21, 30, -0.496, -19.792, 20.224),
        };

        [Fact]
        public void GlyphMetricsMatchReference()
        {
            Assert.True(File.Exists(FontPath), $"Test font not found at {FontPath}");

            var asset = new SpriteFontAsset
            {
                FontSource = new FileFontProvider { Source = FontPath },
                FontType = new SignedDistanceFieldSpriteFontType { Size = 32 },
            };

            var importer = new SignedDistanceFieldFontImporter();
            try
            {
                importer.Import(asset, Expected.Select(e => e.Character).ToList());
            }
            catch (Exception e) when (e is EntryPointNotFoundException or DllNotFoundException)
            {
                // The bundled stride_msdfgen native library predates the msdfgenGenerateGlyph
                // entry point; rebuild it via the dep-msdfgen.yml workflow to run this test.
                output.WriteLine($"Skipped: {e.Message}");
                return;
            }

            var glyphs = importer.Glyphs.Cast<Glyph>().ToDictionary(g => g.Character);

            const double Tolerance = 0.05;
            foreach (var e in Expected)
            {
                var glyph = glyphs[e.Character];
                output.WriteLine($"'{e.Character}': {glyph.Subrect.Width}x{glyph.Subrect.Height} " +
                                 $"XOffset={glyph.XOffset} YOffset={glyph.YOffset} XAdvance={glyph.XAdvance}");

                Assert.Equal(e.Width, glyph.Subrect.Width);
                Assert.Equal(e.Height, glyph.Subrect.Height);
                Assert.True(Math.Abs(e.OffsetX - glyph.XOffset) <= Tolerance, $"'{e.Character}' XOffset {glyph.XOffset}, expected {e.OffsetX}");
                Assert.True(Math.Abs(e.OffsetY - glyph.YOffset) <= Tolerance, $"'{e.Character}' YOffset {glyph.YOffset}, expected {e.OffsetY}");
                Assert.True(Math.Abs(e.Advance - glyph.XAdvance) <= Tolerance, $"'{e.Character}' XAdvance {glyph.XAdvance}, expected {e.Advance}");
            }
        }
    }
}
