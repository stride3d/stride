// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using NUnit.Framework;

using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Rendering.Sprites;
using Xenko.UI.Controls;

namespace Xenko.UI.Tests.Layering
{
    /// <summary>
    /// Unit tests for the UI <see cref="Slider"/> elements.
    /// </summary>
    public class SliderTests
    {
        /// <summary>
        /// Launch all the tests contained in <see cref="ControlTests"/>
        /// </summary>
        public void TestAll()
        {
            TestProperties();
        }

        [Test]
        public void TestProperties()
        {
            var slider = new Slider();

            // test properties default values
            Assert.AreEqual(10, slider.TickFrequency);
            Assert.AreEqual(0, slider.Minimum);
            Assert.AreEqual(1, slider.Maximum);
            Assert.AreEqual(0.1f, slider.Step);
            Assert.AreEqual(0, slider.Value);
            Assert.AreEqual(5, slider.DrawLayerNumber);
            Assert.AreEqual(false, slider.IsDirectionReversed);
            Assert.AreEqual(false, slider.AreTicksDisplayed);
            Assert.AreEqual(false, slider.ShouldSnapToTicks);
            Assert.AreEqual(true, slider.CanBeHitByUser);
            Assert.AreEqual(Orientation.Horizontal, slider.Orientation);
            Assert.AreEqual(HorizontalAlignment.Center, slider.HorizontalAlignment);
            Assert.AreEqual(VerticalAlignment.Center, slider.VerticalAlignment);
            Assert.AreEqual(DepthAlignment.Center, slider.DepthAlignment);
        }

        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Test]
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
        [Test]
        public void TestMinimumMaximumValues()
        {
            var slider = new Slider();

            // test Minimum
            Assert.DoesNotThrow(() => slider.Minimum = 0.5f);
            Assert.AreEqual(0.5f, slider.Minimum);
            Assert.DoesNotThrow(() => slider.Minimum = -1f);
            Assert.AreEqual(-1f, slider.Minimum);
            Assert.DoesNotThrow(() => slider.Minimum = 5f);
            Assert.AreEqual(5f, slider.Minimum);
            Assert.AreEqual(5f, slider.Maximum); // value updated to Minimum

            // Restore values
            slider.Minimum = -1.0f;
            slider.Maximum = 1.0f;

            // test Maximum
            Assert.DoesNotThrow(() => slider.Maximum = 5f);
            Assert.AreEqual(5f, slider.Maximum);
            Assert.DoesNotThrow(() => slider.Maximum = -0.5f);
            Assert.AreEqual(-0.5f, slider.Maximum);
            Assert.DoesNotThrow(() => slider.Maximum = -5f);
            Assert.AreEqual(-1f, slider.Maximum); // value clamped to Minimum
            
            // Restore values
            slider.Minimum = -1.0f;
            slider.Maximum = 1.0f;

            // test Value
            Assert.DoesNotThrow(() => slider.Value = -10f);
            Assert.AreEqual(-1.0f, slider.Value);
            Assert.DoesNotThrow(() => slider.Value = 10);
            Assert.AreEqual(1.0f, slider.Value);
        }
        
        /// <summary>
        /// Test the <see cref="Slider.ValueChanged"/> event.
        /// </summary>
        [Test]
        public void TestValueChanged()
        {
            var valueChanged = false;
            var slider = new Slider();
            slider.ValueChanged += (s, e) => valueChanged = true;

            slider.Value = 0;
            Assert.AreEqual(false, valueChanged);
            valueChanged = false;

            slider.Value = 1;
            Assert.AreEqual(true, valueChanged);
            valueChanged = false;

            slider.Value = 2;
            Assert.AreEqual(false, valueChanged); // because of maximum
            valueChanged = false;

            slider.Value = 0.55f;
            valueChanged = false;
            slider.SnapToClosestTick();
            Assert.AreEqual(true, valueChanged);
            valueChanged = false;

            slider.Value = 0.5f;
            valueChanged = false;
            slider.TickFrequency = 3f;
            slider.ShouldSnapToTicks = true;
            Assert.AreEqual(true, valueChanged);
            valueChanged = false;

            slider.TickFrequency = 4f;
            Assert.AreEqual(true, valueChanged);
            valueChanged = false;
        }

        /// <summary>
        /// Test for the <see cref="Slider.Increase"/> and <see cref="Slider.Decrease"/> functions.
        /// </summary>
        [Test]
        public void TestIncreateDecrease()
        {
            var slider = new Slider { Value = 0.5f };

            slider.Increase();
            Assert.AreEqual(0.6f, slider.Value);
            slider.Decrease();
            Assert.AreEqual(0.5f, slider.Value);

            slider.Step = 0.01f;
            slider.Decrease();
            Assert.AreEqual(0.49f, slider.Value);
            slider.Increase();
            Assert.AreEqual(0.5f, slider.Value);

            slider.Step = 5f;
            slider.Increase();
            Assert.AreEqual(1f, slider.Value);
            slider.Decrease();
            Assert.AreEqual(0f, slider.Value);

            slider.Step = 0f;
            slider.ShouldSnapToTicks = true;
            slider.Increase();
            Assert.AreEqual(0.1f, slider.Value);
            slider.Value = 0.5f;
            slider.Decrease();
            Assert.AreEqual(0.4f, slider.Value);

            slider.Step = 0.16f;
            slider.Increase();
            Assert.AreEqual(0.6f, slider.Value);
        }

        /// <summary>
        /// Test the tick snapping functions.
        /// </summary>
        [Test]
        public void TestTickSnapping()
        {
            var slider = new Slider { TickFrequency = 1 };

            slider.Value = 0.55f;
            slider.SnapToClosestTick();
            Assert.AreEqual(1f, slider.Value);

            slider.Value = 0.45f;
            slider.SnapToClosestTick();
            Assert.AreEqual(0f, slider.Value);

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
        [Test]
        public void TestMeasureOverride()
        {
            var slider = new Slider();
            var sprite = new Sprite { Region = new RectangleF(2, 3, 40, 50) };

            slider.Measure(new Vector3(100, 200, 300));
            Assert.AreEqual(new Vector3(0), slider.RenderSize);
            
            slider.TrackBackgroundImage = (SpriteFromTexture)sprite;
            slider.Measure(new Vector3(100, 200, 300));
            Assert.AreEqual(new Vector3(100, 50, 0), slider.DesiredSize);

            slider.Orientation = Orientation.Vertical;
            slider.Measure(new Vector3(100, 200, 300));
            Assert.AreEqual(new Vector3(50, 200, 0), slider.DesiredSize);

            slider.Orientation = Orientation.InDepth;
            slider.Measure(new Vector3(100, 200, 300));
            Assert.AreEqual(new Vector3(50, 50, 300), slider.DesiredSize); // subject to changes

            slider.Orientation = Orientation.Horizontal;
            sprite.Orientation = ImageOrientation.Rotated90;
            slider.Measure(new Vector3(100, 200, 300));
            Assert.AreEqual(new Vector3(100, 40, 0), slider.DesiredSize);
            
            slider.Orientation = Orientation.Vertical;
            slider.Measure(new Vector3(100, 200, 300));
            Assert.AreEqual(new Vector3(40, 200, 0), slider.DesiredSize);

            sprite.Orientation = ImageOrientation.AsIs;
            slider.Measure(new Vector3(100, 200, 300));
            Assert.AreEqual(new Vector3(50, 200, 0), slider.DesiredSize);
        }
    }
}
