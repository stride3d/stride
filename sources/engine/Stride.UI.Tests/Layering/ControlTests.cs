// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

using Stride.UI.Controls;

namespace Stride.UI.Tests.Layering
{
    /// <summary>
    /// Unit tests for <see cref="Control"/>
    /// </summary>
    public class ControlTests : Control
    {
        protected override IEnumerable<IUIElementChildren> EnumerateChildren()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Launch all the tests contained in <see cref="ControlTests"/>
        /// </summary>
        internal void TestAll()
        {
            TestProperties();
        }

        [Fact]
        public void TestProperties()
        {
            var control = new ControlTests();

            // test properties default values
            Assert.Equal(Thickness.UniformCuboid(0), control.Padding);
        }
        
        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Fact]
        public void TestBasicInvalidations()
        {
            // - test the properties that are not supposed to invalidate the object layout state

            UIElementLayeringTests.TestMeasureInvalidation(this, () => Padding = Thickness.UniformRectangle(23));
        }
    }
}
