// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NUnit.Framework;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Graphics.Regression;
using Xenko.Rendering.Sprites;

namespace Xenko.Engine.Tests
{
    public class SpriteProviderTests : GameTestBase
    {
        [Test]
        public void SpriteFromSheetTests()
        {
            var fromNullSheet = new SpriteFromSheet();
            Assert.AreEqual(0, fromNullSheet.SpritesCount);
            Assert.IsNull(fromNullSheet.GetSprite());

            var emptySheet = new SpriteSheet();
            var fromEmptySheet = new SpriteFromSheet { Sheet = emptySheet };
            Assert.AreEqual(0, fromEmptySheet.SpritesCount);
            Assert.IsNull(fromEmptySheet.GetSprite());

            var validSheet = new SpriteSheet { Sprites = { new Sprite("0"), new Sprite("1") } };
            var fromValidSheet = new SpriteFromSheet { Sheet = validSheet };
            Assert.AreEqual(2, fromValidSheet.SpritesCount);
            for (var i = 0; i < fromValidSheet.SpritesCount; i++)
            {
                fromValidSheet.CurrentFrame = i;
                Assert.AreEqual(i.ToString(), fromValidSheet.GetSprite().Name);
            }
        }

        [Test]
        public void SpriteFromTextureTests()
        {
            PerformTest(game =>
            {
                var provider = new SpriteFromTexture();
                Assert.AreEqual(1, provider.SpritesCount);
                Assert.IsNotNull(provider.GetSprite());

                var sprite = provider.GetSprite();
                Assert.IsNull(sprite.Texture);
                Assert.AreEqual(new RectangleF(), sprite.Region);
                Assert.AreEqual(Vector2.Zero, sprite.Center);

                var texture1 = Texture.New2D(game.GraphicsDevice, 123, 234, 1, PixelFormat.B8G8R8A8_UNorm);
                provider.Texture = texture1;
                Assert.AreEqual(texture1, sprite.Texture);
                Assert.AreEqual(new RectangleF(0, 0, texture1.Width, texture1.Height), sprite.Region);
                Assert.AreEqual(new Vector2(texture1.Width, texture1.Height) / 2, sprite.Center);

                var texture2 = Texture.New2D(game.GraphicsDevice, 12, 23, 1, PixelFormat.B8G8R8A8_UNorm);
                provider.Texture = texture2;
                Assert.AreEqual(texture2, sprite.Texture);
                Assert.AreEqual(new RectangleF(0, 0, texture2.Width, texture2.Height), sprite.Region);
                Assert.AreEqual(new Vector2(texture2.Width, texture2.Height) / 2, sprite.Center);

                provider.IsTransparent = false;
                Assert.IsFalse(sprite.IsTransparent);
                provider.IsTransparent = true;
                Assert.IsTrue(sprite.IsTransparent);

                provider.CenterFromMiddle = false;
                Assert.AreEqual(Vector2.Zero, sprite.Center);
                provider.Center = new Vector2(43, 54);
                Assert.AreEqual(provider.Center, sprite.Center);
                provider.CenterFromMiddle = true;
                Assert.AreEqual(provider.Center + new Vector2(texture2.Width, texture2.Height) / 2, sprite.Center);

                Assert.AreEqual(new Vector2(provider.PixelsPerUnit), sprite.PixelsPerUnit);
                provider.PixelsPerUnit = 1;
                Assert.AreEqual(new Vector2(provider.PixelsPerUnit), sprite.PixelsPerUnit);
                Assert.AreEqual(new Vector2(texture2.Width, texture2.Height), sprite.Size);
            });
        }
    }
}
