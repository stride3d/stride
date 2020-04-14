// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using Stride.Core.Annotations;
using Stride.Core.IO;

namespace Stride.Core.Windows
{
    /// <summary>
    /// A class representing an thread-safe, process-safe file lock.
    /// </summary>
    public class FileLock : IDisposable
    {
        private FileStream lockFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLock"/> class.
        /// </summary>
        /// <param name="lockFile">A file that was locked.</param>
        private FileLock(FileStream lockFile)
        {
            this.lockFile = lockFile;
        }

        /// <summary>
        /// Releases the file lock.
        /// </summary>
        public void Dispose()
        {
            if (lockFile != null)
            {
                var overlapped = new NativeOverlapped();
                NativeLockFile.UnlockFileEx(lockFile.SafeFileHandle, 0, uint.MaxValue, uint.MaxValue, ref overlapped);
                lockFile.Dispose();

                // Try to delete the file
                // Ideally we would use FileOptions.DeleteOnClose, but it doesn't seem to work well with FileShare for second instance
                try
                {
                    File.Delete(lockFile.Name);
                }
                catch (Exception)
                {
                }

                lockFile = null;
            }
        }

        /// <summary>
        /// Tries to take ownership of the file lock without waiting.
        /// </summary>
        /// Tries to take ownership of the file lock within a given delay.
        /// <returns>A new instance of <see cref="FileLock"/> if the ownership could be taken, <c>null</c> otherwise.</returns>
        /// <remarks>The returned <see cref="FileLock"/> must be disposed to release the mutex.</remarks>
        [CanBeNull]
        public static FileLock TryLock(string name)
        {
            return Wait(name, 0);
        }

        /// <summary>
        /// Waits indefinitely to take ownership of the file lock.
        /// </summary>
        /// Tries to take ownership of the file lock within a given delay.
        /// <returns>A new instance of <see cref="FileLock"/> if the ownership could be taken, <c>null</c> otherwise.</returns>
        /// <remarks>The returned <see cref="FileLock"/> must be disposed to release the file lock.</remarks>
        [CanBeNull]
        public static FileLock Wait(string name)
        {
            return Wait(name, -1);
        }

        /// <summary>
        /// Tries to take ownership of the file lock within a given delay.
        /// </summary>
        /// <param name="name">A unique name identifying the file lock.</param>
        /// <param name="millisecondsTimeout">The maximum delay to wait before returning, in milliseconds.</param>
        /// <returns>A new instance of <see cref="FileLock"/> if the ownership could be taken, <c>null</c> otherwise.</returns>
        /// <remarks>
        /// The returned <see cref="FileLock"/> must be disposed to release the file lock.
        /// Calling this method with 0 for <see paramref="millisecondsTimeout"/> is equivalent to call <see cref="TryLock"/>.
        /// Calling this method with a negative value for <see paramref="millisecondsTimeout"/> is equivalent to call <see cref="Wait(string)"/>.
        /// </remarks>
        [CanBeNull]
        public static FileLock Wait(string name, int millisecondsTimeout)
        {
            var fileLock = BuildFileLock(name);
            try
            {
                if (millisecondsTimeout != 0 && millisecondsTimeout != -1)
                    throw new NotImplementedException("GlobalMutex.Wait() is implemented only for millisecondsTimeout 0 or -1");

                var overlapped = new NativeOverlapped();
                bool hasHandle = NativeLockFile.LockFileEx(fileLock.SafeFileHandle, NativeLockFile.LOCKFILE_EXCLUSIVE_LOCK | (millisecondsTimeout == 0 ? NativeLockFile.LOCKFILE_FAIL_IMMEDIATELY : 0), 0, uint.MaxValue, uint.MaxValue, ref overlapped);
                return hasHandle == false ? null : new FileLock(fileLock);
            }
            catch (AbandonedMutexException)
            {
                return new FileLock(fileLock);
            }
        }

        [NotNull]
        private static FileStream BuildFileLock(string name)
        {
            // We open with FileShare.ReadWrite mode so that we can implement `Wait`.
            return new FileStream(name, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
        }
    }
}
