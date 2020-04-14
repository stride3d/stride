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
    /// Unit tests for the UI <see cref="Slider"/> elements.
    /// </summary>
    public class SliderTests
    {
        /// <summary>
        /// Launch all the tests contained in <see cref="ControlTests"/>
        /// </summary>
        internal void TestAll()
        {
            TestProperties();
        }

        [Fact]
        public void TestProperties()
        {
            var slider = new Slider();

            // test properties default values
            Assert.Equal(10, slider.TickFrequency);
            Assert.Equal(0, slider.Minimum);
            Assert.Equal(1, slider.Maximum);
            Assert.Equal(0.1f, slider.Step);
            Assert.Equal(0, slider.Value);
            Assert.Equal(5, slider.DrawLayerNumber);
            Assert.False(slider.IsDirectionReversed);
            Assert.False(slider.AreTicksDisplayed);
            Assert.False(slider.ShouldSnapToTicks);
            Assert.True(slider.CanBeHitByUser);
            Assert.Equal(Orientation.Horizontal, slider.Orientation);
            Assert.Equal(HorizontalAlignment.Center, slider.HorizontalAlignment);
            Assert.Equal(VerticalAlignment.Center, slider.VerticalAlignment);
            Assert.Equal(DepthAlignment.Center, slider.DepthAlignment);
        }

        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Fact]
        public void TestBasicInvalidations()
        {
            var slider = new Slider();

            // - test the properties that are supposed to invalidate the object layout state
            UIElementLayeringTests.TestMeasureInvalidation(slider, () => slider.Orientation = Orientation.Vertical);
            UIElementLayeringTests.TestMeasureInvalidation(slider, () => slider.TrackBackgroundImage = (SpriteFromTexture)new Sprite());
            
            // - test the properties that are not supposed to invalidate the object layout state
            UIElementLayeringTests.TestNoInvalidation(slider, () => slider.AreTicksDisplayed = true);
            UIElementLayeringTests.TestNoInvalidation(slider, () => slider.CanBeHitByUser = false);
            UIElementLayeringTests.TestNoInvalidation(slider, () => slider.ShouldSnapToTicks = true);
            UIElementLayeringTests.TestNoInvalidation(slider, () => slider.DrawLayerNumber = 60);
            UIElementLayeringTests.TestNoInvalidation(slider, () => slider.Value = 0.5f);
            UIElementLayeringTests.TestNoInvalidation(slider, () => slider.Step = 0.2f);
            UIElementLayeringTests.TestNoInvalidation(slider, () => slider.Maximum = 0.2f);
            UIElementLayeringTests.TestNoInvalidation(slider, () => slider.Minimum = 0.1f);
            UIElementLayeringTests.TestNoInvalidation(slider, () => slider.TrackForegroundImage = (SpriteFromTexture)new Sprite());
            UIElementLayeringTests.TestNoInvalidation(slider, () => slider.ThumbImage = (SpriteFromTexture)new Sprite());
            UIElementLayeringTests.TestNoInvalidation(slider, () => slider.MouseOverThumbImage = (SpriteFromTexture)new Sprite());
            UIElementLayeringTests.TestNoInvalidation(slider, () => slider.TickImage = (SpriteFromTexture)new Sprite());
            UIElementLayeringTests.TestNoInvalidation(slider, () => slider.TickOffset = new float());
            UIElementLayeringTests.TestNoInvalidation(slider, () => slider.TrackStartingOffsets = new Vector2());
        }

        /// <summary>
        /// Test the <see cref="Slider.Minimum"/> and <see cref="Slider.Maximum"/> properties.
        /// </summary>
        [Fact]
        public void TestMinimumMaximumValues()
        {
            var slider = new Slider();

            // test Minimum
            slider.Minimum = 0.5f;
            Assert.Equal(0.5f, slider.Minimum);
            slider.Minimum = -1f;
            Assert.Equal(-1f, slider.Minimum);
            slider.Minimum = 5f;
            Assert.Equal(5f, slider.Minimum);
            Assert.Equal(5f, slider.Maximum); // value updated to Minimum

            // Restore values
            slider.Minimum = -1.0f;
            slider.Maximum = 1.0f;

            // test Maximum
            slider.Maximum = 5f;
            Assert.Equal(5f, slider.Maximum);
            slider.Maximum = -0.5f;
            Assert.Equal(-0.5f, slider.Maximum);
            slider.Maximum = -5f;
            Assert.Equal(-1f, slider.Maximum); // value clamped to Minimum
            
            // Restore values
            slider.Minimum = -1.0f;
            slider.Maximum = 1.0f;

            // test Value
            slider.Value = -10f;
            Assert.Equal(-1.0f, slider.Value);
            slider.Value = 10;
            Assert.Equal(1.0f, slider.Value);
        }
        
        /// <summary>
        /// Test the <see cref="Slider.ValueChanged"/> event.
        /// </summary>
        [Fact]
        public void TestValueChanged()
        {
            var valueChanged = false;
            var slider = new Slider();
            slider.ValueChanged += (s, e) => valueChanged = true;

            slider.Value = 0;
            Assert.False(valueChanged);
            valueChanged = false;

            slider.Value = 1;
            Assert.True(valueChanged);
            valueChanged = false;

            slider.Value = 2;
            Assert.False(valueChanged); // because of maximum
            valueChanged = false;

            slider.Value = 0.55f;
            valueChanged = false;
            slider.SnapToClosestTick();
            Assert.True(valueChanged);
            valueChanged = false;

            slider.Value = 0.5f;
            valueChanged = false;
            slider.TickFrequency = 3f;
            slider.ShouldSnapToTicks = true;
            Assert.True(valueChanged);
            valueChanged = false;

            slider.TickFrequency = 4f;
            Assert.True(valueChanged);
            valueChanged = false;
        }

        /// <summary>
        /// Test for the <see cref="Slider.Increase"/> and <see cref="Slider.Decrease"/> functions.
        /// </summary>
        [Fact]
        public void TestIncreateDecrease()
        {
            var slider = new Slider { Value = 0.5f };

            slider.Increase();
            Assert.Equal(0.6f, slider.Value);
            slider.Decrease();
            Assert.Equal(0.5f, slider.Value);

            slider.Step = 0.01f;
            slider.Decrease();
            Assert.Equal(0.49f, slider.Value);
            slider.Increase();
            Assert.Equal(0.5f, slider.Value);

            slider.Step = 5f;
            slider.Increase();
            Assert.Equal(1f, slider.Value);
            slider.Decrease();
            Assert.Equal(0f, slider.Value);

            slider.Step = 0f;
            slider.ShouldSnapToTicks = true;
            slider.Increase();
            Assert.Equal(0.1f, slider.Value);
            slider.Value = 0.5f;
            slider.Decrease();
            Assert.Equal(0.4f, slider.Value);

            slider.Step = 0.16f;
            slider.Increase();
            Assert.Equal(0.6f, slider.Value);
        }

        /// <summary>
        /// Test the tick snapping functions.
        /// </summary>
        [Fact]
        public void TestTickSnapping()
        {
            var slider = new Slider { TickFrequency = 1 };

            slider.Value = 0.55f;
            slider.SnapToClosestTick();
            Assert.Equal(1f, slider.Value);

            slider.Value = 0.45f;
            slider.SnapToClosestTick();
            Assert.Equal(0f, slider.Value);

            slider.TickFrequency = 20;
            slider.Value = 0.44f;
            slider.ShouldSnapToTicks = true;
            Utilities.AssertAreNearlyEqual(0.45f, slider.Value);

            slider.TickFrequency = 5;
            Utilities.AssertAreNearlyEqual(0.4f, slider.Value);

            slider.Value = 0.22f;
            Utilities.AssertAreNearlyEqual(0.2f, slider.Value);

            slider.Step = 0.16f;
            slider.Increase();
            Utilities.AssertAreNearlyEqual(0.4f, slider.Value);
        }

        /// <summary>
        /// Test the <see cref="Slider.MeasureOverride"/> method.
        /// </summary>
        [Fact]
        public void TestMeasureOverride()
        {
            var slider = new Slider();
            var sprite = new Sprite { Region = new RectangleF(2, 3, 40, 50) };

            slider.Measure(new Vector3(100, 200, 300));
            Assert.Equal(new Vector3(0), slider.RenderSize);
            
            slider.TrackBackgroundImage = (SpriteFromTexture)sprite;
            slider.Measure(new Vector3(100, 200, 300));
            Assert.Equal(new Vector3(100, 50, 0), slider.DesiredSize);

            slider.Orientation = Orientation.Vertical;
            slider.Measure(new Vector3(100, 200, 300));
            Assert.Equal(new Vector3(50, 200, 0), slider.DesiredSize);

            slider.Orientation = Orientation.InDepth;
            slider.Measure(new Vector3(100, 200, 300));
            Assert.Equal(new Vector3(50, 50, 300), slider.DesiredSize); // subject to changes

            slider.Orientation = Orientation.Horizontal;
            sprite.Orientation = ImageOrientation.Rotated90;
            slider.Measure(new Vector3(100, 200, 300));
            Assert.Equal(new Vector3(100, 40, 0), slider.DesiredSize);
            
            slider.Orientation = Orientation.Vertical;
            slider.Measure(new Vector3(100, 200, 300));
            Assert.Equal(new Vector3(40, 200, 0), slider.DesiredSize);

            sprite.Orientation = ImageOrientation.AsIs;
            slider.Measure(new Vector3(100, 200, 300));
            Assert.Equal(new Vector3(50, 200, 0), slider.DesiredSize);
        }
    }
}
