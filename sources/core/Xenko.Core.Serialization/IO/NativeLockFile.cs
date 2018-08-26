// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_WINDOWS_DESKTOP
#pragma warning disable SA1310 // Field names must not contain underscore
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Xenko.Core.IO
{
    internal class NativeLockFile
    {
        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool LockFileEx(SafeFileHandle handle, uint flags, uint reserved, uint countLow, uint countHigh, ref System.Threading.NativeOverlapped overlapped);

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool UnlockFileEx(SafeFileHandle handle, uint reserved, uint countLow, uint countHigh, ref System.Threading.NativeOverlapped overlapped);

        public const uint LOCKFILE_FAIL_IMMEDIATELY = 0x00000001;
        public const uint LOCKFILE_EXCLUSIVE_LOCK = 0x00000002;
    }
}
#endif
