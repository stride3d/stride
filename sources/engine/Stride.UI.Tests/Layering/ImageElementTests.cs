// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.Sprites;
using Stride.UI.Controls;

namespace Stride.UI.Tests.Layering
{
    /// <summary>
    /// A class that contains test functions for layering of the <see cref="ImageElement"/> class.
    /// </summary>
    [System.ComponentModel.Description("Tests for ImageElement layering")]
    public class ImageElementTests : ImageElement
    {
        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Fact]
        public void TestBasicInvalidations()
        {
            var source = new Sprite();

            // ReSharper disable ImplicitlyCapturedClosure

            // - test the properties that are supposed to invalidate the object measurement
            UIElementLayeringTests.TestMeasureInvalidation(this, () => StretchType = StretchType.None);
            UIElementLayeringTests.TestMeasureInvalidation(this, () => StretchDirection = StretchDirection.DownOnly);
            UIElementLayeringTests.TestMeasureInvalidation(this, () => Source = (SpriteFromTexture)source);
            UIElementLayeringTests.TestMeasureInvalidation(this, () => source.Region = new Rectangle(1, 2, 3, 4));
            UIElementLayeringTests.TestMeasureInvalidation(this, () => source.Orientation = ImageOrientation.Rotated90);
            UIElementLayeringTests.TestMeasureInvalidation(this, () => source.Borders = Vector4.One);

            // - test the properties that are not supposed to invalidate the object layout state
            UIElementLayeringTests.TestNoInvalidation(this, () => source.Region = new Rectangle(8, 9, 3, 4)); // if the size of the region does not change we avoid re-measuring
            UIElementLayeringTests.TestNoInvalidation(this, () => source.Orientation = ImageOrientation.Rotated90); // no changes
            UIElementLayeringTests.TestNoInvalidation(this, () => source.Borders = Vector4.One); // no changes

            // ReSharper restore ImplicitlyCapturedClosure
        }

        /// <summary>
        /// Test the <see cref="UIElement.MeasureOverride"/> function
        /// </summary>
        [Fact]
        public void TestMeasureOverride()
        {
            var rand = new Random();
            var imageSize = new Vector3(100, 50, 0);
            var sprite = new Sprite { Region = new Rectangle(0, 0, (int)imageSize.X, (int)imageSize.Y), Borders = new Vector4(1, 2, 3, 4) };
            var image = new ImageElement { Source = (SpriteFromTexture)sprite };

            // Fixed sized
            image.StretchType = StretchType.None;
            image.Measure(rand.NextVector3());
            Assert.Equal(imageSize, image.DesiredSizeWithMargins);

            // Uniform sized
            image.StretchType = StretchType.Uniform;
            image.Measure(new Vector3(50));
            Assert.Equal(new Vector3(50, 25, 0), image.DesiredSizeWithMargins);

            // Uniform to fill sized
            image.StretchType = StretchType.UniformToFill;
            image.Measure(new Vector3(50));
            Assert.Equal(new Vector3(100, 50, 0), image.DesiredSizeWithMargins);

            // Fill on stretch
            image.StretchType = StretchType.FillOnStretch;
            image.Measure(new Vector3(50));
            Assert.Equal(new Vector3(50, 25, 0), image.DesiredSizeWithMargins);

            // Fill
            image.StretchType = StretchType.Fill;
            image.Measure(new Vector3(50));
            Assert.Equal(new Vector3(50, 50, 0), image.DesiredSizeWithMargins);

            // Test minimal size due to borders
            image.StretchType = StretchType.Fill;
            image.Measure(new Vector3());
            Assert.Equal(new Vector3(4, 6, 0), image.DesiredSizeWithMargins);

            // Test with infinite value
            for (var type = 0; type < 5; ++type)
                TestMeasureOverrideInfiniteValues((StretchType)type);

            // Test stretch directions
            image.StretchType = StretchType.Fill;
            image.StretchDirection = StretchDirection.DownOnly;
            image.Measure(new Vector3(200, 300, 220));
            Assert.Equal(new Vector3(100, 50, 0), image.DesiredSizeWithMargins);
            image.Measure(new Vector3(20, 15, 30));
            Assert.Equal(new Vector3(20, 15, 0), image.DesiredSizeWithMargins);
            image.StretchDirection = StretchDirection.UpOnly;
            image.Measure(new Vector3(200, 300, 220));
            Assert.Equal(new Vector3(200, 300, 0), image.DesiredSizeWithMargins);
            image.Measure(new Vector3(20, 30, 22));
            Assert.Equal(new Vector3(100, 50, 0), image.DesiredSizeWithMargins);
        }

