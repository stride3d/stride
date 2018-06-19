// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NUnit.Framework;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Graphics.Regression;

namespace Xenko.Engine.Tests
{
    /// <summary>
    /// Test class for <see cref="Sprite"/>
    /// </summary>
    public class SpriteTests : GameTestBase
    {
        private const string DefaultName = "toto";

        [Test]
        public void Constructor1Tests()
        {
            {
                // empty
                var sprite = new Sprite();
                var sprite2 = new Sprite();
                Assert.IsNotNull(sprite.Name);
                Assert.AreNotEqual(sprite.Name, sprite2.Name);
                Assert.AreEqual(ImageOrientation.AsIs, sprite.Orientation);
                Assert.AreEqual(Vector2.Zero, sprite.Center);
                Assert.AreEqual(Vector4.Zero, sprite.Borders);
                Assert.IsFalse(sprite.HasBorders);
                Assert.IsNull(sprite.Texture);
                Assert.IsTrue(sprite.IsTransparent);
                Assert.AreEqual(new Vector2(100), sprite.PixelsPerUnit);
                Assert.AreEqual(new RectangleF(), sprite.Region);
                Assert.AreEqual(Vector2.Zero, sprite.Size);
                Assert.AreEqual(Vector2.Zero, sprite.SizeInPixels);
            }

            {
                // name
                var sprite = new Sprite(DefaultName);
                Assert.AreEqual(DefaultName, sprite.Name);
                Assert.AreEqual(ImageOrientation.AsIs, sprite.Orientation);
                Assert.AreEqual(Vector2.Zero, sprite.Center);
                Assert.AreEqual(Vector4.Zero, sprite.Borders);
                Assert.IsFalse(sprite.HasBorders);
                Assert.IsNull(sprite.Texture);
                Assert.IsTrue(sprite.IsTransparent);
                Assert.AreEqual(new Vector2(100), sprite.PixelsPerUnit);
                Assert.AreEqual(new RectangleF(), sprite.Region);
                Assert.AreEqual(Vector2.Zero, sprite.Size);
                Assert.AreEqual(Vector2.Zero, sprite.SizeInPixels);
            }
        }

        [Test]
        public void Constructor2Tests()
        {
            PerformTest(game =>
            {
                // texture
                var textureSize = new Vector2(50, 75);
                var texture = Texture.New2D(game.GraphicsDevice, (int)textureSize.X, (int)textureSize.Y, 1, PixelFormat.R8G8B8A8_UNorm);
                var sprite = new Sprite(texture);
                var sprite2 = new Sprite(texture);
                Assert.IsNotNull(sprite.Name);
                Assert.AreNotEqual(sprite.Name, sprite2.Name);
                Assert.AreEqual(ImageOrientation.AsIs, sprite.Orientation);
                Assert.AreEqual(textureSize/2, sprite.Center);
                Assert.AreEqual(Vector4.Zero, sprite.Borders);
                Assert.IsFalse(sprite.HasBorders);
                Assert.AreEqual(texture, sprite.Texture);
                Assert.IsTrue(sprite.IsTransparent);
                Assert.AreEqual(new Vector2(100), sprite.PixelsPerUnit);
                Assert.AreEqual(new RectangleF(0, 0, textureSize.X, textureSize.Y), sprite.Region);
                Assert.AreEqual(textureSize/100, sprite.Size);
                Assert.AreEqual(textureSize, sprite.SizeInPixels);
            });

            PerformTest(game =>
            {
                // texture + name
                var textureSize = new Vector2(50, 75);
                var texture = Texture.New2D(game.GraphicsDevice, (int)textureSize.X, (int)textureSize.Y, 1, PixelFormat.R8G8B8A8_UNorm);
                var sprite = new Sprite(DefaultName, texture);
                Assert.AreEqual(DefaultName, sprite.Name);
                Assert.AreEqual(ImageOrientation.AsIs, sprite.Orientation);
                Assert.AreEqual(textureSize / 2, sprite.Center);
                Assert.AreEqual(Vector4.Zero, sprite.Borders);
                Assert.IsFalse(sprite.HasBorders);
                Assert.AreEqual(texture, sprite.Texture);
                Assert.IsTrue(sprite.IsTransparent);
                Assert.AreEqual(new Vector2(100), sprite.PixelsPerUnit);
                Assert.AreEqual(new RectangleF(0, 0, textureSize.X, textureSize.Y), sprite.Region);
                Assert.AreEqual(textureSize / 100, sprite.Size);
                Assert.AreEqual(textureSize, sprite.SizeInPixels);
            });
        }

