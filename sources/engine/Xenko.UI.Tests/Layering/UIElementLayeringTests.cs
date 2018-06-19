// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using NUnit.Framework;

using Xenko.Core.Mathematics;
using Xenko.UI.Controls;
using Xenko.UI.Panels;

namespace Xenko.UI.Tests.Layering
{
    /// <summary>
    /// A class that contains test functions for layering of the <see cref="UIElement"/> class.
    /// </summary>
    [TestFixture]
    [System.ComponentModel.Description("Tests for UIElement layering")]
    public class UIElementLayeringTests : UIElement
    {
        protected override IEnumerable<IUIElementChildren> EnumerateChildren()
        {
            throw new NotImplementedException();
        }

        private Random rand;

        /// <summary>
        /// Initialize the series of tests.
        /// </summary>
        [TestFixtureSetUp]
        public void InitializeTest()
        {
            // create a rand variable changing from a test to the other
            rand = new Random(DateTime.Now.Millisecond);
        }

        /// <summary>
        /// Performs all the tests
        /// </summary>
        public void TestAll()
        {
            InitializeTest();
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
        [Test]
        public void TestUpdateWorldMatrix()
        {
            ResetElementState();

            DepthAlignment = DepthAlignment.Stretch;
            Arrange(Vector3.Zero, false);

            var identity = Matrix.Identity;
            Matrix matrix;

            // test that Identity is return when there are no transformation
            UpdateWorldMatrix(ref identity, true);
            Assert.AreEqual(Matrix.Identity, WorldMatrix);

            // test that parent matrix is return when there no internal transformations
            var parentWorld = Matrix.LookAtLH(new Vector3(1, 2, 3), Vector3.Zero, new Vector3(0, 0, 1));
            UpdateWorldMatrix(ref parentWorld, true);
            Assert.AreEqual(parentWorld, WorldMatrix);

            // test that child local matrix is returned parent matrix is identity and no layering has been done
            var localMatrix = Matrix.LookAtLH(new Vector3(3, 2, 1), Vector3.Zero, new Vector3(0, 0, 1));
            LocalMatrix = localMatrix;
            UpdateWorldMatrix(ref identity, true);
            Assert.AreEqual(localMatrix, WorldMatrix);

            // check that the composition of the parent world matrix and child local matrix is correct
            LocalMatrix = Matrix.Translation(new Vector3(1, 2, 3));
            matrix = Matrix.Scaling(0.5f, 0.5f, 0.5f);
            UpdateWorldMatrix(ref matrix, true);
            Assert.AreEqual(new Matrix(0.5f, 0, 0, 0, 0, 0.5f, 0, 0, 0, 0, 0.5f, 0, 0.5f, 1f, 1.5f, 1), WorldMatrix);

            // check that the margin and half render size offsets are properly included in the world transformation
            LocalMatrix = Matrix.Identity;
            Margin = new Thickness(1,2,3,4,5,6);
            Arrange(new Vector3(15,27,39), false);
            UpdateWorldMatrix(ref identity, true);
            Assert.AreEqual(Matrix.Translation(1+5,2+10,6+15), WorldMatrix);

            // check that the result of the composition between margin and parent world matrix is correct
            matrix = Matrix.Scaling(0.5f, 0.5f, 0.5f);
            UpdateWorldMatrix(ref matrix, true);
            Assert.AreEqual(new Matrix(0.5f, 0, 0, 0, 0, 0.5f, 0, 0, 0, 0, 0.5f, 0, 3f, 6f, 10.5f, 1), WorldMatrix);

            // check that the composition of the margins, local matrix and parent matrix is correct.
            LocalMatrix = new Matrix(0,-1,0,0, 1,0,0,0, 0,0,1,0, 0,0,0,1);
            matrix = Matrix.Scaling(0.1f, 0.2f, 0.4f);
            UpdateWorldMatrix(ref matrix, true);
            Assert.AreEqual(new Matrix(0,-0.2f,0,0, 0.1f,0,0,0, 0,0,0.4f,0, 0.6f,2.4f,8.4f,1), WorldMatrix);
        }

        /// <summary>
        /// Test for <see cref="UIElement.DependencyProperties"/>
        /// </summary>
        [Test]
        public void TestDependencyProperties()
        {
            ResetElementState();

            var newElement = new UIElementLayeringTests();

            // check dependency property default values
            Assert.IsTrue(newElement.ForceNextMeasure);
            Assert.IsTrue(newElement.ForceNextArrange);
            Assert.IsTrue(newElement.IsEnabled);
            Assert.AreEqual(1f, newElement.Opacity);
            Assert.AreEqual(Visibility.Visible, newElement.Visibility);
            Assert.AreEqual(0f, newElement.DefaultWidth);
            Assert.AreEqual(0f, newElement.DefaultHeight);
            Assert.AreEqual(0f, newElement.DefaultDepth);
            Assert.AreEqual(float.NaN, newElement.Height);
            Assert.AreEqual(float.NaN, newElement.Width);
            Assert.AreEqual(float.NaN, newElement.Depth);
            Assert.AreEqual(0f, newElement.MinimumHeight);
            Assert.AreEqual(0f, newElement.MinimumWidth);
            Assert.AreEqual(0f, newElement.MinimumDepth);
            Assert.AreEqual(float.PositiveInfinity, newElement.MaximumHeight);
            Assert.AreEqual(float.PositiveInfinity, newElement.MaximumWidth);
            Assert.AreEqual(float.PositiveInfinity, newElement.MaximumDepth);
            Assert.AreEqual(HorizontalAlignment.Stretch, newElement.HorizontalAlignment);
            Assert.AreEqual(VerticalAlignment.Stretch, newElement.VerticalAlignment);
            Assert.AreEqual(DepthAlignment.Center, newElement.DepthAlignment);
            Assert.AreEqual(null, newElement.Name);
            Assert.AreEqual(Thickness.UniformCuboid(0), newElement.Margin);
            Assert.AreEqual(Matrix.Identity, newElement.LocalMatrix);

            /////////////////////////////////////////
            // check dependency property validators

            // opacity validator
            Opacity = -1;
            Assert.AreEqual(0f, Opacity);
            Opacity = 2;
            Assert.AreEqual(1f, Opacity);
            Opacity = 0.5f;
            Assert.AreEqual(0.5f, Opacity);

            // default sizes (values should remain in range [0, float.MaxValue])
            Assert.DoesNotThrow(() => DefaultWidth = -1f);
            Assert.AreEqual(0f, DefaultWidth);
            Assert.DoesNotThrow(() => DefaultWidth = float.NaN);
            Assert.AreEqual(0f, DefaultWidth); // previous value unchanged
            Assert.DoesNotThrow(() => DefaultWidth = float.PositiveInfinity);
            Assert.AreEqual(float.MaxValue, DefaultWidth);

            Assert.DoesNotThrow(() => DefaultHeight = -1f);
            Assert.AreEqual(0f, DefaultHeight);
            Assert.DoesNotThrow(() => DefaultHeight = float.NaN);
            Assert.AreEqual(0f, DefaultHeight); // previous value unchanged
            Assert.DoesNotThrow(() => DefaultHeight = float.PositiveInfinity);
            Assert.AreEqual(float.MaxValue, DefaultHeight);

            Assert.DoesNotThrow(() => DefaultDepth = -1f);
            Assert.AreEqual(0f, DefaultDepth);
            Assert.DoesNotThrow(() => DefaultDepth = float.NaN);
            Assert.AreEqual(0f, DefaultDepth); // previous value unchanged
            Assert.DoesNotThrow(() => DefaultDepth = float.PositiveInfinity);
            Assert.AreEqual(float.MaxValue, DefaultDepth);

            // sizes (values should remain in range [0, float.MaxValue])
            Assert.DoesNotThrow(() => Width = -1f);
            Assert.AreEqual(0f, Width);
            Assert.DoesNotThrow(() => Width = float.PositiveInfinity);
            Assert.AreEqual(float.MaxValue, Width);

            Assert.DoesNotThrow(() => Height = -1f);
            Assert.AreEqual(0f, Height);
            Assert.DoesNotThrow(() => Height = float.PositiveInfinity);
            Assert.AreEqual(float.MaxValue, Height);

            Assert.DoesNotThrow(() => Depth = -1f);
            Assert.AreEqual(0f, Depth);
            Assert.DoesNotThrow(() => Depth = float.PositiveInfinity);
            Assert.AreEqual(float.MaxValue, Depth);

            // minimum sizes (values should remain in range [0, float.MaxValue])
            Assert.DoesNotThrow(() => MinimumWidth = -1f);
            Assert.AreEqual(0f, MinimumWidth);
            Assert.DoesNotThrow(() => MinimumWidth = float.NaN);
            Assert.AreEqual(0f, MinimumWidth); // previous value unchanged
            Assert.DoesNotThrow(() => MinimumWidth = float.PositiveInfinity);
            Assert.AreEqual(float.MaxValue, MinimumWidth);

            Assert.DoesNotThrow(() => MinimumHeight = -1f);
            Assert.AreEqual(0f, MinimumHeight);
            Assert.DoesNotThrow(() => MinimumHeight = float.NaN);
            Assert.AreEqual(0f, MinimumHeight); // previous value unchanged
            Assert.DoesNotThrow(() => MinimumHeight = float.PositiveInfinity);
            Assert.AreEqual(float.MaxValue, MinimumHeight);

            Assert.DoesNotThrow(() => MinimumDepth = -1f);
            Assert.AreEqual(0f, MinimumDepth);
            Assert.DoesNotThrow(() => MinimumDepth = float.NaN);
            Assert.AreEqual(0f, MinimumDepth); // previous value unchanged
            Assert.DoesNotThrow(() => MinimumDepth = float.PositiveInfinity);
            Assert.AreEqual(float.MaxValue, MinimumDepth);

            // maximum sizes (values should remain in range [0, float.PositiveInfinity])
            Assert.DoesNotThrow(() => MaximumWidth = -1f);
            Assert.AreEqual(0f, MaximumWidth);
            Assert.DoesNotThrow(() => MaximumWidth = float.NaN);
            Assert.AreEqual(0f, MaximumWidth); // previous value unchanged

            Assert.DoesNotThrow(() => MaximumHeight = -1f);
            Assert.AreEqual(0f, MaximumHeight);
            Assert.DoesNotThrow(() => MaximumHeight = float.NaN);
            Assert.AreEqual(0f, MaximumHeight); // previous value unchanged

            Assert.DoesNotThrow(() => MaximumDepth = -1f);
            Assert.AreEqual(0f, MaximumDepth);
            Assert.DoesNotThrow(() => MaximumDepth = float.NaN);
            Assert.AreEqual(0f, MaximumDepth); // previous value unchanged
        }

        /// <summary>
        /// Test for <see cref="UIElement.CalculateSizeWithoutThickness"/>
        /// </summary>
        [Test]
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
        [Test]
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
            Assert.IsTrue((v1 - v2).Length() <= v1.Length() * MathUtil.ZeroTolerance);
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
        [Test]
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
            Assert.AreEqual(Vector3.Zero, DesiredSize);
            Assert.AreEqual(Vector3.Zero, DesiredSizeWithMargins);
            Assert.AreEqual(true, IsMeasureValid);
        }

