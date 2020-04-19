// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
            Assert.Equal(float.NaN, newElement.DependencyProperties.Get(RelativeSizePropertyKey).X);
            Assert.Equal(float.NaN, newElement.DependencyProperties.Get(RelativeSizePropertyKey).Y);
            Assert.Equal(float.NaN, newElement.DependencyProperties.Get(RelativeSizePropertyKey).Z);
            Assert.Equal(Vector3.Zero, newElement.DependencyProperties.Get(RelativePositionPropertyKey));
            Assert.Equal(Vector3.Zero, newElement.DependencyProperties.Get(AbsolutePositionPropertyKey));
            Assert.Equal(Vector3.Zero, newElement.DependencyProperties.Get(PinOriginPropertyKey));

            // test pin origin validator
            newElement.DependencyProperties.Set(PinOriginPropertyKey, new Vector3(-1, -1, -1));
            Assert.Equal(Vector3.Zero, newElement.DependencyProperties.Get(PinOriginPropertyKey));
            newElement.DependencyProperties.Set(PinOriginPropertyKey, new Vector3(2, 2, 2));
            Assert.Equal(Vector3.One, newElement.DependencyProperties.Get(PinOriginPropertyKey));
            newElement.DependencyProperties.Set(PinOriginPropertyKey, new Vector3(0.5f, 0.5f, 0.5f));
            Assert.Equal(new Vector3(0.5f, 0.5f, 0.5f), newElement.DependencyProperties.Get(PinOriginPropertyKey));

            // test relative size validator
            newElement.DependencyProperties.Set(RelativeSizePropertyKey, new Vector3(0.5f, 0.5f, 0.5f));
            Assert.Equal(new Vector3(0.5f, 0.5f, 0.5f), newElement.DependencyProperties.Get(RelativeSizePropertyKey));
            newElement.DependencyProperties.Set(RelativeSizePropertyKey, new Vector3(2.5f, 3.5f, 4.5f));
            Assert.Equal(new Vector3(2.5f, 3.5f, 4.5f), newElement.DependencyProperties.Get(RelativeSizePropertyKey));
            newElement.DependencyProperties.Set(RelativeSizePropertyKey, new Vector3(-2.4f, 3.5f, 4.5f));
            Assert.Equal(new Vector3(2.4f, 3.5f, 4.5f), newElement.DependencyProperties.Get(RelativeSizePropertyKey));
            newElement.DependencyProperties.Set(RelativeSizePropertyKey, new Vector3(2.5f, -3.4f, 4.5f));
            Assert.Equal(new Vector3(2.5f, 3.4f, 4.5f), newElement.DependencyProperties.Get(RelativeSizePropertyKey));
            newElement.DependencyProperties.Set(RelativeSizePropertyKey, new Vector3(2.5f, 3.5f, -4.4f));
            Assert.Equal(new Vector3(2.5f, 3.5f, 4.4f), newElement.DependencyProperties.Get(RelativeSizePropertyKey));
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
            UIElementLayeringTests.TestMeasureInvalidation(canvas, () => child.DependencyProperties.Set(PinOriginPropertyKey, new Vector3(0.1f, 0.2f, 0.3f)));
            UIElementLayeringTests.TestMeasureInvalidation(canvas, () => child.DependencyProperties.Set(RelativePositionPropertyKey, new Vector3(1f, 2f, 3f)));
            UIElementLayeringTests.TestMeasureInvalidation(canvas, () => child.DependencyProperties.Set(AbsolutePositionPropertyKey, new Vector3(1f, 2f, 3f)));
            UIElementLayeringTests.TestMeasureInvalidation(canvas, () => child.DependencyProperties.Set(RelativeSizePropertyKey, new Vector3(1f, 2f, 3f)));
        }
        
        /// <summary>
        /// Test the function <see cref="Canvas.MeasureOverride"/>
        /// </summary>
        [Fact]
        public void TestMeasureOverrideRelative()
        {
            ResetState();

            // check that desired size is null if no children
            Measure(1000 * rand.NextVector3());
            Assert.Equal(Vector3.Zero, DesiredSize);

            var child = new MeasureValidator();
            child.DependencyProperties.Set(RelativeSizePropertyKey, new Vector3(0.2f,0.3f,0.4f));
            Children.Add(child);
            
            child.ExpectedMeasureValue = new Vector3(2,3,4);
            child.ReturnedMeasuredValue = new Vector3(4,3,2);
            Measure(10 * Vector3.One);
            // canvas size does not depend on its children
            Assert.Equal(Vector3.Zero, DesiredSize);
        }

        /// <summary>
        /// Test for the function <see cref="Canvas.ArrangeOverride"/>
        /// </summary>
        [Fact]
        public void TestArrangeOverrideRelative()
        {
            ResetState();

            DepthAlignment = DepthAlignment.Stretch;

            // test that arrange set render size to provided size when there is no children
            var providedSize = 1000 * rand.NextVector3();
            var providedSizeWithoutMargins = CalculateSizeWithoutThickness(ref providedSize, ref MarginInternal);
            Measure(providedSize);
            Arrange(providedSize, false);
            Assert.Equal(providedSizeWithoutMargins, RenderSize);

            ResetState();

            DepthAlignment = DepthAlignment.Stretch;
            
            var child = new ArrangeValidator();
            child.DependencyProperties.Set(UseAbsolutePositionPropertyKey, false);
            child.DependencyProperties.Set(RelativeSizePropertyKey, new Vector3(0.2f, 0.3f, 0.4f));
            child.DependencyProperties.Set(PinOriginPropertyKey, new Vector3(0f, 0.5f, 1f));
            child.DependencyProperties.Set(RelativePositionPropertyKey, new Vector3(0.2f, 0.4f, 0.6f));
            Children.Add(child);

            child.ReturnedMeasuredValue = 2 * new Vector3(2, 6, 12);
            child.ExpectedArrangeValue = child.ReturnedMeasuredValue;
            providedSize = new Vector3(10, 20, 30);
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
            childOne.Depth = rand.NextFloat();
            childTwo.Width = 10 * rand.NextFloat();
            childTwo.Height = 20 * rand.NextFloat();
            childTwo.Depth = 30 * rand.NextFloat();

            // add the children to the stack panel 
            Children.Add(childOne);
            Children.Add(childTwo);

            // arrange the stack panel and check children size
            Arrange(1000 * rand.NextVector3(), true);
            Assert.Equal(Vector3.Zero, childOne.RenderSize);
            Assert.Equal(Vector3.Zero, childTwo.RenderSize);
        }

        /// <summary>
        /// Test the function <see cref="Canvas.MeasureOverride"/> with absolute position
        /// </summary>
        [Fact]
        public void TestMeasureOverrideAbsolute()
        {
            ResetState();

            // check that desired size is null if no children
            Measure(1000 * rand.NextVector3());
            Assert.Equal(Vector3.Zero, DesiredSize);

            var child = new MeasureValidator();
            Children.Add(child);
            child.Margin = Thickness.UniformCuboid(10);

            // check canvas desired size and child provided size with one child out of the available zone
            var availableSize = new Vector3(100, 200, 300);
            var childDesiredSize = new Vector3(30, 80, 130);

            var pinOrigin = Vector3.Zero;
            TestOutOfBounds(child, childDesiredSize, new Vector3(float.PositiveInfinity), new Vector3(-1, 100, 150), pinOrigin, availableSize, Vector3.Zero);
        }

        private void TestOutOfBounds(MeasureValidator child, Vector3 childDesiredSize, Vector3 childExpectedValue, Vector3 pinPosition, Vector3 pinOrigin, Vector3 availableSize, Vector3 expectedSize)
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
            var nullCanvas = new Canvas { DepthAlignment = DepthAlignment.Stretch};
            var providedSize = 1000 * rand.NextVector3();
            var providedSizeWithoutMargins = CalculateSizeWithoutThickness(ref providedSize, ref MarginInternal);
            nullCanvas.Measure(providedSize);
            nullCanvas.Arrange(providedSize, false);
            Assert.Equal(providedSizeWithoutMargins, nullCanvas.RenderSize);

            // test that arrange works properly with valid children.
            var availablesizeWithMargins = new Vector3(200, 300, 500);
            var canvas = new Canvas { DepthAlignment = DepthAlignment.Stretch };
            for (int i = 0; i < 10; i++)
            {
                var child = new ArrangeValidator { Name = i.ToString() };

                child.SetCanvasPinOrigin(new Vector3(0, 0.5f, 1));
                child.SetCanvasAbsolutePosition(((i>>1)-1) * 0.5f * availablesizeWithMargins);
                child.Margin = new Thickness(10, 11, 12, 13, 14, 15);

                child.ReturnedMeasuredValue = (i%2)==0? new Vector3(1000) : availablesizeWithMargins/3f;
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
                var childOffsets = (pinPosition - Vector3.Modulate(pinOrigin, canvas.Children[i].RenderSize)) - canvas.RenderSize / 2;
                Assert.Equal(Matrix.Translation(childOffsets), canvas.Children[i].DependencyProperties.Get(PanelArrangeMatrixPropertyKey));
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
            var parentSize = new Vector3(2);
            child.SetCanvasRelativePosition(new Vector3(float.NaN));
            child.SetCanvasAbsolutePosition(new Vector3(-1.5f, 0, 1.5f));
            Assert.Equal(child.GetCanvasAbsolutePosition(), ComputeAbsolutePinPosition(child, ref parentSize));
            child.SetCanvasAbsolutePosition(new Vector3(float.NaN));
            child.SetCanvasRelativePosition(new Vector3(-1.5f, 0, 1.5f));
            Assert.Equal(2*child.GetCanvasRelativePosition(), ComputeAbsolutePinPosition(child, ref parentSize));

            // indirectly set the value
            child.SetCanvasAbsolutePosition(new Vector3(-1.5f, 0, 1.5f));
            child.SetCanvasRelativePosition(new Vector3(float.NaN));
            Assert.Equal(child.GetCanvasAbsolutePosition(), ComputeAbsolutePinPosition(child, ref parentSize));
            child.SetCanvasRelativePosition(new Vector3(-1.5f, 0, 1.5f));
            child.SetCanvasAbsolutePosition(new Vector3(float.NaN));
            Assert.Equal(2*child.GetCanvasRelativePosition(), ComputeAbsolutePinPosition(child, ref parentSize));

            // indirect/direct mix
            child.SetCanvasAbsolutePosition(new Vector3(-1.5f, float.NaN, 1.5f));
            child.SetCanvasRelativePosition(new Vector3(float.NaN, 1, float.NaN));
            Assert.Equal(new Vector3(-1.5f, 2, 1.5f), ComputeAbsolutePinPosition(child, ref parentSize));
            child.SetCanvasRelativePosition(new Vector3(-1.5f, float.NaN, 1.5f));
            child.SetCanvasAbsolutePosition(new Vector3(float.NaN, 1, float.NaN));
            Assert.Equal(new Vector3(-3f, 1, 3f), ComputeAbsolutePinPosition(child, ref parentSize));

            // infinite values
            parentSize = new Vector3(float.PositiveInfinity);
            child.SetCanvasRelativePosition(new Vector3(-1.5f, 0, 1.5f));
            Utilities.AreExactlyEqual(new Vector3(float.NegativeInfinity, 0f, float.PositiveInfinity), ComputeAbsolutePinPosition(child, ref parentSize));
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
            child1.SetCanvasRelativeSize(Vector3.Zero);
            child1.ExpectedMeasureValue = Vector3.Zero;
            canvas.Measure(new Vector3(float.PositiveInfinity));
            child1.SetCanvasRelativeSize(new Vector3(float.NaN));

            // check sizes with infinite measure values and absolute position
            child1.SetCanvasAbsolutePosition(new Vector3(1, -1, -3));
            child1.ExpectedMeasureValue = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            child1.ReturnedMeasuredValue = new Vector3(2);
            canvas.Measure(new Vector3(float.PositiveInfinity));
            // canvas size does not depend on its children
            Assert.Equal(Vector3.Zero, canvas.DesiredSizeWithMargins);

            // check sizes with infinite measure values and relative position
            child1.SetCanvasPinOrigin(new Vector3(0, .5f, 1));
            child1.SetCanvasRelativePosition(new Vector3(-1));
            child1.ExpectedMeasureValue = new Vector3(0);
            child1.ReturnedMeasuredValue = new Vector3(1);
            canvas.Measure(new Vector3(float.PositiveInfinity));
            // canvas size does not depend on its children
            Assert.Equal(Vector3.Zero, canvas.DesiredSizeWithMargins);
            child1.SetCanvasRelativePosition(new Vector3(0));
            child1.ExpectedMeasureValue = new Vector3(float.PositiveInfinity, 0, 0);
            child1.ReturnedMeasuredValue = new Vector3(1);
            canvas.Measure(new Vector3(float.PositiveInfinity));
            // canvas size does not depend on its children
            Assert.Equal(Vector3.Zero, canvas.DesiredSizeWithMargins);
            child1.SetCanvasRelativePosition(new Vector3(0.5f));
            child1.ExpectedMeasureValue = new Vector3(float.PositiveInfinity);
            child1.ReturnedMeasuredValue = new Vector3(1);
            canvas.Measure(new Vector3(float.PositiveInfinity));
            // canvas size does not depend on its children
            Assert.Equal(Vector3.Zero, canvas.DesiredSizeWithMargins);
            child1.SetCanvasRelativePosition(new Vector3(1f));
            child1.ExpectedMeasureValue = new Vector3(float.PositiveInfinity);
            child1.ReturnedMeasuredValue = new Vector3(1);
            canvas.Measure(new Vector3(float.PositiveInfinity));
            // canvas size does not depend on its children
            Assert.Equal(Vector3.Zero, canvas.DesiredSizeWithMargins);
            child1.SetCanvasRelativePosition(new Vector3(2f));
            child1.ExpectedMeasureValue = new Vector3(float.PositiveInfinity);
            child1.ReturnedMeasuredValue = new Vector3(1);
            canvas.Measure(new Vector3(float.PositiveInfinity));
            // canvas size does not depend on its children
            Assert.Equal(Vector3.Zero, canvas.DesiredSizeWithMargins);

            // check that the maximum is correctly taken
            var child2 = new MeasureValidator();
            var child3 = new MeasureValidator();
            canvas.Children.Add(child2);
            canvas.Children.Add(child3);
            child1.InvalidateMeasure();
            child1.SetCanvasPinOrigin(new Vector3(0.5f));
            child1.SetCanvasRelativePosition(new Vector3(0.5f));
            child1.ExpectedMeasureValue = new Vector3(float.PositiveInfinity);
            child1.ReturnedMeasuredValue = new Vector3(10);
            child2.SetCanvasPinOrigin(new Vector3(0.5f));
            child2.SetCanvasRelativePosition(new Vector3(-.1f, .5f, 1.2f));
            child2.ExpectedMeasureValue = new Vector3(float.PositiveInfinity);
            child2.ReturnedMeasuredValue = new Vector3(30.8f, 5, 48);
            child3.SetCanvasRelativeSize(new Vector3(0f, 1f, 2f));
            child3.ExpectedMeasureValue = new Vector3(0, float.PositiveInfinity, float.PositiveInfinity);
            child3.ReturnedMeasuredValue = new Vector3(0, 5, 50);
            canvas.Measure(new Vector3(float.PositiveInfinity));
            // canvas size does not depend on its children
            Assert.Equal(Vector3.Zero, canvas.DesiredSizeWithMargins);
        }
    }
}