        private Sprite CreateSprite(Game game)
        {
            var textureSize = new Vector2(50, 75);
            var texture = Texture.New2D(game.GraphicsDevice, (int)textureSize.X, (int)textureSize.Y, 1, PixelFormat.R8G8B8A8_UNorm);
            return new Sprite(DefaultName, texture);
        }

        [Test]
        public void NamePropertyTests()
        {
            PerformTest(game =>
            {
                const string otherName = "tutu";
                var sprite = CreateSprite(game);
                Assert.AreEqual(DefaultName, sprite.Name);
                sprite.Name = otherName;
                Assert.AreEqual(otherName, sprite.Name);
            });
        }

        [Test]
        public void TexturePropertyTests()
        {
            // no checks on texture affectation for the moment.
            PerformTest(game =>
            {
                var sprite = CreateSprite(game);
                var region = sprite.Region;
                var center = sprite.Center;

                sprite.Texture = null;
                Assert.AreEqual(null, sprite.Texture);
                Assert.AreEqual(region, sprite.Region);
                Assert.AreEqual(center, sprite.Center);

                var otherText = Texture.New2D(game.GraphicsDevice, 10, 20, 1, PixelFormat.R8G8B8A8_UNorm);
                sprite.Texture = otherText;
                Assert.AreEqual(otherText, sprite.Texture);
                Assert.AreEqual(region, sprite.Region);
                Assert.AreEqual(center, sprite.Center);
            });
        }

        [Test]
        public void CenterPropertyTests()
        {
            // no checks on center affectation for the moment.
            PerformTest(game =>
            {
                var sprite = CreateSprite(game);
                var textureSize = new Vector2(sprite.Texture.Width, sprite.Texture.Height);

                Assert.AreEqual(textureSize/2, sprite.Center);

                var newCenter = new Vector2(-1, -2);
                sprite.Center = newCenter;
                Assert.AreEqual(newCenter, sprite.Center);
                
                newCenter = new Vector2(1000, 2000);
                sprite.Center = newCenter;
                Assert.AreEqual(newCenter, sprite.Center);
            });
        }

        [Test]
        public void RegionPropertyTests()
        {
            PerformTest(game =>
            {
                var sprite = CreateSprite(game);
                var textureSize = new Vector2(sprite.Texture.Width, sprite.Texture.Height);

                Assert.AreEqual(new RectangleF(0, 0, textureSize.X, textureSize.Y), sprite.Region);
                Assert.AreEqual(textureSize, sprite.SizeInPixels);
                Assert.AreEqual(new Vector2(textureSize.X/sprite.PixelsPerUnit.X, textureSize.Y/sprite.PixelsPerUnit.Y), sprite.Size);

                textureSize = new Vector2(3, 4);
                var newRegion = new RectangleF(1, 2, textureSize.X, textureSize.Y);
                sprite.Region = newRegion;
                Assert.AreEqual(newRegion, sprite.Region);
                Assert.AreEqual(textureSize, sprite.SizeInPixels);
                Assert.AreEqual(new Vector2(textureSize.X / sprite.PixelsPerUnit.X, textureSize.Y / sprite.PixelsPerUnit.Y), sprite.Size);

                textureSize = new Vector2(0, -1);
                newRegion = new RectangleF(-10, -20, textureSize.X, textureSize.Y);
                sprite.Region = newRegion;
                Assert.AreEqual(newRegion, sprite.Region);
                Assert.AreEqual(textureSize, sprite.SizeInPixels);
                Assert.AreEqual(new Vector2(textureSize.X / sprite.PixelsPerUnit.X, textureSize.Y / sprite.PixelsPerUnit.Y), sprite.Size);
            });
        }
        
