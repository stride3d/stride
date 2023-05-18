// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#if (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1132 // Do not combine fields
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter

using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace Stride.Games
{
    internal static partial class Win32Native
    {
        /// <summary>
        /// Internal class to interact with Native Message
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeMessage
        {
            public IntPtr handle;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PAINTSTRUCT
        {
            public IntPtr Hdc;
            public bool Erase;
            public RECT PaintRectangle;
            public bool Restore;
            public bool IncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] Reserved;
        }

        public enum WindowLongType : int
        {
            WndProc = (-4),
            HInstance = (-6),
            HwndParent = (-8),
            Style = (-16),
            ExtendedStyle = (-20),
            UserData = (-21),
            Id = (-12),
        }

        public delegate IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public static IntPtr GetWindowLong(IntPtr hWnd, WindowLongType index)
        {
            if (IntPtr.Size == 4)
            {
                return GetWindowLong32(hWnd, index);
            }
            return GetWindowLong64(hWnd, index);
        }

        [DllImport("user32.dll", EntryPoint = "GetFocus", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetFocus();

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetWindowLong32(IntPtr hwnd, WindowLongType index);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetWindowLong64(IntPtr hwnd, WindowLongType index);

        public static IntPtr SetWindowLong(IntPtr hwnd, WindowLongType index, IntPtr wndProcPtr)
        {
            if (IntPtr.Size == 4)
            {
                return SetWindowLong32(hwnd, index, wndProcPtr);
            }
            return SetWindowLongPtr64(hwnd, index, wndProcPtr);
        }

        [DllImport("user32.dll", EntryPoint = "SetParent", CharSet = CharSet.Unicode)]
        public static extern IntPtr SetParent(IntPtr hWnd, IntPtr hWndParent);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Unicode)]
        private static extern IntPtr SetWindowLong32(IntPtr hwnd, WindowLongType index, IntPtr wndProc);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern short GetKeyState(int keyCode);

        public static bool ShowWindow(IntPtr hWnd, bool windowVisible)
        {
            return ShowWindow(hWnd, windowVisible ? 1 : 0);
        }

        [DllImport("user32.dll", EntryPoint = "ShowWindow", CharSet = CharSet.Unicode)]
        private static extern bool ShowWindow(IntPtr hWnd, int mCmdShow);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Unicode)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hwnd, WindowLongType index, IntPtr wndProc);

        [DllImport("user32.dll", EntryPoint = "CallWindowProc", CharSet = CharSet.Unicode)]
        public static extern IntPtr CallWindowProc(IntPtr wndProc, IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandle", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("ole32.dll")]
        public static extern int CoInitialize(IntPtr pvReserved);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool PeekMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern sbyte GetMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin,
            uint wMsgFilterMax);

        [DllImport("user32.dll", EntryPoint = "PeekMessage")]
        public static extern int PeekMessage(out NativeMessage lpMsg, IntPtr hWnd, int wMsgFilterMin, int wMsgFilterMax, int wRemoveMsg);

        [DllImport("user32.dll", EntryPoint = "GetMessage")]
        public static extern int GetMessage(out NativeMessage lpMsg, IntPtr hWnd, int wMsgFilterMin, int wMsgFilterMax);

        [DllImport("user32.dll", EntryPoint = "TranslateMessage", CharSet = CharSet.Unicode)]
        public static extern int TranslateMessage(ref NativeMessage lpMsg);

        [DllImport("user32.dll", EntryPoint = "DispatchMessage", CharSet = CharSet.Unicode)]
        public static extern int DispatchMessage(ref NativeMessage lpMsg);

        [DllImport("user32.dll", EntryPoint = "BeginPaint")]
        public static extern IntPtr BeginPaint(IntPtr hWnd, ref PAINTSTRUCT paintStruct);

        [DllImport("user32.dll", EntryPoint = "EndPaint")]
        public static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT paintStruct);

        [DllImport("imm32.dll", EntryPoint = "ImmGetContext")]
        public static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("imm32.dll", EntryPoint = "ImmReleaseContext")]
        public static extern IntPtr ImmReleaseContext(IntPtr hWnd, IntPtr context);

        [DllImport("imm32.dll", EntryPoint = "ImmGetCompositionString", CharSet = CharSet.Unicode)]
        public static extern int ImmGetCompositionString(IntPtr himc, int dwIndex, IntPtr buf, int bufLen);

        public const int GCS_COMPSTR = 0x0008;

        public const int WM_SIZE = 0x0005;
        public const int WM_ACTIVATEAPP = 0x001C;
        public const int WM_POWERBROADCAST = 0x0218;
        public const int WM_MENUCHAR = 0x0120;
        public const int WM_SYSCOMMAND = 0x0112;
        public const int WM_KEYDOWN = 0x100;
        public const int WM_KEYUP = 0x101;
        public const int WM_CHAR = 0x102;
        public const int WM_SYSKEYDOWN = 0x104;
        public const int WM_SYSKEYUP = 0x105;
        public const int WM_DEVICECHANGE = 0x0219;
        public const int WM_INPUTLANGCHANGE = 0x0051;
        public const int WM_IME_CHAR = 0x0286;
        public const int WM_IME_COMPOSITION = 0x010F;
        public const int WM_IME_COMPOSITIONFULL = 0x0284;
        public const int WM_IME_CONTROL = 0x0283;
        public const int WM_IME_ENDCOMPOSITION = 0x010E;
        public const int WM_IME_KEYDOWN = 0x0290;
        public const int WM_IME_KEYLAST = 0x010F;
        public const int WM_IME_KEYUP = 0x0291;
        public const int WM_IME_NOTIFY = 0x0282;
        public const int WM_IME_REQUEST = 0x0288;
        public const int WM_IME_SELECT = 0x0285;
        public const int WM_IME_SETCONTEXT = 0x0281;
        public const int WM_IME_STARTCOMPOSITION = 0x010D;
        public const int WM_PAINT = 0x000F;
        public const int WM_NCPAINT = 0x0085;

        public const int PM_REMOVE = 0x0001;
    }

    internal static class TouchUtils
    {
        public const int WM_POINTERDOWN = 0x0246;
        public const int WM_POINTERUP = 0x0247;
        public const int WM_POINTERUPDATE = 0x0245;
        public const int WM_POINTERWHEEL = 0x024E;
        public const int WM_POINTERHWHEEL = 0x024F;
        public const int WM_POINTERCAPTURECHANGED = 0x024C;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPointerType(uint pointerId, out PointerInputType pointerType);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPointerTouchInfo(uint pointerId, out POINTER_TOUCH_INFO touchInfo);

        [Flags]
        public enum TouchFlags
        {
            TOUCH_FLAG_NONE = 0x00000000
        }

        [Flags]
        public enum TouchMask
        {
            TOUCH_MASK_NONE = 0x00000000,
            TOUCH_MASK_CONTACTAREA = 0x00000001,
            TOUCH_MASK_ORIENTATION = 0x00000002,
            TOUCH_MASK_PRESSURE = 0x00000004,
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct POINTER_TOUCH_INFO
        {
            public POINTER_INFO pointerInfo;
            public TouchFlags touchFlags;
            public TouchMask touchMask;
            public int rcContactLeft;
            public int rcContactTop;
            public int rcContactRight;
            public int rcContactBottom;
            public int rcContactRawLeft;
            public int rcContactRawTop;
            public int rcContactRawRight;
            public int rcContactRawBottom;
            public uint orientation;
            public uint pressure;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct POINTER_INFO
        {
            public PointerInputType pointerType;
            public uint pointerId;
            public uint frameId;
            public PointerFlags pointerFlags;
            public IntPtr sourceDevice;
            public IntPtr hwndTarget;
            public int ptPixelLocationX;
            public int ptPixelLocationY;
            public int ptHimetricLocationX;
            public int ptHimetricLocationY;
            public int ptPixelLocationRawX;
            public int ptPixelLocationRawY;
            public int ptHimetricLocationRawX;
            public int ptHimetricLocationRawY;
            public uint dwTime;
            public uint historyCount;
            public int inputData;
            public uint dwKeyStates;
            public ulong PerformanceCount;
            public PointerButtonChangeType ButtonChangeType;
        }

        public enum PointerInputType
        {
            PT_NONE = 0x00000000,
            PT_POINTER = 0x00000001,
            PT_TOUCH = 0x00000002,
            PT_PEN = 0x00000003,
            PT_MOUSE = 0x00000004,
            PT_TOUCHPAD = 0x00000005
        }

        [Flags]
        public enum PointerFlags
        {
            POINTER_FLAG_NONE = 0x00000000,
            POINTER_FLAG_NEW = 0x00000001,
            POINTER_FLAG_INRANGE = 0x00000002,
            POINTER_FLAG_INCONTACT = 0x00000004,
            POINTER_FLAG_FIRSTBUTTON = 0x00000010,
            POINTER_FLAG_SECONDBUTTON = 0x00000020,
            POINTER_FLAG_THIRDBUTTON = 0x00000040,
            POINTER_FLAG_FOURTHBUTTON = 0x00000080,
            POINTER_FLAG_FIFTHBUTTON = 0x00000100,
            POINTER_FLAG_PRIMARY = 0x00002000,
            POINTER_FLAG_CONFIDENCE = 0x00000400,
            POINTER_FLAG_CANCELED = 0x00000800,
            POINTER_FLAG_DOWN = 0x00010000,
            POINTER_FLAG_UPDATE = 0x00020000,
            POINTER_FLAG_UP = 0x00040000,
            POINTER_FLAG_WHEEL = 0x00080000,
            POINTER_FLAG_HWHEEL = 0x00100000,
            POINTER_FLAG_CAPTURECHANGED = 0x00200000,
            POINTER_FLAG_HASTRANSFORM = 0x00400000
        }

        public enum PointerButtonChangeType : ulong
        {
            POINTER_CHANGE_NONE,
            POINTER_CHANGE_FIRSTBUTTON_DOWN,
            POINTER_CHANGE_FIRSTBUTTON_UP,
            POINTER_CHANGE_SECONDBUTTON_DOWN,
            POINTER_CHANGE_SECONDBUTTON_UP,
            POINTER_CHANGE_THIRDBUTTON_DOWN,
            POINTER_CHANGE_THIRDBUTTON_UP,
            POINTER_CHANGE_FOURTHBUTTON_DOWN,
            POINTER_CHANGE_FOURTHBUTTON_UP,
            POINTER_CHANGE_FIFTHBUTTON_DOWN,
            POINTER_CHANGE_FIFTHBUTTON_UP
        }

        public static ushort LOWORD(ulong l) { return (ushort)(l & 0xFFFF); }
        public static ushort HIWORD(ulong l) { return (ushort)((l >> 16) & 0xFFFF); }
        public static ushort GET_POINTERID_WPARAM(ulong wParam) { return LOWORD(wParam); }
        public static ushort GET_X_LPARAM(ulong lp) { return LOWORD(lp); }
        public static ushort GET_Y_LPARAM(ulong lp) { return HIWORD(lp); }
    }
}

#endif
