// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xunit;

using Stride.UI.Controls;
using Stride.UI.Events;

namespace Stride.UI.Tests.Events
{
    /// <summary>
    /// class used to test <see cref="RoutedEventArgs"/>
    /// </summary>
    public class RoutedEventArgsTest : RoutedEventArgs
    {
        /// <summary>
        /// Launch all tests of the class
        /// </summary>
        internal void TestAll()
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
            Assert.False(IsBeingRouted);
            var image = new ImageElement();
            var routedEvent = EventManager.RegisterRoutedEvent<RoutedEventArgs>("test", RoutingStrategy.Tunnel, typeof(RoutedEventArgsTest));
            Source = image;
            Assert.Equal(image, Source);
            Source = null;
            Assert.Null(Source);
            RoutedEvent = routedEvent;
            Assert.Equal(routedEvent, RoutedEvent);
            RoutedEvent = null;
            Assert.Null(RoutedEvent);

            // check that value of IsBeingRouted is updated
            StartEventRouting();
            Assert.True(IsBeingRouted);

            // check that modifications are now prohibited
            Assert.Throws<InvalidOperationException>(() => Source = null);
            Assert.Throws<InvalidOperationException>(() => RoutedEvent = null);

            // check that value of IsBeingRouted is update
            EndEventRouting();
            Assert.False(IsBeingRouted);
        }
    }
}