        [Test]
        public void OrientationPropertyTests()
        {
            PerformTest(game =>
            {
                var sprite = CreateSprite(game);
                var sizePixel = sprite.SizeInPixels;
                var size = sprite.Size;
                var region = sprite.Region;
                var center = sprite.Center;
                var border = sprite.Borders;
                
                Assert.AreEqual(ImageOrientation.AsIs, sprite.Orientation);

                sprite.Orientation = ImageOrientation.Rotated90;
                
                Assert.AreEqual(ImageOrientation.Rotated90, sprite.Orientation);

                // this information is region based and should not change
                Assert.AreEqual(region, sprite.Region);
                Assert.AreEqual(center, sprite.Center);
                Assert.AreEqual(border, sprite.Borders);

                // this information orientation based should change
                Assert.AreEqual(size.YX(), sprite.Size);
                Assert.AreEqual(sizePixel.YX(), sprite.SizeInPixels);
            });
        }

        [Test]
        public void SizePropertyTests()
        {
            PerformTest(game =>
            {
                var sprite = CreateSprite(game);

                var newSize = new Vector2(66, 77);
                sprite.Region = new RectangleF(1, 2, newSize.X, newSize.Y);

                Assert.AreEqual(new Vector2(newSize.X / sprite.PixelsPerUnit.X, newSize.Y / sprite.PixelsPerUnit.Y), sprite.Size);

                var newRatio = new Vector2(1, 2);
                sprite.PixelsPerUnit = newRatio;
                Assert.AreEqual(newRatio, sprite.PixelsPerUnit);
                Assert.AreEqual(new Vector2(newSize.X / sprite.PixelsPerUnit.X, newSize.Y / sprite.PixelsPerUnit.Y), sprite.Size);

                newRatio = new Vector2(-1, 0);
                sprite.PixelsPerUnit = newRatio;
                Assert.IsTrue(sprite.PixelsPerUnit.X > 0);
                Assert.IsTrue(sprite.PixelsPerUnit.Y > 0);
                Assert.IsFalse(float.IsInfinity(sprite.Size.X));
                Assert.IsFalse(float.IsInfinity(sprite.Size.Y));
                Assert.IsFalse(float.IsNaN(sprite.Size.X));
                Assert.IsFalse(float.IsNaN(sprite.Size.Y));
            });
        }

        [Test]
        public void SizeInPixelPropertyTests()
        {
            PerformTest(game =>
            {
                var sizeChanged = false;
                var sprite = CreateSprite(game);
                sprite.SizeChanged += (e, _) => sizeChanged = true;

                var newSize = new Vector2(66, 77);
                sprite.Region = new RectangleF(1, 2, newSize.X, newSize.Y);

                Assert.AreEqual(newSize, sprite.SizeInPixels);
                Assert.IsTrue(sizeChanged);

                sizeChanged = false;
                sprite.Orientation = ImageOrientation.Rotated90;

                Assert.IsTrue(sizeChanged);
                Assert.AreEqual(newSize.YX(), sprite.SizeInPixels);

                sizeChanged = false;
                sprite.Region = sprite.Region;

                Assert.IsFalse(sizeChanged);
            });
        }

        [Test]
        public void CloneMethodTests()
        {
            PerformTest(game =>
            {
                var sprite = CreateSprite(game);
                sprite.Region = new RectangleF(1,2,3,4);
                sprite.Name = "toto124";
                sprite.PixelsPerUnit = new Vector2(123, 1234);
                sprite.Borders = new Vector4(4,3,2,1);
                sprite.Center = new Vector2(21, 43);
                sprite.Orientation = ImageOrientation.Rotated90;
                sprite.IsTransparent = true;

                var clone = sprite.Clone();
                Assert.AreEqual(sprite.Name, clone.Name);
                Assert.AreEqual(sprite.Texture, clone.Texture);
                Assert.AreEqual(sprite.Center, clone.Center);
                Assert.AreEqual(sprite.Region, clone.Region);
                Assert.AreEqual(sprite.IsTransparent, clone.IsTransparent);
                Assert.AreEqual(sprite.Orientation, clone.Orientation);
                Assert.AreEqual(sprite.Borders, clone.Borders);
                Assert.AreEqual(sprite.HasBorders, clone.HasBorders);
                Assert.AreEqual(sprite.Size, clone.Size);
                Assert.AreEqual(sprite.SizeInPixels, clone.SizeInPixels);
                Assert.AreEqual(sprite.PixelsPerUnit, clone.PixelsPerUnit);
            });
        }
    }
}
