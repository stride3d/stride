// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_WINDOWS_DESKTOP && (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
#if STRIDE_INPUT_RAWINPUT
using SharpDX.RawInput;
#endif
using Stride.Games;
using WinFormsKeys = System.Windows.Forms.Keys;

namespace Stride.Input
{
    /// <summary>
    /// Provides support for mouse and keyboard input on windows forms
    /// </summary>
    internal class InputSourceWinforms : InputSourceBase
    {
        private readonly HashSet<WinFormsKeys> heldKeys = new HashSet<WinFormsKeys>();
        private readonly List<WinFormsKeys> keysToRelease = new List<WinFormsKeys>();
        
        private KeyboardWinforms keyboard;
        private MouseWinforms mouse;

        private IntPtr defaultWndProc;
        private Win32Native.WndProc inputWndProc;

        // My input devices
        private GameContext<Control> gameContext;
        private Control uiControl;
        private InputManager input;

        /// <summary>
        /// Gets the value indicating if the mouse position is currently locked or not.
        /// </summary>
        public bool IsMousePositionLocked { get; protected set; }

        public override void Initialize(InputManager inputManager)
        {
            input = inputManager;
            gameContext = inputManager.Game.Context as GameContext<Control>;
            uiControl = gameContext.Control;
            uiControl.LostFocus += UIControlOnLostFocus;
            MissingInputHack();

            // Hook window proc
            defaultWndProc = Win32Native.GetWindowLong(uiControl.Handle, Win32Native.WindowLongType.WndProc);
            // This is needed to prevent garbage collection of the delegate.
            inputWndProc = WndProc;
            var inputWndProcPtr = Marshal.GetFunctionPointerForDelegate(inputWndProc);
            Win32Native.SetWindowLong(uiControl.Handle, Win32Native.WindowLongType.WndProc, inputWndProcPtr);

            // Do not register keyboard devices when using raw input instead
            keyboard = new KeyboardWinforms(this, uiControl);
            RegisterDevice(keyboard);

            mouse = new MouseWinforms(this, uiControl);
            RegisterDevice(mouse);
        }

        /// <summary>
        /// This function houses a hack to fix the window missing some input events,
        /// see Stride pull #181 for more information (https://github.com/stride3d/stride/pull/181).
        /// TODO: Find a proper solution to replace this workaround.
        /// </summary>
        private void MissingInputHack()
        {
#if STRIDE_INPUT_RAWINPUT
            Device.RegisterDevice(SharpDX.Multimedia.UsagePage.Generic, SharpDX.Multimedia.UsageId.GenericKeyboard, DeviceFlags.None);
            Device.KeyboardInput += (sender, args) =>
            {
                switch (args.State)
                {
                    case KeyState.SystemKeyDown:
                    case KeyState.ImeKeyDown:
                    case KeyState.KeyDown:
                    {
                        keyboard?.HandleKeyUp(args.Key);
                        heldKeys.Add(args.Key);
                        break;
                    }
                    case KeyState.SystemKeyUp:
                    case KeyState.ImeKeyUp:
                    case KeyState.KeyUp:
                    {
                        heldKeys.Remove(args.Key);
                        keyboard?.HandleKeyDown(args.Key);
                        break;
                    }
                }
            };
#endif
        }

        public override void Dispose()
        {
            // Unregisters devices
            base.Dispose();

            mouse?.Dispose();
            keyboard?.Dispose();
        }

        public override void Update()
        {
            // This check handles the case where the game window focus changes during key events causing it to drop WM_KEYUP events
            if (heldKeys.Count > 0)
            {
                foreach (var key in heldKeys)
                {
                    if ((Win32Native.GetKeyState((int)key) & 0x8000) == 0)
                        keysToRelease.Add(key);
                }
            
                foreach (var keyToRelease in keysToRelease)
                {
                    keyboard?.HandleKeyUp(keyToRelease);
                    heldKeys.Remove(keyToRelease);
                }
            
                keysToRelease.Clear();
            }
        }
        
        private void UIControlOnLostFocus(object sender, EventArgs eventArgs)
        {
            // Release keys/buttons when control focus is lost (this prevents some keys getting stuck when a focus loss happens when moving the camera)
            if (keyboard != null)
            {
                foreach (var key in keyboard.KeyRepeats.Keys.ToArray())
                {
                    keyboard.HandleKeyUp(key);
                }
            }

            mouse?.ForceReleaseButtons();
        }

        internal void HandleKeyDown(IntPtr wParam, IntPtr lParam)
        {
            var lParamLong = lParam.ToInt64();
            if (MessageIsDownAutoRepeat(lParamLong))
                return;

            var virtualKey = (WinFormsKeys)wParam.ToInt64();
            virtualKey = GetCorrectExtendedKey(virtualKey, lParamLong);
            keyboard?.HandleKeyDown(virtualKey);
            heldKeys.Add(virtualKey);
        }

        internal void HandleKeyUp(IntPtr wParam, IntPtr lParam)
        {
            var virtualKey = (WinFormsKeys)wParam.ToInt64();
            virtualKey = GetCorrectExtendedKey(virtualKey, lParam.ToInt64());
            heldKeys.Remove(virtualKey);
            keyboard?.HandleKeyUp(virtualKey);
        }

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case Win32Native.WM_KEYDOWN:
                case Win32Native.WM_SYSKEYDOWN:
                    HandleKeyDown(wParam, lParam);
                    break;

                case Win32Native.WM_KEYUP:
                case Win32Native.WM_SYSKEYUP:
                    HandleKeyUp(wParam, lParam);
                    break;

                case Win32Native.WM_DEVICECHANGE:
                    // Trigger scan on device changed
                    input.Scan();
                    break;
            }

            var result = Win32Native.CallWindowProc(defaultWndProc, hWnd, msg, wParam, lParam);
            return result;
        }
        
        private static WinFormsKeys GetCorrectExtendedKey(WinFormsKeys virtualKey, long lParam)
        {
            if (virtualKey == WinFormsKeys.ControlKey)
            {
                // We check if the key is an extended key. Extended keys are R-keys, non-extended are L-keys.
                return (lParam & 0x01000000) == 0 ? WinFormsKeys.LControlKey : WinFormsKeys.RControlKey;
            }

            if (virtualKey == WinFormsKeys.ShiftKey)
            {
                // We need to check the scan code to check which SHIFT key it is.
                var scanCode = (lParam & 0x00FF0000) >> 16;
                return (scanCode != 36) ? WinFormsKeys.LShiftKey : WinFormsKeys.RShiftKey;
            }

            if (virtualKey == WinFormsKeys.Menu)
            {
                // We check if the key is an extended key. Extended keys are R-keys, non-extended are L-keys.
                return (lParam & 0x01000000) == 0 ? WinFormsKeys.LMenu : WinFormsKeys.RMenu;
            }

            return virtualKey;
        }

        /// <summary>
        /// Windows keeps sending KEYDOWN messages while the user holds down the key.
        /// <br/>This function is used to find out if the received message is a repeated KEYDOWN.
        /// </summary>
        /// <param name="lParam">lParam of the KEYDOWN message</param>
        /// <returns><c>True</c> if this message is a repeated KeyDown, <c>false</c> if it's an actual keydown</returns>
        private static bool MessageIsDownAutoRepeat(long lParam)
        {
            // According to the microsoft docs on WM_KEYDOWN
            // (https://docs.microsoft.com/en-us/windows/desktop/inputdev/wm-keydown)
            // The second to last bit is 0 when the last keyboard message was up and 1 if it was already down
            return (lParam & (1 << 30)) != 0;
        }
    }
}

#endif
