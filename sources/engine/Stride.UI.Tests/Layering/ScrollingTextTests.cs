// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

using Stride.UI.Controls;

namespace Stride.UI.Tests.Layering
{
    /// <summary>
    /// A class that contains test functions for layering of the <see cref="ScrollingText"/> class.
    /// </summary>
    [System.ComponentModel.Description("Tests for ScrollingText layering")]
    public class ScrollingTextTests : ScrollingText
    {
        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Fact]
        public void TestBasicInvalidations()
        {
            // - test the properties that are supposed to invalidate the object measurement
            UIElementLayeringTests.TestMeasureInvalidation(this, () => DesiredCharacterNumber = 99);

            // - test the properties that are not supposed to invalidate the object layout state
            UIElementLayeringTests.TestNoInvalidation(this, () => ScrollingSpeed = 5.5f);
            UIElementLayeringTests.TestNoInvalidation(this, () => RepeatText = !RepeatText);
        }
    }
}
