// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.SpriteFont;
using Stride.Assets.SpriteFont.Compiler;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Graphics.Font;
using Xunit;

namespace Stride.Assets.Tests;

public class TestSpriteFontBitmaps
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BitmapImportCropAndPack(bool transparent)
    {
        var path = Path.Combine(Path.GetTempPath(), $"stride-font-{Guid.NewGuid():N}.png");
        try
        {
            using (var source = Image.New2D(4, 4, 1, PixelFormat.R8G8B8A8_UNorm))
            {
                for (int y = 0; y < 4; y++)
                    for (int x = 0; x < 4; x++)
                        source.PixelBuffer[0].SetPixel(x, y, new Color(255, 0, 255, 255));

                var empty = transparent ? new Color(3, 5, 7, 0) : new Color(0, 0, 0, 255);
                var filled = transparent ? new Color(20, 40, 60, 128) : new Color(128, 128, 128, 255);
                source.PixelBuffer[0].SetPixel(1, 1, empty);
                source.PixelBuffer[0].SetPixel(2, 1, empty);
                source.PixelBuffer[0].SetPixel(1, 2, empty);
                source.PixelBuffer[0].SetPixel(2, 2, filled);
                using var stream = File.Create(path);
                source.Save(stream, ImageFileType.Png);
            }

            var importer = new BitmapImporter();
            importer.Import(new SpriteFontAsset { FontSource = new FileFontProvider { Source = path } }, ['A']);
            var glyph = Assert.Single(importer.Glyphs);
            Assert.Equal('A', glyph.Character);
            Assert.Equal(2, glyph.XAdvance);
            Assert.Equal(2, importer.LineSpacing);
            GlyphCropper.Crop(glyph);
            Assert.Equal(new Rectangle(2, 2, 1, 1), glyph.Subrect);
            Assert.Equal(1, glyph.XOffset);
            Assert.Equal(1, glyph.YOffset);

            var expected = transparent ? new Color(20, 40, 60, 128) : new Color(255, 255, 255, 128);
            Assert.Equal(expected, glyph.Bitmap[2, 2]);
            var atlas = GlyphPacker.ArrangeGlyphs([glyph]);
            Assert.Same(atlas, glyph.Bitmap);
            Assert.Equal(expected, atlas[glyph.Subrect.X, glyph.Subrect.Y]);
            Assert.Equal(new Color(expected.R, expected.G, expected.B, 0), atlas[glyph.Subrect.X - 1, glyph.Subrect.Y]);
            BitmapUtils.PremultiplyAlpha(atlas, false);
            var premultiplied = new Color((byte)(expected.R * expected.A / 255), (byte)(expected.G * expected.A / 255), (byte)(expected.B * expected.A / 255), expected.A);
            Assert.Equal(premultiplied, atlas[glyph.Subrect.X, glyph.Subrect.Y]);

            var type = new OfflineRasterizedSpriteFontType();
            type.CharacterRegions.Add(new CharacterRegion('A', 'A'));
            var asset = new SpriteFontAsset { FontSource = new FileFontProvider { Source = path }, FontType = type, DefaultCharacter = 'A' };
            var font = OfflineRasterizedFontCompiler.Compile(new FontDataFactory(), asset, false);
            Assert.True(font.IsCharPresent('A'));
            using var image = Assert.Single(font.Textures).GetSerializationData().Image;
            Assert.Equal(premultiplied, image.PixelBuffer[0].GetPixel<Color>(1, 1));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GifBitmapImportIsWindowsOnly()
    {
        var path = Path.Combine(Path.GetTempPath(), $"stride-font-{Guid.NewGuid():N}.gif");
        try
        {
            var importer = new BitmapImporter();
            var asset = new SpriteFontAsset { FontSource = new FileFontProvider { Source = path } };
            if (!OperatingSystem.IsWindows())
            {
                File.WriteAllBytes(path, [71, 73, 70, 56, 57, 97]);
                Assert.Throws<PlatformNotSupportedException>(() => importer.Import(asset, ['A']));
                return;
            }

            using (var source = Image.New2D(3, 3, 1, PixelFormat.R8G8B8A8_UNorm))
            {
                for (int y = 0; y < 3; y++)
                    for (int x = 0; x < 3; x++)
                        source.PixelBuffer[0].SetPixel(x, y, new Color(255, 0, 255, 255));
                source.PixelBuffer[0].SetPixel(1, 1, new Color(255, 255, 255, 255));
                using var stream = File.Create(path);
                source.Save(stream, ImageFileType.Gif);
            }
            importer.Import(asset, ['A']);
            var glyph = Assert.Single(importer.Glyphs);
            Assert.Equal(new Color(255, 255, 255, 255), glyph.Bitmap[1, 1]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DistanceFieldWriterPreservesRgba()
    {
        var bitmap = new FontBitmap(1, 1);
        var expected = new Color(3, 21, 64, 13);
        bitmap[0, 0] = expected;
        var glyph = new Stride.Assets.SpriteFont.Compiler.Glyph('A', bitmap);
        var asset = new SpriteFontAsset { FontType = new SignedDistanceFieldSpriteFontType(), DefaultCharacter = 'A' };
        var font = SignedDistanceFieldFontWriter.CreateSpriteFontData(new FontDataFactory(), asset, [glyph], 1, 0, bitmap);
        Assert.True(font.IsCharPresent('A'));
        using var image = Assert.Single(font.Textures).GetSerializationData().Image;
        Assert.Equal(expected, image.PixelBuffer[0].GetPixel<Color>(0, 0));
    }

    [Fact]
    public void FreeTypeFontCompilesToAtlas()
    {
        var type = new OfflineRasterizedSpriteFontType { Size = 32 };
        type.CharacterRegions.Add(new CharacterRegion('A', 'A'));
        var path = Path.Combine(AppContext.BaseDirectory, "Fonts", "NotoSans-Bold.ttf");
        var asset = new SpriteFontAsset { FontSource = new FileFontProvider { Source = path }, FontType = type, DefaultCharacter = 'A' };
        var font = OfflineRasterizedFontCompiler.Compile(new FontDataFactory(), asset, false);
        Assert.True(font.IsCharPresent('A'));
        using var image = Assert.Single(font.Textures).GetSerializationData().Image;
        Assert.Contains(image.PixelBuffer[0].GetPixels<Color>(), color => color.A > 0);
    }
}
