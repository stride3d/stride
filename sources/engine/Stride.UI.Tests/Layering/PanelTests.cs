// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

using Stride.Core.Mathematics;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.UI.Tests.Layering
{
    /// <summary>
    /// Class that performs tests on the <see cref="Panel"/> implementation
    /// </summary>
    public class PanelTests : Panel
    {
        private void ResetState()
        {
            DependencyProperties.Clear();
            LocalMatrix = Matrix.Identity;
            Children.Clear();
        }

        /// <summary>
        /// Launch all the test present in <see cref="PanelTests"/>
        /// </summary>
        internal void TestAll()
        {
            TestProperties();
            TestChildrenManagement();
            TestUpdateWorldMatrix();
        }

        /// <summary>
        /// Tests the Panel properties.
        /// </summary>
        [Fact]
        public void TestProperties()
        {
            ResetState();

            // default values
            Assert.Equal(0, DependencyProperties.Get(ZIndexPropertyKey));
            Assert.False(ClipToBounds);
            Assert.Equal(Matrix.Identity, DependencyProperties.Get(PanelArrangeMatrixPropertyKey));
        }

        /// <summary>
        /// Tests on the <see cref="Panel.Children"/> collection management.
        /// </summary>
        [Fact]
        public void TestChildrenManagement()
        {
            ResetState();

            // Check that parent is added to child
            var newChild = new PanelTests { Name = "child 1"};
            Assert.Null(newChild.Parent);
            Children.Add(newChild);
            Assert.Equal(this, newChild.Parent);

            // check that parent is removed from child
            Children.Remove(newChild);
            Assert.Null(newChild.Parent);

            // check that adding or removing a child invalidate the measure
            Measure(Vector3.Zero);
            Children.Add(newChild);
            Assert.False(IsMeasureValid);
            Measure(Vector3.Zero);
            Children.Remove(newChild);
            Assert.False(IsMeasureValid);

            // test that children are correctly ordered by Z
            var newChild2 = new PanelTests { Name = "child 2" };
            newChild2.DependencyProperties.Set(ZIndexPropertyKey, 2);
            Children.Add(newChild2);
            Children.Add(newChild);
            Assert.Equal(2, VisualChildrenCollection.Count);
            Assert.Equal(newChild, VisualChildrenCollection[0]);
            Assert.Equal(newChild2, VisualChildrenCollection[1]);
            newChild.DependencyProperties.Set(ZIndexPropertyKey, 3);
            Assert.Equal(2, VisualChildrenCollection.Count);
            Assert.Equal(newChild2, VisualChildrenCollection[0]);
            Assert.Equal(newChild, VisualChildrenCollection[1]);
            Children.Remove(newChild);
            Assert.Single(VisualChildrenCollection);
            Assert.Equal(newChild2, VisualChildrenCollection[0]);
            Children.Remove(newChild2);
            Assert.Empty(VisualChildrenCollection);
        }

        /// <summary>
        /// Test the update of the world matrix of children
        /// </summary>
        [Fact]
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
            Assert.Equal(new Matrix(0.1f,0,0,0, 0,0.4f,0,0, 0,0,0.4f,0, 0.5f, 0.4f, 0.2f,1), WorldMatrix);

            // add a child and set its local matrix
            var child = new PanelTests { DepthAlignment = DepthAlignment.Stretch };
            var childArrangementMatrix = Matrix.Translation(10, 20, 30);
            var childLocalMatrix = new Matrix(0,-1,0,0, 1,0,0,0, 0,0,1,0, 0,0,0,1);
            child.LocalMatrix = childLocalMatrix;
            Children.Add(child);

            // set the child's panel arrangement matrix
            child.DependencyProperties.Set(PanelArrangeMatrixPropertyKey, childArrangementMatrix);

            // arrange the child (set its size to 1)
            child.Arrange(Vector3.One, false);

            // check that the value of the world matrix of the child is correctly updated too
            UpdateWorldMatrix(ref worldMatrix, true);
            Assert.Equal(new Matrix(0,-0.4f,0,0, 0.1f,0,0,0, 0,0,0.4f,0, 1.55f,8.6f,12.4f,1), child.WorldMatrix);
        }

        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Fact]
        public void TestBasicInvalidations()
        {
            var panel = new StackPanel();
            var child = new StackPanel();
            panel.Children.Add(child);

            // - test the properties that are not supposed to invalidate the object layout state
            UIElementLayeringTests.TestNoInvalidation(panel, () => child.DependencyProperties.Set(ZIndexPropertyKey, 37));
        }

        /// <summary>
        /// Test the update of the world matrix of children invalidation
        /// </summary>
        [Fact]
        public void TestUpdateWorldMatrixInvalidation()
        {
            ResetState();

            var child = new Button();
            Children.Add(child);

            var worldMatrix = Matrix.Zero;
            var localMatrix = Matrix.Identity;
            
            Measure(Vector3.Zero);
            Arrange(Vector3.Zero, false);
            UpdateWorldMatrix(ref worldMatrix, false);
            
            worldMatrix.M11 = 2;
            UpdateWorldMatrix(ref worldMatrix, true);
            Assert.Equal(worldMatrix.M11, child.WorldMatrix.M11);
            
            worldMatrix.M11 = 3;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.Equal(2, child.WorldMatrix.M11);
            
            worldMatrix.M11 = 1;
            localMatrix.M11 = 4;
            LocalMatrix = localMatrix;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.Equal(localMatrix.M11, child.WorldMatrix.M11);
            
            localMatrix.M11 = 1;
            LocalMatrix = localMatrix;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.Equal(localMatrix.M11, child.WorldMatrix.M11);
            
            InvalidateArrange();
            Arrange(Vector3.Zero, false);
            
            worldMatrix.M11 = 5;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.Equal(worldMatrix.M11, child.WorldMatrix.M11);

            var secondButton = new Button();
            Children.Add(secondButton);
            Measure(Vector3.Zero);
            Arrange(Vector3.Zero, false);
            worldMatrix.M11 = 7;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.Equal(worldMatrix.M11, child.WorldMatrix.M11);

            Children.Remove(secondButton);
            Measure(Vector3.Zero);
            Arrange(Vector3.Zero, false);
            worldMatrix.M11 = 9;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.Equal(worldMatrix.M11, child.WorldMatrix.M11);

            worldMatrix.M11 = 1;
            UpdateWorldMatrix(ref worldMatrix, true);
            Assert.Equal(worldMatrix.M11, child.WorldMatrix.M11);

            var childArrangeMatrix = 10 * Matrix.Identity;
            child.DependencyProperties.Set(PanelArrangeMatrixPropertyKey, childArrangeMatrix);
            worldMatrix.M11 = 1;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.Equal(childArrangeMatrix.M11, child.WorldMatrix.M11);
        }

        /// <summary>
        /// Test for <see cref="UniformGrid.ScrollOwner"/>
        /// </summary>
        [Fact]
        public void TestScrollOwner()
        {
            var grid = new UniformGrid();
            Assert.Null(grid.ScrollOwner);

            var scrollViewer = new ScrollViewer { Content = grid };
            Assert.Equal(scrollViewer, grid.ScrollOwner);

            scrollViewer.Content = null;
            Assert.Null(grid.ScrollOwner);

            var scrollViewer2 = new ScrollViewer { Content = grid };
            Assert.Equal(scrollViewer2, grid.ScrollOwner);
        }

        /// <summary>
        /// Test for the <see cref="StackPanel.ShouldAnchor"/>
        /// </summary>
        [Fact]
        public void TestShouldAnchor()
        {
            var panel = new PanelTests();

            // default state
            Assert.True(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.True(panel.ShouldAnchor(Orientation.Vertical));
            Assert.True(panel.ShouldAnchor(Orientation.InDepth));

            // horizontally 
            panel.ActivateAnchoring(Orientation.Horizontal, false);

            Assert.False(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.True(panel.ShouldAnchor(Orientation.Vertical));
            Assert.True(panel.ShouldAnchor(Orientation.InDepth));

            panel.ActivateAnchoring(Orientation.Horizontal, true);

            Assert.True(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.True(panel.ShouldAnchor(Orientation.Vertical));
            Assert.True(panel.ShouldAnchor(Orientation.InDepth));

            // vertically
            panel.ActivateAnchoring(Orientation.Vertical, false);

            Assert.True(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.False(panel.ShouldAnchor(Orientation.Vertical));
            Assert.True(panel.ShouldAnchor(Orientation.InDepth));

            panel.ActivateAnchoring(Orientation.Vertical, true);

            Assert.True(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.True(panel.ShouldAnchor(Orientation.Vertical));
            Assert.True(panel.ShouldAnchor(Orientation.InDepth));

            // in depth
            panel.ActivateAnchoring(Orientation.InDepth, false);

            Assert.True(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.True(panel.ShouldAnchor(Orientation.Vertical));
            Assert.False(panel.ShouldAnchor(Orientation.InDepth));

            panel.ActivateAnchoring(Orientation.InDepth, true);

            Assert.True(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.True(panel.ShouldAnchor(Orientation.Vertical));
            Assert.True(panel.ShouldAnchor(Orientation.InDepth));

            // combination
            panel.ActivateAnchoring(Orientation.Horizontal, false);
            panel.ActivateAnchoring(Orientation.Vertical, false);
            panel.ActivateAnchoring(Orientation.InDepth, false);

            Assert.False(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.False(panel.ShouldAnchor(Orientation.Vertical));
            Assert.False(panel.ShouldAnchor(Orientation.InDepth));

            panel.ActivateAnchoring(Orientation.Horizontal, true);
            panel.ActivateAnchoring(Orientation.Vertical, true);
            panel.ActivateAnchoring(Orientation.InDepth, true);

            Assert.True(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.True(panel.ShouldAnchor(Orientation.Vertical));
            Assert.True(panel.ShouldAnchor(Orientation.InDepth));
        }

        /// <summary>
        /// Test for the <see cref="StackPanel.GetSurroudingAnchorDistances"/>
        /// </summary>
        [Fact]
        public void TestSurroudingAnchor()
        {
            var stackSize = new Vector3(100, 200, 300);

            var stackPanel = new PanelTests { Size = stackSize };

            stackPanel.Arrange(Vector3.Zero, false);
            
            Assert.Equal(new Vector2( 0, 100), stackPanel.GetSurroudingAnchorDistances(Orientation.Horizontal, -1f));
            Assert.Equal(new Vector2( 0, 100), stackPanel.GetSurroudingAnchorDistances(Orientation.Horizontal, 0));
            Assert.Equal(new Vector2(-20, 80), stackPanel.GetSurroudingAnchorDistances(Orientation.Horizontal, 20));
            Assert.Equal(new Vector2(-100, 0), stackPanel.GetSurroudingAnchorDistances(Orientation.Horizontal, 100));
            Assert.Equal(new Vector2(-100, 0), stackPanel.GetSurroudingAnchorDistances(Orientation.Horizontal, 150));

            Assert.Equal(new Vector2( 0, 200), stackPanel.GetSurroudingAnchorDistances(Orientation.Vertical, -1f));
            Assert.Equal(new Vector2( 0, 200), stackPanel.GetSurroudingAnchorDistances(Orientation.Vertical, 0));
            Assert.Equal(new Vector2(-100, 100), stackPanel.GetSurroudingAnchorDistances(Orientation.Vertical, 100));
            Assert.Equal(new Vector2(-200, 0), stackPanel.GetSurroudingAnchorDistances(Orientation.Vertical, 200));
            Assert.Equal(new Vector2(-200, 0), stackPanel.GetSurroudingAnchorDistances(Orientation.Vertical, 220));

            Assert.Equal(new Vector2( 0, 300), stackPanel.GetSurroudingAnchorDistances(Orientation.InDepth, -1f));
            Assert.Equal(new Vector2( 0, 300), stackPanel.GetSurroudingAnchorDistances(Orientation.InDepth, 0));
            Assert.Equal(new Vector2(-200, 100), stackPanel.GetSurroudingAnchorDistances(Orientation.InDepth, 200));
            Assert.Equal(new Vector2(-300, 0), stackPanel.GetSurroudingAnchorDistances(Orientation.InDepth, 300));
            Assert.Equal(new Vector2(-300, 0), stackPanel.GetSurroudingAnchorDistances(Orientation.InDepth, 330));
        }
    }
}
