// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_UI_SDL
using System;
using SDL2;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics.SDL;

namespace Stride.Input
{
    internal class MouseSDL : MouseDeviceBase, IDisposable
    {
        private readonly GameBase game;
        private readonly Window uiControl;

        private bool isMousePositionLocked;
        private Point relativeCapturedPosition;

        public MouseSDL(InputSourceSDL source, GameBase game, Window uiControl)
        {
            Source = source;
            this.game = game;
            this.uiControl = uiControl;
            
            uiControl.MouseMoveActions += OnMouseMoveEvent;
            uiControl.PointerButtonPressActions += OnMouseInputEvent;
            uiControl.PointerButtonReleaseActions += OnMouseInputEvent;
            uiControl.MouseWheelActions += OnMouseWheelEvent;
            uiControl.ResizeEndActions += OnSizeChanged;
            OnSizeChanged(new SDL.SDL_WindowEvent());
        }
        
        public override string Name => "SDL Mouse";

        public override Guid Id => new Guid("0ccaf48e-e371-4b34-b6bb-a3720f6742a8");

        public override bool IsPositionLocked => isMousePositionLocked;

        public override IInputSource Source { get; }

        public void Dispose()
        {
            uiControl.MouseMoveActions -= OnMouseMoveEvent;
            uiControl.PointerButtonPressActions -= OnMouseInputEvent;
            uiControl.PointerButtonReleaseActions -= OnMouseInputEvent;
            uiControl.MouseWheelActions -= OnMouseWheelEvent;
            uiControl.ResizeEndActions -= OnSizeChanged;
        }

        public override void LockPosition(bool forceCenter = false)
        {
            if (!IsPositionLocked)
            {
                if (forceCenter)
                {
                    relativeCapturedPosition = new Point(uiControl.ClientSize.Width / 2, uiControl.ClientSize.Height / 2);
                }
                else
                {
                    relativeCapturedPosition = uiControl.RelativeCursorPosition;
                }

                uiControl.SetRelativeMouseMode(true);

                isMousePositionLocked = true;
            }
        }

        public override void UnlockPosition()
        {
            if (IsPositionLocked)
            {
                uiControl.SetRelativeMouseMode(false);
                uiControl.RelativeCursorPosition = relativeCapturedPosition;
                isMousePositionLocked = false;
                relativeCapturedPosition = Point.Zero;
            }
        }

        public override void SetPosition(Vector2 normalizedPosition)
        {
            Vector2 position = normalizedPosition * SurfaceSize;
            uiControl.RelativeCursorPosition = new Point((int)position.X, (int)position.Y);
        }
        
        private void OnSizeChanged(SDL.SDL_WindowEvent eventArgs)
        {
            SetSurfaceSize(new Vector2(uiControl.ClientSize.Width, uiControl.ClientSize.Height));
        }

        private void OnMouseWheelEvent(SDL.SDL_MouseWheelEvent sdlMouseWheelEvent)
        {
            var flip = sdlMouseWheelEvent.direction == (uint)SDL.SDL_MouseWheelDirection.SDL_MOUSEWHEEL_FLIPPED ? -1 : 1;
            MouseState.HandleMouseWheel(sdlMouseWheelEvent.y * flip);
        }

        private void OnMouseInputEvent(SDL.SDL_MouseButtonEvent e)
        {
            MouseButton button = ConvertMouseButton(e.button);

            if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN)
            {
                MouseState.HandleButtonDown(button);
            }
            else
            {
                MouseState.HandleButtonUp(button);
            }
        }

        private void OnMouseMoveEvent(SDL.SDL_MouseMotionEvent e)
        {
            if (IsPositionLocked)
            {
                MouseState.HandleMouseDelta(new Vector2(e.xrel, e.yrel));
            }
            else
            {
                MouseState.HandleMove(new Vector2(e.x, e.y));
            }
        }

        private static MouseButton ConvertMouseButton(uint mouseButton)
        {
            switch (mouseButton)
            {
                case SDL.SDL_BUTTON_LEFT:
                    return MouseButton.Left;
                case SDL.SDL_BUTTON_RIGHT:
                    return MouseButton.Right;
                case SDL.SDL_BUTTON_MIDDLE:
                    return MouseButton.Middle;
                case SDL.SDL_BUTTON_X1:
                    return MouseButton.Extended1;
                case SDL.SDL_BUTTON_X2:
                    return MouseButton.Extended2;
            }

            return (MouseButton)(-1);
        }
    }
}
#endif
