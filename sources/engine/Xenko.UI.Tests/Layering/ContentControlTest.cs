// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using NUnit.Framework;

using Xenko.Core.Mathematics;
using Xenko.UI.Controls;

namespace Xenko.UI.Tests.Layering
{
    class ContentControlTest : ContentControl
    {
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
        /// Launch all the test of ContentControlTest
        /// </summary>
        public void TestAll()
        {
            InitializeTest();
            TestProperties();
            TestContent();
            TestCollapseOverride();
            TestMeasureOverride();
            TestArrangeOverride();
            TestUpdateWorldMatrix();
        }

        private void ResetState()
        {
            DependencyProperties.Clear();
            InvalidateArrange();
            InvalidateMeasure();
            Content = null;
            LocalMatrix = Matrix.Identity;
        }

        /// <summary>
        /// Test the properties of <see cref="ContentControl"/>
        /// </summary>
        [Test]
        public void TestProperties()
        {
            ResetState();

            // default values
            Assert.AreEqual(Matrix.Identity, DependencyProperties.Get(ContentArrangeMatrixPropertyKey));
        }

        /// <summary>
        /// Test <see cref="ContentControl.Content"/>
        /// </summary>
        [Test]
        public void TestContent()
        {
            ResetState();

            // default value
            Assert.AreEqual(null, Content);

            // test parent setting
            var content = new ContentControlTest();
            Assert.DoesNotThrow(()=>Content = content);
            Assert.AreEqual(this, content.Parent);
            Assert.AreEqual(content, Content);

            // unset content
            Assert.DoesNotThrow(()=>Content = null);
            Assert.AreEqual(null, content.Parent);
            Assert.AreEqual(Content, null);
            
            // reset the content
            var contentControl = new ContentControlTest { Content = content };
            Assert.DoesNotThrow(()=>contentControl.Content = content);

            // content reused
            Assert.Throws<InvalidOperationException>(() => Content = content);
        }

        /// <summary>
        /// Test function <see cref="ContentControl.CollapseOverride"/>
        /// </summary>
        [Test]
        public void TestCollapseOverride()
        {
            ResetState();

            // create a content
            var child = new ContentControlTest();
            Content = child;

            // set the size of the content
            child.Width = 100 * rand.NextFloat();
            child.Height = 100 * rand.NextFloat();
            child.Depth = 150 * rand.NextFloat();

            // arrange and check child render size
            Arrange(1000 * rand.NextVector3(), true);
            Assert.AreEqual(Vector3.Zero, child.RenderSize);
        }

        /// <summary>
        /// Test for <see cref="ContentControl.MeasureOverride"/>
        /// </summary>
        [Test]
        public void TestMeasureOverride()
        {
            ResetState();

            // test that desired size without content correspond to padding
            Padding = rand.NextThickness(10, 20, 30, 40, 50, 60);
            Measure(1000*rand.NextVector3());
            var v0 = Vector3.Zero;
            var expectedSize = CalculateSizeWithThickness(ref v0, ref padding);
            Assert.AreEqual(expectedSize, DesiredSize);

            // test desired size with a child
            var content = new MeasureValidator();
            Content = content;
            var availableSize = 1000 * rand.NextVector3();
            content.Margin = rand.NextThickness(60, 50, 40, 30, 20, 10);
            var availableSizeWithoutPadding = CalculateSizeWithoutThickness(ref availableSize, ref padding);
            var availableSizeWithoutPaddingChildMargin = CalculateSizeWithoutThickness(ref availableSizeWithoutPadding, ref content.MarginInternal);
            content.ExpectedMeasureValue = availableSizeWithoutPaddingChildMargin;
            content.ReturnedMeasuredValue = 100 * rand.NextVector3();
            var returnedValueWithMargin = CalculateSizeWithThickness(ref content.ReturnedMeasuredValue, ref content.MarginInternal);
            expectedSize = CalculateSizeWithThickness(ref returnedValueWithMargin, ref padding);
            Measure(availableSize);
            Assert.AreEqual(expectedSize, DesiredSize);
        }

