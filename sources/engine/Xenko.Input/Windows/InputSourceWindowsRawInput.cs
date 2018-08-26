// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_PLATFORM_WINDOWS_DESKTOP && (XENKO_UI_WINFORMS || XENKO_UI_WPF)
using System;
using System.Windows.Forms;
using SharpDX.Multimedia;
using SharpDX.RawInput;
using Xenko.Games;

namespace Xenko.Input
{
    /// <summary>
    /// Provides support for raw keyboard input on windows
    /// </summary>
    internal class InputSourceWindowsRawInput : InputSourceBase
    {
        private KeyboardWindowsRawInput keyboard;
        private Control uiControl;

        public override void Initialize(InputManager inputManager)
        {
            var gameContext = inputManager.Game.Context as GameContext<Control>;
            uiControl = gameContext.Control;

            keyboard = new KeyboardWindowsRawInput(this);
            RegisterDevice(keyboard);
            BindRawInputKeyboard(uiControl);
        }

        public override void Update()
        {
        }

        public override void Dispose()
        {
            // Unregisters devices
            base.Dispose();

            if (keyboard != null)
            {
                // Unregister raw input using DeviceFlags.Remove
                if (uiControl != null && !uiControl.IsDisposed)
                    Device.RegisterDevice(UsagePage.Generic, UsageId.GenericKeyboard, DeviceFlags.Remove, uiControl.Handle);
                Device.KeyboardInput -= DeviceOnKeyboardInput;
            }
        }

        private void DeviceOnKeyboardInput(object sender, KeyboardInputEventArgs rawKb)
        {
            // Code partially from: http://molecularmusings.wordpress.com/2011/09/05/properly-handling-keyboard-input/
            var key = Keys.None;

            var virtualKey = rawKb.Key;
            var scanCode = rawKb.MakeCode;
            var flags = rawKb.ScanCodeFlags;

            if ((int)virtualKey == 255)
            {
                // discard "fake keys" which are part of an escaped sequence
                return;
            }

            if (virtualKey == System.Windows.Forms.Keys.ShiftKey)
            {
                // correct left-hand / right-hand SHIFT
                virtualKey = (System.Windows.Forms.Keys)WinKeys.MapVirtualKey(scanCode, WinKeys.MAPVK_VSC_TO_VK_EX);
            }
            else if (virtualKey == System.Windows.Forms.Keys.NumLock)
            {
                // correct PAUSE/BREAK and NUM LOCK silliness, and set the extended bit
                scanCode = WinKeys.MapVirtualKey((int)virtualKey, WinKeys.MAPVK_VK_TO_VSC) | 0x100;
            }

            // e0 and e1 are escape sequences used for certain special keys, such as PRINT and PAUSE/BREAK.
            // see http://www.win.tue.nl/~aeb/linux/kbd/scancodes-1.html
            bool isE0 = ((flags & ScanCodeFlags.E0) != 0);
            bool isE1 = ((flags & ScanCodeFlags.E1) != 0);

            if (isE1)
            {
                // for escaped sequences, turn the virtual key into the correct scan code using MapVirtualKey.
                // however, MapVirtualKey is unable to map VK_PAUSE (this is a known bug), hence we map that by hand.
                scanCode = virtualKey == System.Windows.Forms.Keys.Pause ? 0x45 : WinKeys.MapVirtualKey((int)virtualKey, WinKeys.MAPVK_VK_TO_VSC);
            }

            switch (virtualKey)
            {
                // right-hand CONTROL and ALT have their e0 bit set
                case System.Windows.Forms.Keys.ControlKey:
                    virtualKey = isE0 ? System.Windows.Forms.Keys.RControlKey : System.Windows.Forms.Keys.LControlKey;
                    break;

                case System.Windows.Forms.Keys.Menu:
                    virtualKey = isE0 ? System.Windows.Forms.Keys.RMenu : System.Windows.Forms.Keys.LMenu;
                    break;

                // NUMPAD ENTER has its e0 bit set
                case System.Windows.Forms.Keys.Return:
                    if (isE0)
                        key = Keys.NumPadEnter;
                    break;
            }
            
            if (key == Keys.None)
            {
                WinKeys.MapKeys.TryGetValue(virtualKey, out key);
            }
            
            if (key != Keys.None)
            {
                bool isKeyUp = (flags & ScanCodeFlags.Break) != 0;

                if (isKeyUp)
                {
                    keyboard.HandleKeyUp(key);
                }
                else
                {
                    keyboard.HandleKeyDown(key);
                }
            }
        }
        
        private void BindRawInputKeyboard(Control winformControl)
        {
            if (winformControl.Handle == IntPtr.Zero)
            {
                winformControl.HandleCreated += (sender, args) =>
                {
                    if (winformControl.Handle != IntPtr.Zero)
                    {
                        BindRawInputKeyboard(winformControl);
                    }
                };
            }
            else
            {
                Device.RegisterDevice(UsagePage.Generic, UsageId.GenericKeyboard, DeviceFlags.None, winformControl.Handle);
                Device.KeyboardInput += DeviceOnKeyboardInput;
            }
        }
    }
}
#endif