// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.UI.Controls;

namespace Stride.UI.Tests.Layering
{
    /// <summary>
    /// A class that contains test functions for layering of the <see cref="TextBlock"/> class.
    /// </summary>
    [System.ComponentModel.Description("Tests for TextBlock layering")]
    public class TextBlockTests : TextBlock
    {
        private class DummyFont : SpriteFont { }

        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Fact]
        public void TestBasicInvalidations()
        {
            // - test the properties that are supposed to invalidate the object measurement
            UIElementLayeringTests.TestMeasureInvalidation(this, () => Font = new DummyFont());
            Font = null;
            UIElementLayeringTests.TestMeasureInvalidation(this, () => Text = "New Text");

            // - test the properties that are not supposed to invalidate the object layout state
            UIElementLayeringTests.TestNoInvalidation(this, () => TextColor = new Color(1, 2, 3, 4));
        }
    }
}
