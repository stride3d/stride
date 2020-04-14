// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

using Stride.UI.Controls;

namespace Stride.UI.Tests.Layering
{
    /// <summary>
    /// A class that contains test functions for layering of the <see cref="ImageButton"/> class.
    /// </summary>
    [System.ComponentModel.Description("Tests for ImageButton layering")]
    public class ImageButtonTests
    {
        [Fact(Skip = "ImageButton is deprecated")]
        public void TestProperties()
        {
            var control = new ImageButton();

            // test properties default values
            Assert.Equal(new Thickness(0, 0, 0, 0), control.Padding);
        }
    }
}
