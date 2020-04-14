// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Xunit;

using Xenko.UI.Controls;
using Xenko.UI.Events;
using Xenko.UI.Panels;
using Xenko.UI.Tests.Layering;

namespace Xenko.UI.Tests.Events
{
    /// <summary>
    /// A class that contains test functions for layering of the UIElement class.
    /// </summary>
    [System.ComponentModel.Description("Tests for UIElement events")]
    public class UIElementEventTests : UIElement
    {
        protected override IEnumerable<IUIElementChildren> EnumerateChildren()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs all the tests
        /// </summary>
        internal void TestAll()
        {
            TestAddRemoveHandler();
            TestRaiseEvent();
            TestPropagateEvent();
        }

        private void TestDelegate2(Object sender, RoutedEventArgs args)
        {
        }

        /// <summary>
        /// Tests for functions <see cref="UIElement.AddHandler{T}"/> and <see cref="UIElement.RemoveHandler{T}"/>
        /// </summary>
        [Fact]
        public void TestAddRemoveHandler()
        {
            var testRoutedEvent = EventManager.RegisterRoutedEvent<RoutedEventArgs>("Test1", RoutingStrategy.Tunnel, typeof(UIElementLayeringTests));
            var element = new UIElementLayeringTests();

            // test for ArgumentNullException
            Assert.Throws<ArgumentNullException>(() => element.AddHandler<RoutedEventArgs>(null, TestDelegate2));
            Assert.Throws<ArgumentNullException>(() => element.AddHandler(testRoutedEvent, null));
            Assert.Throws<ArgumentNullException>(() => element.RemoveHandler<RoutedEventArgs>(null, TestDelegate2));
            Assert.Throws<ArgumentNullException>(() => element.RemoveHandler(testRoutedEvent, null));

            // test that adding and removing 2 times the same element does not throws any exceptions
            element.AddHandler(testRoutedEvent, TestDelegate2);
            element.AddHandler(testRoutedEvent, TestDelegate2);
            element.RemoveHandler(testRoutedEvent, TestDelegate2);
            element.RemoveHandler(testRoutedEvent, TestDelegate2);
            element.RemoveHandler(testRoutedEvent, TestDelegate2);
        }

        private RoutedEventArgs argsPassedToRaiseEvent = new RoutedEventArgs();

        private static readonly RoutedEvent<RoutedEventArgs> eventPassedToRaiseEvent = EventManager.RegisterRoutedEvent<RoutedEventArgs>("TestRaiseEvent", RoutingStrategy.Tunnel, typeof(UIElementLayeringTests));

        private UIElement sourcePassedToRaiseEvent;

        private bool enteredInTestArgsDelegate;

        private void TestArgsDelegate(object sender, RoutedEventArgs e)
        {
            Assert.Equal(argsPassedToRaiseEvent, e);
            Assert.Equal(eventPassedToRaiseEvent, e.RoutedEvent);
            Assert.Equal(sourcePassedToRaiseEvent, e.Source);
            Assert.Throws<InvalidOperationException>(() => e.Source = null);
            Assert.Throws<InvalidOperationException>(() => e.RoutedEvent = null);
            Assert.False(e.Handled);

            enteredInTestArgsDelegate = true;
        }

        /// <summary>
        /// Test for <see cref="UIElement.RaiseEvent"/>
        /// </summary>
        [Fact]
        public void TestRaiseEvent()
        {
            // Test ArgumentNullException
            Assert.Throws<ArgumentNullException>(() => RaiseEvent(null));

            AddHandler(eventPassedToRaiseEvent, TestArgsDelegate);

            // test that if RoutedEvent of argument is null nothing special happens
            RaiseEvent(new RoutedEventArgs());

            // test the values of the arguments in the delegate
            sourcePassedToRaiseEvent = this;
            argsPassedToRaiseEvent = new RoutedEventArgs(eventPassedToRaiseEvent);
            RaiseEvent(argsPassedToRaiseEvent);
            sourcePassedToRaiseEvent = new UIElementLayeringTests();
            argsPassedToRaiseEvent = new RoutedEventArgs(eventPassedToRaiseEvent, sourcePassedToRaiseEvent);
            RaiseEvent(argsPassedToRaiseEvent);

            // check that the delegate has been called
            Assert.True(enteredInTestArgsDelegate);

            // check that value of the event raised can be modified again after being raised
            argsPassedToRaiseEvent.RoutedEvent = null;
            argsPassedToRaiseEvent.Source = null;

            // test InvalidOperationException
            var eventMyTest = EventManager.RegisterRoutedEvent<MyTestRoutedEventArgs>("myEventTestRaise", RoutingStrategy.Direct, typeof(UIElementLayeringTests));
            Assert.Throws<InvalidOperationException>(() => RaiseEvent(new RoutedEventArgs(eventMyTest)));
        }

        private readonly List<Object> senderList = new List<object>();

