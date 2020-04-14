// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;

namespace Xenko.Input
{
    /// <summary>
    /// Class that keeps track of the the global input state of all devices
    /// </summary>
    public partial class InputManager : IInputEventListener<KeyEvent>, 
        IInputEventListener<PointerEvent>, 
        IInputEventListener<MouseWheelEvent>
    {
        private Vector2 mousePosition;

        private Vector2 absoluteMousePosition;
        
        private readonly ReadOnlySet<MouseButton> NoButtons = new ReadOnlySet<MouseButton>(new HashSet<MouseButton>());
        private readonly ReadOnlySet<Keys> NoKeys = new ReadOnlySet<Keys>(new HashSet<Keys>());

        private readonly List<KeyEvent> keyEvents = new List<KeyEvent>();
        
        private readonly List<PointerEvent> pointerEvents = new List<PointerEvent>();
        
        /// <summary>
        /// The mouse position in normalized coordinates.
        /// </summary>
        public Vector2 MousePosition
        {
            get { return mousePosition; }
            set { SetMousePosition(value); }
        }

        /// <summary>
        /// Mouse coordinates in device coordinates
        /// </summary>
        public Vector2 AbsoluteMousePosition
        {
            get { return absoluteMousePosition; }
        }

        /// <summary>
        /// Mouse delta in normalized coordinate space
        /// </summary>
        public Vector2 MouseDelta { get; private set; }

        /// <summary>
        /// Mouse movement in device coordinates
        /// </summary>
        public Vector2 AbsoluteMouseDelta { get; private set; }
        
        /// <summary>
        /// The delta value of the mouse wheel button since last frame.
        /// </summary>
        public float MouseWheelDelta { get; private set; }

        /// <summary>
        /// Device that is responsible for setting the current <see cref="MouseDelta"/> and <see cref="MousePosition"/>
        /// </summary>
        public IPointerDevice LastPointerDevice { get; private set; }

        /// <summary>
        /// Determines whether one or more keys are pressed
        /// </summary>
        /// <returns><c>true</c> if one or more keys are pressed; otherwise, <c>false</c>.</returns>
        public bool HasPressedKeys
        {
            get
            {
                if (!HasKeyboard) return false;
                return Keyboard.PressedKeys.Count > 0;
            }
        }

        /// <summary>
        /// Determines whether one or more keys are released
        /// </summary>
        /// <returns><c>true</c> if one or more keys are released; otherwise, <c>false</c>.</returns>
        public bool HasReleasedKeys
        {
            get
            {
                if (!HasKeyboard) return false;
                return Keyboard.ReleasedKeys.Count > 0;
            }
        }

        /// <summary>
        /// Determines whether one or more keys are down
        /// </summary>
        /// <returns><c>true</c> if one or more keys are down; otherwise, <c>false</c>.</returns>
        public bool HasDownKeys
        {
            get
            {
                if (!HasKeyboard) return false;
                return Keyboard.DownKeys.Count > 0;
            }
        }

        /// <summary>
        /// The keys that have been pressed since the last frame
        /// </summary>
        public IReadOnlySet<Keys> PressedKeys
        {
            get
            {
                if (!HasKeyboard) return NoKeys;
                return Keyboard.PressedKeys;
            }
        }

        /// <summary>
        /// The keys that have been released since the last frame
        /// </summary>
        public IReadOnlySet<Keys> ReleasedKeys
        {
            get
            {
                if (!HasKeyboard) return NoKeys;
                return Keyboard.ReleasedKeys;
            }
        }

        /// <summary>
        /// The keys that are down
        /// </summary>
        public IReadOnlySet<Keys> DownKeys
        {
            get
            {
                if (!HasKeyboard) return NoKeys;
                return Keyboard.DownKeys;
            }
        }

        /// <summary>
        /// The mouse buttons that have been pressed since the last frame
        /// </summary>
        public IReadOnlySet<MouseButton> PressedButtons
        {
            get
            {
                if (!HasMouse) return NoButtons;
                return Mouse.PressedButtons;
            }
        }

        /// <summary>
        /// The mouse buttons that have been released since the last frame
        /// </summary>
        public IReadOnlySet<MouseButton> ReleasedButtons
        {
            get
            {
                if (!HasMouse) return NoButtons;
                return Mouse.ReleasedButtons;
            }
        }

        /// <summary>
        /// The mouse buttons that are down
        /// </summary>
        public IReadOnlySet<MouseButton> DownButtons
        {
            get
            {
                if (!HasMouse) return NoButtons;
                return Mouse.DownButtons;
            }
        }

        /// <summary>
        /// Determines whether one or more of the mouse buttons are pressed
        /// </summary>
        /// <returns><c>true</c> if one or more of the mouse buttons are pressed; otherwise, <c>false</c>.</returns>
        public bool HasPressedMouseButtons
        {
            get
            {
                if (!HasMouse) return false;
                return Mouse.PressedButtons.Count > 0;
            }
        }

        /// <summary>
        /// Determines whether one or more of the mouse buttons are released
        /// </summary>
        /// <returns><c>true</c> if one or more of the mouse buttons are released; otherwise, <c>false</c>.</returns>
        public bool HasReleasedMouseButtons
        {
            get
            {
                if (!HasMouse) return false;
                return Mouse.ReleasedButtons.Count > 0;
            }
        }

        /// <summary>
        /// Determines whether one or more of the mouse buttons are down
        /// </summary>
        /// <returns><c>true</c> if one or more of the mouse buttons are down; otherwise, <c>false</c>.</returns>
        public bool HasDownMouseButtons
        {
            get
            {
                if (!HasMouse) return false;
                return Mouse.DownButtons.Count > 0;
            }
        }

        /// <summary>
        /// Pointer events that happened since the last frame
        /// </summary>
        public IReadOnlyList<PointerEvent> PointerEvents => pointerEvents;

        /// <summary>
        /// Key events that happened since the last frame
        /// </summary>
        public IReadOnlyList<KeyEvent> KeyEvents => keyEvents;

        public void ProcessEvent(KeyEvent inputEvent)
        {
            keyEvents.Add(inputEvent);
        }

        public void ProcessEvent(PointerEvent inputEvent)
        {
            pointerEvents.Add(inputEvent);

            // Update position and delta from whatever device sends position updates
            LastPointerDevice = inputEvent.Pointer;

            if (inputEvent.Device is IMouseDevice)
            {
                mousePosition = inputEvent.Position;
                absoluteMousePosition = inputEvent.AbsolutePosition;

                // Add deltas together, so nothing gets lost if a down events gets sent after a move event with the actual delta
                MouseDelta += inputEvent.DeltaPosition;
                AbsoluteMouseDelta += inputEvent.AbsoluteDeltaPosition;
            }
        }

        public void ProcessEvent(MouseWheelEvent inputEvent)
        {
            if (Math.Abs(inputEvent.WheelDelta) > Math.Abs(MouseWheelDelta))
            {
                MouseWheelDelta = inputEvent.WheelDelta;
            }
        }

        /// <summary>
        /// Determines whether the specified key is pressed since the previous update.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if the specified key is pressed; otherwise, <c>false</c>.</returns>
        public bool IsKeyPressed(Keys key)
        {
            return Keyboard?.IsKeyPressed(key) ?? false;
        }

        /// <summary>
        /// Determines whether the specified key is released since the previous update.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if the specified key is released; otherwise, <c>false</c>.</returns>
        public bool IsKeyReleased(Keys key)
        {
            return Keyboard?.IsKeyReleased(key) ?? false;
        }

        /// <summary>
        /// Determines whether the specified key is being pressed down.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if the specified key is being pressed down; otherwise, <c>false</c>.</returns>
        public bool IsKeyDown(Keys key)
        {
            return Keyboard?.IsKeyDown(key) ?? false;
        }

        /// <summary>
        /// Determines whether the specified mouse button is pressed since the previous update.
        /// </summary>
        /// <param name="mouseButton">The mouse button.</param>
        /// <returns><c>true</c> if the specified mouse button is pressed since the previous update; otherwise, <c>false</c>.</returns>
        public bool IsMouseButtonPressed(MouseButton mouseButton)
        {
            return Mouse?.IsButtonPressed(mouseButton) ?? false;
        }

        /// <summary>
        /// Determines whether the specified mouse button is released.
        /// </summary>
        /// <param name="mouseButton">The mouse button.</param>
        /// <returns><c>true</c> if the specified mouse button is released; otherwise, <c>false</c>.</returns>
        public bool IsMouseButtonReleased(MouseButton mouseButton)
        {
            return Mouse?.IsButtonReleased(mouseButton) ?? false;
        }

        /// <summary>
        /// Determines whether the specified mouse button is being pressed down.
        /// </summary>
        /// <param name="mouseButton">The mouse button.</param>
        /// <returns><c>true</c> if the specified mouse button is being pressed down; otherwise, <c>false</c>.</returns>
        public bool IsMouseButtonDown(MouseButton mouseButton)
        {
            return Mouse?.IsButtonDown(mouseButton) ?? false;
        }

        /// <summary>
        /// Resets the state before updating
        /// </summary>
        private void ResetGlobalInputState()
        {
            keyEvents.Clear();
            pointerEvents.Clear();
            MouseWheelDelta = 0;
            MouseDelta = Vector2.Zero;
            AbsoluteMouseDelta = Vector2.Zero;
        }
    }
}
