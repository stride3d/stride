// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using NUnit.Framework;

using Xenko.Core.Mathematics;
using Xenko.UI.Controls;

namespace Xenko.UI.Tests.Layering
{
    /// <summary>
    /// A class that contains test functions for layering of the <see cref="ScrollViewer"/> class.
    /// </summary>
    [TestFixture]
    [System.ComponentModel.Description("Tests for ScrollViewer layering")]
    public class ScrollViewerTests : ScrollViewer
    {
        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Test]
        public void TestBasicInvalidations()
        {
            // - test the properties that are supposed to invalidate the object measurement
            UIElementLayeringTests.TestMeasureInvalidation(this, () => ScrollMode = ScrollingMode.InDepthHorizontal);

            // - test the properties that are not supposed to invalidate the object layout state
            UIElementLayeringTests.TestNoInvalidation(this, () => Deceleration = 5.5f);
            UIElementLayeringTests.TestNoInvalidation(this, () => TouchScrollingEnabled = !TouchScrollingEnabled);
            UIElementLayeringTests.TestNoInvalidation(this, () => ScrollBarColor = new Color(1, 2, 3, 4));
            UIElementLayeringTests.TestNoInvalidation(this, () => ScrollBarThickness = 34);
        }


        [Test]
        public void TestScrolling()
        {
            const float elementWidth = 100;
            const float elementHeight = 200;
            const float elementDepth = 300;

            var rand = new Random();
            var scrollViewer = new ScrollViewer { ScrollMode = ScrollingMode.HorizontalVertical, Width = elementWidth, Height = elementHeight, Depth = elementDepth };
            scrollViewer.Measure(Vector3.Zero);
            scrollViewer.Arrange(Vector3.Zero, false);

            // tests that no crashes happen with no content
            scrollViewer.ScrollTo(rand.NextVector3());
            Assert.AreEqual(Vector3.Zero, ScrollPosition);
            scrollViewer.ScrollOf(rand.NextVector3());
            Assert.AreEqual(Vector3.Zero, ScrollPosition);
            scrollViewer.ScrollToBeginning(Orientation.Horizontal);
            Assert.AreEqual(Vector3.Zero, ScrollPosition);
            scrollViewer.ScrollToBeginning(Orientation.InDepth);
            Assert.AreEqual(Vector3.Zero, ScrollPosition);
            scrollViewer.ScrollToEnd(Orientation.Horizontal);
            Assert.AreEqual(Vector3.Zero, ScrollPosition);
            scrollViewer.ScrollToEnd(Orientation.InDepth);
            Assert.AreEqual(Vector3.Zero, ScrollPosition);

            // tests with an arranged element
            const float contentWidth = 1000;
            const float contentHeight = 2000;
            const float contentDepth = 3000;
            var content = new ContentDecorator { Width = contentWidth, Height = contentHeight, Depth = contentDepth };
            scrollViewer.Content = content;
            scrollViewer.Measure(Vector3.Zero);
            scrollViewer.Arrange(Vector3.Zero, false);

            var scrollValue = new Vector3(123, 456, 789);
            scrollViewer.ScrollTo(scrollValue);
            Assert.AreEqual(new Vector3(scrollValue.X, scrollValue.Y, 0), scrollViewer.ScrollPosition);

            scrollViewer.ScrollToEnd(Orientation.Horizontal);
            Assert.AreEqual(new Vector3(contentWidth - elementWidth, scrollValue.Y, 0), scrollViewer.ScrollPosition);
            scrollViewer.ScrollToEnd(Orientation.Vertical);
            Assert.AreEqual(new Vector3(contentWidth - elementWidth, contentHeight - elementHeight, 0), scrollViewer.ScrollPosition);
            scrollViewer.ScrollToEnd(Orientation.InDepth);
            Assert.AreEqual(new Vector3(contentWidth - elementWidth, contentHeight - elementHeight, 0), scrollViewer.ScrollPosition);

            scrollViewer.ScrollToBeginning(Orientation.Horizontal);
            Assert.AreEqual(new Vector3(0, contentHeight - elementHeight, 0), scrollViewer.ScrollPosition);
            scrollViewer.ScrollToBeginning(Orientation.Vertical);
            Assert.AreEqual(new Vector3(0, 0, 0), scrollViewer.ScrollPosition);
            scrollViewer.ScrollToBeginning(Orientation.InDepth);
            Assert.AreEqual(new Vector3(0, 0, 0), scrollViewer.ScrollPosition);

            scrollViewer.ScrollOf(scrollValue);
            Assert.AreEqual(new Vector3(scrollValue.X, scrollValue.Y, 0), scrollViewer.ScrollPosition);

            // tests with an not arranged element
            content.InvalidateArrange();
            scrollViewer.ScrollTo(scrollValue);
            scrollViewer.Arrange(Vector3.Zero, false);
            Assert.AreEqual(new Vector3(scrollValue.X, scrollValue.Y, 0), scrollViewer.ScrollPosition);
            content.InvalidateArrange();
            scrollViewer.ScrollOf(2*scrollValue);
            scrollViewer.ScrollTo(scrollValue);
            scrollViewer.Arrange(Vector3.Zero, false);
            Assert.AreEqual(new Vector3(scrollValue.X, scrollValue.Y, 0), scrollViewer.ScrollPosition);

            content.InvalidateArrange();
            scrollViewer.ScrollToEnd(Orientation.Horizontal);
            scrollViewer.ScrollToEnd(Orientation.Vertical);
            scrollViewer.ScrollToEnd(Orientation.InDepth);
            scrollViewer.Arrange(Vector3.Zero, false);
            Assert.AreEqual(new Vector3(contentWidth - elementWidth, contentHeight - elementHeight, 0), scrollViewer.ScrollPosition);

            content.InvalidateArrange();
            scrollViewer.ScrollToBeginning(Orientation.Horizontal);
            scrollViewer.ScrollToBeginning(Orientation.Vertical);
            scrollViewer.ScrollToBeginning(Orientation.InDepth);
            scrollViewer.Arrange(Vector3.Zero, false);
            Assert.AreEqual(new Vector3(0, 0, 0), scrollViewer.ScrollPosition);

            content.InvalidateArrange();
            scrollViewer.ScrollOf(scrollValue);
            scrollViewer.Arrange(Vector3.Zero, false);
            Assert.AreEqual(new Vector3(scrollValue.X, scrollValue.Y, 0), scrollViewer.ScrollPosition);
            content.InvalidateArrange();
            scrollViewer.ScrollToBeginning(Orientation.Horizontal);
            scrollViewer.ScrollToBeginning(Orientation.Vertical);
            scrollViewer.ScrollToBeginning(Orientation.InDepth);
            scrollViewer.ScrollOf(scrollValue);
            scrollViewer.ScrollOf(scrollValue);
            scrollViewer.Arrange(Vector3.Zero, false);
            Assert.AreEqual(new Vector3(2*scrollValue.X, 2*scrollValue.Y, 0), scrollViewer.ScrollPosition);
        }

