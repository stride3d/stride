// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

using Stride.UI.Panels;

namespace Stride.UI.Tests.Layering
{
    /// <summary>
    /// A class that contains test functions for layering of the <see cref="GridBase"/> class.
    /// </summary>
    [System.ComponentModel.Description("Tests for GridBase layering")]
    public class GridBaseTests : GridBase
    {
        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Fact]
        public void TestBasicInvalidations()
        {
            var grid = new UniformGrid { Rows = 2, Columns = 2, Layers = 2 };
            var child = new UniformGrid();
            grid.Children.Add(child);

            // - test the properties that are not supposed to invalidate the object layout state
            UIElementLayeringTests.TestMeasureInvalidation(grid, () => child.DependencyProperties.Set(ColumnPropertyKey, 2));
            UIElementLayeringTests.TestMeasureInvalidation(grid, () => child.DependencyProperties.Set(RowPropertyKey, 2));
            UIElementLayeringTests.TestMeasureInvalidation(grid, () => child.DependencyProperties.Set(LayerPropertyKey, 2));
            UIElementLayeringTests.TestMeasureInvalidation(grid, () => child.DependencyProperties.Set(ColumnSpanPropertyKey, 2));
            UIElementLayeringTests.TestMeasureInvalidation(grid, () => child.DependencyProperties.Set(RowSpanPropertyKey, 2));
            UIElementLayeringTests.TestMeasureInvalidation(grid, () => child.DependencyProperties.Set(LayerSpanPropertyKey, 2));
        }
    }
}
