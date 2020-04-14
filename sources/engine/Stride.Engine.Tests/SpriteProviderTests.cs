// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Rendering.Sprites;

namespace Stride.Engine.Tests
{
    public class SpriteProviderTests : GameTestBase
    {
        [Fact]
        public void SpriteFromSheetTests()
        {
            var fromNullSheet = new SpriteFromSheet();
            Assert.Equal(0, fromNullSheet.SpritesCount);
            Assert.Null(fromNullSheet.GetSprite());

            var emptySheet = new SpriteSheet();
            var fromEmptySheet = new SpriteFromSheet { Sheet = emptySheet };
            Assert.Equal(0, fromEmptySheet.SpritesCount);
            Assert.Null(fromEmptySheet.GetSprite());

            var validSheet = new SpriteSheet { Sprites = { new Sprite("0"), new Sprite("1") } };
            var fromValidSheet = new SpriteFromSheet { Sheet = validSheet };
            Assert.Equal(2, fromValidSheet.SpritesCount);
            for (var i = 0; i < fromValidSheet.SpritesCount; i++)
            {
                fromValidSheet.CurrentFrame = i;
                Assert.Equal(i.ToString(), fromValidSheet.GetSprite().Name);
            }
        }

        [Fact]
        public void SpriteFromTextureTests()
        {
            PerformTest(game =>
            {
                var provider = new SpriteFromTexture();
                Assert.Equal(1, provider.SpritesCount);
                Assert.NotNull(provider.GetSprite());

                var sprite = provider.GetSprite();
                Assert.Null(sprite.Texture);
                Assert.Equal(new RectangleF(), sprite.Region);
                Assert.Equal(Vector2.Zero, sprite.Center);

                var texture1 = Texture.New2D(game.GraphicsDevice, 123, 234, 1, PixelFormat.B8G8R8A8_UNorm);
                provider.Texture = texture1;
                Assert.Equal(texture1, sprite.Texture);
                Assert.Equal(new RectangleF(0, 0, texture1.Width, texture1.Height), sprite.Region);
                Assert.Equal(new Vector2(texture1.Width, texture1.Height) / 2, sprite.Center);

                var texture2 = Texture.New2D(game.GraphicsDevice, 12, 23, 1, PixelFormat.B8G8R8A8_UNorm);
                provider.Texture = texture2;
                Assert.Equal(texture2, sprite.Texture);
                Assert.Equal(new RectangleF(0, 0, texture2.Width, texture2.Height), sprite.Region);
                Assert.Equal(new Vector2(texture2.Width, texture2.Height) / 2, sprite.Center);

                provider.IsTransparent = false;
                Assert.False(sprite.IsTransparent);
                provider.IsTransparent = true;
                Assert.True(sprite.IsTransparent);

                provider.CenterFromMiddle = false;
                Assert.Equal(Vector2.Zero, sprite.Center);
                provider.Center = new Vector2(43, 54);
                Assert.Equal(provider.Center, sprite.Center);
                provider.CenterFromMiddle = true;
                Assert.Equal(provider.Center + new Vector2(texture2.Width, texture2.Height) / 2, sprite.Center);

                Assert.Equal(new Vector2(provider.PixelsPerUnit), sprite.PixelsPerUnit);
                provider.PixelsPerUnit = 1;
                Assert.Equal(new Vector2(provider.PixelsPerUnit), sprite.PixelsPerUnit);
                Assert.Equal(new Vector2(texture2.Width, texture2.Height), sprite.Size);
            });
        }
    }
}
