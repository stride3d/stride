// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

using Stride.Core.Mathematics;
using Stride.UI.Controls;

namespace Stride.UI.Tests.Layering
{
    /// <summary>
    /// A class that contains test functions for layering of the <see cref="ContentPresenter"/> class.
    /// </summary>
    [System.ComponentModel.Description("Tests for ContentPresenter layering")]
    public class ContentPresenterTests : ContentPresenter
    {
        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Fact(Skip = "ContentPresenter is deprecated.")]
        public void TestBasicInvalidations()
        {
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
        [Fact(Skip = "ContentPresenter is deprecated.")]
        public void TestUpdateWorldMatrixInvalidation()
        {
            var children = new Button();
            Content = children;

            var worldMatrix = Matrix.Zero;
            var localMatrix = Matrix.Identity;

            Measure(Vector3.Zero);
            Arrange(Vector3.Zero, false);
            UpdateWorldMatrix(ref worldMatrix, false);

            worldMatrix.M11 = 2;
            UpdateWorldMatrix(ref worldMatrix, true);
            Assert.Equal(worldMatrix.M11, children.WorldMatrix.M11);

            worldMatrix.M11 = 3;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.Equal(2, children.WorldMatrix.M11);

            worldMatrix.M11 = 1;
            localMatrix.M11 = 4;
            LocalMatrix = localMatrix;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.Equal(localMatrix.M11, children.WorldMatrix.M11);

            localMatrix.M11 = 1;
            LocalMatrix = localMatrix;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.Equal(localMatrix.M11, children.WorldMatrix.M11);

            InvalidateArrange();
            Arrange(Vector3.Zero, false);

            worldMatrix.M11 = 5;
            UpdateWorldMatrix(ref worldMatrix, false);
            Assert.Equal(worldMatrix.M11, children.WorldMatrix.M11);
        }
    }
}
