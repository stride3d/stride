// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

using Stride.UI.Controls;

namespace Stride.UI.Tests.Layering
{
    /// <summary>
    /// A class that contains test functions for layering of the <see cref="ToggleButton"/> class.
    /// </summary>
    [System.ComponentModel.Description("Tests for ToggleButton layering")]
    public class ToggleButtonTests : ToggleButton
    {
        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Fact]
        public void TestBasicInvalidations()
        {
            // CheckedImage, IndeterminateImage, UncheckedImage and State are not tested because they can potentially invalidate or not the layout states

            // - test the properties that are not supposed to invalidate the object layout state
            UIElementLayeringTests.TestNoInvalidation(this, () => IsThreeState = true);
        }
    }
}
