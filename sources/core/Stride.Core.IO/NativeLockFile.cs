// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1310 // Field names must not contain underscore
using System.Runtime.InteropServices;

namespace Stride.Core.IO;

public static partial class NativeLockFile
{
    [DllImport("Kernel32.dll", SetLastError = true)]
    private static extern bool LockFileEx(Microsoft.Win32.SafeHandles.SafeFileHandle handle, uint flags, uint reserved, uint countLow, uint countHigh, ref NativeOverlapped overlapped);

    [DllImport("Kernel32.dll", SetLastError = true)]
    private static extern bool UnlockFileEx(Microsoft.Win32.SafeHandles.SafeFileHandle handle, uint reserved, uint countLow, uint countHigh, ref NativeOverlapped overlapped);

    internal const uint LOCKFILE_FAIL_IMMEDIATELY = 0x00000001;
    internal const uint LOCKFILE_EXCLUSIVE_LOCK = 0x00000002;

    public static bool TryLockFile(FileStream fileStream, long offset, long count, bool exclusive, bool failImmediately = false)
    {
        if (Platform.Type == PlatformType.Android)
        {
            // Android does not support large file and thus is limited to files
            // whose sizes are less than 2GB.
            // We substract the offset to not go beyond the 2GB limit.
            count = (count + offset > int.MaxValue) ? int.MaxValue - offset : count;
        }

#if STRIDE_PLATFORM_UWP
        if (Platform.Type = PlatformType.UWP)
#else
        if (OperatingSystem.IsWindows())
#endif
        {
            var countLow = (uint)count;
            var countHigh = (uint)(count >> 32);

            var overlapped = new NativeOverlapped()
            {
                OffsetLow = (int)(offset & uint.MaxValue),
                OffsetHigh = (int)(offset >> 32),
            };

            return LockFileEx(fileStream.SafeFileHandle, (exclusive ? LOCKFILE_EXCLUSIVE_LOCK : 0) + (failImmediately ? LOCKFILE_FAIL_IMMEDIATELY : 0), 0, countLow, countHigh, ref overlapped);
        }
        else if (!OperatingSystem.IsMacOS())
        {
            bool tryAgain;
            do
            {
                tryAgain = false;
                try
                {
                    fileStream.Lock(offset, count);
                    return true;
                }
                catch (IOException)
                {
                    tryAgain = true;
                }
            } while (tryAgain);
        }
        return false;
    }

    public static void TryUnlockFile(FileStream fileStream, long offset, long count)
    {
        if (Platform.Type == PlatformType.Android)
        {
            // See comment on `LockFile`.
            count = (count + offset > int.MaxValue) ? int.MaxValue - offset : count;
        }

#if STRIDE_PLATFORM_UWP
        if (Platform.Type = PlatformType.UWP)
#else
        if (OperatingSystem.IsWindows())
#endif
        {
            var countLow = (uint)count;
            var countHigh = (uint)(count >> 32);

            var overlapped = new NativeOverlapped()
            {
                OffsetLow = (int)(offset & uint.MaxValue),
                OffsetHigh = (int)(offset >> 32),
            };

            if (!UnlockFileEx(fileStream.SafeFileHandle, 0, countLow, countHigh, ref overlapped))
            {
                throw new IOException("Couldn't unlock file.");
            }
        }
        else if (!OperatingSystem.IsMacOS())
        {
            fileStream.Unlock(offset, count);
        }
    }
}