        /// <summary>
        /// Tests that <see cref="ScrollViewer.CurrentScrollingSpeed"/> is properly reseted or not.
        /// </summary>
        [Test]
        public void TestStopScrolling()
        {
            var referenceValue = Vector3.One;

            ScrollMode = ScrollingMode.Horizontal;

            CurrentScrollingSpeed = referenceValue;

            // tests the function itself
            StopCurrentScrolling();
            Assert.AreEqual(Vector3.Zero, CurrentScrollingSpeed);

            CurrentScrollingSpeed = referenceValue;

            // tests ScrollTo function
            ScrollTo(Vector3.Zero, false);
            Assert.AreEqual(referenceValue, CurrentScrollingSpeed);
            ScrollTo(Vector3.Zero);
            Assert.AreEqual(Vector3.Zero, CurrentScrollingSpeed);
            
            CurrentScrollingSpeed = referenceValue;

            // tests ScrollOf function
            ScrollOf(Vector3.Zero, false);
            Assert.AreEqual(referenceValue, CurrentScrollingSpeed);
            ScrollOf(Vector3.Zero);
            Assert.AreEqual(Vector3.Zero, CurrentScrollingSpeed);

            CurrentScrollingSpeed = referenceValue;

            // test ScrollToBeginning
            ScrollToBeginning(Orientation.Horizontal, false);
            Assert.AreEqual(referenceValue, CurrentScrollingSpeed);
            ScrollToBeginning(Orientation.Vertical, false);
            Assert.AreEqual(referenceValue, CurrentScrollingSpeed);
            ScrollToBeginning(Orientation.Horizontal);
            Assert.AreEqual(Vector3.Zero, CurrentScrollingSpeed);
            CurrentScrollingSpeed = referenceValue;
            ScrollToBeginning(Orientation.Vertical);
            Assert.AreEqual(Vector3.Zero, CurrentScrollingSpeed);

            CurrentScrollingSpeed = referenceValue;

            // test ScrollToEnd
            ScrollToEnd(Orientation.Horizontal, false);
            Assert.AreEqual(referenceValue, CurrentScrollingSpeed);
            ScrollToEnd(Orientation.Vertical, false);
            Assert.AreEqual(referenceValue, CurrentScrollingSpeed);
            ScrollToEnd(Orientation.Horizontal);
            Assert.AreEqual(Vector3.Zero, CurrentScrollingSpeed);
            CurrentScrollingSpeed = referenceValue;
            ScrollToEnd(Orientation.Vertical);
            Assert.AreEqual(Vector3.Zero, CurrentScrollingSpeed);

            CurrentScrollingSpeed = referenceValue;
        }
    }
}
