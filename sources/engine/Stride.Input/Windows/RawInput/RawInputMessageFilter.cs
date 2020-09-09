// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_WINDOWS_DESKTOP && (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)

using System;
using System.Windows.Forms;

namespace Stride.Input.RawInput
{
    internal class RawInputMessageFilter : IMessageFilter
    {
        private const int WM_INPUT = 0x00FF;

        private readonly Action<RawInput.RawMouse> mouseInputHandler;
        private readonly Action<RawInput.RawKeyboard> keyboardInputHandler;
        private readonly Action<(uint count, uint size, byte[] data)> hidInputHandler;

        public RawInputMessageFilter(Action<RawInput.RawMouse> mouseInputHandler, Action<RawInput.RawKeyboard> keyboardInputHandler, Action<(uint count, uint size, byte[] data)> hidInputHandler)
        {
            this.mouseInputHandler = mouseInputHandler;
            this.keyboardInputHandler = keyboardInputHandler;
            this.hidInputHandler = hidInputHandler;
        }

        public unsafe bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_INPUT)
            {
                uint cbSize = 0;
                Win32.GetRawInputData(m.LParam, (uint)RawInputDataType.RID_INPUT, IntPtr.Zero, ref cbSize, (uint)sizeof(RawInput.RawInputHeader));
                if (cbSize == 0)
                {
                    return false;
                }
                var buffer = stackalloc byte[(int)cbSize];
                var count = Win32.GetRawInputData(m.LParam, (uint)RawInputDataType.RID_INPUT, (IntPtr)buffer, ref cbSize, (uint)sizeof(RawInput.RawInputHeader));
                var rawInput = (RawInput.RawInputData*)buffer;

                switch (rawInput->header.dwType)
                {
                    case 0: // Mouse
                        mouseInputHandler?.Invoke(rawInput->data.Mouse);
                        break;
                    case 1: // Keyboard
                        keyboardInputHandler?.Invoke(rawInput->data.Keyboard);
                        break;
                    case 2: // HID
                        var byteData = RawInput.GetHIDRawData(ref rawInput->data.Hid);
                        var data = (rawInput->data.Hid.dwCount, rawInput->data.Hid.dwSizeHid, byteData);
                        hidInputHandler?.Invoke(data);
                        break;
                }
            }
            return false;
        }
    }
}

#endif