        /// <summary>
        /// Test for <see cref="UIElement.Measure"/>
        /// </summary>
        [Test]
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
            Assert.AreEqual(truncedExpectedSize, DesiredSize);
            Assert.AreEqual(truncedExpectedSizeWithMargins, DesiredSizeWithMargins);
            Assert.AreEqual(true, IsMeasureValid);
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
        [Test]
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
            Assert.AreEqual(true, IsArrangeValid);
            Assert.AreEqual(expectedSize, RenderSize);
            Assert.AreEqual(expectedOffset, RenderOffsets);
            Assert.AreEqual(shouldBeCollapsed, collaspedHasBeenCalled);
            Assert.AreEqual(!shouldBeCollapsed, arrangeOverridedHasBeenCalled);
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
        [Test]
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
            Assert.AreEqual(true, IsMeasureValid);

            // set the default callbacks
            var desiredSize = 1000 * rand.NextVector3();
            onMeasureOverride = _ => desiredSize;
            onCollapsedOverride = () => collaspedHasBeenCalled = true;
            onArrageOverride = delegate(Vector3 size)
                {
                    Assert.AreEqual(expectedProvidedSizeInMeasureOverride, size);
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
        [Test]
        public void TestInvalidationSystem()
        {
            TestInvalidationSystemCore(true, ()=> collapseOverrideHasBeenCalled);
            TestInvalidationSystemCore(false, () => arrangeOverrideHasBeenCalled);


            ResetElementState();

            // - Check that changing value of "parentWorldChanged" correctly force the re-calculation of the worldMatrix

            var worldMatrix = Matrix.Zero;
            
            worldMatrix.M11 = 2;
            UpdateWorldMatrix(ref worldMatrix, true);
            Assert.AreEqual(worldMatrix.M11, WorldMatrix.M11);

            worldMatrix.M11 = 3;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.AreEqual(2, WorldMatrix.M11);

            worldMatrix.M11 = 1;
            UpdateWorldMatrix(ref worldMatrix, true);
            Assert.AreEqual(worldMatrix.M11, WorldMatrix.M11);

            // - Check that changing value of the "localMatrix" correctly force the re-calculation of the worldMatrix

            var localMatrix = Matrix.Zero;

            localMatrix.M11 = 4;
            LocalMatrix = localMatrix;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.AreEqual(localMatrix.M11, WorldMatrix.M11);

            worldMatrix.M11 = 33;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.AreEqual(localMatrix.M11, WorldMatrix.M11);

            worldMatrix.M11 = 1;
            localMatrix.M11 = 5;
            LocalMatrix = localMatrix;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.AreEqual(localMatrix.M11, WorldMatrix.M11);
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
            Assert.IsTrue(IsMeasureValid);
            Assert.IsTrue(IsArrangeValid);

            measureOverrideHasBeenCalled = false;
            arrangeOverrideHasBeenCalled = false;
            collapseOverrideHasBeenCalled = false;

            InvalidateMeasure();
            Assert.IsFalse(IsMeasureValid);
            Assert.IsFalse(IsArrangeValid);
            Measure(Vector3.Zero);
            Assert.IsTrue(IsMeasureValid);
            Assert.IsFalse(IsArrangeValid);
            Assert.IsTrue(measureOverrideHasBeenCalled);
            Arrange(Vector3.Zero, parentIsCollapsed);
            Assert.IsTrue(IsArrangeValid);
            Assert.IsTrue(IsMeasureValid);
            Assert.IsTrue(getArrangeHasBeenCalledVal());
            worldMatrix.M11 = 2;
            UpdateWorldMatrix(ref worldMatrix, false); // check that invalidation of arrange force the update of the world matrix
            Assert.AreEqual(worldMatrix.M11, WorldMatrix.M11);

            measureOverrideHasBeenCalled = false;
            arrangeOverrideHasBeenCalled = false;
            collapseOverrideHasBeenCalled = false;

            InvalidateArrange();
            Assert.IsTrue(IsMeasureValid);
            Assert.IsFalse(IsArrangeValid);
            Measure(Vector3.Zero);
            Assert.IsTrue(IsMeasureValid);
            Assert.IsFalse(IsArrangeValid);
            Assert.IsFalse(measureOverrideHasBeenCalled);
            Arrange(Vector3.Zero, parentIsCollapsed);
            Assert.IsTrue(IsMeasureValid);
            Assert.IsTrue(IsArrangeValid);
            Assert.IsTrue(getArrangeHasBeenCalledVal());
            worldMatrix.M11 = 3;
            UpdateWorldMatrix(ref worldMatrix, false); // check that invalidation of arrange force the update of the world matrix
            Assert.AreEqual(worldMatrix.M11, WorldMatrix.M11);

            measureOverrideHasBeenCalled = false;
            arrangeOverrideHasBeenCalled = false;
            collapseOverrideHasBeenCalled = false;

            Measure(Vector3.Zero);
            Assert.IsTrue(IsMeasureValid);
            Assert.IsTrue(IsArrangeValid);
            Assert.IsFalse(measureOverrideHasBeenCalled);
            Arrange(Vector3.Zero, parentIsCollapsed);
            Assert.IsTrue(IsMeasureValid);
            Assert.IsTrue(IsArrangeValid);
            Assert.IsFalse(getArrangeHasBeenCalledVal());
            worldMatrix.M11 = 4;
            UpdateWorldMatrix(ref worldMatrix, false); // check that the world matrix is not re-calculated if the arrangement is not invalidated
            Assert.AreEqual(3, WorldMatrix.M11);
            
            measureOverrideHasBeenCalled = false;
            arrangeOverrideHasBeenCalled = false;
            collapseOverrideHasBeenCalled = false;

            Measure(Vector3.One); // check that measuring with a new value force the re-measurement but not re-arrangement
            Assert.IsTrue(IsMeasureValid);
            Assert.IsFalse(IsArrangeValid);
            Assert.IsTrue(measureOverrideHasBeenCalled);
            Arrange(Vector3.Zero, parentIsCollapsed);
            Assert.IsTrue(IsMeasureValid);
            Assert.IsTrue(IsArrangeValid);
            Assert.IsFalse(getArrangeHasBeenCalledVal());

            measureOverrideHasBeenCalled = false;
            arrangeOverrideHasBeenCalled = false;
            collapseOverrideHasBeenCalled = false;

            Measure(Vector3.One);
            Assert.IsTrue(IsMeasureValid);
            Assert.IsTrue(IsArrangeValid);
            Assert.IsFalse(measureOverrideHasBeenCalled);
            Arrange(Vector3.One, parentIsCollapsed); // check that arranging with a new value force the re-arrangement
            Assert.IsTrue(IsMeasureValid);
            Assert.IsTrue(IsArrangeValid);
            Assert.IsTrue(getArrangeHasBeenCalledVal());

            Measure(Vector3.One);
            Assert.IsTrue(IsMeasureValid);
            Assert.IsTrue(IsArrangeValid);
            Assert.IsFalse(measureOverrideHasBeenCalled);
            Arrange(Vector3.One, !parentIsCollapsed); // check that arranging with a new value of the parent collapse state force the re-arrangement
            Assert.IsTrue(IsMeasureValid);
            Assert.IsTrue(IsArrangeValid);
            Assert.IsTrue(getArrangeHasBeenCalledVal());
        }

        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Test]
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

