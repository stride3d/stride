// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using NUnit.Framework;

using Xenko.UI.Controls;

namespace Xenko.UI.Tests.Layering
{
    /// <summary>
    /// A class that contains test functions for layering of the <see cref="ImageButton"/> class.
    /// </summary>
    [TestFixture, Ignore("ImageButton is deprecated")]
    [System.ComponentModel.Description("Tests for ImageButton layering")]
    public class ImageButtonTests : ImageButton
    {
        [Test]
        public void TestProperties()
        {
            var control = new ImageButton();

            // test properties default values
            Assert.AreEqual(new Thickness(0, 0, 0, 0), control.Padding);
        }
    }
}