        /// <summary>
        /// Test for <see cref="ContentControl.ArrangeOverride"/>
        /// </summary>
        [Test]
        public void TestArrangeOverride()
        {
            ResetState();

            DepthAlignment = DepthAlignment.Stretch;

            // Test that returned value is the one provided when no content
            var providedSize = 1000 * rand.NextVector3();
            var arrangedSize = ArrangeOverride(providedSize);
            Assert.AreEqual(providedSize, arrangedSize);

            ResetState();

            // Test arrange with some content
            providedSize = 1000 * rand.NextVector3();
            var content = new ArrangeValidator { DepthAlignment = DepthAlignment.Stretch };
            Content = content;
            Padding = rand.NextThickness(10, 20, 30, 40, 50, 60);
            var providedSizeWithoutPadding = CalculateSizeWithoutThickness(ref providedSize, ref padding);
            content.ExpectedArrangeValue = providedSizeWithoutPadding;
            arrangedSize = ArrangeOverride(providedSize);
            Assert.AreEqual(providedSize, arrangedSize);
            var childOffsets = new Vector3(Padding.Left, Padding.Top, Padding.Front) - arrangedSize / 2;
            Assert.AreEqual(Matrix.Translation(childOffsets), VisualContent.DependencyProperties.Get(ContentArrangeMatrixPropertyKey));
        }

        /// <summary>
        /// Test the update of the world matrix of children
        /// </summary>
        [Test]
        public void TestUpdateWorldMatrix()
        {
            ResetState();

            DepthAlignment = DepthAlignment.Stretch;

            // set the panel size to 1
            Arrange(Vector3.One, false);

            // test that the world matrix of the panel is correctly updated.
            var localMatrix = Matrix.Scaling(0.1f, 0.5f, 1f);
            var worldMatrix = Matrix.Scaling(1f, 0.8f, 0.4f);
            LocalMatrix = localMatrix;
            UpdateWorldMatrix(ref worldMatrix, true);
            Assert.AreEqual(new Matrix(0.1f, 0, 0, 0, 0, 0.4f, 0, 0, 0, 0, 0.4f, 0, 0.5f, 0.4f, 0.2f, 1), WorldMatrix);

            // add a child and set its local matrix
            var child = new ContentControlTest { DepthAlignment = DepthAlignment.Stretch };
            var childArrangementMatrix = Matrix.Translation(10, 20, 30);
            var childLocalMatrix = new Matrix(0, -1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
            child.LocalMatrix = childLocalMatrix;
            Content = child;

            // set the child's panel arrangement matrix
            VisualContent.DependencyProperties.Set(ContentArrangeMatrixPropertyKey, childArrangementMatrix);

            // arrange the child (set its size to 1)
            child.Arrange(Vector3.One, false);

            // check that the value of the world matrix of the child is correctly updated too
            UpdateWorldMatrix(ref worldMatrix, true);
            Assert.AreEqual(new Matrix(0, -0.4f, 0, 0, 0.1f, 0, 0, 0, 0, 0, 0.4f, 0, 1.55f, 8.6f, 12.4f, 1), child.WorldMatrix);
        }

        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Test]
        public void TestBasicInvalidations()
        {
            ResetState();

            var newButton = new Button();

            // - test the properties that are supposed to invalidate the object measurement
            UIElementLayeringTests.TestMeasureInvalidation(this, () => Content = newButton);

            var sameButton = newButton;

            // - test the properties that are not supposed to invalidate the object layout state
            UIElementLayeringTests.TestNoInvalidation(this, () => Content = sameButton);
        }

        /// <summary>
        /// Test the update of the world matrix of children invalidation
        /// </summary>
        [Test]
        public void TestUpdateWorldMatrixInvalidation()
        {
            ResetState();

            var children = new Button();
            Content = children;

            var worldMatrix = Matrix.Zero;
            var localMatrix = Matrix.Identity;

            Measure(Vector3.Zero);
            Arrange(Vector3.Zero, false);
            UpdateWorldMatrix(ref worldMatrix, false);

            worldMatrix.M11 = 2;
            UpdateWorldMatrix(ref worldMatrix, true);
            Assert.AreEqual(worldMatrix.M11, children.WorldMatrix.M11);

            worldMatrix.M11 = 3;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.AreEqual(2, children.WorldMatrix.M11);

            worldMatrix.M11 = 1;
            localMatrix.M11 = 4;
            LocalMatrix = localMatrix;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.AreEqual(localMatrix.M11, children.WorldMatrix.M11);

            localMatrix.M11 = 1;
            LocalMatrix = localMatrix;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.AreEqual(localMatrix.M11, children.WorldMatrix.M11);

            InvalidateArrange();
            Arrange(Vector3.Zero, false);

            worldMatrix.M11 = 5;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.AreEqual(worldMatrix.M11, children.WorldMatrix.M11);
        }
    }
}