        public void TestMeasureInvalidation(Action changeProperty)
        {
            TestMeasureInvalidation(this, changeProperty);
        }

        public void TestArrangeInvalidation(Action changeProperty)
        {
            TestArrangeInvalidation(this, changeProperty);
        }

        public void TestNoInvalidation(Action changeProperty)
        {
            TestNoInvalidation(this, changeProperty);
        }

        public static void TestMeasureInvalidation(UIElement element, Action changeProperty)
        {
            element.Measure(Vector3.Zero);
            changeProperty();
            Assert.IsFalse(element.IsMeasureValid);
        }

        public static void TestArrangeInvalidation(UIElement element, Action changeProperty)
        {
            element.Measure(Vector3.Zero);
            element.Arrange(Vector3.Zero, false);
            changeProperty();
            Assert.IsTrue(element.IsMeasureValid);
            Assert.IsFalse(element.IsArrangeValid);
        }

        public static void TestNoInvalidation(UIElement element, Action changeProperty)
        {
            element.Measure(Vector3.Zero);
            element.Arrange(Vector3.Zero, false);
            changeProperty();
            Assert.IsTrue(element.IsMeasureValid);
            Assert.IsTrue(element.IsArrangeValid);
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
                Assert.IsTrue(element.IsArrangeValid);
                Assert.IsTrue(element.IsMeasureValid);
                Assert.IsFalse(element.ForceNextMeasure);
                Assert.IsFalse(element.ForceNextArrange);
            }
        }

