// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xunit;

using System.Linq;

using Stride.UI.Events;

namespace Stride.UI.Tests.Events
{
    /// <summary>
    /// Test class for the <see cref="EventManager"/> class.
    /// </summary>
    public class EventManagerTests : IDisposable
    {
        private int originalRoutedEventCount = EventManager.GetRoutedEvents().Length;
        private RoutedEvent<RoutedEventArgs> testBasicHandler;
        private RoutedEvent<MyTestRoutedEventArgs> testSpecialHandler;

        public EventManagerTests()
        {
            testBasicHandler = EventManager.RegisterRoutedEvent<RoutedEventArgs>("TestBasicHandler", RoutingStrategy.Tunnel, typeof(EventManagerTests));
            testSpecialHandler = EventManager.RegisterRoutedEvent<MyTestRoutedEventArgs>("TestSpecialHandler", RoutingStrategy.Tunnel, typeof(EventManagerTests));
        }

        public void Dispose()
        {
            EventManager.UnregisterRoutedEvent(testBasicHandler);
            EventManager.UnregisterRoutedEvent(testSpecialHandler);
        }

        private void TestRoutedEventHandler(Object sender, RoutedEventArgs e)
        {
        }
        private void TestMyTestRoutedEventHandler(Object sender, MyTestRoutedEventArgs e)
        {
        }

        class Parent
        {
        }

        class Child : Parent
        {
        }

        class GrandChild: Child
        {
        }

        /// <summary>
        /// Tests for <see cref="EventManager.RegisterClassHandler{T}"/> and <see cref="EventManager.GetClassHandler"/>
        /// </summary>
        [Fact]
        public void TestClassHandler()
        {
            // test the ArgumentNullException
            Assert.Throws<ArgumentNullException>(() => EventManager.RegisterClassHandler(null, testBasicHandler, TestRoutedEventHandler));
            Assert.Throws<ArgumentNullException>(() => EventManager.RegisterClassHandler<RoutedEventArgs>(typeof(EventManagerTests), null, TestRoutedEventHandler));
            Assert.Throws<ArgumentNullException>(() => EventManager.RegisterClassHandler(typeof(EventManagerTests), testBasicHandler, null));
            Assert.Throws<ArgumentNullException>(() => EventManager.GetClassHandler(null, testBasicHandler));
            Assert.Throws<ArgumentNullException>(() => EventManager.GetClassHandler(typeof(EventManagerTests), null));
            
            // test that handlers are correctly registered.
            EventManager.RegisterClassHandler(typeof(EventManagerTests), testBasicHandler, TestRoutedEventHandler);
            Assert.Equal((EventHandler<RoutedEventArgs>)TestRoutedEventHandler, EventManager.GetClassHandler(typeof(EventManagerTests), testBasicHandler).Handler);
            EventManager.RegisterClassHandler(typeof(EventManagerTests), testSpecialHandler, TestMyTestRoutedEventHandler);
            Assert.Equal((EventHandler<MyTestRoutedEventArgs>)TestMyTestRoutedEventHandler, EventManager.GetClassHandler(typeof(EventManagerTests), testSpecialHandler).Handler);

            // test propagation of the handlers via inheritance 
            EventManager.RegisterClassHandler(typeof(Child), testBasicHandler, TestRoutedEventHandler);
            Assert.Equal((EventHandler<RoutedEventArgs>)TestRoutedEventHandler, EventManager.GetClassHandler(typeof(Child), testBasicHandler).Handler);
            Assert.Equal((EventHandler<RoutedEventArgs>)TestRoutedEventHandler, EventManager.GetClassHandler(typeof(GrandChild), testBasicHandler).Handler);
            Assert.Null(EventManager.GetClassHandler(typeof(Parent), testBasicHandler));
            
            // test the value of HandledEventsToo
            EventManager.RegisterClassHandler(typeof(EventManagerTests), testBasicHandler, TestRoutedEventHandler);
            Assert.False(EventManager.GetClassHandler(typeof(EventManagerTests), testBasicHandler).HandledEventToo);
            EventManager.RegisterClassHandler(typeof(EventManagerTests), testBasicHandler, TestRoutedEventHandler, true);
            Assert.True(EventManager.GetClassHandler(typeof(EventManagerTests), testBasicHandler).HandledEventToo); 
        }

