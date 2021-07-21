// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_UI_SDL
using System;
using System.Collections.Generic;
using SDL2;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics.SDL;

namespace Stride.Input
{
    /// <summary>
    /// Class handling finger touch inputs using the SDL backend
    /// </summary>
    internal class PointerSDL : PointerDeviceBase, IDisposable
    {
        private readonly Window uiControl;
        private readonly Dictionary<long, int> touchFingerIndexMap = new Dictionary<long, int>();
        private int touchCounter;

        public PointerSDL(InputSourceSDL source, Window uiControl)
        {
            Source = source;
            this.uiControl = uiControl;

            // Disable Touch-Mouse synthesis
            SDL.SDL_SetHint(SDL.SDL_HINT_TOUCH_MOUSE_EVENTS, "false");

            uiControl.FingerMoveActions += OnFingerMoveEvent;
            uiControl.FingerPressActions += OnFingerPressEvent;
            uiControl.FingerReleaseActions += OnFingerReleaseEvent;

            uiControl.ResizeEndActions += OnSizeChanged;
            OnSizeChanged(new SDL.SDL_WindowEvent());

            Id = InputDeviceUtils.DeviceNameToGuid(uiControl.SdlHandle.ToString() + Name);
        }

        public override string Name => "SDL Pointer";

        public override Guid Id { get; }

        public override IInputSource Source { get; }

        public void Dispose()
        {
            uiControl.FingerMoveActions -= OnFingerMoveEvent;
            uiControl.FingerPressActions -= OnFingerPressEvent;
            uiControl.FingerReleaseActions -= OnFingerReleaseEvent;

            uiControl.ResizeEndActions -= OnSizeChanged;
        }

        private void OnSizeChanged(SDL.SDL_WindowEvent eventArgs)
        {
            SetSurfaceSize(new Vector2(uiControl.ClientSize.Width, uiControl.ClientSize.Height));
        }

        // TODO Code from PointeriOS with slight modifications, consider creating and referencing a utility class
        private int GetFingerId(long touchId, PointerEventType type)
        {
            // Assign finger index (starting at 0) to touch ID
            int touchFingerIndex = 0;
            if (type == PointerEventType.Pressed)
            {
                touchFingerIndex = touchCounter++;
                touchFingerIndexMap.Add(touchId, touchFingerIndex);
            }
            else
            {
                touchFingerIndex = touchFingerIndexMap[touchId];
            }

            // Remove index
            if (type == PointerEventType.Released)
            {
                touchFingerIndexMap.Remove(touchId);
                touchCounter = 0; // Reset touch counter

                // Recalculate next finger index
                if (touchFingerIndexMap.Count > 0)
                {
                    touchFingerIndexMap.ForEach(pair => touchCounter = Math.Max(touchCounter, pair.Value));
                    touchCounter++; // next
                }
            }

            return touchFingerIndex;
        }

        private void HandleFingerEvent(SDL.SDL_TouchFingerEvent e, PointerEventType type)
        {
            var newPosition = new Vector2(e.x, e.y);
            var id = GetFingerId(e.fingerId, type);
            PointerState.PointerInputEvents.Add(new PointerDeviceState.InputEvent { Type = type, Position = newPosition, Id = id });
        }

        private void OnFingerMoveEvent(SDL.SDL_TouchFingerEvent e)
        {
            HandleFingerEvent(e, PointerEventType.Moved);
        }

        private void OnFingerPressEvent(SDL.SDL_TouchFingerEvent e)
        {
            HandleFingerEvent(e, PointerEventType.Pressed);
        }

        private void OnFingerReleaseEvent(SDL.SDL_TouchFingerEvent e)
        {
            HandleFingerEvent(e, PointerEventType.Released);
        }
    }
}
#endif
