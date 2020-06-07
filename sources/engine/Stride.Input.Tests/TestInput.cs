// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Xunit;
using Stride.Core.Mathematics;
using Stride.Graphics.Regression;

namespace Stride.Input.Tests
{
    public class TestInput : GameTestBase
    {
        public TestInput()
        {
            EnableSimulatedInputSource();
        }

        /// <summary>
        /// Checks keyboard press/release
        /// </summary>
        void TestPressRelease()
        {
            var events = Input.Events;
            var keyboard = KeyboardSimulated;

            keyboard.SimulateDown(Keys.A);
            Input.Update(DrawTime);

            // Test press
            Assert.Equal(1, events.Count);
            Assert.True(events[0] is KeyEvent);
            var keyEvent = (KeyEvent)events[0];
            Assert.True(keyEvent.Key == Keys.A);
            Assert.True(keyEvent.IsDown);
            Assert.True(keyEvent.RepeatCount == 0);
            Assert.True(keyEvent.Device == keyboard);

            // Check pressed/released states
            Assert.True(keyboard.IsKeyPressed(Keys.A));
            Assert.False(keyboard.IsKeyReleased(Keys.A));
            Assert.True(keyboard.IsKeyDown(Keys.A));

            keyboard.SimulateUp(Keys.A);
            Input.Update(DrawTime);

            // Test release
            Assert.Equal(1, events.Count);
            Assert.True(events[0] is KeyEvent);
            keyEvent = (KeyEvent)events[0];
            Assert.True(keyEvent.Key == Keys.A);
            Assert.True(!keyEvent.IsDown);

            // Check pressed/released states
            Assert.False(keyboard.IsKeyPressed(Keys.A));
            Assert.True(keyboard.IsKeyReleased(Keys.A));
            Assert.False(keyboard.IsKeyDown(Keys.A));
        }

        /// <summary>
        /// Checks reported events and state when key repeats occur
        /// </summary>
        void TestRepeat()
        {
            var events = Input.Events;
            var keyboard = KeyboardSimulated;

            keyboard.SimulateDown(Keys.A);
            Input.Update(DrawTime);
            keyboard.SimulateDown(Keys.A);
            Input.Update(DrawTime);
            keyboard.SimulateDown(Keys.A);
            Input.Update(DrawTime);
            keyboard.SimulateDown(Keys.A);
            Input.Update(DrawTime);

            // Test press with release
            Assert.Equal(1, events.Count);
            Assert.True(events[0] is KeyEvent);
            var keyEvent = (KeyEvent)events[0];
            Assert.True(keyEvent.Key == Keys.A);
            Assert.True(keyEvent.IsDown);
            Assert.True(keyEvent.RepeatCount == 3);
            Assert.True(keyEvent.Device == keyboard);

            // Check pressed/released states (Pressed events should still be sent when repeating)
            Assert.True(keyboard.IsKeyPressed(Keys.A));
            Assert.False(keyboard.IsKeyReleased(Keys.A));
            Assert.True(keyboard.IsKeyDown(Keys.A));

            keyboard.SimulateUp(Keys.A);
            Input.Update(DrawTime);

            // Test release
            Assert.Equal(1, events.Count);
            Assert.True(events[0] is KeyEvent);
            keyEvent = (KeyEvent)events[0];
            Assert.True(keyEvent.Key == Keys.A);
            Assert.True(!keyEvent.IsDown);
        }

        /// <summary>
        /// Checks mouse and pointer events
        /// </summary>
        void TestMouse()
        {
            var mouse = MouseSimulated;

            Vector2 targetPosition;
            mouse.SetPosition(targetPosition = new Vector2(0.5f, 0.5f));
            Input.Update(DrawTime);

            Assert.Equal(targetPosition, Input.MousePosition);

            mouse.SetPosition(targetPosition = new Vector2(0.6f, 0.5f));
            mouse.SimulateMouseDown(MouseButton.Left);
            Input.Update(DrawTime);

            // Check for pointer events (2, 1 move, 1 down)
            Assert.Equal(2, Input.PointerEvents.Count);
            Assert.Equal(PointerEventType.Moved, Input.PointerEvents[0].EventType);
            Assert.False(Input.PointerEvents[0].IsDown);

            // Check down
            Assert.Equal(PointerEventType.Pressed, Input.PointerEvents[1].EventType);
            Assert.True(Input.PointerEvents[1].IsDown);

            // Check pressed/released states
            Assert.True(mouse.IsButtonPressed(MouseButton.Left));
            Assert.False(mouse.IsButtonReleased(MouseButton.Left));
            Assert.True(mouse.IsButtonDown(MouseButton.Left));

            Assert.Equal(1, mouse.PressedPointers.Count);
            Assert.Equal(0, mouse.ReleasedPointers.Count);
            Assert.Equal(1, mouse.DownPointers.Count);

            // Check delta
            Assert.Equal(new Vector2(0.1f, 0.0f), Input.PointerEvents[0].DeltaPosition);
            Assert.Equal(new Vector2(0.0f, 0.0f), Input.PointerEvents[1].DeltaPosition);

            // And the position after that, when the pointer goes down
            Assert.Equal(targetPosition, Input.PointerEvents[1].Position);

            // Check if new absolute delta matches the one reported in the input manager
            Assert.Equal(Input.PointerEvents[0].AbsoluteDeltaPosition, Input.AbsoluteMouseDelta);

            mouse.SimulateMouseUp(MouseButton.Left);
            Input.Update(DrawTime);

            // Check up
            Assert.Equal(1, Input.PointerEvents.Count);
            Assert.Equal(PointerEventType.Released, Input.PointerEvents[0].EventType);
            Assert.False(Input.PointerEvents[0].IsDown);

            // Check pressed/released states
            Assert.False(mouse.IsButtonPressed(MouseButton.Left));
            Assert.True(mouse.IsButtonReleased(MouseButton.Left));
            Assert.False(mouse.IsButtonDown(MouseButton.Left));

            Assert.Equal(0, mouse.PressedPointers.Count);
            Assert.Equal(1, mouse.ReleasedPointers.Count);
            Assert.Equal(0, mouse.DownPointers.Count);
        }

