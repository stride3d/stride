// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1310 // Field names must not contain underscore
using System.Runtime.InteropServices;

namespace Stride.Core.IO;

public static partial class NativeLockFile
{
    // Windows
    [DllImport("Kernel32.dll", SetLastError = true)]
    private static extern bool LockFileEx(Microsoft.Win32.SafeHandles.SafeFileHandle handle, uint flags, uint reserved, uint countLow, uint countHigh, ref NativeOverlapped overlapped);

    [DllImport("Kernel32.dll", SetLastError = true)]
    private static extern bool UnlockFileEx(Microsoft.Win32.SafeHandles.SafeFileHandle handle, uint reserved, uint countLow, uint countHigh, ref NativeOverlapped overlapped);

    internal const uint LOCKFILE_FAIL_IMMEDIATELY = 0x00000001;
    internal const uint LOCKFILE_EXCLUSIVE_LOCK = 0x00000002;

    // Unix (Linux, macOS, iOS, Android)
    [DllImport("libc", EntryPoint = "fcntl", SetLastError = true)]
    private static extern int fcntl(int fd, int cmd, ref Flock lockInfo);

    // Standard fcntl lock commands (per-process, all Unix platforms)
    private const int F_SETLK = 6;   // Non-blocking
    private const int F_SETLKW = 7;  // Blocking

    // OFD lock commands (per-file-description, Linux 3.15+ / Android 5.0+)
    // These behave like Windows LockFileEx — per-handle, not per-process.
    private const int F_OFD_SETLK = 37;   // Non-blocking
    private const int F_OFD_SETLKW = 38;  // Blocking

    private const short F_RDLCK = 0;
    private const short F_WRLCK = 1;
    private const short F_UNLCK = 2;

    private const int EINVAL = 22;

    [StructLayout(LayoutKind.Sequential)]
    private struct Flock
    {
        public short l_type;
        public short l_whence;  // SEEK_SET = 0
        public long l_start;
        public long l_len;      // 0 = to EOF
        public int l_pid;       // must be 0 for OFD locks
    }

    /// <summary>
    ///   On Linux/Android: true = use OFD locks, false = use standard fcntl, null = not yet determined.
    /// </summary>
    private static bool? useOfdLocks;

    /// <summary>
    ///   Attempts to lock a byte range of a file.
    /// </summary>
    /// <returns><c>true</c> if the lock was acquired, <c>false</c> if the lock could not be acquired.</returns>
    public static bool TryLockFile(FileStream fileStream, long offset, long count, bool exclusive, bool failImmediately = false)
    {
        if (Platform.Type == PlatformType.Android)
        {
            count = (count + offset > int.MaxValue) ? int.MaxValue - offset : count;
        }

        if (OperatingSystem.IsWindows())
        {
            var countLow = (uint)count;
            var countHigh = (uint)(count >> 32);

            var overlapped = new NativeOverlapped()
            {
                OffsetLow = (int)(offset & uint.MaxValue),
                OffsetHigh = (int)(offset >> 32),
            };

            return LockFileEx(fileStream.SafeFileHandle,
                (exclusive ? LOCKFILE_EXCLUSIVE_LOCK : 0) + (failImmediately ? LOCKFILE_FAIL_IMMEDIATELY : 0),
                0, countLow, countHigh, ref overlapped);
        }
        else
        {
            return UnixLock(fileStream, offset, count, exclusive, failImmediately);
        }
    }

    /// <summary>
    ///   Unlocks a byte range of a file previously locked with <see cref="TryLockFile"/>.
    /// </summary>
    public static void TryUnlockFile(FileStream fileStream, long offset, long count)
    {
        if (Platform.Type == PlatformType.Android)
        {
            count = (count + offset > int.MaxValue) ? int.MaxValue - offset : count;
        }

        if (OperatingSystem.IsWindows())
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
        else
        {
            UnixUnlock(fileStream, offset, count);
        }
    }

    /// <summary>
    ///   Lock on Unix: on Linux/Android, tries OFD locks first (per-handle, like Windows LockFileEx),
    ///   falls back to standard fcntl (per-process) if OFD is not supported.
    ///   On macOS/iOS, uses standard fcntl directly (OFD not available on XNU).
    /// </summary>
    private static bool UnixLock(FileStream fileStream, long offset, long count, bool exclusive, bool failImmediately)
    {
        int fd = fileStream.SafeFileHandle.DangerousGetHandle().ToInt32();

        var lockInfo = new Flock
        {
            l_type = exclusive ? F_WRLCK : F_RDLCK,
            l_whence = 0, // SEEK_SET
            l_start = offset,
            l_len = count,
            l_pid = 0, // required for OFD locks, ignored by standard fcntl
        };

        // On Linux/Android, try OFD locks first (per-handle byte-range locking)
        if (OperatingSystem.IsLinux() || Platform.Type == PlatformType.Android)
        {
            if (useOfdLocks != false)
            {
                int ofdCmd = failImmediately ? F_OFD_SETLK : F_OFD_SETLKW;
                if (fcntl(fd, ofdCmd, ref lockInfo) == 0)
                {
                    useOfdLocks = true;
                    return true;
                }

                int errno = Marshal.GetLastWin32Error();
                if (errno == EINVAL && useOfdLocks == null)
                {
                    // OFD not supported by this kernel — fall through to standard fcntl
                    useOfdLocks = false;
                }
                else
                {
                    // OFD is supported but lock failed (contention or other error)
                    return false;
                }
            }
        }

        // Standard fcntl (per-process) — used on macOS/iOS, or as fallback on old Linux kernels.
        // Always non-blocking to avoid hanging on filesystems like WSL2 9p.
        return fcntl(fd, F_SETLK, ref lockInfo) == 0;
    }

    private static void UnixUnlock(FileStream fileStream, long offset, long count)
    {
        int fd = fileStream.SafeFileHandle.DangerousGetHandle().ToInt32();

        var lockInfo = new Flock
        {
            l_type = F_UNLCK,
            l_whence = 0,
            l_start = offset,
            l_len = count,
            l_pid = 0,
        };

        // Unlock failure is silently ignored — the lock may not have been acquired
        // (e.g. unsupported filesystem, or TryLockFile returned false).
        int cmd = useOfdLocks == true ? F_OFD_SETLK : F_SETLK;
        _ = fcntl(fd, cmd, ref lockInfo);
    }
}