        private void TestMeasureOverrideInfiniteValues(StretchType stretch)
        {
            var imageSize = new Vector3(100, 50, 0);
            var sprite = new Sprite { Region = new Rectangle(0, 0, (int)imageSize.X, (int)imageSize.Y), Borders = new Vector4(1, 2, 3, 4) };
            var image = new ImageElement { Source = (SpriteFromTexture)sprite, StretchType = stretch };
            
            image.Measure(new Vector3(float.PositiveInfinity));
            Assert.Equal(imageSize, image.DesiredSizeWithMargins);

            image.Measure(new Vector3(150, float.PositiveInfinity, 10));
            Assert.Equal(stretch == StretchType.None ? imageSize : new Vector3(150, 75, 0), image.DesiredSizeWithMargins);
        }

        /// <summary>
        /// Test the <see cref="UIElement.ArrangeOverride"/> function
        /// </summary>
        [Fact]
        public void TestArrangeOverride()
        {
            var rand = new Random();
            var imageSize = new Vector3(100, 50, 0);
            var sprite = new Sprite { Region = new Rectangle(0, 0, (int)imageSize.X, (int)imageSize.Y), Borders = new Vector4(1, 2, 3, 4) };
            var image = new ImageElement { Source = (SpriteFromTexture)sprite };

            // Fixed sized
            image.StretchType = StretchType.None;
            image.Arrange(rand.NextVector3(), false);
            Assert.Equal(imageSize, image.RenderSize);

            // Uniform sized
            image.StretchType = StretchType.Uniform;
            image.Arrange(new Vector3(50), false);
            Assert.Equal(new Vector3(50, 25, 0), image.RenderSize);

            // Uniform to fill sized
            image.StretchType = StretchType.UniformToFill;
            image.Arrange(new Vector3(50), false);
            Assert.Equal(new Vector3(100, 50, 0), image.RenderSize);

            // Fill on stretch
            image.StretchType = StretchType.FillOnStretch;
            image.Arrange(new Vector3(50), false);
            Assert.Equal(new Vector3(50, 50, 0), image.RenderSize);

            // Fill
            image.StretchType = StretchType.Fill;
            image.Arrange(new Vector3(50), false);
            Assert.Equal(new Vector3(50, 50, 0), image.RenderSize);

            // Test there is no minimal size due to borders in arrange
            image.StretchType = StretchType.Fill;
            image.Arrange(new Vector3(), false);
            Assert.Equal(new Vector3(), image.RenderSize);

            // Test with infinite value
            for (var type = 0; type < 5; ++type)
                TestArrangeOverrideInfiniteValues((StretchType)type);

            // Test stretch directions
            image.StretchType = StretchType.Fill;
            image.StretchDirection = StretchDirection.DownOnly;
            image.Arrange(new Vector3(200, 300, 220), false);
            Assert.Equal(new Vector3(100, 50, 0), image.RenderSize);
            image.Arrange(new Vector3(20, 15, 30), false);
            Assert.Equal(new Vector3(20, 15, 0), image.RenderSize);
            image.StretchDirection = StretchDirection.UpOnly;
            image.Arrange(new Vector3(200, 300, 220), false);
            Assert.Equal(new Vector3(200, 300, 0), image.RenderSize);
            image.Arrange(new Vector3(20, 30, 22), false);
            Assert.Equal(new Vector3(100, 50, 0), image.RenderSize);
        }

        private void TestArrangeOverrideInfiniteValues(StretchType stretch)
        {
            var imageSize = new Vector3(100, 50, 0);
            var sprite = new Sprite { Region = new Rectangle(0, 0, (int)imageSize.X, (int)imageSize.Y), Borders = new Vector4(1, 2, 3, 4) };
            var image = new ImageElement { Source = (SpriteFromTexture)sprite, StretchType = stretch };

            image.Arrange(new Vector3(float.PositiveInfinity), false);
            Assert.Equal(imageSize, image.RenderSize);

            image.Arrange(new Vector3(150, float.PositiveInfinity, 10), false);
            Assert.Equal(stretch == StretchType.None ? imageSize : new Vector3(150, 75, 0), image.RenderSize);
        }
    }
}