        /// <summary>
        /// Checks if the pressed/released states work correctly when the occur on the same frame
        /// </summary>
        void TestSingleFrameStates()
        {
            var keyboard = KeyboardSimulated;

            keyboard.SimulateDown(Keys.Space);
            keyboard.SimulateUp(Keys.Space);
            Input.Update(DrawTime);

            Assert.True(Input.IsKeyPressed(Keys.Space));
            Assert.True(Input.IsKeyReleased(Keys.Space));
            Assert.False(Input.IsKeyDown(Keys.Space));

            var mouse = MouseSimulated;

            mouse.SimulateMouseDown(MouseButton.Extended2);
            mouse.SimulateMouseUp(MouseButton.Extended2);
            Input.Update(DrawTime);

            Assert.True(Input.IsMouseButtonPressed(MouseButton.Extended2));
            Assert.True(Input.IsMouseButtonReleased(MouseButton.Extended2));
            Assert.False(Input.IsMouseButtonDown(MouseButton.Extended2));

            mouse.SimulateMouseDown(MouseButton.Left);
            mouse.SimulateMouseUp(MouseButton.Left);
            mouse.SimulateMouseDown(MouseButton.Left);
            Input.Update(DrawTime);

            Assert.True(Input.IsMouseButtonPressed(MouseButton.Left));
            Assert.True(Input.IsMouseButtonReleased(MouseButton.Left));
            Assert.True(Input.IsMouseButtonDown(MouseButton.Left));

            mouse.SimulateMouseUp(MouseButton.Left);
            Input.Update(DrawTime);
        }

        /// <summary>
        /// Checks adding/removal of keyboard and mouse
        /// </summary>
        void TestConnectedDevices()
        {
            Assert.True(Input.HasMouse);
            Assert.NotNull(Input.Mouse);
            Assert.True(Input.HasPointer);
            Assert.NotNull(Input.Pointer);
            Assert.True(Input.HasKeyboard);
            Assert.NotNull(Input.Keyboard);
            Assert.False(Input.HasGamePad);
            Assert.False(Input.HasGameController);

            bool keyboardAdded = false;
            bool keyboardRemoved = false;

            Input.DeviceRemoved += (sender, args) =>
            {
                Assert.Equal(DeviceChangedEventType.Removed, args.Type);
                if (args.Device is KeyboardSimulated)
                    keyboardRemoved = true;
            };
            Input.DeviceAdded += (sender, args) =>
            {
                Assert.Equal(DeviceChangedEventType.Added, args.Type);
                if (args.Device is KeyboardSimulated)
                    keyboardAdded = true;
            };

            // Check keyboard removal
            InputSourceSimulated.RemoveAllKeyboards();
            Assert.True(keyboardRemoved);
            Assert.False(keyboardAdded);
            Assert.Null(Input.Keyboard);
            Assert.False(Input.HasKeyboard);

            // Check keyboard addition
            InputSourceSimulated.AddKeyboard();
            Assert.True(keyboardAdded);
            Assert.NotNull(Input.Keyboard);
            Assert.True(Input.HasKeyboard);

            // Test not crashing with no keyboard/mouse in a few update loops
            InputSourceSimulated.RemoveAllKeyboards();
            InputSourceSimulated.RemoveAllMice();

            for(int i = 0; i < 3; i++)
                Input.Update(DrawTime);

            // Reset input source state
            EnableSimulatedInputSource();
        }