        /// <summary>
        /// Test function for <see cref="EventManager.RegisterRoutedEvent{T}"/>, <see cref="EventManager.GetRoutedEvent"/>, 
        /// <see cref="EventManager.GetRoutedEvents"/>, <see cref="EventManager.GetRoutedEventsForOwner"/>.
        /// </summary>
        [Fact]
        public void TestRoutedEvent()
        {
            // test argument null exception
            Assert.Throws<ArgumentNullException>(() => EventManager.RegisterRoutedEvent<RoutedEventArgs>(null, RoutingStrategy.Tunnel, typeof(EventManagerTests)));
            Assert.Throws<ArgumentNullException>(() => EventManager.RegisterRoutedEvent<RoutedEventArgs>("Test", RoutingStrategy.Tunnel, null));

            // test InvalidOperationException when element is already added
            Assert.Throws<InvalidOperationException>(() => EventManager.RegisterRoutedEvent<RoutedEventArgs>("TestBasicHandler", RoutingStrategy.Tunnel, typeof(EventManagerTests)));
            Assert.Throws<InvalidOperationException>(() => EventManager.RegisterRoutedEvent<MyTestRoutedEventArgs>("TestBasicHandler", RoutingStrategy.Tunnel, typeof(EventManagerTests)));
            var testBasicHandlerOtherOwner = EventManager.RegisterRoutedEvent<RoutedEventArgs>("TestBasicHandler", RoutingStrategy.Tunnel, typeof(EventManager)); // should not throw owner type is different

            // check the values of the returned routed event
            const string eventName = "CheckValues";
            const RoutingStrategy strategy = RoutingStrategy.Bubble;
            var ownerType = typeof(EventManagerTests);
            var checkValues = EventManager.RegisterRoutedEvent<RoutedEventArgs>(eventName, strategy, ownerType);
            try
            {
                Assert.Equal(eventName, checkValues.Name);
                Assert.Equal(strategy, checkValues.RoutingStrategy);
                Assert.Equal(ownerType, checkValues.OwnerType);
                Assert.Equal(typeof(RoutedEventArgs), checkValues.HandlerSecondArgumentType);

                // check the get routed events functions
                Assert.Equal(4, EventManager.GetRoutedEvents().Length - originalRoutedEventCount);
                Assert.Contains(checkValues, EventManager.GetRoutedEvents());
                Assert.Contains(testBasicHandler, EventManager.GetRoutedEvents());
                Assert.Contains(testSpecialHandler, EventManager.GetRoutedEvents());
                Assert.Contains(testBasicHandlerOtherOwner, EventManager.GetRoutedEvents());
            }
            finally
            {
                EventManager.UnregisterRoutedEvent(checkValues);
                EventManager.UnregisterRoutedEvent(testBasicHandlerOtherOwner);
            }

            // test ownership
            var testChild = EventManager.RegisterRoutedEvent<RoutedEventArgs>("Test", RoutingStrategy.Tunnel, typeof(Child));
            var testGrandChild = EventManager.RegisterRoutedEvent<RoutedEventArgs>("Test2", RoutingStrategy.Tunnel, typeof(GrandChild));

            try
            {
                Assert.Empty(EventManager.GetRoutedEventsForOwner(typeof(Parent)));
                Assert.Single(EventManager.GetRoutedEventsForOwner(typeof(Child)));
                Assert.Equal(testChild, EventManager.GetRoutedEventsForOwner(typeof(Child))[0]);
                Assert.Equal(2, EventManager.GetRoutedEventsForOwner(typeof(GrandChild)).Length);
                Assert.Equal(testGrandChild, EventManager.GetRoutedEventsForOwner(typeof(GrandChild))[0]);
                Assert.Equal(testChild, EventManager.GetRoutedEventsForOwner(typeof(GrandChild))[1]);

                // test research by name
                Assert.Null(EventManager.GetRoutedEvent(typeof(Parent), "Test"));
                Assert.Equal(testChild, EventManager.GetRoutedEvent(typeof(Child), "Test"));
                Assert.Equal(testChild, EventManager.GetRoutedEvent(typeof(GrandChild), "Test"));
                Assert.Equal(testGrandChild, EventManager.GetRoutedEvent(typeof(GrandChild), "Test2"));
            }
            finally
            {
                EventManager.UnregisterRoutedEvent(testChild);
                EventManager.UnregisterRoutedEvent(testGrandChild);
            }
        }
    }
}
