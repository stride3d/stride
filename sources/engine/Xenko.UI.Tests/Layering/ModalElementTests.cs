// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

using Xenko.Core.Mathematics;
using Xenko.UI.Controls;

namespace Xenko.UI.Tests.Layering
{
    /// <summary>
    /// A class that contains test functions for layering of the <see cref="ModalElement"/> class.
    /// </summary>
    [System.ComponentModel.Description("Tests for ModalElement layering")]
    public class ModalElementTests : ModalElement
    {
        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Fact]
        public void TestBasicInvalidations()
        {
            // - test the properties that are not supposed to invalidate the object layout state
            UIElementLayeringTests.TestNoInvalidation(this, () => OverlayColor = new Color(1, 2, 3, 4));
            UIElementLayeringTests.TestNoInvalidation(this, () => IsModal = !IsModal);
        }
    }
}