        /// <summary>
        /// Checks reported mouse delta and position with cursor position locked
        /// </summary>
        void TestLockedMousePosition()
        {
            var mouse = MouseSimulated;
            mouse.LockPosition(true);
            Input.Update(DrawTime);

            Assert.Equal(new Vector2(0.5f), mouse.Position);
            Input.Update(DrawTime);

            Assert.Equal(new Vector2(0.0f), mouse.Delta);

            // Validate mouse delta with locked position
            mouse.SetPosition(new Vector2(0.6f, 0.5f));
            Input.Update(DrawTime);
            Assert.Equal(new Vector2(0.1f, 0.0f), mouse.Delta);
            Input.Update(DrawTime);
            Assert.Equal(new Vector2(0.0f, 0.0f), mouse.Delta);
            Assert.Equal(new Vector2(0.5f, 0.5f), mouse.Position);

            mouse.UnlockPosition();

            // Validate mouse delta with unlocked position
            mouse.SetPosition(new Vector2(0.6f, 0.5f));
            Input.Update(DrawTime);
            Assert.Equal(new Vector2(0.1f, 0.0f), mouse.Delta);
            Assert.Equal(new Vector2(0.6f, 0.5f), mouse.Position);
        }

        /// <summary>
        /// Checks adding/removing gamepads and index assignment
        /// </summary>
        void TestGamePad()
        {
            Assert.Equal(0, InputSourceSimulated.GamePads.Count);
            Assert.False(Input.HasGamePad);
            Assert.Null(Input.DefaultGamePad);

            var gamePad0 = InputSourceSimulated.AddGamePad();

            Assert.Equal(1, Input.GamePadCount);
            Assert.True(Input.HasGamePad);
            Assert.NotNull(Input.DefaultGamePad);

            // Add another gamepad
            var gamePad1 = InputSourceSimulated.AddGamePad();

            // Test automatic index assignment
            Assert.Equal(0, gamePad0.Index);
            Assert.Equal(1, gamePad1.Index);

            Assert.Single(Input.GetGamePadsByIndex(0));
            Assert.Single(Input.GetGamePadsByIndex(1));

            // Test putting both gamepads on the same index
            gamePad1.Index = 0;
            Assert.Equal(2, Input.GetGamePadsByIndex(0).Count());
            Assert.Empty(Input.GetGamePadsByIndex(1));

            // Test reassign suggestions
            gamePad1.Index = Input.GetFreeGamePadIndex(gamePad1);
            gamePad0.Index = Input.GetFreeGamePadIndex(gamePad0);
            Assert.True(gamePad1.Index == 0 || gamePad0.Index == 0);
            Assert.True(gamePad0.Index != gamePad1.Index);

            // Test button states
            gamePad0.SetButton(GamePadButton.A, true);
            Input.Update(DrawTime);

            Assert.True(gamePad0.IsButtonPressed(GamePadButton.A));
            Assert.False(gamePad0.IsButtonReleased(GamePadButton.A));
            Assert.True(gamePad0.IsButtonDown(GamePadButton.A));

            gamePad0.SetButton(GamePadButton.A, false);
            Input.Update(DrawTime);

            Assert.False(gamePad0.IsButtonPressed(GamePadButton.A));
            Assert.True(gamePad0.IsButtonReleased(GamePadButton.A));
            Assert.False(gamePad0.IsButtonDown(GamePadButton.A));

            InputSourceSimulated.RemoveGamePad(gamePad0);
            InputSourceSimulated.RemoveGamePad(gamePad1);

            Assert.Equal(0, Input.GamePadCount);
        }

        /// <summary>
        /// Checks whether VirtualButton.Find works for Keyboard, Mouse and GamePad.
        /// </summary>
        void TestVirtualButtonFind()
        {
            // Test Keyboard Keys
            Assert.Equal(VirtualButton.Keyboard.A, VirtualButton.Find("Keyboard.a"));
            Assert.Equal(VirtualButton.Keyboard.Z, VirtualButton.Find("Keyboard.z"));
            Assert.Equal(VirtualButton.Keyboard.LeftCtrl, VirtualButton.Find("Keyboard.leftctrl"));
            Assert.Equal(VirtualButton.Keyboard.LeftShift, VirtualButton.Find("Keyboard.leftshift"));

            // Test Mouse Buttons
            Assert.Equal(VirtualButton.Mouse.Left, VirtualButton.Find("Mouse.Left"));
            Assert.Equal(VirtualButton.Mouse.PositionX, VirtualButton.Find("Mouse.PositionX"));
            Assert.Equal(VirtualButton.Mouse.DeltaY, VirtualButton.Find("Mouse.DeltaY"));

            // Test GamePad Buttons
            Assert.Equal(VirtualButton.GamePad.LeftThumbAxisX, VirtualButton.Find("GamePad.LeftThumbAxisX"));
            Assert.Equal(VirtualButton.GamePad.RightThumb, VirtualButton.Find("GamePad.RightThumb"));
            Assert.Equal(VirtualButton.GamePad.A, VirtualButton.Find("GamePad.A"));
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Update(TestConnectedDevices);
            FrameGameSystem.Update(TestPressRelease);
            FrameGameSystem.Update(TestRepeat);
            FrameGameSystem.Update(TestMouse);
            FrameGameSystem.Update(TestLockedMousePosition);
            FrameGameSystem.Update(TestSingleFrameStates);
            FrameGameSystem.Update(TestGamePad);
            FrameGameSystem.Update(TestVirtualButtonFind);
        }

        [Fact]
        public static void RunInputTest()
        {
            RunGameTest(new TestInput());
        }
    }
}
