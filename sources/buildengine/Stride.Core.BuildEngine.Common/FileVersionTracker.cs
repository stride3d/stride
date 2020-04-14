// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Storage;

namespace Stride.Core.BuildEngine
{
    /// <summary>
    /// A tracker of file date.
    /// </summary>
    public class FileVersionTracker : IDisposable
    {
        private const string DefaultFileVersionTrackerFile = @"Stride\FileVersionTracker.cache";
        private readonly FileVersionStorage storage;
        private readonly Dictionary<FileVersionKey, object> locks;
        private static readonly Logger log = GlobalLogger.GetLogger("FileVersionTracker");
        private static readonly object lockDefaultTracker = new object();
        private static FileVersionTracker defaultFileVersionTracker;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileVersionTracker"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public FileVersionTracker(Stream stream)
        {
            storage = new FileVersionStorage(stream);
            locks = new Dictionary<FileVersionKey, object>();
        }

        /// <summary>
        /// Gets the default file version tracker for this machine.
        /// </summary>
        /// <returns>FileVersionTracker.</returns>
        public static FileVersionTracker GetDefault()
        {
            lock (lockDefaultTracker)
            {
                if (defaultFileVersionTracker == null)
                {
                    var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DefaultFileVersionTrackerFile);
                    var directory = Path.GetDirectoryName(filePath);
                    if (directory != null && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // Loads the file version cache
                    defaultFileVersionTracker = Load(filePath);
                }
            }
            return defaultFileVersionTracker;
        }

        /// <summary>
        /// Loads previous versions stored from the specified file path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>FileVersionTracker.</returns>
        public static FileVersionTracker Load(string filePath)
        {
            // Try to compact it before using it
            FileVersionStorage.Compact(filePath);

            bool isFirstPass = true;
            while (true)
            {
                FileStream fileStream = null;

                // Try to open the file, if we get an exception, this might be due only because someone is locking the file to
                // save it while we are trying to open it
                const int RetryOpenFileStream = 20;
                var random = new Random();
                for (int i = 0; i < RetryOpenFileStream; i++)
                {
                    try
                    {
                        fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                        break;
                    }
                    catch (Exception)
                    {
                        if ((i + 1) == RetryOpenFileStream)
                            throw;

                        Thread.Sleep(50 + random.Next(100));
                    }
                }

                var tracker = new FileVersionTracker(fileStream);
                try
                {
                    tracker.storage.LoadNewValues();
                    return tracker;
                }
                catch (Exception)
                {
                    // If an exception occurred, we are going to try to recover from it by reseting it.
                    // reset file length to 0
                    fileStream.SetLength(0);
                    tracker.Dispose();
                    if (!isFirstPass)
                    {
                        throw;
                    }
                }
                isFirstPass = false;
            }
        }

        public ObjectId ComputeFileHash(string filePath)
        {
            var inputVersionKey = new FileVersionKey(filePath);
            storage.LoadNewValues();

            // Perform a lock per file as it can be expensive to compute 
            // them at the same time (for large file)
            object versionLock;
            lock (locks)
            {
                if (!locks.TryGetValue(inputVersionKey, out versionLock))
                {
                    versionLock = new object();
                    locks.Add(inputVersionKey, versionLock);
                }
            }

            var hash = ObjectId.Empty;
            lock (versionLock)
            {
                if (!storage.TryGetValue(inputVersionKey, out hash))
                {
                    // TODO: we might want to allow retries, timeout, etc. since file processed here are files currently being edited by user
                    try
                    {
                        using (var fileStream = File.OpenRead(filePath))
                        using (var stream = new DigestStream(Stream.Null))
                        {
                            fileStream.CopyTo(stream);
                            hash = stream.CurrentHash;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Debug($"Cannot calculate hash for file [{filePath}]", ex);
                    }
                    storage[inputVersionKey] = hash;
                }
            }

            return hash;
        }

        public void Dispose()
        {
            storage.Dispose();
        }
    }

    [DataContract]
    public struct FileVersionKey : IEquatable<FileVersionKey>
    {
        public string Path;

        public DateTime LastModifiedDate;

        public long FileSize;

        public FileVersionKey(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            Path = path;
            LastModifiedDate = DateTime.MinValue;
            FileSize = -1;

            if (File.Exists(path))
            {
                LastModifiedDate = File.GetLastWriteTime(path);
                FileSize = new FileInfo(path).Length;
            }
        }

        public bool Equals(FileVersionKey other)
        {
            return string.Equals(Path, other.Path) && LastModifiedDate.Equals(other.LastModifiedDate) && FileSize == other.FileSize;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is FileVersionKey && Equals((FileVersionKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Path != null ? Path.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ LastModifiedDate.GetHashCode();
                hashCode = (hashCode * 397) ^ FileSize.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(FileVersionKey left, FileVersionKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FileVersionKey left, FileVersionKey right)
        {
            return !left.Equals(right);
        }
    }

}
