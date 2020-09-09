// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stride.Input.RawInput
{
    internal static class RawInput
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RawInputDevice
        {
            public ushort UsagePage;

            public ushort Usage;

            public uint Flags;

            public IntPtr Target;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawInputHeader
        {
            public int dwType;
            public int dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawMouse
        {
            [StructLayout(LayoutKind.Explicit)]
            public struct RawMouseButtonsData
            {
                [FieldOffset(0)]
                public int Buttons;

                [FieldOffset(0)]
                public short ButtonFlags;

                [FieldOffset(2)]
                public short ButtonData;
            }

            public ushort Flags;
            public RawMouseButtonsData ButtonsData;
            public int RawButtons;
            public int LastX;
            public int LastY;
            public int ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawKeyboard
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawHid
        {
            public uint dwSizeHid;
            public uint dwCount;
            public int bRawData;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct RawInputDataInner
        {
            [FieldOffset(0)]
            public RawMouse Mouse;

            [FieldOffset(0)]
            public RawKeyboard Keyboard;

            [FieldOffset(0)]
            public RawHid Hid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawInputData
        {
            public RawInputHeader header;
            public RawInputDataInner data;
        }

        public static unsafe byte[] GetHIDRawData(ref RawHid rawHid)
        {
            var byteCount = rawHid.dwCount * rawHid.dwSizeHid;
            var result = new byte[byteCount];
            fixed (byte* dest = result)
            {
                fixed (int* src = &rawHid.bRawData)
                {
                    Unsafe.CopyBlockUnaligned(dest, src, byteCount);
                }
            }
            return result;
        }

        private static unsafe bool RegisterDevice(RawInputDevice device)
        {
            var devices = new RawInputDevice[1] { device };
            fixed (void* ptr = devices)
            {
                return Win32.RegisterRawInputDevices(ptr, 1, (uint)sizeof(RawInputDevice));
            }
        }

        public static bool RegisterDevice(UsagePage usagePage, UsageId usageId, ModeFlags flags, IntPtr target)
        {
            var device = new RawInputDevice
            {
                UsagePage = (ushort)usagePage,
                Usage = (ushort)usageId,
                Flags = (uint)flags,
                Target = target
            };
            return RegisterDevice(device);
        }
    }
}
