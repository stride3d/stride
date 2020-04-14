// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Regression;

namespace Stride.Engine.Tests
{
    /// <summary>
    /// Test class for <see cref="Sprite"/>
    /// </summary>
    public class SpriteTests : GameTestBase
    {
        private const string DefaultName = "toto";

        [Fact]
        public void Constructor1Tests()
        {
            {
                // empty
                var sprite = new Sprite();
                var sprite2 = new Sprite();
                Assert.NotNull(sprite.Name);
                Assert.NotEqual(sprite.Name, sprite2.Name);
                Assert.Equal(ImageOrientation.AsIs, sprite.Orientation);
                Assert.Equal(Vector2.Zero, sprite.Center);
                Assert.Equal(Vector4.Zero, sprite.Borders);
                Assert.False(sprite.HasBorders);
                Assert.Null(sprite.Texture);
                Assert.True(sprite.IsTransparent);
                Assert.Equal(new Vector2(100), sprite.PixelsPerUnit);
                Assert.Equal(new RectangleF(), sprite.Region);
                Assert.Equal(Vector2.Zero, sprite.Size);
                Assert.Equal(Vector2.Zero, sprite.SizeInPixels);
            }

            {
                // name
                var sprite = new Sprite(DefaultName);
                Assert.Equal(DefaultName, sprite.Name);
                Assert.Equal(ImageOrientation.AsIs, sprite.Orientation);
                Assert.Equal(Vector2.Zero, sprite.Center);
                Assert.Equal(Vector4.Zero, sprite.Borders);
                Assert.False(sprite.HasBorders);
                Assert.Null(sprite.Texture);
                Assert.True(sprite.IsTransparent);
                Assert.Equal(new Vector2(100), sprite.PixelsPerUnit);
                Assert.Equal(new RectangleF(), sprite.Region);
                Assert.Equal(Vector2.Zero, sprite.Size);
                Assert.Equal(Vector2.Zero, sprite.SizeInPixels);
            }
        }

        [Fact]
        public void Constructor2Tests()
        {
            PerformTest(game =>
            {
                // texture
                var textureSize = new Vector2(50, 75);
                var texture = Texture.New2D(game.GraphicsDevice, (int)textureSize.X, (int)textureSize.Y, 1, PixelFormat.R8G8B8A8_UNorm);
                var sprite = new Sprite(texture);
                var sprite2 = new Sprite(texture);
                Assert.NotNull(sprite.Name);
                Assert.NotEqual(sprite.Name, sprite2.Name);
                Assert.Equal(ImageOrientation.AsIs, sprite.Orientation);
                Assert.Equal(textureSize/2, sprite.Center);
                Assert.Equal(Vector4.Zero, sprite.Borders);
                Assert.False(sprite.HasBorders);
                Assert.Equal(texture, sprite.Texture);
                Assert.True(sprite.IsTransparent);
                Assert.Equal(new Vector2(100), sprite.PixelsPerUnit);
                Assert.Equal(new RectangleF(0, 0, textureSize.X, textureSize.Y), sprite.Region);
                Assert.Equal(textureSize/100, sprite.Size);
                Assert.Equal(textureSize, sprite.SizeInPixels);
            });

            PerformTest(game =>
            {
                // texture + name
                var textureSize = new Vector2(50, 75);
                var texture = Texture.New2D(game.GraphicsDevice, (int)textureSize.X, (int)textureSize.Y, 1, PixelFormat.R8G8B8A8_UNorm);
                var sprite = new Sprite(DefaultName, texture);
                Assert.Equal(DefaultName, sprite.Name);
                Assert.Equal(ImageOrientation.AsIs, sprite.Orientation);
                Assert.Equal(textureSize / 2, sprite.Center);
                Assert.Equal(Vector4.Zero, sprite.Borders);
                Assert.False(sprite.HasBorders);
                Assert.Equal(texture, sprite.Texture);
                Assert.True(sprite.IsTransparent);
                Assert.Equal(new Vector2(100), sprite.PixelsPerUnit);
                Assert.Equal(new RectangleF(0, 0, textureSize.X, textureSize.Y), sprite.Region);
                Assert.Equal(textureSize / 100, sprite.Size);
                Assert.Equal(textureSize, sprite.SizeInPixels);
            });
        }

        private Sprite CreateSprite(Game game)
        {
            var textureSize = new Vector2(50, 75);
            var texture = Texture.New2D(game.GraphicsDevice, (int)textureSize.X, (int)textureSize.Y, 1, PixelFormat.R8G8B8A8_UNorm);
            return new Sprite(DefaultName, texture);
        }

        [Fact]
        public void NamePropertyTests()
        {
            PerformTest(game =>
            {
                const string otherName = "tutu";
                var sprite = CreateSprite(game);
                Assert.Equal(DefaultName, sprite.Name);
                sprite.Name = otherName;
                Assert.Equal(otherName, sprite.Name);
            });
        }

        [Fact]
        public void TexturePropertyTests()
        {
            // no checks on texture affectation for the moment.
            PerformTest(game =>
            {
                var sprite = CreateSprite(game);
                var region = sprite.Region;
                var center = sprite.Center;

                sprite.Texture = null;
                Assert.Null(sprite.Texture);
                Assert.Equal(region, sprite.Region);
                Assert.Equal(center, sprite.Center);

                var otherText = Texture.New2D(game.GraphicsDevice, 10, 20, 1, PixelFormat.R8G8B8A8_UNorm);
                sprite.Texture = otherText;
                Assert.Equal(otherText, sprite.Texture);
                Assert.Equal(region, sprite.Region);
                Assert.Equal(center, sprite.Center);
            });
        }

