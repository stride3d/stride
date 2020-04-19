// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

using Stride.Core.Mathematics;
using Stride.UI.Panels;

namespace Stride.UI.Tests.Layering
{
    /// <summary>
    /// A class that contains test functions for layering of the <see cref="UniformGrid"/> class.
    /// </summary>
    [System.ComponentModel.Description("Tests for UniformGrid layering")]
    public class UniformGridTests : UniformGrid
    {
        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Fact]
        public void TestBasicInvalidations()
        {
            // - test the properties that are supposed to invalidate the object measurement
            UIElementLayeringTests.TestMeasureInvalidation(this, () => Columns = 7);
            UIElementLayeringTests.TestMeasureInvalidation(this, () => Rows = 34);
            UIElementLayeringTests.TestMeasureInvalidation(this, () => Layers = 34);
        }

        /// <summary>
        /// Test for the <see cref="StackPanel.GetSurroudingAnchorDistances"/>
        /// </summary>
        [Fact]
        public void TestSurroudingAnchor()
        {
            var childSize1 = new Vector3(50, 150, 250);
            var childSize2 = new Vector3(100, 200, 300);
            
            var grid = new UniformGrid { Columns = 2, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center};

            var child1 = new UniformGrid { Size = childSize1 };
            var child2 = new UniformGrid { Size = childSize2 };
            child2.DependencyProperties.Set(ColumnPropertyKey, 1);
            
            grid.Children.Add(child1);
            grid.Children.Add(child2);

            grid.Measure(1000 * Vector3.One);
            grid.Arrange(1000 * Vector3.One, false);
            
            Assert.Equal(new Vector2(0, 100), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, -1));
            Assert.Equal(new Vector2(0, 100), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 0));
            Assert.Equal(new Vector2(-50, 50), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 50));
            Assert.Equal(new Vector2(-80, 20), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 80));
            Assert.Equal(new Vector2(0, 100), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 100));
            Assert.Equal(new Vector2(-10, 90), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 110));
            Assert.Equal(new Vector2(-100, 0), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 200));
            Assert.Equal(new Vector2(-100, 0), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 300));

            Assert.Equal(new Vector2(0, 200), grid.GetSurroudingAnchorDistances(Orientation.Vertical, -1));
            Assert.Equal(new Vector2(-100, 100), grid.GetSurroudingAnchorDistances(Orientation.Vertical, 100));
            Assert.Equal(new Vector2(-200, 0), grid.GetSurroudingAnchorDistances(Orientation.Vertical, 500));

            Assert.Equal(new Vector2(0, 300), grid.GetSurroudingAnchorDistances(Orientation.InDepth, -1));
            Assert.Equal(new Vector2(-150, 150), grid.GetSurroudingAnchorDistances(Orientation.InDepth, 150));
            Assert.Equal(new Vector2(-300, 0), grid.GetSurroudingAnchorDistances(Orientation.InDepth, 500));
        }
    }
}
