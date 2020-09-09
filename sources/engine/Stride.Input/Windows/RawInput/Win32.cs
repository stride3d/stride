// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Stride.Input.RawInput
{
    internal static class Win32
    {
        [DllImport("user32.dll")]
        public unsafe static extern bool RegisterRawInputDevices(void* pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("user32.dll")]
        public unsafe static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll")]
        public static extern void ClipCursor(IntPtr rect);
    }
}
