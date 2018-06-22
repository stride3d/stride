// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xunit;

using Xenko.UI.Controls;
using Xenko.UI.Events;

namespace Xenko.UI.Tests.Events
{
    /// <summary>
    /// class used to test <see cref="RoutedEventArgs"/>
    /// </summary>
    public class RoutedEventArgsTest : RoutedEventArgs
    {
        /// <summary>
        /// Launch all tests of the class
        /// </summary>
        public void TestAll()
        {
            TestEventFreezing();
        }

        /// <summary>
        /// Test that when the event is marked as routed its values cannot be modified any more
        /// </summary>
        [Fact]
        public void TestEventFreezing()
        {
            // check that values can freely be modified by default
            Assert.Equal(false, IsBeingRouted);
            var image = new ImageElement();
            var routedEvent = EventManager.RegisterRoutedEvent<RoutedEventArgs>("test", RoutingStrategy.Tunnel, typeof(RoutedEventArgsTest));
            Source = image;
            Assert.Equal(image, Source);
            Source = null;
            Assert.Equal(null, Source);
            RoutedEvent = routedEvent;
            Assert.Equal(routedEvent, RoutedEvent);
            RoutedEvent = null;
            Assert.Equal(null, RoutedEvent);

            // check that value of IsBeingRouted is updated
            StartEventRouting();
            Assert.Equal(true, IsBeingRouted);

            // check that modifications are now prohibited
            Assert.Throws<InvalidOperationException>(() => Source = null);
            Assert.Throws<InvalidOperationException>(() => RoutedEvent = null);

            // check that value of IsBeingRouted is update
            EndEventRouting();
            Assert.Equal(false, IsBeingRouted);
        }
    }
}