        [Fact]
        public void CenterPropertyTests()
        {
            // no checks on center affectation for the moment.
            PerformTest(game =>
            {
                var sprite = CreateSprite(game);
                var textureSize = new Vector2(sprite.Texture.Width, sprite.Texture.Height);

                Assert.Equal(textureSize/2, sprite.Center);

                var newCenter = new Vector2(-1, -2);
                sprite.Center = newCenter;
                Assert.Equal(newCenter, sprite.Center);
                
                newCenter = new Vector2(1000, 2000);
                sprite.Center = newCenter;
                Assert.Equal(newCenter, sprite.Center);
            });
        }

        [Fact]
        public void RegionPropertyTests()
        {
            PerformTest(game =>
            {
                var sprite = CreateSprite(game);
                var textureSize = new Vector2(sprite.Texture.Width, sprite.Texture.Height);

                Assert.Equal(new RectangleF(0, 0, textureSize.X, textureSize.Y), sprite.Region);
                Assert.Equal(textureSize, sprite.SizeInPixels);
                Assert.Equal(new Vector2(textureSize.X/sprite.PixelsPerUnit.X, textureSize.Y/sprite.PixelsPerUnit.Y), sprite.Size);

                textureSize = new Vector2(3, 4);
                var newRegion = new RectangleF(1, 2, textureSize.X, textureSize.Y);
                sprite.Region = newRegion;
                Assert.Equal(newRegion, sprite.Region);
                Assert.Equal(textureSize, sprite.SizeInPixels);
                Assert.Equal(new Vector2(textureSize.X / sprite.PixelsPerUnit.X, textureSize.Y / sprite.PixelsPerUnit.Y), sprite.Size);

                textureSize = new Vector2(0, -1);
                newRegion = new RectangleF(-10, -20, textureSize.X, textureSize.Y);
                sprite.Region = newRegion;
                Assert.Equal(newRegion, sprite.Region);
                Assert.Equal(textureSize, sprite.SizeInPixels);
                Assert.Equal(new Vector2(textureSize.X / sprite.PixelsPerUnit.X, textureSize.Y / sprite.PixelsPerUnit.Y), sprite.Size);
            });
        }
        
        [Fact]
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
                
                Assert.Equal(ImageOrientation.AsIs, sprite.Orientation);

                sprite.Orientation = ImageOrientation.Rotated90;
                
                Assert.Equal(ImageOrientation.Rotated90, sprite.Orientation);

                // this information is region based and should not change
                Assert.Equal(region, sprite.Region);
                Assert.Equal(center, sprite.Center);
                Assert.Equal(border, sprite.Borders);

                // this information orientation based should change
                Assert.Equal(size.YX(), sprite.Size);
                Assert.Equal(sizePixel.YX(), sprite.SizeInPixels);
            });
        }

        [Fact]
        public void SizePropertyTests()
        {
            PerformTest(game =>
            {
                var sprite = CreateSprite(game);

                var newSize = new Vector2(66, 77);
                sprite.Region = new RectangleF(1, 2, newSize.X, newSize.Y);

                Assert.Equal(new Vector2(newSize.X / sprite.PixelsPerUnit.X, newSize.Y / sprite.PixelsPerUnit.Y), sprite.Size);

                var newRatio = new Vector2(1, 2);
                sprite.PixelsPerUnit = newRatio;
                Assert.Equal(newRatio, sprite.PixelsPerUnit);
                Assert.Equal(new Vector2(newSize.X / sprite.PixelsPerUnit.X, newSize.Y / sprite.PixelsPerUnit.Y), sprite.Size);

                newRatio = new Vector2(-1, 0);
                sprite.PixelsPerUnit = newRatio;
                Assert.True(sprite.PixelsPerUnit.X > 0);
                Assert.True(sprite.PixelsPerUnit.Y > 0);
                Assert.False(float.IsInfinity(sprite.Size.X));
                Assert.False(float.IsInfinity(sprite.Size.Y));
                Assert.False(float.IsNaN(sprite.Size.X));
                Assert.False(float.IsNaN(sprite.Size.Y));
            });
        }

        [Fact]
        public void SizeInPixelPropertyTests()
        {
            PerformTest(game =>
            {
                var sizeChanged = false;
                var sprite = CreateSprite(game);
                sprite.SizeChanged += (e, _) => sizeChanged = true;

                var newSize = new Vector2(66, 77);
                sprite.Region = new RectangleF(1, 2, newSize.X, newSize.Y);

                Assert.Equal(newSize, sprite.SizeInPixels);
                Assert.True(sizeChanged);

                sizeChanged = false;
                sprite.Orientation = ImageOrientation.Rotated90;

                Assert.True(sizeChanged);
                Assert.Equal(newSize.YX(), sprite.SizeInPixels);

                sizeChanged = false;
                sprite.Region = sprite.Region;

                Assert.False(sizeChanged);
            });
        }

        [Fact]
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
                Assert.Equal(sprite.Name, clone.Name);
                Assert.Equal(sprite.Texture, clone.Texture);
                Assert.Equal(sprite.Center, clone.Center);
                Assert.Equal(sprite.Region, clone.Region);
                Assert.Equal(sprite.IsTransparent, clone.IsTransparent);
                Assert.Equal(sprite.Orientation, clone.Orientation);
                Assert.Equal(sprite.Borders, clone.Borders);
                Assert.Equal(sprite.HasBorders, clone.HasBorders);
                Assert.Equal(sprite.Size, clone.Size);
                Assert.Equal(sprite.SizeInPixels, clone.SizeInPixels);
                Assert.Equal(sprite.PixelsPerUnit, clone.PixelsPerUnit);
            });
        }
    }
}
