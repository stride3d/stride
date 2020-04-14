// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.UI.Tests.Events;
using Xenko.UI.Tests.Layering;

namespace Xenko.UI.Tests
{
    public class Program
    {
        public static void Main()
        {
            var uiElementTest = new UIElementLayeringTests();
            uiElementTest.TestAll();

            var panelTest = new PanelTests();
            panelTest.TestAll();

            var controlTest = new ControlTests();
            controlTest.TestAll();

            var stackPanelTest = new StackPanelTests();
            stackPanelTest.TestAll();

            var canvasTest = new CanvasTests();
            canvasTest.TestAll();

            var contentControlTest = new ContentControlTest();
            contentControlTest.TestAll();

            var eventManagerTest = new EventManagerTests();
            eventManagerTest.TestAll();

            var routedEventArgTest = new RoutedEventArgsTest();
            routedEventArgTest.TestAll();

            var uiElementEventTest = new UIElementEventTests();
            uiElementEventTest.TestAll();

            var gridTest = new GridTests();
            gridTest.TestAll();
        }
    }
}
