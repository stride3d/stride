// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Stride.Core.IO
{
    /// <summary>
    /// Abstract base class for <see cref="IVirtualFileProvider"/>.
    /// </summary>
    public abstract class VirtualFileProviderBase : IVirtualFileProvider
    {
        protected VirtualFileProviderBase(string rootPath)
        {
            RootPath = rootPath;

            // Ensure RootPath ends with /
            if (RootPath != null)
            {
                if (RootPath == string.Empty)
                    throw new ArgumentException("rootPath");

                if (RootPath[RootPath.Length - 1] != VirtualFileSystem.DirectorySeparatorChar)
                    RootPath += VirtualFileSystem.DirectorySeparatorChar;
            }
            VirtualFileSystem.RegisterProvider(this);
        }

        /// <inheritdoc/>
        public string RootPath { get; private set; }

        /// <inheritdoc/>
        public virtual string GetAbsolutePath(string path)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual bool TryGetFileLocation(string path, out string filePath, out long start, out long end)
        {
            filePath = null;
            start = 0;
            end = -1;
            return false;
        }

        /// <inheritdoc/>
        public abstract Stream OpenStream(string url, VirtualFileMode mode, VirtualFileAccess access, VirtualFileShare share = VirtualFileShare.Read, StreamFlags streamFlags = StreamFlags.None);

        /// <summary>
        /// Resolves the path (can map virtual to absolute path).
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The resolved path.</returns>
        protected virtual string ResolvePath(string path)
        {
            return path;
        }

        public virtual bool DirectoryExists(string url)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual string[] ListFiles(string url, string searchPattern, VirtualSearchOption searchOption)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual bool FileExists(string url)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual void FileDelete(string url)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual void FileMove(string sourceUrl, string destinationUrl)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual void FileMove(string sourceUrl, IVirtualFileProvider destinationProvider, string destinationUrl)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual long FileSize(string url)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual DateTime GetLastWriteTime(string url)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual void CreateDirectory(string url)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            VirtualFileSystem.UnregisterProvider(this);
        }
    }
}
