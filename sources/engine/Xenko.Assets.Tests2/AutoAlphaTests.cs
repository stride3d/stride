// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xenko.Core;
using Xenko.Assets.Textures;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Graphics.Regression;

namespace Xenko.Assets.Tests2
{
    /// <summary>
    /// Tests for automatic alpha detection in textures and sprite sheets
    /// </summary>
    public class AutoAlphaTests : GameTestBase
    {
        private static readonly Dictionary<Tuple<PlatformType, AlphaFormat>, PixelFormat> PlaformAndAlphaToPixelFormats = new Dictionary<Tuple<PlatformType, AlphaFormat>, PixelFormat>
        {
            { Tuple.Create(PlatformType.Windows, AlphaFormat.None), PixelFormat.BC1_UNorm },
            { Tuple.Create(PlatformType.Windows, AlphaFormat.Mask), PixelFormat.BC1_UNorm },
            { Tuple.Create(PlatformType.Windows, AlphaFormat.Explicit), PixelFormat.BC2_UNorm },
            { Tuple.Create(PlatformType.Windows, AlphaFormat.Interpolated), PixelFormat.BC3_UNorm },

            { Tuple.Create(PlatformType.UWP, AlphaFormat.None), PixelFormat.BC1_UNorm },
            { Tuple.Create(PlatformType.UWP, AlphaFormat.Mask), PixelFormat.BC1_UNorm },
            { Tuple.Create(PlatformType.UWP, AlphaFormat.Explicit), PixelFormat.BC2_UNorm },
            { Tuple.Create(PlatformType.UWP, AlphaFormat.Interpolated), PixelFormat.BC3_UNorm },

            { Tuple.Create(PlatformType.Android, AlphaFormat.None), PixelFormat.ETC1 },
            { Tuple.Create(PlatformType.Android, AlphaFormat.Mask), PixelFormat.ETC2_RGB_A1 },
            { Tuple.Create(PlatformType.Android, AlphaFormat.Explicit), PixelFormat.ETC2_RGBA },
            { Tuple.Create(PlatformType.Android, AlphaFormat.Interpolated), PixelFormat.ETC2_RGBA },

            { Tuple.Create(PlatformType.iOS, AlphaFormat.None), PixelFormat.ETC1 },
            { Tuple.Create(PlatformType.iOS, AlphaFormat.Mask), PixelFormat.ETC2_RGB_A1 },
            { Tuple.Create(PlatformType.iOS, AlphaFormat.Explicit), PixelFormat.ETC2_RGBA },
            { Tuple.Create(PlatformType.iOS, AlphaFormat.Interpolated), PixelFormat.ETC2_RGBA },
        };

        private static void CheckTextureFormat(Game game, string textureUrl, AlphaFormat expectedFormat)
        {
            var expectedPixelFormat = PlaformAndAlphaToPixelFormats[Tuple.Create(Platform.Type, expectedFormat)];
            var texture = game.Content.Load<Texture>(textureUrl);
            Assert.Equal(expectedPixelFormat, texture.Format);
            game.Content.Unload(texture);
        }

        [Fact]
        public void TextureAlphaFormatTests()
        {
            PerformTest(
                game =>
                {
                    CheckTextureFormat(game, "JpegNone", AlphaFormat.None);
                    CheckTextureFormat(game, "JpegMask", AlphaFormat.Mask);
                    CheckTextureFormat(game, "JpegAuto", AlphaFormat.None);
                    CheckTextureFormat(game, "JpegExplicit", AlphaFormat.Explicit);
                    CheckTextureFormat(game, "JpegInterpolated", AlphaFormat.Interpolated);
                }
                );
        }

        [Fact]
        public void TextureAutoAlphaResultNoneTests()
        {
            PerformTest(
                game =>
                {
                    CheckTextureFormat(game, "JpegAuto", AlphaFormat.None);
                    CheckTextureFormat(game, "PngNoAlpha", AlphaFormat.None);
                    CheckTextureFormat(game, "DdsNoAlpha", AlphaFormat.None);
                    CheckTextureFormat(game, "JpegColorResultFalse", AlphaFormat.None);
                    CheckTextureFormat(game, "PngColorResultFalse", AlphaFormat.None);
                }
                );
        }

        [Fact]
        public void TextureAutoAlphaResultMaskTests()
        {
            PerformTest(
                game =>
                {
                    CheckTextureFormat(game, "PngMask", AlphaFormat.Mask);
                    CheckTextureFormat(game, "DdsMaskBC1", AlphaFormat.Mask);
                    CheckTextureFormat(game, "DdsMaskBC3", AlphaFormat.Mask);
                    CheckTextureFormat(game, "JpegColorResultTrue", AlphaFormat.Mask);
                    CheckTextureFormat(game, "PngColorResultTrue", AlphaFormat.Mask);
                }
                );
        }

