// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using NUnit.Framework;

using System.Linq;

using Xenko.UI.Events;

namespace Xenko.UI.Tests.Events
{
    /// <summary>
    /// Test class for the <see cref="EventManager"/> class.
    /// </summary>
    public class EventManagerTests
    {
        /// <summary>
        /// Launch all the tests of <see cref="EventManagerTests"/>
        /// </summary>
        public void TestAll()
        {
            Initialize();
            TestClassHandler();
            TestRoutedEvent();
        }

        private void ResetState()
        {
            EventManager.ResetRegisters();
        }

        /// <summary>
        /// Initialize the test series
        /// </summary>
        [TestFixtureSetUp]
        public void Initialize()
        {
            ResetState();

            testBasicHandler = EventManager.RegisterRoutedEvent<RoutedEventArgs>("TestBasicHandler", RoutingStrategy.Tunnel, typeof(EventManagerTests));
            testSpecialHandler = EventManager.RegisterRoutedEvent<MyTestRoutedEventArgs>("TestSpecialHandler", RoutingStrategy.Tunnel, typeof(EventManagerTests));
        }

        private RoutedEvent<RoutedEventArgs> testBasicHandler;
        private RoutedEvent<MyTestRoutedEventArgs> testSpecialHandler;

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
        [Test]
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
            Assert.AreEqual((EventHandler<RoutedEventArgs>)TestRoutedEventHandler, EventManager.GetClassHandler(typeof(EventManagerTests), testBasicHandler).Handler);
            EventManager.RegisterClassHandler(typeof(EventManagerTests), testSpecialHandler, TestMyTestRoutedEventHandler);
            Assert.AreEqual((EventHandler<MyTestRoutedEventArgs>)TestMyTestRoutedEventHandler, EventManager.GetClassHandler(typeof(EventManagerTests), testSpecialHandler).Handler);

            // test propagation of the handlers via inheritance 
            EventManager.RegisterClassHandler(typeof(Child), testBasicHandler, TestRoutedEventHandler);
            Assert.AreEqual((EventHandler<RoutedEventArgs>)TestRoutedEventHandler, EventManager.GetClassHandler(typeof(Child), testBasicHandler).Handler);
            Assert.AreEqual((EventHandler<RoutedEventArgs>)TestRoutedEventHandler, EventManager.GetClassHandler(typeof(GrandChild), testBasicHandler).Handler);
            Assert.AreEqual(null, EventManager.GetClassHandler(typeof(Parent), testBasicHandler));
            
            // test the value of HandledEventsToo
            EventManager.RegisterClassHandler(typeof(EventManagerTests), testBasicHandler, TestRoutedEventHandler);
            Assert.AreEqual(false, EventManager.GetClassHandler(typeof(EventManagerTests), testBasicHandler).HandledEventToo);
            EventManager.RegisterClassHandler(typeof(EventManagerTests), testBasicHandler, TestRoutedEventHandler, true);
            Assert.AreEqual(true, EventManager.GetClassHandler(typeof(EventManagerTests), testBasicHandler).HandledEventToo); 
        }

        /// <summary>
        /// Test function for <see cref="EventManager.RegisterRoutedEvent{T}"/>, <see cref="EventManager.GetRoutedEvent"/>, 
        /// <see cref="EventManager.GetRoutedEvents"/>, <see cref="EventManager.GetRoutedEventsForOwner"/>.
        /// </summary>
        [Test]
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
            Assert.AreEqual(eventName, checkValues.Name);
            Assert.AreEqual(strategy, checkValues.RoutingStrategy);
            Assert.AreEqual(ownerType, checkValues.OwnerType);
            Assert.AreEqual(typeof(RoutedEventArgs), checkValues.HandlerSecondArgumentType);

            // check the get routed events functions
            Assert.AreEqual(4, EventManager.GetRoutedEvents().Length);
            Assert.IsTrue(EventManager.GetRoutedEvents().Contains(checkValues));
            Assert.IsTrue(EventManager.GetRoutedEvents().Contains(testBasicHandler));
            Assert.IsTrue(EventManager.GetRoutedEvents().Contains(testSpecialHandler));
            Assert.IsTrue(EventManager.GetRoutedEvents().Contains(testBasicHandlerOtherOwner));

            // test ownership
            var testChild = EventManager.RegisterRoutedEvent<RoutedEventArgs>("Test", RoutingStrategy.Tunnel, typeof(Child));
            var testGrandChild = EventManager.RegisterRoutedEvent<RoutedEventArgs>("Test2", RoutingStrategy.Tunnel, typeof(GrandChild));
            Assert.AreEqual(0, EventManager.GetRoutedEventsForOwner(typeof(Parent)).Length);
            Assert.AreEqual(1, EventManager.GetRoutedEventsForOwner(typeof(Child)).Length);
            Assert.AreEqual(testChild, EventManager.GetRoutedEventsForOwner(typeof(Child))[0]);
            Assert.AreEqual(2, EventManager.GetRoutedEventsForOwner(typeof(GrandChild)).Length);
            Assert.AreEqual(testGrandChild, EventManager.GetRoutedEventsForOwner(typeof(GrandChild))[0]);
            Assert.AreEqual(testChild, EventManager.GetRoutedEventsForOwner(typeof(GrandChild))[1]);

            // test research by name
            Assert.AreEqual(null, EventManager.GetRoutedEvent(typeof(Parent), "Test"));
            Assert.AreEqual(testChild, EventManager.GetRoutedEvent(typeof(Child), "Test"));
            Assert.AreEqual(testChild, EventManager.GetRoutedEvent(typeof(GrandChild), "Test"));
            Assert.AreEqual(testGrandChild, EventManager.GetRoutedEvent(typeof(GrandChild), "Test2"));
        }
    }
}