        private void TestAddSenderToList(Object sender, RoutedEventArgs e)
        {
            senderList.Add(sender);
        }

        private bool testMyTestHandlerCalled;

        private void TestMyTestHandler(Object sender, MyTestRoutedEventArgs e)
        {
            testMyTestHandlerCalled = true;
        }
        private bool testEventHandledTooCalled;

        private void TestEventHandledHandler(Object sender, RoutedEventArgs e)
        {
            testEventHandledTooCalled = true;
        }

        private void TestHandledHandler(Object sender, RoutedEventArgs e)
        {
            senderList.Add(sender);
            e.Handled = true;
        }


        private readonly List<Object> classHandlerSenderList = new List<object>();

        private void TestAddSenderToClassHandlerList(Object sender, RoutedEventArgs e)
        {
            classHandlerSenderList.Add(sender);
        }
        private void TestClassHandlerHandled(Object sender, RoutedEventArgs e)
        {
            classHandlerSenderList.Add(sender);
            e.Handled = true;
        }

        private bool testClassHandlerEventHandledTooCalled;

        private void TestClassHandlerEventHandled(Object sender, RoutedEventArgs e)
        {
            testClassHandlerEventHandledTooCalled = true;
        }

        /// <summary>
        /// Test for <see cref="UIElement.PropagateRoutedEvent"/>
        /// </summary>
        [Fact]
        public void TestPropagateEvent()
        {
            // create a hierarchy of UIElements
            //               (00)
            //                | 
            //               (10)
            //             /     \
            //          (20)      (21)
            //            |       /  \
            //          (30)    (31)  (32)
            var element00 = new ContentControlTest();
            var element10 = new StackPanel();
            var element20 = new ContentControlTest();
            var element21 = new StackPanel();
            var element30 = new UIElementLayeringTests();
            var element31 = new UIElementLayeringTests();
            var element32 = new UIElementLayeringTests();
            element00.Content = element10;
            element10.Children.Add(element20);
            element10.Children.Add(element21);
            element20.Content = element30;
            element21.Children.Add(element31);
            element21.Children.Add(element32);
            var elements = new List<UIElement> { element00, element10, element20, element21, element30, element31, element32 };

            // create routed events
            var tunnelingEvent = EventManager.RegisterRoutedEvent<RoutedEventArgs>("TestTuneling", RoutingStrategy.Tunnel, typeof(UIElementLayeringTests));
            var bubblingEvent = EventManager.RegisterRoutedEvent<RoutedEventArgs>("TestBubbling", RoutingStrategy.Bubble, typeof(UIElementLayeringTests));
            var directEvent = EventManager.RegisterRoutedEvent<RoutedEventArgs>("TestDirect", RoutingStrategy.Direct, typeof(UIElementLayeringTests));

            // test propagation direction, propagation bounds and sender values
            foreach (var uiElement in elements)
            {
                uiElement.AddHandler(tunnelingEvent, TestAddSenderToList);
                uiElement.AddHandler(bubblingEvent, TestAddSenderToList);
                uiElement.AddHandler(directEvent, TestAddSenderToList);
            }

            // tunneling test 1
            senderList.Clear();
            element20.RaiseEvent(new RoutedEventArgs(tunnelingEvent));
            Assert.Equal(3, senderList.Count);
            Assert.Equal(element00, senderList[0]);
            Assert.Equal(element10, senderList[1]);
            Assert.Equal(element20, senderList[2]);

            // tunneling test 2
            senderList.Clear();
            element31.RaiseEvent(new RoutedEventArgs(tunnelingEvent));
            Assert.Equal(4, senderList.Count);
            Assert.Equal(element00, senderList[0]);
            Assert.Equal(element10, senderList[1]);
            Assert.Equal(element21, senderList[2]);
            Assert.Equal(element31, senderList[3]);

            // direct test
            senderList.Clear();
            element10.RaiseEvent(new RoutedEventArgs(directEvent));
            Assert.Single(senderList);
            Assert.Equal(element10, senderList[0]);

            // tunneling test 1
            senderList.Clear();
            element30.RaiseEvent(new RoutedEventArgs(bubblingEvent));
            Assert.Equal(4, senderList.Count);
            Assert.Equal(element30, senderList[0]);
            Assert.Equal(element20, senderList[1]);
            Assert.Equal(element10, senderList[2]);
            Assert.Equal(element00, senderList[3]);

            // tunneling test 2
            senderList.Clear();
            element20.RaiseEvent(new RoutedEventArgs(bubblingEvent));
            Assert.Equal(3, senderList.Count);
            Assert.Equal(element20, senderList[0]);
            Assert.Equal(element10, senderList[1]);
            Assert.Equal(element00, senderList[2]);

            // test with another type of handler
            var eventMyTestHandler = EventManager.RegisterRoutedEvent<MyTestRoutedEventArgs>("TestMyTestHandler", RoutingStrategy.Direct, typeof(UIElementLayeringTests));
            AddHandler(eventMyTestHandler, TestMyTestHandler);
            RaiseEvent(new MyTestRoutedEventArgs(eventMyTestHandler));
            Assert.True(testMyTestHandlerCalled);

            // test Handled and EventHandledToo
            foreach (var uiElement in elements)
            {
                uiElement.RemoveHandler(bubblingEvent, TestAddSenderToList);
                uiElement.AddHandler(bubblingEvent, TestHandledHandler);
            }
            senderList.Clear();
            element00.AddHandler(bubblingEvent, TestEventHandledHandler, true);
            element32.RaiseEvent(new RoutedEventArgs(bubblingEvent));
            Assert.Single(senderList);
            Assert.Equal(element32, senderList[0]);
            Assert.True(testEventHandledTooCalled);

            // test class handlers basic working
            foreach (var uiElement in elements)
                uiElement.RemoveHandler(bubblingEvent, TestHandledHandler);
            EventManager.RegisterClassHandler(typeof(ContentControl), bubblingEvent, TestAddSenderToClassHandlerList);
            element30.RaiseEvent(new RoutedEventArgs(bubblingEvent));
            Assert.Equal(2, classHandlerSenderList.Count);
            Assert.Equal(element20, classHandlerSenderList[0]);
            Assert.Equal(element00, classHandlerSenderList[1]);

            // test that class handlers are called before instance handlers + test handledEventToo for class handlers
            senderList.Clear();
            classHandlerSenderList.Clear();
            EventManager.RegisterClassHandler(typeof(ContentControl), bubblingEvent, TestClassHandlerHandled);
            EventManager.RegisterClassHandler(typeof(StackPanel), bubblingEvent, TestClassHandlerEventHandled, true);
            foreach (var uiElement in elements)
                uiElement.AddHandler(bubblingEvent, TestAddSenderToList);
            element20.RaiseEvent(new RoutedEventArgs(bubblingEvent));
            Assert.Single(classHandlerSenderList);
            Assert.Equal(element20, classHandlerSenderList[0]);
            Assert.Empty(senderList);
            Assert.True(testClassHandlerEventHandledTooCalled);
        }