        private void AssertMeasureState(UIElement element, bool noForce, bool isValid)
        {
            Assert.AreEqual(isValid, element.IsArrangeValid);
            Assert.AreEqual(isValid, element.IsMeasureValid);
            Assert.AreEqual(!noForce, element.ForceNextMeasure);
            Assert.AreEqual(!noForce, element.ForceNextArrange);
        }

        private void AssertArrangeState(UIElement element, bool noForce, bool isValid)
        {
            Assert.AreEqual(isValid, element.IsArrangeValid);
            Assert.AreEqual(true, element.IsMeasureValid);
            Assert.AreEqual(false, element.ForceNextMeasure);
            Assert.AreEqual(!noForce, element.ForceNextArrange);
        }

        private void AssertMeasureCalls(MeasureArrangeCallChecker element, bool shouldBeCalled)
        {
            Assert.AreEqual(shouldBeCalled, element.MeasureHasBeenCalled);
            Assert.AreEqual(shouldBeCalled, element.ArrangeHasBeenCalled);
        }

        private void AssertArrangeCalls(MeasureArrangeCallChecker element, bool shouldBeCalled)
        {
            Assert.AreEqual(false, element.MeasureHasBeenCalled);
            Assert.AreEqual(shouldBeCalled, element.ArrangeHasBeenCalled);
        }

        private void AssertUpdateMeasureCalls(MeasureArrangeCallChecker element, bool shouldBeCalled)
        {
            Assert.AreEqual(shouldBeCalled, element.MeasureHasBeenCalled);
            Assert.AreEqual(false, element.ArrangeHasBeenCalled);
        }
        
        /// <summary>
        /// Test the invalidations are propagated correctly along the tree
        /// </summary>
        [Test]
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
