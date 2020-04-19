// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Xunit;

using Stride.Core.Mathematics;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.UI.Tests.Layering
{
    /// <summary>
    /// A class that contains test functions for layering of the <see cref="UIElement"/> class.
    /// </summary>
    [System.ComponentModel.Description("Tests for UIElement layering")]
    public class UIElementLayeringTests : UIElement
    {
        protected override IEnumerable<IUIElementChildren> EnumerateChildren()
        {
            throw new NotImplementedException();
        }

        private Random rand = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// Performs all the tests
        /// </summary>
        internal void TestAll()
        {
            TestCalculateAdjustmentOffsets();
            TestCalculateAvailableSizeWithoutThickness();
            TestDependencyProperties();
            TestUpdateWorldMatrix();
            TestMeasureCollapsed();
            TestMeasureNotCollapsed();
            TestArrangeCollapsed();
            TestArrangeNotCollapsed();
        }

        private void ResetElementState()
        {
            Margin = Thickness.UniformCuboid(0f);
            Visibility = Visibility.Visible;
            Opacity = 1.0f;
            IsEnabled = true;
            ClipToBounds = false;
            Width = float.NaN;
            Height = float.NaN;
            Depth = float.NaN;
            MaximumWidth = float.PositiveInfinity;
            MaximumHeight = float.PositiveInfinity;
            MaximumDepth = float.PositiveInfinity;
            MinimumWidth = 0.0f;
            MinimumHeight = 0.0f;
            MinimumDepth = 0.0f;
            LocalMatrix = Matrix.Identity;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            DepthAlignment = DepthAlignment.Back;
            DependencyProperties.Clear();
            onMeasureOverride = null;
            onArrageOverride = null;
            Arrange(Vector3.Zero, false);
            InvalidateArrange();
            InvalidateMeasure();
        }

        /// <summary>
        /// Test the <see cref="UIElement.UpdateWorldMatrix"/> function.
        /// </summary>
        [Fact]
        public void TestUpdateWorldMatrix()
        {
            ResetElementState();

            DepthAlignment = DepthAlignment.Stretch;
            Arrange(Vector3.Zero, false);

            var identity = Matrix.Identity;
            Matrix matrix;

            // test that Identity is return when there are no transformation
            UpdateWorldMatrix(ref identity, true);
            Assert.Equal(Matrix.Identity, WorldMatrix);

            // test that parent matrix is return when there no internal transformations
            var parentWorld = Matrix.LookAtLH(new Vector3(1, 2, 3), Vector3.Zero, new Vector3(0, 0, 1));
            UpdateWorldMatrix(ref parentWorld, true);
            Assert.Equal(parentWorld, WorldMatrix);

            // test that child local matrix is returned parent matrix is identity and no layering has been done
            var localMatrix = Matrix.LookAtLH(new Vector3(3, 2, 1), Vector3.Zero, new Vector3(0, 0, 1));
            LocalMatrix = localMatrix;
            UpdateWorldMatrix(ref identity, true);
            Assert.Equal(localMatrix, WorldMatrix);

            // check that the composition of the parent world matrix and child local matrix is correct
            LocalMatrix = Matrix.Translation(new Vector3(1, 2, 3));
            matrix = Matrix.Scaling(0.5f, 0.5f, 0.5f);
            UpdateWorldMatrix(ref matrix, true);
            Assert.Equal(new Matrix(0.5f, 0, 0, 0, 0, 0.5f, 0, 0, 0, 0, 0.5f, 0, 0.5f, 1f, 1.5f, 1), WorldMatrix);

            // check that the margin and half render size offsets are properly included in the world transformation
            LocalMatrix = Matrix.Identity;
            Margin = new Thickness(1,2,3,4,5,6);
            Arrange(new Vector3(15,27,39), false);
            UpdateWorldMatrix(ref identity, true);
            Assert.Equal(Matrix.Translation(1+5,2+10,6+15), WorldMatrix);

            // check that the result of the composition between margin and parent world matrix is correct
            matrix = Matrix.Scaling(0.5f, 0.5f, 0.5f);
            UpdateWorldMatrix(ref matrix, true);
            Assert.Equal(new Matrix(0.5f, 0, 0, 0, 0, 0.5f, 0, 0, 0, 0, 0.5f, 0, 3f, 6f, 10.5f, 1), WorldMatrix);

            // check that the composition of the margins, local matrix and parent matrix is correct.
            LocalMatrix = new Matrix(0,-1,0,0, 1,0,0,0, 0,0,1,0, 0,0,0,1);
            matrix = Matrix.Scaling(0.1f, 0.2f, 0.4f);
            UpdateWorldMatrix(ref matrix, true);
            Assert.Equal(new Matrix(0,-0.2f,0,0, 0.1f,0,0,0, 0,0,0.4f,0, 0.6f,2.4f,8.4f,1), WorldMatrix);
        }

        /// <summary>
        /// Test for <see cref="UIElement.DependencyProperties"/>
        /// </summary>
        [Fact]
        public void TestDependencyProperties()
        {
            ResetElementState();

            var newElement = new UIElementLayeringTests();

            // check dependency property default values
            Assert.True(newElement.ForceNextMeasure);
            Assert.True(newElement.ForceNextArrange);
            Assert.True(newElement.IsEnabled);
            Assert.Equal(1f, newElement.Opacity);
            Assert.Equal(Visibility.Visible, newElement.Visibility);
            Assert.Equal(0f, newElement.DefaultWidth);
            Assert.Equal(0f, newElement.DefaultHeight);
            Assert.Equal(0f, newElement.DefaultDepth);
            Assert.Equal(float.NaN, newElement.Height);
            Assert.Equal(float.NaN, newElement.Width);
            Assert.Equal(float.NaN, newElement.Depth);
            Assert.Equal(0f, newElement.MinimumHeight);
            Assert.Equal(0f, newElement.MinimumWidth);
            Assert.Equal(0f, newElement.MinimumDepth);
            Assert.Equal(float.PositiveInfinity, newElement.MaximumHeight);
            Assert.Equal(float.PositiveInfinity, newElement.MaximumWidth);
            Assert.Equal(float.PositiveInfinity, newElement.MaximumDepth);
            Assert.Equal(HorizontalAlignment.Stretch, newElement.HorizontalAlignment);
            Assert.Equal(VerticalAlignment.Stretch, newElement.VerticalAlignment);
            Assert.Equal(DepthAlignment.Center, newElement.DepthAlignment);
            Assert.Null(newElement.Name);
            Assert.Equal(Thickness.UniformCuboid(0), newElement.Margin);
            Assert.Equal(Matrix.Identity, newElement.LocalMatrix);

            /////////////////////////////////////////
            // check dependency property validators

            // opacity validator
            Opacity = -1;
            Assert.Equal(0f, Opacity);
            Opacity = 2;
            Assert.Equal(1f, Opacity);
            Opacity = 0.5f;
            Assert.Equal(0.5f, Opacity);

            // default sizes (values should remain in range [0, float.MaxValue])
            DefaultWidth = -1f;
            Assert.Equal(0f, DefaultWidth);
            DefaultWidth = float.NaN;
            Assert.Equal(0f, DefaultWidth); // previous value unchanged
            DefaultWidth = float.PositiveInfinity;
            Assert.Equal(float.MaxValue, DefaultWidth);

            DefaultHeight = -1f;
            Assert.Equal(0f, DefaultHeight);
            DefaultHeight = float.NaN;
            Assert.Equal(0f, DefaultHeight); // previous value unchanged
            DefaultHeight = float.PositiveInfinity;
            Assert.Equal(float.MaxValue, DefaultHeight);

            DefaultDepth = -1f;
            Assert.Equal(0f, DefaultDepth);
            DefaultDepth = float.NaN;
            Assert.Equal(0f, DefaultDepth); // previous value unchanged
            DefaultDepth = float.PositiveInfinity;
            Assert.Equal(float.MaxValue, DefaultDepth);

            // sizes (values should remain in range [0, float.MaxValue])
            Width = -1f;
            Assert.Equal(0f, Width);
            Width = float.PositiveInfinity;
            Assert.Equal(float.MaxValue, Width);

            Height = -1f;
            Assert.Equal(0f, Height);
            Height = float.PositiveInfinity;
            Assert.Equal(float.MaxValue, Height);

            Depth = -1f;
            Assert.Equal(0f, Depth);
            Depth = float.PositiveInfinity;
            Assert.Equal(float.MaxValue, Depth);

            // minimum sizes (values should remain in range [0, float.MaxValue])
            MinimumWidth = -1f;
            Assert.Equal(0f, MinimumWidth);
            MinimumWidth = float.NaN;
            Assert.Equal(0f, MinimumWidth); // previous value unchanged
            MinimumWidth = float.PositiveInfinity;
            Assert.Equal(float.MaxValue, MinimumWidth);

            MinimumHeight = -1f;
            Assert.Equal(0f, MinimumHeight);
            MinimumHeight = float.NaN;
            Assert.Equal(0f, MinimumHeight); // previous value unchanged
            MinimumHeight = float.PositiveInfinity;
            Assert.Equal(float.MaxValue, MinimumHeight);

            MinimumDepth = -1f;
            Assert.Equal(0f, MinimumDepth);
            MinimumDepth = float.NaN;
            Assert.Equal(0f, MinimumDepth); // previous value unchanged
            MinimumDepth = float.PositiveInfinity;
            Assert.Equal(float.MaxValue, MinimumDepth);

            // maximum sizes (values should remain in range [0, float.PositiveInfinity])
            MaximumWidth = -1f;
            Assert.Equal(0f, MaximumWidth);
            MaximumWidth = float.NaN;
            Assert.Equal(0f, MaximumWidth); // previous value unchanged

            MaximumHeight = -1f;
            Assert.Equal(0f, MaximumHeight);
            MaximumHeight = float.NaN;
            Assert.Equal(0f, MaximumHeight); // previous value unchanged

            MaximumDepth = -1f;
            Assert.Equal(0f, MaximumDepth);
            MaximumDepth = float.NaN;
            Assert.Equal(0f, MaximumDepth); // previous value unchanged
        }

        /// <summary>
        /// Test for <see cref="UIElement.CalculateSizeWithoutThickness"/>
        /// </summary>
        [Fact]
        public void TestCalculateAvailableSizeWithoutThickness()
        {
            ResetElementState();

            // testing that a null thickness return a good value
            var size = 1000 * rand.NextVector3();
            var emptyThickness = Thickness.UniformCuboid(0f);
            AssertAreNearlySame(size, CalculateSizeWithoutThickness(ref size, ref emptyThickness));

            // testing with a positive thickness
            size = 1000 * Vector3.One;
            var thickness = rand.NextThickness(100, 200, 300, 400, 500, 600);
            var expectedSize = new Vector3(size.X - thickness.Left - thickness.Right, size.Y - thickness.Top - thickness.Bottom, size.Z - thickness.Back - thickness.Front);
            AssertAreNearlySame(expectedSize, CalculateSizeWithoutThickness(ref size, ref thickness));

            // testing with a negative thickness 
            size = 1000 * Vector3.One;
            thickness = -rand.NextThickness(100, 200, 300, 400, 500, 600);
            expectedSize = new Vector3(size.X - thickness.Left - thickness.Right, size.Y - thickness.Top - thickness.Bottom, size.Z - thickness.Back - thickness.Front);
            AssertAreNearlySame(expectedSize, CalculateSizeWithoutThickness(ref size, ref thickness));

            // test with a over constrained thickness
            size = 100 * rand.NextVector3();
            thickness = new Thickness(100, 200, 300, 400, 500, 600);
            AssertAreNearlySame(Vector3.Zero, CalculateSizeWithoutThickness(ref size, ref thickness));
        }

        /// <summary>
        /// Test for <see cref="UIElement.CalculateAdjustmentOffsets"/>.
        /// </summary>
        [Fact]
        public void TestCalculateAdjustmentOffsets()
        {
            ResetElementState();

            // test that  left, top, back value are returned if aligned to beginning
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
            DepthAlignment = DepthAlignment.Front;
            Margin = rand.NextThickness(10, 20, 30, 40, 50, 60);
            var expectedOffsets = new Vector3(Margin.Left, Margin.Top, Margin.Front);
            var randV1 = rand.NextVector3();
            var randV2 = rand.NextVector3();
            AssertAreNearlySame(expectedOffsets, CalculateAdjustmentOffsets(ref MarginInternal, ref randV1, ref randV2));

            // test that element is correctly centered 
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Center;
            DepthAlignment = DepthAlignment.Center;
            Margin = rand.NextThickness(10, 20, 30, 40, 50, 60);
            var givenSpace = 100 * rand.NextVector3();
            var usedSpace = 100 * rand.NextVector3();
            var usedSpaceWithMargins = CalculateSizeWithThickness(ref usedSpace, ref MarginInternal);
            expectedOffsets = new Vector3(Margin.Left, Margin.Top, Margin.Front) + (givenSpace - usedSpaceWithMargins) / 2;
            AssertAreNearlySame(expectedOffsets, CalculateAdjustmentOffsets(ref MarginInternal, ref givenSpace, ref usedSpace));

            // test that stretched is equivalent to centered
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            DepthAlignment = DepthAlignment.Stretch;
            AssertAreNearlySame(expectedOffsets, CalculateAdjustmentOffsets(ref MarginInternal, ref givenSpace, ref usedSpace));

            // test that the element is correctly right aligned
            HorizontalAlignment = HorizontalAlignment.Right;
            VerticalAlignment = VerticalAlignment.Bottom;
            DepthAlignment = DepthAlignment.Back;
            Margin = rand.NextThickness(10, 20, 30, 40, 50, 60);
            givenSpace = 100 * rand.NextVector3();
            usedSpace = 100 * rand.NextVector3();
            usedSpaceWithMargins = CalculateSizeWithThickness(ref usedSpace, ref MarginInternal);
            expectedOffsets = new Vector3(Margin.Left, Margin.Top, Margin.Front) + givenSpace - usedSpaceWithMargins;
            AssertAreNearlySame(expectedOffsets, CalculateAdjustmentOffsets(ref MarginInternal, ref givenSpace, ref usedSpace));
        }

        private void AssertAreNearlySame(Vector3 v1, Vector3 v2)
        {
            Assert.True((v1 - v2).Length() <= v1.Length() * MathUtil.ZeroTolerance);
        }

        delegate Vector3 MeasureOverrideDelegate(Vector3 availableSizeWithoutMargins);

        private MeasureOverrideDelegate onMeasureOverride;

        protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            return onMeasureOverride != null ? onMeasureOverride(availableSizeWithoutMargins) : base.MeasureOverride(availableSizeWithoutMargins);
        }

        /// <summary>
        /// Test <see cref="UIElement.Measure"/> when the element is collapsed
        /// </summary>
        [Fact]
        public void TestMeasureCollapsed()
        {
            // reset state of the element and set it to collapsed
            ResetElementState();
            Visibility = Visibility.Collapsed;

            // Test that DesiredSize and DesiredSizeWithMargin are null by default
            MeasuredWithRandomSizeAndCheckThatDesiredSizesAreNull();
            
            // Test that DesiredSize and DesiredSizeWithMargin are null with non null margins
            Margin = rand.NextThickness(10, 20, 30, 40, 50, 60);
            MeasuredWithRandomSizeAndCheckThatDesiredSizesAreNull();
            
            // set minimum size and test again
            MinimumWidth = 1000*rand.NextFloat();
            MinimumHeight = 1000*rand.NextFloat();
            MinimumDepth = 1000 * rand.NextFloat(); 
            MeasuredWithRandomSizeAndCheckThatDesiredSizesAreNull();

            // set maximum size and test again
            MaximumWidth = 1000 * rand.NextFloat();
            MaximumHeight = 1000 * rand.NextFloat();
            MaximumDepth = 1000 * rand.NextFloat();
            MeasuredWithRandomSizeAndCheckThatDesiredSizesAreNull();

            // set fixed size and test again
            Width = 1000 * rand.NextFloat();
            Height = 1000 * rand.NextFloat();
            Depth = 1000 * rand.NextFloat();
            MeasuredWithRandomSizeAndCheckThatDesiredSizesAreNull();

            // set default size and test again
            DefaultWidth = 1000 * rand.NextFloat();
            DefaultHeight = 1000 * rand.NextFloat();
            DefaultDepth = 1000 * rand.NextFloat();
            MeasuredWithRandomSizeAndCheckThatDesiredSizesAreNull();

            // set the MeasureOverred function and try again
            onMeasureOverride += size => new Vector3(1, 2, 3);
            MeasuredWithRandomSizeAndCheckThatDesiredSizesAreNull();
        }

        private void MeasuredWithRandomSizeAndCheckThatDesiredSizesAreNull()
        {
            Measure(10 * Vector3.One + 1000 * rand.NextVector3());
            Assert.Equal(Vector3.Zero, DesiredSize);
            Assert.Equal(Vector3.Zero, DesiredSizeWithMargins);
            Assert.True(IsMeasureValid);
        }

        /// <summary>
        /// Test for <see cref="UIElement.Measure"/>
        /// </summary>
        [Fact]
        public void TestMeasureNotCollapsed()
        {
            TestMeasureNotCollapsedWithMinAndMax(Vector3.Zero, new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity));
            TestMeasureNotCollapsedWithMinAndMax(new Vector3(1000,2000,3000), new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity));
            TestMeasureNotCollapsedWithMinAndMax(Vector3.Zero, new Vector3(1, 2, 3));
        }

        private void TestMeasureNotCollapsedWithMinAndMax(Vector3 min, Vector3 max)
        {
            // reset state of the element
            ResetElementState();

            DepthAlignment = DepthAlignment.Stretch;

            // set the min and max values
            MinimumWidth = min.X;
            MinimumHeight = min.Y;
            MinimumDepth = min.Z;
            MaximumWidth = max.X;
            MaximumHeight = max.Y;
            MaximumDepth = max.Z;

            // check with fixed size
            Width = 1000 * rand.NextFloat();
            Height = 1000 * rand.NextFloat();
            Depth = 1000 * rand.NextFloat();
            Margin = rand.NextThickness(10, 20, 30, 40, 50, 60);
            MeasuredAndCheckThatDesiredSizesAreCorrect(100 * rand.NextVector3(), new Vector3(Width, Height, Depth), min, max);

            // reset fixed size
            Width = float.NaN;
            Height = float.NaN;
            Depth = float.NaN;

            // check with MeasureOverride
            var onMeasureOverrideSize = new Vector3(10, 20, 30);
            onMeasureOverride += _ => onMeasureOverrideSize;
            MeasuredAndCheckThatDesiredSizesAreCorrect(100 * rand.NextVector3(), onMeasureOverrideSize, min, max);

            // check size given to MeasureOverride
            onMeasureOverride = availableSize => availableSize / 2;
            var providedSize = 100 * rand.NextVector3();
            var providedSizeWithoutMargin = CalculateSizeWithoutThickness(ref providedSize, ref MarginInternal);
            var expectedSize = new Vector3(Math.Min(providedSizeWithoutMargin.X, max.X), Math.Min(providedSizeWithoutMargin.Y, max.Y), Math.Min(providedSizeWithoutMargin.Z, max.Z)) / 2;
            MeasuredAndCheckThatDesiredSizesAreCorrect(providedSize, expectedSize, min, max);

            // check default values
            expectedSize = new Vector3(40, 50, 60);
            onMeasureOverride = _ => new Vector3(float.NaN, float.NaN, float.NaN);
            DefaultWidth = expectedSize.X;
            DefaultHeight = expectedSize.Y;
            DefaultDepth = expectedSize.Z;
            MeasuredAndCheckThatDesiredSizesAreCorrect(100 * rand.NextVector3(), expectedSize, min, max);

            // check blend of all
            onMeasureOverride = _ => new Vector3(0, onMeasureOverrideSize.Y, float.NaN);
            Width = 100 * rand.NextFloat();
            expectedSize = new Vector3(Width, onMeasureOverrideSize.Y, DefaultDepth);
            MeasuredAndCheckThatDesiredSizesAreCorrect(100 * rand.NextVector3(), expectedSize, min, max);
        }

        private void MeasuredAndCheckThatDesiredSizesAreCorrect(Vector3 availableSize, Vector3 expectedSizeWithoutMargins, Vector3 min, Vector3 max)
        {
            var truncedExpectedSize = new Vector3(
                Math.Min(max.X, Math.Max(min.X, expectedSizeWithoutMargins.X)),
                Math.Min(max.Y, Math.Max(min.Y, expectedSizeWithoutMargins.Y)),
                Math.Min(max.Z, Math.Max(min.Z, expectedSizeWithoutMargins.Z)));

            var truncedExpectedSizeWithMargins = CalculateSizeWithThickness(ref truncedExpectedSize, ref MarginInternal);

            Measure(availableSize);
            Assert.Equal(truncedExpectedSize, DesiredSize);
            Assert.Equal(truncedExpectedSizeWithMargins, DesiredSizeWithMargins);
            Assert.True(IsMeasureValid);
        }

        private Action onCollapsedOverride;

        protected override void CollapseOverride()
        {
            onCollapsedOverride?.Invoke();
        }

        private delegate Vector3 ArrangeOverrideDelegate(Vector3 finalSizeWithoutMargins);

        private ArrangeOverrideDelegate onArrageOverride;

        protected override Vector3 ArrangeOverride(Vector3 finalSizeWithoutMargins)
        {
            if (onArrageOverride != null)
                return onArrageOverride(finalSizeWithoutMargins);

            return base.ArrangeOverride(finalSizeWithoutMargins);
        }

        private bool collaspedHasBeenCalled;
        private bool arrangeOverridedHasBeenCalled;

        /// <summary>
        /// Test the function <see cref="UIElement.Arrange"/> when the element is collapsed.
        /// </summary>
        [Fact]
        public void TestArrangeCollapsed()
        {
            // reset state 
            ResetElementState();

            // set margins
            Margin = new Thickness(11,12,13,14,15,16);

            // set the callbacks
            onCollapsedOverride = () => collaspedHasBeenCalled = true;
            onArrageOverride = size => { arrangeOverridedHasBeenCalled = true; return base.ArrangeOverride(size); };

            // check with the parent
            PertubArrangeResultValues();
            ArrangeAndPerformsCollapsedStateTests(true);

            // check with current element collapsed
            Visibility = Visibility.Collapsed;
            PertubArrangeResultValues();
            ArrangeAndPerformsCollapsedStateTests(false);
        }

        private void ArrangeAndPerformsCollapsedStateTests(bool isParentCollapsed)
        {
            ArrangeAndPerformsStateTests(new Vector3(10, 20, 30), Vector3.Zero, Vector3.Zero, isParentCollapsed, true);
        }

        private void ArrangeAndPerformsNotCollapsedStateTests(Vector3 providedSizeWithMargin, Vector3 expectedSizeWithoutMargin)
        {
            var expectedOffsets = CalculateAdjustmentOffsets(ref MarginInternal, ref providedSizeWithMargin, ref expectedSizeWithoutMargin);
            ArrangeAndPerformsStateTests(providedSizeWithMargin, expectedOffsets, expectedSizeWithoutMargin, false, false);
        }

        private void ArrangeAndPerformsStateTests(Vector3 arrangeSize, Vector3 expectedOffset, Vector3 expectedSize, bool isParentCollapsed, bool shouldBeCollapsed)
        {
            Measure(arrangeSize);
            Arrange(arrangeSize, isParentCollapsed);
            Assert.True(IsArrangeValid);
            Assert.Equal(expectedSize, RenderSize);
            Assert.Equal(expectedOffset, RenderOffsets);
            Assert.Equal(shouldBeCollapsed, collaspedHasBeenCalled);
            Assert.Equal(!shouldBeCollapsed, arrangeOverridedHasBeenCalled);
        }

        private void PertubArrangeResultValues()
        {
            collaspedHasBeenCalled = false;
            arrangeOverridedHasBeenCalled = false;
            InvalidateArrange();
            InvalidateMeasure();
        }
        
        /// <summary>
        /// Test the function <see cref="UIElement.Arrange"/> when the element is not collapsed.
        /// </summary>
        [Fact]
        public void TestArrangeNotCollapsed()
        {
            TestArrangeNotCollapsedCore(HorizontalAlignment.Stretch);
            TestArrangeNotCollapsedCore(HorizontalAlignment.Left);
            TestArrangeNotCollapsedCore(HorizontalAlignment.Center);
            TestArrangeNotCollapsedCore(HorizontalAlignment.Right);
        }

        private Vector3 expectedProvidedSizeInMeasureOverride;

        private void TestArrangeNotCollapsedCore(HorizontalAlignment alignX)
        {
            // reset state 
            ResetElementState();

            // set the alignments
            HorizontalAlignment = alignX;
            switch (alignX)
            {
                case HorizontalAlignment.Left:
                    VerticalAlignment = VerticalAlignment.Top;
                    DepthAlignment = DepthAlignment.Back;
                    break;
                case HorizontalAlignment.Center:
                    VerticalAlignment = VerticalAlignment.Center;
                    DepthAlignment = DepthAlignment.Center;
                    break;
                case HorizontalAlignment.Right:
                    VerticalAlignment = VerticalAlignment.Bottom;
                    DepthAlignment = DepthAlignment.Front;
                    break;
                case HorizontalAlignment.Stretch:
                    VerticalAlignment = VerticalAlignment.Stretch;
                    DepthAlignment = DepthAlignment.Stretch;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("alignX");
            }

            // check that the element is measured if necessary
            InvalidateMeasure();
            Measure(Vector3.Zero);
            Arrange(Vector3.Zero, false);
            Assert.True(IsMeasureValid);

            // set the default callbacks
            var desiredSize = 1000 * rand.NextVector3();
            onMeasureOverride = _ => desiredSize;
            onCollapsedOverride = () => collaspedHasBeenCalled = true;
            onArrageOverride = delegate(Vector3 size)
                {
                    Assert.Equal(expectedProvidedSizeInMeasureOverride, size);
                    arrangeOverridedHasBeenCalled = true; 

                    return base.ArrangeOverride(size);
                };

            // check size and offset when size is fixed
            Width = 100 * rand.NextFloat();
            Height = 100 * rand.NextFloat();
            Depth = 100 * rand.NextFloat();
            PertubArrangeResultValues();
            expectedProvidedSizeInMeasureOverride = new Vector3(Width, Height, Depth);
            ArrangeAndPerformsNotCollapsedStateTests(1000 * rand.NextVector3(), expectedProvidedSizeInMeasureOverride);

            // revert fixed size
            Width = float.NaN;
            Height = float.NaN;
            Depth = float.NaN;

            // check size and offset when size is not fixed
            PertubArrangeResultValues();
            var providedSpace = 1000 * rand.NextVector3();
            var providedWithoutMargins = CalculateSizeWithoutThickness(ref providedSpace, ref MarginInternal);
            if (HorizontalAlignment == HorizontalAlignment.Stretch && VerticalAlignment == VerticalAlignment.Stretch && DepthAlignment == DepthAlignment.Stretch)
            {
                expectedProvidedSizeInMeasureOverride = providedWithoutMargins;
                ArrangeAndPerformsNotCollapsedStateTests(providedSpace, providedWithoutMargins);
            }
            else
            {
                Measure(providedSpace);
                expectedProvidedSizeInMeasureOverride = new Vector3(
                    Math.Min(DesiredSize.X, providedWithoutMargins.X),
                    Math.Min(DesiredSize.Y, providedWithoutMargins.Y),
                    Math.Min(DesiredSize.Z, providedWithoutMargins.Z));
                ArrangeAndPerformsNotCollapsedStateTests(providedSpace, expectedProvidedSizeInMeasureOverride);
            }

            // check the size if extrema values are set
            PertubArrangeResultValues();
            var extremum = new Vector3(21, 22, 23);
            MinimumWidth = extremum.X;
            MinimumHeight = extremum.Y;
            MinimumDepth = extremum.Z;
            MaximumWidth = extremum.X;
            MaximumHeight = extremum.Y;
            MaximumDepth = extremum.Z;
            expectedProvidedSizeInMeasureOverride = extremum;
            ArrangeAndPerformsNotCollapsedStateTests(providedSpace, extremum);

            // revert extrema values
            MinimumWidth = 0;
            MinimumHeight = 0;
            MinimumDepth = 0;
            MaximumWidth = float.PositiveInfinity;
            MaximumHeight =float.PositiveInfinity;
            MaximumDepth = float.PositiveInfinity;

            // check blend of above cases
            PertubArrangeResultValues();
            MinimumWidth = extremum.X;
            MaximumWidth = extremum.X;
            Height = 100 * rand.NextFloat();
            providedWithoutMargins = CalculateSizeWithoutThickness(ref providedSpace, ref MarginInternal);
            if (HorizontalAlignment == HorizontalAlignment.Stretch && VerticalAlignment == VerticalAlignment.Stretch && DepthAlignment == DepthAlignment.Stretch)
            {
                expectedProvidedSizeInMeasureOverride = new Vector3(extremum.X, Height, providedWithoutMargins.Z);
                ArrangeAndPerformsNotCollapsedStateTests(providedSpace, expectedProvidedSizeInMeasureOverride);
            }
            else
            {
                expectedProvidedSizeInMeasureOverride = new Vector3(extremum.X, Height, Math.Min(desiredSize.Z, providedWithoutMargins.Z));
                ArrangeAndPerformsNotCollapsedStateTests(providedSpace, expectedProvidedSizeInMeasureOverride);
            }

            // check that the size returned by ArrangeOverride override the previous calculated size for RenderSize
            PertubArrangeResultValues();
            var onArrangeOverrideSize = new Vector3(10000, 20000, 30000);
            onArrageOverride = delegate { arrangeOverridedHasBeenCalled = true; return onArrangeOverrideSize; };
            ArrangeAndPerformsNotCollapsedStateTests(providedSpace, onArrangeOverrideSize);
        }

        private bool measureOverrideHasBeenCalled;
        private bool arrangeOverrideHasBeenCalled;
        private bool collapseOverrideHasBeenCalled;

        private Vector3 SetMeasureOverrideToCalled(Vector3 input)
        {
            measureOverrideHasBeenCalled = true;
            return Vector3.Zero;
        }

        private Vector3 SetArrangeOverrideToCalled(Vector3 input)
        {
            arrangeOverrideHasBeenCalled = true;
            return Vector3.Zero;
        }

        private void SetCollapseOverrideToCalled()
        {
            collapseOverrideHasBeenCalled = true;
        }

        private delegate bool ReturnBoolDelegate();

        /// <summary>
        /// Test the invalidation system. This invalidation mechanism with Invalidate functions, the IsValid properties and the Measure/Arrange functions.
        /// </summary>
        [Fact]
        public void TestInvalidationSystem()
        {
            TestInvalidationSystemCore(true, ()=> collapseOverrideHasBeenCalled);
            TestInvalidationSystemCore(false, () => arrangeOverrideHasBeenCalled);


            ResetElementState();

            // - Check that changing value of "parentWorldChanged" correctly force the re-calculation of the worldMatrix

            var worldMatrix = Matrix.Zero;
            
            worldMatrix.M11 = 2;
            UpdateWorldMatrix(ref worldMatrix, true);
            Assert.Equal(worldMatrix.M11, WorldMatrix.M11);

            worldMatrix.M11 = 3;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.Equal(2, WorldMatrix.M11);

            worldMatrix.M11 = 1;
            UpdateWorldMatrix(ref worldMatrix, true);
            Assert.Equal(worldMatrix.M11, WorldMatrix.M11);

            // - Check that changing value of the "localMatrix" correctly force the re-calculation of the worldMatrix

            var localMatrix = Matrix.Zero;

            localMatrix.M11 = 4;
            LocalMatrix = localMatrix;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.Equal(localMatrix.M11, WorldMatrix.M11);

            worldMatrix.M11 = 33;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.Equal(localMatrix.M11, WorldMatrix.M11);

            worldMatrix.M11 = 1;
            localMatrix.M11 = 5;
            LocalMatrix = localMatrix;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.Equal(localMatrix.M11, WorldMatrix.M11);
        }

        private void TestInvalidationSystemCore(bool parentIsCollapsed, ReturnBoolDelegate getArrangeHasBeenCalledVal)
        {
            var worldMatrix = Matrix.Zero;

            ResetElementState();

            onMeasureOverride += SetMeasureOverrideToCalled;
            onArrageOverride += SetArrangeOverrideToCalled;
            onCollapsedOverride += SetCollapseOverrideToCalled;

            Measure(Vector3.Zero);
            Arrange(Vector3.Zero, parentIsCollapsed);
            Assert.True(IsMeasureValid);
            Assert.True(IsArrangeValid);

            measureOverrideHasBeenCalled = false;
            arrangeOverrideHasBeenCalled = false;
            collapseOverrideHasBeenCalled = false;

            InvalidateMeasure();
            Assert.False(IsMeasureValid);
            Assert.False(IsArrangeValid);
            Measure(Vector3.Zero);
            Assert.True(IsMeasureValid);
            Assert.False(IsArrangeValid);
            Assert.True(measureOverrideHasBeenCalled);
            Arrange(Vector3.Zero, parentIsCollapsed);
            Assert.True(IsArrangeValid);
            Assert.True(IsMeasureValid);
            Assert.True(getArrangeHasBeenCalledVal());
            worldMatrix.M11 = 2;
            UpdateWorldMatrix(ref worldMatrix, false); // check that invalidation of arrange force the update of the world matrix
            Assert.Equal(worldMatrix.M11, WorldMatrix.M11);

            measureOverrideHasBeenCalled = false;
            arrangeOverrideHasBeenCalled = false;
            collapseOverrideHasBeenCalled = false;

            InvalidateArrange();
            Assert.True(IsMeasureValid);
            Assert.False(IsArrangeValid);
            Measure(Vector3.Zero);
            Assert.True(IsMeasureValid);
            Assert.False(IsArrangeValid);
            Assert.False(measureOverrideHasBeenCalled);
            Arrange(Vector3.Zero, parentIsCollapsed);
            Assert.True(IsMeasureValid);
            Assert.True(IsArrangeValid);
            Assert.True(getArrangeHasBeenCalledVal());
            worldMatrix.M11 = 3;
            UpdateWorldMatrix(ref worldMatrix, false); // check that invalidation of arrange force the update of the world matrix
            Assert.Equal(worldMatrix.M11, WorldMatrix.M11);

            measureOverrideHasBeenCalled = false;
            arrangeOverrideHasBeenCalled = false;
            collapseOverrideHasBeenCalled = false;

            Measure(Vector3.Zero);
            Assert.True(IsMeasureValid);
            Assert.True(IsArrangeValid);
            Assert.False(measureOverrideHasBeenCalled);
            Arrange(Vector3.Zero, parentIsCollapsed);
            Assert.True(IsMeasureValid);
            Assert.True(IsArrangeValid);
            Assert.False(getArrangeHasBeenCalledVal());
            worldMatrix.M11 = 4;
            UpdateWorldMatrix(ref worldMatrix, false); // check that the world matrix is not re-calculated if the arrangement is not invalidated
            Assert.Equal(3, WorldMatrix.M11);
            
            measureOverrideHasBeenCalled = false;
            arrangeOverrideHasBeenCalled = false;
            collapseOverrideHasBeenCalled = false;

            Measure(Vector3.One); // check that measuring with a new value force the re-measurement but not re-arrangement
            Assert.True(IsMeasureValid);
            Assert.False(IsArrangeValid);
            Assert.True(measureOverrideHasBeenCalled);
            Arrange(Vector3.Zero, parentIsCollapsed);
            Assert.True(IsMeasureValid);
            Assert.True(IsArrangeValid);
            Assert.False(getArrangeHasBeenCalledVal());

            measureOverrideHasBeenCalled = false;
            arrangeOverrideHasBeenCalled = false;
            collapseOverrideHasBeenCalled = false;

            Measure(Vector3.One);
            Assert.True(IsMeasureValid);
            Assert.True(IsArrangeValid);
            Assert.False(measureOverrideHasBeenCalled);
            Arrange(Vector3.One, parentIsCollapsed); // check that arranging with a new value force the re-arrangement
            Assert.True(IsMeasureValid);
            Assert.True(IsArrangeValid);
            Assert.True(getArrangeHasBeenCalledVal());

            Measure(Vector3.One);
            Assert.True(IsMeasureValid);
            Assert.True(IsArrangeValid);
            Assert.False(measureOverrideHasBeenCalled);
            Arrange(Vector3.One, !parentIsCollapsed); // check that arranging with a new value of the parent collapse state force the re-arrangement
            Assert.True(IsMeasureValid);
            Assert.True(IsArrangeValid);
            Assert.True(getArrangeHasBeenCalledVal());
        }

        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Fact]
        public void TestBasicInvalidations()
        {
            ResetElementState();

            // - test the properties that are not supposed to invalidate the object layout state

            TestNoInvalidation(() => Name = Name+"Test");
            TestNoInvalidation(() => DrawLayerNumber = DrawLayerNumber+1);
            TestNoInvalidation(() => CanBeHitByUser = !CanBeHitByUser);
            TestNoInvalidation(() => IsEnabled = !IsEnabled);
            TestNoInvalidation(() => Opacity = Opacity / 3 + 0.5f);
            TestNoInvalidation(() => ClipToBounds = !ClipToBounds);
            TestNoInvalidation(() => LocalMatrix = Matrix.Zero);

            // - test the properties that are supposed to invalidate the object measurement

            TestMeasureInvalidation(() => DefaultWidth = DefaultWidth+1);
            TestMeasureInvalidation(() => DefaultHeight = DefaultHeight+1);
            TestMeasureInvalidation(() => DefaultDepth = DefaultDepth+1);
            TestMeasureInvalidation(() => Width = 1);
            TestMeasureInvalidation(() => Height = 1);
            TestMeasureInvalidation(() => Depth = 1);
            TestMeasureInvalidation(() => Size = new Vector3(1, 2, 3));
            TestMeasureInvalidation(() => SetSize(0, 37));
            TestMeasureInvalidation(() => MinimumWidth = MinimumDepth+1);
            TestMeasureInvalidation(() => MinimumHeight = MinimumDepth+1);
            TestMeasureInvalidation(() => MinimumDepth = MinimumDepth+1);
            TestMeasureInvalidation(() => MaximumWidth = 1);
            TestMeasureInvalidation(() => MaximumHeight = 1);
            TestMeasureInvalidation(() => MaximumDepth = 1);
            TestMeasureInvalidation(() => Margin = Thickness.UniformCuboid(1));

            // - test the properties that are supposed to invalidate the object measurement

            TestArrangeInvalidation(() => HorizontalAlignment = HorizontalAlignment.Left);
            TestArrangeInvalidation(() => VerticalAlignment = VerticalAlignment.Bottom);
            TestArrangeInvalidation(() => DepthAlignment = DepthAlignment.Center);
        }

        internal void TestMeasureInvalidation(Action changeProperty)
        {
            TestMeasureInvalidation(this, changeProperty);
        }

        internal void TestArrangeInvalidation(Action changeProperty)
        {
            TestArrangeInvalidation(this, changeProperty);
        }

        internal void TestNoInvalidation(Action changeProperty)
        {
            TestNoInvalidation(this, changeProperty);
        }

        internal static void TestMeasureInvalidation(UIElement element, Action changeProperty)
        {
            element.Measure(Vector3.Zero);
            changeProperty();
            Assert.False(element.IsMeasureValid);
        }

        internal static void TestArrangeInvalidation(UIElement element, Action changeProperty)
        {
            element.Measure(Vector3.Zero);
            element.Arrange(Vector3.Zero, false);
            changeProperty();
            Assert.True(element.IsMeasureValid);
            Assert.False(element.IsArrangeValid);
        }

        internal static void TestNoInvalidation(UIElement element, Action changeProperty)
        {
            element.Measure(Vector3.Zero);
            element.Arrange(Vector3.Zero, false);
            changeProperty();
            Assert.True(element.IsMeasureValid);
            Assert.True(element.IsArrangeValid);
        }

        class MeasureArrangeCallChecker : StackPanel
        {
            public bool MeasureHasBeenCalled { get; private set; }
            public bool ArrangeHasBeenCalled { get; private set; }

            public MeasureOverrideDelegate OnMeasureOverride;
            public ArrangeOverrideDelegate OnArrangeOverride;

            public void Reset()
            {
                MeasureHasBeenCalled = false;
                ArrangeHasBeenCalled = false;
            }

            protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
            {
                MeasureHasBeenCalled = true;

                var size = (OnMeasureOverride != null) ? OnMeasureOverride(availableSizeWithoutMargins) : availableSizeWithoutMargins;

                return base.MeasureOverride(size);
            }

            protected override Vector3 ArrangeOverride(Vector3 availableSizeWithoutMargins)
            {
                ArrangeHasBeenCalled = true;

                var size = (OnArrangeOverride != null) ? OnArrangeOverride(availableSizeWithoutMargins) : availableSizeWithoutMargins;

                return base.ArrangeOverride(size);
            }
        }

        private void AssetMeasureArrangeStateValid(IEnumerable<MeasureArrangeCallChecker> elements)
        {
            foreach (var element in elements)
            {
                Assert.True(element.IsArrangeValid);
                Assert.True(element.IsMeasureValid);
                Assert.False(element.ForceNextMeasure);
                Assert.False(element.ForceNextArrange);
            }
        }

        private void AssertMeasureState(UIElement element, bool noForce, bool isValid)
        {
            Assert.Equal(isValid, element.IsArrangeValid);
            Assert.Equal(isValid, element.IsMeasureValid);
            Assert.Equal(!noForce, element.ForceNextMeasure);
            Assert.Equal(!noForce, element.ForceNextArrange);
        }

        private void AssertArrangeState(UIElement element, bool noForce, bool isValid)
        {
            Assert.Equal(isValid, element.IsArrangeValid);
            Assert.True(element.IsMeasureValid);
            Assert.False(element.ForceNextMeasure);
            Assert.Equal(!noForce, element.ForceNextArrange);
        }

        private void AssertMeasureCalls(MeasureArrangeCallChecker element, bool shouldBeCalled)
        {
            Assert.Equal(shouldBeCalled, element.MeasureHasBeenCalled);
            Assert.Equal(shouldBeCalled, element.ArrangeHasBeenCalled);
        }

        private void AssertArrangeCalls(MeasureArrangeCallChecker element, bool shouldBeCalled)
        {
            Assert.False(element.MeasureHasBeenCalled);
            Assert.Equal(shouldBeCalled, element.ArrangeHasBeenCalled);
        }

        private void AssertUpdateMeasureCalls(MeasureArrangeCallChecker element, bool shouldBeCalled)
        {
            Assert.Equal(shouldBeCalled, element.MeasureHasBeenCalled);
            Assert.False(element.ArrangeHasBeenCalled);
        }
        
        /// <summary>
        /// Test the invalidations are propagated correctly along the tree
        /// </summary>
        [Fact]
        public void TestInvalidationPropagation()
        {
            // construct a simple hierarchy:
            // (legend: f need forced updates, i are invalidated, u should be updated, o are OK)
            //        o     root
            //        |
            //        o     elt1
            //       / \
            //      o   o   elt2 / elt3
            //         / \
            //        o   o elt4 / elt5
            //            |
            //            o elt6

            var elt6 = new MeasureArrangeCallChecker { Name = "elt6" };
            var elt5 = new MeasureArrangeCallChecker { Name = "elt5" }; elt5.Children.Add(elt6);
            var elt4 = new MeasureArrangeCallChecker { Name = "elt4" };
            var elt3 = new MeasureArrangeCallChecker { Name = "elt3" }; elt3.Children.Add(elt4); elt3.Children.Add(elt5);
            var elt2 = new MeasureArrangeCallChecker { Name = "elt2" };
            var elt1 = new MeasureArrangeCallChecker { Name = "elt1" }; elt1.Children.Add(elt2); elt1.Children.Add(elt3);
            var root = new MeasureArrangeCallChecker { Name = "root" }; root.Children.Add(elt1);
            var elements = new List<MeasureArrangeCallChecker> { root, elt1, elt2, elt3, elt4, elt5, elt6 };
            
            root.Measure(Vector3.Zero);
            root.Arrange(Vector3.Zero, false);
            foreach (var element in elements)
                element.Reset();

            // invalidate el3 measure and check that the tree is correctly updated 
            //        fiu     root
            //        |
            //        fiu     elt1
            //       / \
            //      o   fiu   elt2 / elt3 
            //         / \
            //        i   i elt4 / elt5
            //            |
            //            i elt6
            elt3.DefaultDepth = 333;
            AssertMeasureState(root, false, false);
            AssertMeasureState(elt1, false, false);
            AssertMeasureState(elt2, true, true);
            AssertMeasureState(elt3, false, false);
            AssertMeasureState(elt4, true, false);
            AssertMeasureState(elt5, true, false);
            AssertMeasureState(elt6, true, false);

            root.Measure(Vector3.Zero);
            root.Arrange(Vector3.Zero, false);

            AssetMeasureArrangeStateValid(elements);
            AssertMeasureCalls(root, true);
            AssertMeasureCalls(elt1, true);
            AssertMeasureCalls(elt2, false);
            AssertMeasureCalls(elt3, true);
            AssertMeasureCalls(elt4, false);
            AssertMeasureCalls(elt5, false);
            AssertMeasureCalls(elt6, false);

            foreach (var element in elements)
                element.Reset();

            // invalidate el3 arrange and check that the tree is correctly updated
            //        fiu     root
            //        |
            //        fiu     elt1
            //       / \
            //      o   fiu   elt2 / elt3 
            //         / \
            //        i   i elt4 / elt5
            //            |
            //            i elt6
            elt3.HorizontalAlignment = HorizontalAlignment.Right;
            AssertArrangeState(root, false, false);
            AssertArrangeState(elt1, false, false);
            AssertArrangeState(elt2, true, true);
            AssertArrangeState(elt3, false, false);
            AssertArrangeState(elt4, true, false);
            AssertArrangeState(elt5, true, false);
            AssertArrangeState(elt6, true, false);

            root.Measure(Vector3.Zero);
            root.Arrange(Vector3.Zero, false);

            AssetMeasureArrangeStateValid(elements);
            AssertArrangeCalls(root, true);
            AssertArrangeCalls(elt1, true);
            AssertArrangeCalls(elt2, false);
            AssertArrangeCalls(elt3, true);
            AssertArrangeCalls(elt4, false);
            AssertMeasureCalls(elt5, false);
            AssertMeasureCalls(elt6, false);
            
            elt3.OnMeasureOverride += margins => Vector3.Zero;
            elt3.OnArrangeOverride += margins => Vector3.Zero;

            root.Measure(Vector3.Zero);
            root.Arrange(Vector3.Zero, false);

            foreach (var element in elements)
                element.Reset();
            
            // change root measure provided size and check that the tree is correctly updated
            //        u     root
            //        |
            //        u     elt1
            //       / \
            //      u   u   elt2 / elt3 
            //         / \
            //        o   o elt4 / elt5
            //            |
            //            o elt6
            AssertMeasureState(root, true, true);
            AssertMeasureState(elt1, true, true);
            AssertMeasureState(elt2, true, true);
            AssertMeasureState(elt3, true, true);
            AssertMeasureState(elt4, true, true);
            AssertMeasureState(elt5, true, true);
            AssertMeasureState(elt6, true, true);

            root.Measure(Vector3.One);
            root.Arrange(Vector3.Zero, false);

            AssetMeasureArrangeStateValid(elements);
            AssertUpdateMeasureCalls(root, true);
            AssertUpdateMeasureCalls(elt1, true);
            AssertUpdateMeasureCalls(elt2, true);
            AssertUpdateMeasureCalls(elt3, true);
            AssertUpdateMeasureCalls(elt4, false);
            AssertUpdateMeasureCalls(elt5, false);
            AssertUpdateMeasureCalls(elt6, false);
            
            foreach (var element in elements)
                element.Reset();

            // change root measure provided size and check that the tree is correctly updated (x need measure update)
            //        x     root
            //        |
            //        x     elt1
            //       / \
            //      x   x   elt2 / elt3 
            //         / \
            //        o   o elt4 / elt5
            //            |
            //            o elt6
            AssertMeasureState(root, true, true);
            AssertMeasureState(elt1, true, true);
            AssertMeasureState(elt2, true, true);
            AssertMeasureState(elt3, true, true);
            AssertMeasureState(elt4, true, true);
            AssertMeasureState(elt5, true, true);
            AssertMeasureState(elt6, true, true);

            root.Measure(Vector3.One);
            root.Arrange(Vector3.One, false);

            AssetMeasureArrangeStateValid(elements);
            AssertArrangeCalls(root, true);
            AssertArrangeCalls(elt1, true);
            AssertArrangeCalls(elt2, true);
            AssertArrangeCalls(elt3, true);
            AssertArrangeCalls(elt4, false);
            AssertArrangeCalls(elt5, false);
            AssertArrangeCalls(elt6, false);


            // check that invalidation propagation works with visual parent too.
            var testVisualChildElement = new VisualChildTestClass();

            testVisualChildElement.Measure(Vector3.One);
            testVisualChildElement.Arrange(Vector3.One, false);
            
            AssertMeasureState(testVisualChildElement, true, true);
            testVisualChildElement.InvalidateVisualChildMeasure();
            AssertMeasureState(testVisualChildElement, false, false);
        }

        private class VisualChildTestClass : Button
        {
            private StackPanel child = new StackPanel();

            public VisualChildTestClass()
            {
                VisualContent = child;
            }

            public void InvalidateVisualChildMeasure()
            {
                child.Orientation = Orientation.InDepth;
            }
        }
    }
}
