// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
            UIElementLayeringTests.TestMeasureInvalidation(this, () => ColumnGap = 10f);
            UIElementLayeringTests.TestMeasureInvalidation(this, () => RowGap = 5f);
            UIElementLayeringTests.TestMeasureInvalidation(this, () => LayerGap = 15f);
        }

        /// <summary>
        /// Test for the <see cref="UniformGrid.GetSurroundingAnchorDistances"/>
        /// </summary>
        [Fact]
        public void TestSurroundingAnchor()
        {
            var childSize1 = new Vector3(50, 150, 250);
            var childSize2 = new Vector3(100, 200, 300);

            var grid = new UniformGrid { Columns = 2, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

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


        /// <summary>
        /// Test for the <see cref="UniformGrid.GetSurroudingAnchorDistances"/>
        /// </summary>
        [Fact]
        public void TestSurroudingAnchorWithGap()
        {
            var childSize1 = new Vector3(50, 150, 250);
            var childSize2 = new Vector3(100, 200, 300);

            var grid = new UniformGrid { Columns = 2, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            grid.ColumnGap = 10f; // Add gap for testing

            var child1 = new UniformGrid { Size = childSize1 };
            var child2 = new UniformGrid { Size = childSize2 };
            child2.DependencyProperties.Set(ColumnPropertyKey, 1);

            grid.Children.Add(child1);
            grid.Children.Add(child2);

            grid.Measure(1000 * Vector3.One);
            grid.Arrange(1000 * Vector3.One, false);

            // With gaps, the anchor distance is based on cell content size + gap
            // Cell size is determined by content (100) + gap (10) = 110
            var anchorSpacing = 110f;

            Assert.Equal(new Vector2(0, anchorSpacing), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, -1));
            Assert.Equal(new Vector2(0, anchorSpacing), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 0));
            Assert.Equal(new Vector2(-55f, 55f), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 55f));
            Assert.Equal(new Vector2(-55f, 55f), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, 55f));
            Assert.Equal(new Vector2(0, anchorSpacing), grid.GetSurroudingAnchorDistances(Orientation.Horizontal, anchorSpacing));
        }


        /// <summary>
        /// Test the gap properties and their effects on layout
        /// </summary>
        [Fact]
        public void TestGapProperties()
        {
            var uniformGrid = new UniformGrid();

            // Test default values
            Assert.Equal(0f, uniformGrid.ColumnGap);
            Assert.Equal(0f, uniformGrid.RowGap);
            Assert.Equal(0f, uniformGrid.LayerGap);

            // Test setting values
            uniformGrid.ColumnGap = 10f;
            uniformGrid.RowGap = 5f;
            uniformGrid.LayerGap = 15f;

            Assert.Equal(10f, uniformGrid.ColumnGap);
            Assert.Equal(5f, uniformGrid.RowGap);
            Assert.Equal(15f, uniformGrid.LayerGap);

            // Test negative values are clamped to 0
            uniformGrid.ColumnGap = -5f;
            uniformGrid.RowGap = -10f;
            uniformGrid.LayerGap = -20f;

            Assert.Equal(0f, uniformGrid.ColumnGap);
            Assert.Equal(0f, uniformGrid.RowGap);
            Assert.Equal(0f, uniformGrid.LayerGap);
        }

        /// <summary>
        /// Test measure with gaps
        /// </summary>
        [Fact]
        public void TestMeasureWithGaps()
        {
            var uniformGrid = new UniformGrid { Columns = 3, Rows = 2, Layers = 1 };
            uniformGrid.ColumnGap = 10f;
            uniformGrid.RowGap = 5f;
            uniformGrid.LayerGap = 0f; // No layer gap for 2D grid

            // Create test children
            var child1 = new UniformGridTests { Width = 50, Height = 30 };
            var child2 = new UniformGridTests { Width = 40, Height = 25 };
            var child3 = new UniformGridTests { Width = 45, Height = 35 };

            child1.DependencyProperties.Set(ColumnPropertyKey, 0);
            child1.DependencyProperties.Set(RowPropertyKey, 0);
            child2.DependencyProperties.Set(ColumnPropertyKey, 1);
            child2.DependencyProperties.Set(RowPropertyKey, 0);
            child3.DependencyProperties.Set(ColumnPropertyKey, 2);
            child3.DependencyProperties.Set(RowPropertyKey, 0);

            uniformGrid.Children.Add(child1);
            uniformGrid.Children.Add(child2);
            uniformGrid.Children.Add(child3);

            var measureSize = new Vector3(300, 200, 100);
            uniformGrid.Measure(measureSize);

            // Calculate expected size:
            // Cell size: max child size (50x35) 
            // Total width: 3 * 50 + 2 * 10 (gaps) = 170
            // Total height: 2 * 35 + 1 * 5 (gap) = 75
            var expectedSize = new Vector3(170, 75, 0);
            Assert.Equal(expectedSize, uniformGrid.DesiredSizeWithMargins);
        }

        /// <summary>
        /// Test arrange with gaps
        /// </summary>
        [Fact]
        public void TestArrangeWithGaps()
        {
            var uniformGrid = new UniformGrid { Columns = 2, Rows = 2, Layers = 1 };
            uniformGrid.ColumnGap = 8f;
            uniformGrid.RowGap = 4f;

            // Create test children
            var child1 = new UniformGridTests { Width = 30, Height = 20 };
            var child2 = new UniformGridTests { Width = 25, Height = 18 };
            var child3 = new UniformGridTests { Width = 35, Height = 22 };
            var child4 = new UniformGridTests { Width = 28, Height = 19 };

            // Position children in grid
            child1.DependencyProperties.Set(ColumnPropertyKey, 0);
            child1.DependencyProperties.Set(RowPropertyKey, 0);
            child2.DependencyProperties.Set(ColumnPropertyKey, 1);
            child2.DependencyProperties.Set(RowPropertyKey, 0);
            child3.DependencyProperties.Set(ColumnPropertyKey, 0);
            child3.DependencyProperties.Set(RowPropertyKey, 1);
            child4.DependencyProperties.Set(ColumnPropertyKey, 1);
            child4.DependencyProperties.Set(RowPropertyKey, 1);

            uniformGrid.Children.Add(child1);
            uniformGrid.Children.Add(child2);
            uniformGrid.Children.Add(child3);
            uniformGrid.Children.Add(child4);

            var arrangeSize = new Vector3(200, 150, 100);
            uniformGrid.Measure(arrangeSize);
            uniformGrid.Arrange(arrangeSize, false);

            // Verify children are positioned correctly with gaps
            var child1Matrix = child1.DependencyProperties.Get(PanelArrangeMatrixPropertyKey);
            var child2Matrix = child2.DependencyProperties.Get(PanelArrangeMatrixPropertyKey);
            var child3Matrix = child3.DependencyProperties.Get(PanelArrangeMatrixPropertyKey);
            var child4Matrix = child4.DependencyProperties.Get(PanelArrangeMatrixPropertyKey);

            // Calculate expected positions accounting for gaps
            var cellWidth = (arrangeSize.X - uniformGrid.ColumnGap) / 2; // (200 - 8) / 2 = 96
            var cellHeight = (arrangeSize.Y - uniformGrid.RowGap) / 2; // (150 - 4) / 2 = 73

            // Child 1 at (0,0) - should be at top-left
            var expectedChild1X = -arrangeSize.X / 2;
            var expectedChild1Y = -arrangeSize.Y / 2;
            Assert.Equal(expectedChild1X, child1Matrix.TranslationVector.X);
            Assert.Equal(expectedChild1Y, child1Matrix.TranslationVector.Y);

            // Child 2 at (1,0) - should be at top-right with column gap
            var expectedChild2X = -arrangeSize.X / 2 + cellWidth + uniformGrid.ColumnGap;
            var expectedChild2Y = -arrangeSize.Y / 2;
            Assert.Equal(expectedChild2X, child2Matrix.TranslationVector.X);
            Assert.Equal(expectedChild2Y, child2Matrix.TranslationVector.Y);

            // Child 3 at (0,1) - should be at bottom-left with row gap
            var expectedChild3X = -arrangeSize.X / 2;
            var expectedChild3Y = -arrangeSize.Y / 2 + cellHeight + uniformGrid.RowGap;
            Assert.Equal(expectedChild3X, child3Matrix.TranslationVector.X);
            Assert.Equal(expectedChild3Y, child3Matrix.TranslationVector.Y);

            // Child 4 at (1,1) - should be at bottom-right with both gaps
            var expectedChild4X = -arrangeSize.X / 2 + cellWidth + uniformGrid.ColumnGap;
            var expectedChild4Y = -arrangeSize.Y / 2 + cellHeight + uniformGrid.RowGap;
            Assert.Equal(expectedChild4X, child4Matrix.TranslationVector.X);
            Assert.Equal(expectedChild4Y, child4Matrix.TranslationVector.Y);
        }

        /// <summary>
        /// Test gaps with 3D grid (including layer gaps)
        /// </summary>
        [Fact]
        public void TestGapsWith3DGrid()
        {
            var uniformGrid = new UniformGrid { Columns = 2, Rows = 2, Layers = 2 };
            uniformGrid.ColumnGap = 5f;
            uniformGrid.RowGap = 3f;
            uniformGrid.LayerGap = 7f;

            // Create test children for all positions
            for (int layer = 0; layer < 2; layer++)
            {
                for (int row = 0; row < 2; row++)
                {
                    for (int col = 0; col < 2; col++)
                    {
                        var child = new UniformGridTests { Width = 20, Height = 15, Depth = 10 };
                        child.DependencyProperties.Set(ColumnPropertyKey, col);
                        child.DependencyProperties.Set(RowPropertyKey, row);
                        child.DependencyProperties.Set(LayerPropertyKey, layer);
                        uniformGrid.Children.Add(child);
                    }
                }
            }

            var measureSize = new Vector3(200, 150, 100);
            uniformGrid.Measure(measureSize);

            // Expected total size with gaps:
            // Width: 2 * 20 + 1 * 5 = 45
            // Height: 2 * 15 + 1 * 3 = 33
            // Depth: 2 * 10 + 1 * 7 = 27
            var expectedSize = new Vector3(45, 33, 27);
            Assert.Equal(expectedSize, uniformGrid.DesiredSizeWithMargins);
        }

        /// <summary>
        /// Test gaps with single column/row (no gaps should be added)
        /// </summary>
        [Fact]
        public void TestGapsWithSingleDimensions()
        {
            var uniformGrid = new UniformGrid { Columns = 1, Rows = 1, Layers = 1 };
            uniformGrid.ColumnGap = 10f;
            uniformGrid.RowGap = 5f;
            uniformGrid.LayerGap = 15f;

            var child = new UniformGridTests { Width = 50, Height = 30, Depth = 20 };
            child.DependencyProperties.Set(ColumnPropertyKey, 0);
            child.DependencyProperties.Set(RowPropertyKey, 0);
            child.DependencyProperties.Set(LayerPropertyKey, 0);
            uniformGrid.Children.Add(child);

            var measureSize = new Vector3(200, 150, 100);
            uniformGrid.Measure(measureSize);

            // With single cell, no gaps should be added
            var expectedSize = new Vector3(50, 30, 20);
            Assert.Equal(expectedSize, uniformGrid.DesiredSizeWithMargins);
        }

        /// <summary>
        /// Test gaps with spanned elements
        /// </summary>
        [Fact]
        public void TestGapsWithSpannedElements()
        {
            var uniformGrid = new UniformGrid { Columns = 3, Rows = 2, Layers = 1 };
            uniformGrid.ColumnGap = 6f;
            uniformGrid.RowGap = 4f;

            // Create a child that spans 2 columns
            var spannedChild = new UniformGridTests { Width = 80, Height = 40 };
            spannedChild.DependencyProperties.Set(ColumnPropertyKey, 0);
            spannedChild.DependencyProperties.Set(RowPropertyKey, 0);
            spannedChild.DependencyProperties.Set(ColumnSpanPropertyKey, 2);

            // Create a normal child
            var normalChild = new UniformGridTests { Width = 30, Height = 25 };
            normalChild.DependencyProperties.Set(ColumnPropertyKey, 2);
            normalChild.DependencyProperties.Set(RowPropertyKey, 0);

            uniformGrid.Children.Add(spannedChild);
            uniformGrid.Children.Add(normalChild);

            var measureSize = new Vector3(300, 200, 100);
            uniformGrid.Measure(measureSize);
            uniformGrid.Arrange(measureSize, false);

            // Verify that spanned elements work correctly with gaps
            var spannedMatrix = spannedChild.DependencyProperties.Get(PanelArrangeMatrixPropertyKey);
            var normalMatrix = normalChild.DependencyProperties.Get(PanelArrangeMatrixPropertyKey);

            // The spanned child should start at the left edge
            Assert.Equal(-measureSize.X / 2, spannedMatrix.TranslationVector.X);

            // The normal child should be positioned after the spanned child and gaps
            var cellWidth = (measureSize.X - 2 * uniformGrid.ColumnGap) / 3;
            var expectedNormalX = -measureSize.X / 2 + 2 * cellWidth + 2 * uniformGrid.ColumnGap;
            Assert.Equal(expectedNormalX, normalMatrix.TranslationVector.X);
        }
    }
}
