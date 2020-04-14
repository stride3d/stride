// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1310 // Field names must not contain underscore
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Stride.Core.IO
{
    public static class NativeLockFile
    {
#if STRIDE_PLATFORM_WINDOWS_DESKTOP || STRIDE_PLATFORM_UWP
        [DllImport("Kernel32.dll", SetLastError = true)]
        internal static extern bool LockFileEx(Microsoft.Win32.SafeHandles.SafeFileHandle handle, uint flags, uint reserved, uint countLow, uint countHigh, ref System.Threading.NativeOverlapped overlapped);

        [DllImport("Kernel32.dll", SetLastError = true)]
        internal static extern bool UnlockFileEx(Microsoft.Win32.SafeHandles.SafeFileHandle handle, uint reserved, uint countLow, uint countHigh, ref System.Threading.NativeOverlapped overlapped);

        internal const uint LOCKFILE_FAIL_IMMEDIATELY = 0x00000001;
        internal const uint LOCKFILE_EXCLUSIVE_LOCK = 0x00000002;
#endif

        public static void LockFile(FileStream fileStream, long offset, long count, bool exclusive)
        {
#if STRIDE_PLATFORM_ANDROID
            // Android does not support large file and thus is limited to files
            // whose sizes are less than 2GB.
            // We substract the offset to not go beyond the 2GB limit.
            count =  (count + offset > int.MaxValue) ? int.MaxValue - offset: count;
#endif

#if STRIDE_PLATFORM_WINDOWS_DESKTOP || STRIDE_PLATFORM_UWP
            var countLow = (uint)count;
            var countHigh = (uint)(count >> 32);

            var overlapped = new NativeOverlapped()
            {
                InternalLow = IntPtr.Zero,
                InternalHigh = IntPtr.Zero,
                OffsetLow = (int)(offset & 0x00000000FFFFFFFF),
                OffsetHigh = (int)(offset >> 32),
                EventHandle = IntPtr.Zero,
            };

            if (!LockFileEx(fileStream.SafeFileHandle, exclusive ? LOCKFILE_EXCLUSIVE_LOCK : 0, 0, countLow, countHigh, ref overlapped))
            {
                throw new IOException("Couldn't lock file.");
            }
#else
            bool tryAgain;
            do
            {
                tryAgain = false;
                try
                {
                    fileStream.Lock(offset, count);
                }
                catch (IOException)
                {
                    tryAgain = true;
                }
            } while (tryAgain);
#endif
        }

        public static void UnlockFile(FileStream fileStream, long offset, long count)
        {
#if STRIDE_PLATFORM_ANDROID
            // See comment on `LockFile`.
            count =  (count + offset > int.MaxValue) ? int.MaxValue - offset: count;
#endif

#if STRIDE_PLATFORM_WINDOWS_DESKTOP || STRIDE_PLATFORM_UWP
            var countLow = (uint)count;
            var countHigh = (uint)(count >> 32);

            var overlapped = new NativeOverlapped()
            {
                InternalLow = IntPtr.Zero,
                InternalHigh = IntPtr.Zero,
                OffsetLow = (int)(offset & 0x00000000FFFFFFFF),
                OffsetHigh = (int)(offset >> 32),
                EventHandle = IntPtr.Zero,
            };

            if (!UnlockFileEx(fileStream.SafeFileHandle, 0, countLow, countHigh, ref overlapped))
            {
                throw new IOException("Couldn't unlock file.");
            }
#else
            fileStream.Unlock(offset, count);
#endif
        }
    }
}
