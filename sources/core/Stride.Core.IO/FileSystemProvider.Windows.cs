// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_WINDOWS_DESKTOP || XENKO_PLATFORM_UNIX || XENKO_PLATFORM_UWP
using System;
using System.IO;
using System.Linq;

namespace Xenko.Core.IO
{
    /// <summary>
    /// A file system implementation for IVirtualFileProvider.
    /// </summary>
    public partial class FileSystemProvider
    {
        public override string GetAbsolutePath(string path)
        {
            return ConvertUrlToFullPath(path);
        }

        /// <inheritdoc/>
        public override bool TryGetFileLocation(string path, out string filePath, out long start, out long end)
        {
            filePath = ConvertUrlToFullPath(path);
            start = 0;
            end = -1;
            return true;
        }

        /// <inheritdoc/>
        public override Stream OpenStream(string url, VirtualFileMode mode, VirtualFileAccess access, VirtualFileShare share = VirtualFileShare.Read, StreamFlags streamFlags = StreamFlags.None)
        {
            if (localBasePath != null && url.Split(VirtualFileSystem.DirectorySeparatorChar, VirtualFileSystem.AltDirectorySeparatorChar).Contains(".."))
                throw new InvalidOperationException("Relative path is not allowed in FileSystemProvider.");
            return new FileStream(ConvertUrlToFullPath(url), (FileMode)mode, (FileAccess)access, (FileShare)share);
        }

        /// <inheritdoc/>
        public override string[] ListFiles(string url, string searchPattern, VirtualSearchOption searchOption)
        {
            return Directory.GetFiles(ConvertUrlToFullPath(url), searchPattern, (SearchOption)searchOption).Select(ConvertFullPathToUrl).ToArray();
        }

        public override DateTime GetLastWriteTime(string url)
        {
            return File.GetLastWriteTime(ConvertUrlToFullPath(url));
        }
    }
}
#endif
