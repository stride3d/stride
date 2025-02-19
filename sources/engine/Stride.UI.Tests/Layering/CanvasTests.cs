// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xunit;

using Stride.Core.Mathematics;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.UI.Tests.Layering
{
    /// <summary>
    /// Series of tests for <see cref="Canvas"/>
    /// </summary>
    public class CanvasTests : Canvas
    {
        private Random rand = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// launch all the tests of <see cref="CanvasTests"/>
        /// </summary>
        internal void TestAll()
        {
            TestProperties();
            TestCollapseOverride();
            TestBasicInvalidations();
            TestMeasureOverrideRelative();
            TestArrangeOverrideRelative();
            TestMeasureOverrideAbsolute();
            TestArrangeOverrideAbsolute();
        }

        private void ResetState()
        {
            DependencyProperties.Clear();
            Children.Clear();
            InvalidateArrange();
            InvalidateMeasure();
        }

        /// <summary>
        /// Test the <see cref="Canvas"/> properties
        /// </summary>
        [Fact]
        public void TestProperties()
        {
            var newElement = new Canvas();

            // test default values
            Assert.Equal(float.NaN, newElement.DependencyProperties.Get(RelativeSizePropertyKey).Width);
            Assert.Equal(float.NaN, newElement.DependencyProperties.Get(RelativeSizePropertyKey).Height);
            Assert.Equal(Vector2.Zero, newElement.DependencyProperties.Get(RelativePositionPropertyKey));
            Assert.Equal(Vector2.Zero, newElement.DependencyProperties.Get(AbsolutePositionPropertyKey));
            Assert.Equal(Vector2.Zero, newElement.DependencyProperties.Get(PinOriginPropertyKey));

            // test pin origin validator
            newElement.DependencyProperties.Set(PinOriginPropertyKey, new Vector2(-1, -1));
            Assert.Equal(Vector2.Zero, newElement.DependencyProperties.Get(PinOriginPropertyKey));
            newElement.DependencyProperties.Set(PinOriginPropertyKey, new Vector2(2, 2));
            Assert.Equal(Vector2.One, newElement.DependencyProperties.Get(PinOriginPropertyKey));
            newElement.DependencyProperties.Set(PinOriginPropertyKey, new Vector2(0.5f, 0.5f));
            Assert.Equal(new Vector2(0.5f, 0.5f), newElement.DependencyProperties.Get(PinOriginPropertyKey));

            // test relative size validator
            newElement.DependencyProperties.Set(RelativeSizePropertyKey, new Size2F(0.5f, 0.5f));
            Assert.Equal(new Size2F(0.5f, 0.5f), newElement.DependencyProperties.Get(RelativeSizePropertyKey));
            newElement.DependencyProperties.Set(RelativeSizePropertyKey, new Size2F(2.5f, 3.5f));
            Assert.Equal(new Size2F(2.5f, 3.5f), newElement.DependencyProperties.Get(RelativeSizePropertyKey));
            newElement.DependencyProperties.Set(RelativeSizePropertyKey, new Size2F(-2.4f, 3.5f));
            Assert.Equal(new Size2F(2.4f, 3.5f), newElement.DependencyProperties.Get(RelativeSizePropertyKey));
            newElement.DependencyProperties.Set(RelativeSizePropertyKey, new Size2F(2.5f, -3.4f));
            Assert.Equal(new Size2F(2.5f, 3.4f), newElement.DependencyProperties.Get(RelativeSizePropertyKey));
            newElement.DependencyProperties.Set(RelativeSizePropertyKey, new Size2F(2.5f, 3.5f));
            Assert.Equal(new Size2F(2.5f, 3.5f), newElement.DependencyProperties.Get(RelativeSizePropertyKey));
        }

        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Fact]
        public void TestBasicInvalidations()
        {
            var canvas = new Canvas();
            var child = new Canvas();
            canvas.Children.Add(child);

            // - test the properties that are supposed to invalidate the object measurement
            UIElementLayeringTests.TestMeasureInvalidation(canvas, () => child.DependencyProperties.Set(PinOriginPropertyKey, new Vector2(0.1f, 0.2f)));
            UIElementLayeringTests.TestMeasureInvalidation(canvas, () => child.DependencyProperties.Set(RelativePositionPropertyKey, new Vector2(1f, 2f)));
            UIElementLayeringTests.TestMeasureInvalidation(canvas, () => child.DependencyProperties.Set(AbsolutePositionPropertyKey, new Vector2(1f, 2f)));
            UIElementLayeringTests.TestMeasureInvalidation(canvas, () => child.DependencyProperties.Set(RelativeSizePropertyKey, new Size2F(1f, 2f)));
        }
        
        /// <summary>
        /// Test the function <see cref="Canvas.MeasureOverride"/>
        /// </summary>
        [Fact]
        public void TestMeasureOverrideRelative()
        {
            ResetState();

            // check that desired size is null if no children
            Measure((Size2F)rand.NextVector2() * 1000);
            Assert.Equal(Size2F.Zero, DesiredSize);

            var child = new MeasureValidator();
            child.DependencyProperties.Set(RelativeSizePropertyKey, new Size2F(0.2f,0.3f));
            Children.Add(child);
            
            child.ExpectedMeasureValue = new Size2F(2,3);
            child.ReturnedMeasuredValue = new Size2F(4,3);
            Measure(new Size2F(10));
            // canvas size does not depend on its children
            Assert.Equal(Size2F.Zero, DesiredSize);
        }

        /// <summary>
        /// Test for the function <see cref="Canvas.ArrangeOverride"/>
        /// </summary>
        [Fact]
        public void TestArrangeOverrideRelative()
        {
            ResetState();
            
            // test that arrange set render size to provided size when there is no children
            var providedSize = (Size2F)rand.NextVector2() * 1000;
            var providedSizeWithoutMargins = providedSize - MarginInternal;
            Measure(providedSize);
            Arrange(providedSize, false);
            Assert.Equal(providedSizeWithoutMargins, RenderSize);

            ResetState();
            
            
            var child = new ArrangeValidator();
            child.DependencyProperties.Set(UseAbsolutePositionPropertyKey, false);
            child.DependencyProperties.Set(RelativeSizePropertyKey, new Size2F(0.2f, 0.3f));
            child.DependencyProperties.Set(PinOriginPropertyKey, new Vector2(0f, 0.5f));
            child.DependencyProperties.Set(RelativePositionPropertyKey, new Vector2(0.2f, 0.4f));
            Children.Add(child);

            child.ReturnedMeasuredValue = new Size2F(2, 6) * 2;
            child.ExpectedArrangeValue = child.ReturnedMeasuredValue;
            providedSize = new Size2F(10, 20);
            Measure(providedSize);
            Arrange(providedSize, false);
            Assert.Equal(Matrix.Translation(2f-5f,8f-6f-10f,18f-24f-15f), child.DependencyProperties.Get(PanelArrangeMatrixPropertyKey));
        }
        
        /// <summary>
        /// Test <see cref="Canvas.CollapseOverride"/>
        /// </summary>
        [Fact]
        public void TestCollapseOverride()
        {
            ResetState();

            // create two children
            var childOne = new StackPanelTests();
            var childTwo = new StackPanelTests();

            // set fixed size to the children
            childOne.Width = rand.NextFloat();
            childOne.Height = rand.NextFloat();
            childTwo.Width = 10 * rand.NextFloat();
            childTwo.Height = 20 * rand.NextFloat();

            // add the children to the stack panel 
            Children.Add(childOne);
            Children.Add(childTwo);

            // arrange the stack panel and check children size
            Arrange(rand.NextSize2F() * 1000, true);
            Assert.Equal(Size2F.Zero, childOne.RenderSize);
            Assert.Equal(Size2F.Zero, childTwo.RenderSize);
        }

        /// <summary>
        /// Test the function <see cref="Canvas.MeasureOverride"/> with absolute position
        /// </summary>
        [Fact]
        public void TestMeasureOverrideAbsolute()
        {
            ResetState();

            // check that desired size is null if no children
            Measure(rand.NextSize2F() * 1000);
            Assert.Equal(Size2F.Zero, DesiredSize);

            var child = new MeasureValidator();
            Children.Add(child);
            child.Margin = Thickness.Uniform(10);

            // check canvas desired size and child provided size with one child out of the available zone
            var availableSize = new Size2F(100, 200);
            var childDesiredSize = new Size2F(30, 80);

            var pinOrigin = Vector2.Zero;
            TestOutOfBounds(child, childDesiredSize, new Size2F(float.PositiveInfinity), new Vector2(-1, 100), pinOrigin, availableSize, Size2F.Zero);
        }

        private void TestOutOfBounds(MeasureValidator child, Size2F childDesiredSize, Size2F childExpectedValue, Vector2 pinPosition, Vector2 pinOrigin, Size2F availableSize, Size2F expectedSize)
        {
            child.ExpectedMeasureValue = childExpectedValue;
            child.ReturnedMeasuredValue = childDesiredSize;
            child.DependencyProperties.Set(AbsolutePositionPropertyKey, pinPosition);
            child.DependencyProperties.Set(PinOriginPropertyKey, pinOrigin);
            Measure(availableSize);
            Assert.Equal(expectedSize, DesiredSize);
        }

        /// <summary>
        /// Test for the function <see cref="Canvas.ArrangeOverride"/> with absolute position
        /// </summary>
        [Fact]
        public void TestArrangeOverrideAbsolute()
        {
            // test that arrange set render size to provided size when there is no children
            var nullCanvas = new Canvas();
            var providedSize = rand.NextSize2F() * 1000;
            var providedSizeWithoutMargins = providedSize - MarginInternal;
            nullCanvas.Measure(providedSize);
            nullCanvas.Arrange(providedSize, false);
            Assert.Equal(providedSizeWithoutMargins, nullCanvas.RenderSize);

            // test that arrange works properly with valid children.
            var availablesizeWithMargins = new Size2F(200, 300);
            var canvas = new Canvas();
            for (int i = 0; i < 10; i++)
            {
                var child = new ArrangeValidator { Name = i.ToString() };

                child.SetCanvasPinOrigin(new Vector2(0, 0.5f));
                child.SetCanvasAbsolutePosition((Vector2)(availablesizeWithMargins * ((i >> 1) - 1) * 0.5f));
                child.Margin = new Thickness(10, 11, 13, 14);

                child.ReturnedMeasuredValue = (i%2)==0? new Size2F(1000) : availablesizeWithMargins/3f;
                child.ExpectedArrangeValue = child.ReturnedMeasuredValue;

                canvas.Children.Add(child);
            }

            // Measure the stack
            canvas.Measure(availablesizeWithMargins);
            canvas.Arrange(availablesizeWithMargins, false);

            // checks the stack arranged size
            Assert.Equal(availablesizeWithMargins, canvas.RenderSize);

            // Checks the children arrange matrix
            for (int i = 0; i < canvas.Children.Count; i++)
            {
                var pinPosition = canvas.Children[i].DependencyProperties.Get(AbsolutePositionPropertyKey);
                var pinOrigin = canvas.Children[i].DependencyProperties.Get(PinOriginPropertyKey);
                var childOffsets = (pinPosition - Vector2.Modulate(pinOrigin, (Vector2)canvas.Children[i].RenderSize)) - (Vector2)canvas.RenderSize / 2;
                Assert.Equal(Matrix.Translation(new Vector3(childOffsets.X, childOffsets.Y, 0)), canvas.Children[i].DependencyProperties.Get(PanelArrangeMatrixPropertyKey));
            }
        }

        /// <summary>
        /// Test the function <see cref="Canvas.ComputeAbsolutePinPosition"/>
        /// </summary>
        [Fact]
        public void TestComputeAbsolutePinPosition()
        {
            var child = new Button();

            // directly set the values
            var parentSize = new Size2F(2);
            child.SetCanvasRelativePosition(new Vector2(float.NaN));
            child.SetCanvasAbsolutePosition(new Vector2(-1.5f, 0));
            Assert.Equal(child.GetCanvasAbsolutePosition(), ComputeAbsolutePinPosition(child, ref parentSize));
            child.SetCanvasAbsolutePosition(new Vector2(float.NaN));
            child.SetCanvasRelativePosition(new Vector2(-1.5f, 0));
            Assert.Equal(2*child.GetCanvasRelativePosition(), ComputeAbsolutePinPosition(child, ref parentSize));

            // indirectly set the value
            child.SetCanvasAbsolutePosition(new Vector2(-1.5f, 0));
            child.SetCanvasRelativePosition(new Vector2(float.NaN));
            Assert.Equal(child.GetCanvasAbsolutePosition(), ComputeAbsolutePinPosition(child, ref parentSize));
            child.SetCanvasRelativePosition(new Vector2(-1.5f, 0));
            child.SetCanvasAbsolutePosition(new Vector2(float.NaN));
            Assert.Equal(2*child.GetCanvasRelativePosition(), ComputeAbsolutePinPosition(child, ref parentSize));

            // indirect/direct mix
            child.SetCanvasAbsolutePosition(new Vector2(-1.5f, float.NaN));
            child.SetCanvasRelativePosition(new Vector2(float.NaN, 1));
            Assert.Equal(new Vector2(-1.5f, 2), ComputeAbsolutePinPosition(child, ref parentSize));
            child.SetCanvasRelativePosition(new Vector2(-1.5f, float.NaN));
            child.SetCanvasAbsolutePosition(new Vector2(float.NaN, 1));
            Assert.Equal(new Vector2(-3f, 1), ComputeAbsolutePinPosition(child, ref parentSize));

            // infinite values
            parentSize = new Size2F(float.PositiveInfinity);
            child.SetCanvasRelativePosition(new Vector2(-1.5f, 0));
            Utilities.AreExactlyEqual(new Vector2(float.NegativeInfinity, 0f), ComputeAbsolutePinPosition(child, ref parentSize));
        }

        /// <summary>
        /// Test the function <see cref="Canvas.MeasureOverride"/> when provided size is infinite
        /// </summary>
        [Fact]
        public void TestMeasureOverrideInfinite()
        {
            var child1 = new MeasureValidator();
            var canvas = new Canvas { Children = { child1 } };

            // check that relative 0 x inf available = 0 
            child1.SetCanvasRelativeSize(Size2F.Zero);
            child1.ExpectedMeasureValue = Size2F.Zero;
            canvas.Measure(new Size2F(float.PositiveInfinity));
            child1.SetCanvasRelativeSize(new Size2F(float.NaN));

            // check sizes with infinite measure values and absolute position
            child1.SetCanvasAbsolutePosition(new Vector2(1, -1));
            child1.ExpectedMeasureValue = new Size2F(float.PositiveInfinity, float.PositiveInfinity);
            child1.ReturnedMeasuredValue = new Size2F(2);
            canvas.Measure(new Size2F(float.PositiveInfinity));
            // canvas size does not depend on its children
            Assert.Equal(Size2F.Zero, canvas.DesiredSizeWithMargins);

            // check sizes with infinite measure values and relative position
            child1.SetCanvasPinOrigin(new Vector2(0, .5f));
            child1.SetCanvasRelativePosition(new Vector2(-1));
            child1.ExpectedMeasureValue = new Size2F(0);
            child1.ReturnedMeasuredValue = new Size2F(1);
            canvas.Measure(new Size2F(float.PositiveInfinity));
            // canvas size does not depend on its children
            Assert.Equal(Size2F.Zero, canvas.DesiredSizeWithMargins);
            child1.SetCanvasRelativePosition(new Vector2(0));
            child1.ExpectedMeasureValue = new Size2F(float.PositiveInfinity, 0);
            child1.ReturnedMeasuredValue = new Size2F(1);
            canvas.Measure(new Size2F(float.PositiveInfinity));
            // canvas size does not depend on its children
            Assert.Equal(Size2F.Zero, canvas.DesiredSizeWithMargins);
            child1.SetCanvasRelativePosition(new Vector2(0.5f));
            child1.ExpectedMeasureValue = new Size2F(float.PositiveInfinity);
            child1.ReturnedMeasuredValue = new Size2F(1);
            canvas.Measure(new Size2F(float.PositiveInfinity));
            // canvas size does not depend on its children
            Assert.Equal(Size2F.Zero, canvas.DesiredSizeWithMargins);
            child1.SetCanvasRelativePosition(new Vector2(1f));
            child1.ExpectedMeasureValue = new Size2F(float.PositiveInfinity);
            child1.ReturnedMeasuredValue = new Size2F(1);
            canvas.Measure(new Size2F(float.PositiveInfinity));
            // canvas size does not depend on its children
            Assert.Equal(Size2F.Zero, canvas.DesiredSizeWithMargins);
            child1.SetCanvasRelativePosition(new Vector2(2f));
            child1.ExpectedMeasureValue = new Size2F(float.PositiveInfinity);
            child1.ReturnedMeasuredValue = new Size2F(1);
            canvas.Measure(new Size2F(float.PositiveInfinity));
            // canvas size does not depend on its children
            Assert.Equal(Size2F.Zero, canvas.DesiredSizeWithMargins);

            // check that the maximum is correctly taken
            var child2 = new MeasureValidator();
            var child3 = new MeasureValidator();
            canvas.Children.Add(child2);
            canvas.Children.Add(child3);
            child1.InvalidateMeasure();
            child1.SetCanvasPinOrigin(new Vector2(0.5f));
            child1.SetCanvasRelativePosition(new Vector2(0.5f));
            child1.ExpectedMeasureValue = new Size2F(float.PositiveInfinity);
            child1.ReturnedMeasuredValue = new Size2F(10);
            child2.SetCanvasPinOrigin(new Vector2(0.5f));
            child2.SetCanvasRelativePosition(new Vector2(-.1f, .5f));
            child2.ExpectedMeasureValue = new Size2F(float.PositiveInfinity);
            child2.ReturnedMeasuredValue = new Size2F(30.8f, 5);
            child3.SetCanvasRelativeSize(new Size2F(0f, 1f));
            child3.ExpectedMeasureValue = new Size2F(0, float.PositiveInfinity);
            child3.ReturnedMeasuredValue = new Size2F(0, 5);
            canvas.Measure(new Size2F(float.PositiveInfinity));
            // canvas size does not depend on its children
            Assert.Equal(Size2F.Zero, canvas.DesiredSizeWithMargins);
        }
    }
}
