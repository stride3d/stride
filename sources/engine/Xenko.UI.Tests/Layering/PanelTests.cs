// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using NUnit.Framework;

using Xenko.Core.Mathematics;
using Xenko.UI.Controls;
using Xenko.UI.Panels;

namespace Xenko.UI.Tests.Layering
{
    /// <summary>
    /// Class that performs tests on the <see cref="Panel"/> implementation
    /// </summary>
    class PanelTests : Panel
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
        public void TestAll()
        {
            TestProperties();
            TestChildrenManagement();
            TestUpdateWorldMatrix();
        }

        /// <summary>
        /// Tests the Panel properties.
        /// </summary>
        [Test]
        public void TestProperties()
        {
            ResetState();

            // default values
            Assert.AreEqual(0, DependencyProperties.Get(ZIndexPropertyKey));
            Assert.AreEqual(false, ClipToBounds);
            Assert.AreEqual(Matrix.Identity, DependencyProperties.Get(PanelArrangeMatrixPropertyKey));
        }

        /// <summary>
        /// Tests on the <see cref="Panel.Children"/> collection management.
        /// </summary>
        [Test]
        public void TestChildrenManagement()
        {
            ResetState();

            // Check that parent is added to child
            var newChild = new PanelTests { Name = "child 1"};
            Assert.AreEqual(null, newChild.Parent);
            Children.Add(newChild);
            Assert.AreEqual(this, newChild.Parent);

            // check that parent is removed from child
            Children.Remove(newChild);
            Assert.AreEqual(null, newChild.Parent);

            // check that adding or removing a child invalidate the measure
            Measure(Vector3.Zero);
            Children.Add(newChild);
            Assert.AreEqual(false, IsMeasureValid);
            Measure(Vector3.Zero);
            Children.Remove(newChild);
            Assert.AreEqual(false, IsMeasureValid);

            // test that children are correctly ordered by Z
            var newChild2 = new PanelTests { Name = "child 2" };
            newChild2.DependencyProperties.Set(ZIndexPropertyKey, 2);
            Children.Add(newChild2);
            Children.Add(newChild);
            Assert.AreEqual(VisualChildrenCollection.Count, 2);
            Assert.AreEqual(newChild, VisualChildrenCollection[0]);
            Assert.AreEqual(newChild2, VisualChildrenCollection[1]);
            newChild.DependencyProperties.Set(ZIndexPropertyKey, 3);
            Assert.AreEqual(VisualChildrenCollection.Count, 2);
            Assert.AreEqual(newChild2, VisualChildrenCollection[0]);
            Assert.AreEqual(newChild, VisualChildrenCollection[1]);
            Children.Remove(newChild);
            Assert.AreEqual(VisualChildrenCollection.Count, 1);
            Assert.AreEqual(newChild2, VisualChildrenCollection[0]);
            Children.Remove(newChild2);
            Assert.AreEqual(VisualChildrenCollection.Count, 0);
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
            Assert.AreEqual(new Matrix(0.1f,0,0,0, 0,0.4f,0,0, 0,0,0.4f,0, 0.5f, 0.4f, 0.2f,1), WorldMatrix);

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
            Assert.AreEqual(new Matrix(0,-0.4f,0,0, 0.1f,0,0,0, 0,0,0.4f,0, 1.55f,8.6f,12.4f,1), child.WorldMatrix);
        }

        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Test]
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
        [Test]
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
            Assert.AreEqual(worldMatrix.M11, child.WorldMatrix.M11);
            
            worldMatrix.M11 = 3;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.AreEqual(2, child.WorldMatrix.M11);
            
            worldMatrix.M11 = 1;
            localMatrix.M11 = 4;
            LocalMatrix = localMatrix;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.AreEqual(localMatrix.M11, child.WorldMatrix.M11);
            
            localMatrix.M11 = 1;
            LocalMatrix = localMatrix;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.AreEqual(localMatrix.M11, child.WorldMatrix.M11);
            
            InvalidateArrange();
            Arrange(Vector3.Zero, false);
            
            worldMatrix.M11 = 5;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.AreEqual(worldMatrix.M11, child.WorldMatrix.M11);

            var secondButton = new Button();
            Children.Add(secondButton);
            Measure(Vector3.Zero);
            Arrange(Vector3.Zero, false);
            worldMatrix.M11 = 7;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.AreEqual(worldMatrix.M11, child.WorldMatrix.M11);

            Children.Remove(secondButton);
            Measure(Vector3.Zero);
            Arrange(Vector3.Zero, false);
            worldMatrix.M11 = 9;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.AreEqual(worldMatrix.M11, child.WorldMatrix.M11);

            worldMatrix.M11 = 1;
            UpdateWorldMatrix(ref worldMatrix, true);
            Assert.AreEqual(worldMatrix.M11, child.WorldMatrix.M11);

            var childArrangeMatrix = 10 * Matrix.Identity;
            child.DependencyProperties.Set(PanelArrangeMatrixPropertyKey, childArrangeMatrix);
            worldMatrix.M11 = 1;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.AreEqual(childArrangeMatrix.M11, child.WorldMatrix.M11);
        }

        /// <summary>
        /// Test for <see cref="UniformGrid.ScrollOwner"/>
        /// </summary>
        [Test]
        public void TestScrollOwner()
        {
            var grid = new UniformGrid();
            Assert.AreEqual(null, grid.ScrollOwner);

            var scrollViewer = new ScrollViewer { Content = grid };
            Assert.AreEqual(scrollViewer, grid.ScrollOwner);

            scrollViewer.Content = null;
            Assert.AreEqual(null, grid.ScrollOwner);

            var scrollViewer2 = new ScrollViewer { Content = grid };
            Assert.AreEqual(scrollViewer2, grid.ScrollOwner);
        }

        /// <summary>
        /// Test for the <see cref="StackPanel.ShouldAnchor"/>
        /// </summary>
        [Test]
        public void TestShouldAnchor()
        {
            var panel = new PanelTests();

            // default state
            Assert.IsTrue(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.IsTrue(panel.ShouldAnchor(Orientation.Vertical));
            Assert.IsTrue(panel.ShouldAnchor(Orientation.InDepth));

            // horizontally 
            panel.ActivateAnchoring(Orientation.Horizontal, false);

            Assert.IsFalse(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.IsTrue(panel.ShouldAnchor(Orientation.Vertical));
            Assert.IsTrue(panel.ShouldAnchor(Orientation.InDepth));

            panel.ActivateAnchoring(Orientation.Horizontal, true);

            Assert.IsTrue(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.IsTrue(panel.ShouldAnchor(Orientation.Vertical));
            Assert.IsTrue(panel.ShouldAnchor(Orientation.InDepth));

            // vertically
            panel.ActivateAnchoring(Orientation.Vertical, false);

            Assert.IsTrue(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.IsFalse(panel.ShouldAnchor(Orientation.Vertical));
            Assert.IsTrue(panel.ShouldAnchor(Orientation.InDepth));

            panel.ActivateAnchoring(Orientation.Vertical, true);

            Assert.IsTrue(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.IsTrue(panel.ShouldAnchor(Orientation.Vertical));
            Assert.IsTrue(panel.ShouldAnchor(Orientation.InDepth));

            // in depth
            panel.ActivateAnchoring(Orientation.InDepth, false);

            Assert.IsTrue(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.IsTrue(panel.ShouldAnchor(Orientation.Vertical));
            Assert.IsFalse(panel.ShouldAnchor(Orientation.InDepth));

            panel.ActivateAnchoring(Orientation.InDepth, true);

            Assert.IsTrue(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.IsTrue(panel.ShouldAnchor(Orientation.Vertical));
            Assert.IsTrue(panel.ShouldAnchor(Orientation.InDepth));

            // combination
            panel.ActivateAnchoring(Orientation.Horizontal, false);
            panel.ActivateAnchoring(Orientation.Vertical, false);
            panel.ActivateAnchoring(Orientation.InDepth, false);

            Assert.IsFalse(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.IsFalse(panel.ShouldAnchor(Orientation.Vertical));
            Assert.IsFalse(panel.ShouldAnchor(Orientation.InDepth));

            panel.ActivateAnchoring(Orientation.Horizontal, true);
            panel.ActivateAnchoring(Orientation.Vertical, true);
            panel.ActivateAnchoring(Orientation.InDepth, true);

            Assert.IsTrue(panel.ShouldAnchor(Orientation.Horizontal));
            Assert.IsTrue(panel.ShouldAnchor(Orientation.Vertical));
            Assert.IsTrue(panel.ShouldAnchor(Orientation.InDepth));
        }

        /// <summary>
        /// Test for the <see cref="StackPanel.GetSurroudingAnchorDistances"/>
        /// </summary>
        [Test]
        public void TestSurroudingAnchor()
        {
            var stackSize = new Vector3(100, 200, 300);

            var stackPanel = new PanelTests { Size = stackSize };

            stackPanel.Arrange(Vector3.Zero, false);
            
            Assert.AreEqual(new Vector2( 0, 100), stackPanel.GetSurroudingAnchorDistances(Orientation.Horizontal, -1f));
            Assert.AreEqual(new Vector2( 0, 100), stackPanel.GetSurroudingAnchorDistances(Orientation.Horizontal, 0));
            Assert.AreEqual(new Vector2(-20, 80), stackPanel.GetSurroudingAnchorDistances(Orientation.Horizontal, 20));
            Assert.AreEqual(new Vector2(-100, 0), stackPanel.GetSurroudingAnchorDistances(Orientation.Horizontal, 100));
            Assert.AreEqual(new Vector2(-100, 0), stackPanel.GetSurroudingAnchorDistances(Orientation.Horizontal, 150));

            Assert.AreEqual(new Vector2( 0, 200), stackPanel.GetSurroudingAnchorDistances(Orientation.Vertical, -1f));
            Assert.AreEqual(new Vector2( 0, 200), stackPanel.GetSurroudingAnchorDistances(Orientation.Vertical, 0));
            Assert.AreEqual(new Vector2(-100, 100), stackPanel.GetSurroudingAnchorDistances(Orientation.Vertical, 100));
            Assert.AreEqual(new Vector2(-200, 0), stackPanel.GetSurroudingAnchorDistances(Orientation.Vertical, 200));
            Assert.AreEqual(new Vector2(-200, 0), stackPanel.GetSurroudingAnchorDistances(Orientation.Vertical, 220));

            Assert.AreEqual(new Vector2( 0, 300), stackPanel.GetSurroudingAnchorDistances(Orientation.InDepth, -1f));
            Assert.AreEqual(new Vector2( 0, 300), stackPanel.GetSurroudingAnchorDistances(Orientation.InDepth, 0));
            Assert.AreEqual(new Vector2(-200, 100), stackPanel.GetSurroudingAnchorDistances(Orientation.InDepth, 200));
            Assert.AreEqual(new Vector2(-300, 0), stackPanel.GetSurroudingAnchorDistances(Orientation.InDepth, 300));
            Assert.AreEqual(new Vector2(-300, 0), stackPanel.GetSurroudingAnchorDistances(Orientation.InDepth, 330));
        }
    }
}