        [Fact]
        public void TextureAutoAlphaResultInterpolatedTests()
        {
            PerformTest(
                game =>
                {
                    CheckTextureFormat(game, "PngInterpolated", AlphaFormat.Interpolated);
                    CheckTextureFormat(game, "DdsInterpolated", AlphaFormat.Interpolated);
                }
                );
        }

        private static void CheckSpriteTransparencies(Game game, string spriteSheetName, AlphaFormat alphaFormat)
        {
            var expectedPixelFormat = PlaformAndAlphaToPixelFormats[Tuple.Create(Platform.Type, alphaFormat)];
            var spriteSheet = game.Content.Load<SpriteSheet>(spriteSheetName);

            // check the textures pixel format
            foreach (var texture in spriteSheet.Sprites.Select(s => s.Texture))
                Assert.Equal(expectedPixelFormat, texture.Format);

            for (int i = 0; i < spriteSheet.Sprites.Count; i++)
            {
                var sprite = spriteSheet.Sprites[i];
                Assert.Equal(i!=0 && alphaFormat != AlphaFormat.None, sprite.IsTransparent); // except sprite 0 all sprites have transparency expect if the texture alpha is 0
            }
            game.Content.Unload(spriteSheet);
        }

        [Fact]
        public void SpritesSheetNoAlphaTests()
        {
            PerformTest(
                game =>
                {
                    CheckSpriteTransparencies(game, "SheetNoAlpha", AlphaFormat.None);
                    CheckSpriteTransparencies(game, "SheetNoAlphaPacked", AlphaFormat.None);
                }
                );
        }

        [Fact]
        public void SpritesSheetMaskAlphaTests()
        {
            PerformTest(
                game =>
                {
                    CheckSpriteTransparencies(game, "SheetMaskAlpha", AlphaFormat.Mask);
                    CheckSpriteTransparencies(game, "SheetMaskAlphaPacked", AlphaFormat.Mask);
                }
                );
        }

        [Fact]
        public void SpritesSheetExplicitAlphaTests()
        {
            PerformTest(
                game =>
                {
                    CheckSpriteTransparencies(game, "SheetExplicitAlpha", AlphaFormat.Explicit);
                    CheckSpriteTransparencies(game, "SheetExplicitAlphaPacked", AlphaFormat.Explicit);
                }
                );
        }

        [Fact]
        public void SpritesSheetInterpolatedAlphaTests()
        {
            PerformTest(
                game =>
                {
                    CheckSpriteTransparencies(game, "SheetInterpolatedAlpha", AlphaFormat.Interpolated);
                    CheckSpriteTransparencies(game, "SheetInterpolatedAlphaPacked", AlphaFormat.Interpolated);
                }
                );
        }

        [Fact]
        public void SpritesSheetAutoAlphaTests()
        {
            PerformTest(
                game =>
                {
                    CheckSpriteTransparencies(game, "SheetAutoNoAlpha", AlphaFormat.None);
                    CheckSpriteTransparencies(game, "SheetAutoNoAlphaPacked", AlphaFormat.None);
                    CheckSpriteTransparencies(game, "SheetAutoMask", AlphaFormat.Mask);
                    CheckSpriteTransparencies(game, "SheetAutoMaskPacked", AlphaFormat.Mask);
                    CheckSpriteTransparencies(game, "SheetAutoInterpolated", AlphaFormat.Interpolated);
                    CheckSpriteTransparencies(game, "SheetAutoInterpolatedPacked", AlphaFormat.Interpolated);
                }
                );
        }

        [Fact]
        public void SpritesSheetColorTransparency()
        {
            PerformTest(
                game =>
                {
                    CheckSpriteTransparencies(game, "SheetColorNoAlpha", AlphaFormat.None);
                    CheckSpriteTransparencies(game, "SheetColorNoAlphaPacked", AlphaFormat.None);
                    CheckSpriteTransparencies(game, "SheetColorMask", AlphaFormat.Mask);
                    CheckSpriteTransparencies(game, "SheetColorMaskPacked", AlphaFormat.Mask);
                    //CheckSpriteTransparencies(game, "SheetColorAutoNoAlpha", AlphaFormat.None); // currently failing due to fact that texture with Alpha.None does not remove the alpha channel in generated BC1 texture
                    //CheckSpriteTransparencies(game, "SheetColorAutoNoAlphaPacked", AlphaFormat.None); // TODO reactivate those tests when this problem is fixed.
                    CheckSpriteTransparencies(game, "SheetColorAutoMask", AlphaFormat.Mask);
                    CheckSpriteTransparencies(game, "SheetColorAutoMaskPacked", AlphaFormat.Mask);
                }
                );
        }
    }
}