        /// <summary>
        /// Test that the handlers can be detached inside the handler itself
        /// </summary>
        [Fact]
        public void TestUnregisterHandlerInsideHandler()
        {
            testUnregisterHandlerCallCount = 0;

            var button = new Button();
            button.Click += TestUnregisterHandlerOnClick;
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

            Assert.Equal(1, testUnregisterHandlerCallCount);
        }

        private int testUnregisterHandlerCallCount;

        private void TestUnregisterHandlerOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            ++testUnregisterHandlerCallCount;

            ((Button)sender).Click -= TestUnregisterHandlerOnClick;

            if (testUnregisterHandlerCallCount < 10) // avoid infinite looping on test fail
                ((Button)sender).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        /// <summary>
        /// Test that the handlers are raised in the same order as they are added.
        /// </summary>
        [Fact]
        public void TestHandlerRaiseOrder()
        {
            lastHandlerCalledId = 0;

            var button = new Button();
            button.Click += TestHandlerRaiseOrderOnClick1;
            button.Click += TestHandlerRaiseOrderOnClick2;
            button.Click += TestHandlerRaiseOrderOnClick3;
            button.Click += TestHandlerRaiseOrderOnClick4;

            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

            Assert.Equal(4, lastHandlerCalledId);
        }

        private int lastHandlerCalledId;

        private void TestHandlerRaiseOrderOnClick1(object sender, RoutedEventArgs routedEventArgs)
        {
            Assert.Equal(0, lastHandlerCalledId);
            lastHandlerCalledId = 1;
        }
        private void TestHandlerRaiseOrderOnClick2(object sender, RoutedEventArgs routedEventArgs)
        {
            Assert.Equal(1, lastHandlerCalledId);
            lastHandlerCalledId = 2;
        }
        private void TestHandlerRaiseOrderOnClick3(object sender, RoutedEventArgs routedEventArgs)
        {
            Assert.Equal(2, lastHandlerCalledId);
            lastHandlerCalledId = 3;
        }
        private void TestHandlerRaiseOrderOnClick4(object sender, RoutedEventArgs routedEventArgs)
        {
            Assert.Equal(3, lastHandlerCalledId);
            lastHandlerCalledId = 4;
        }

        /// <summary>
        /// Test for recursive <see cref="UIElement.RaiseEvent"/>
        /// </summary>
        [Fact]
        public void TestReccursiveRaise()
        {
            clickCount = 0;

            var button = new Button();
            button.Click += TestReccursiveRaiseOnClick;
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

            Assert.Equal(10, clickCount);
        }

        private int clickCount;

        private void TestReccursiveRaiseOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            ++clickCount;

            if (clickCount < 10)
                ((Button)sender).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }
    }
}
