// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.IO;

namespace Xenko.Core.IO
{
    /// <summary>
    /// A file system implementation for IVirtualFileProvider.
    /// </summary>
    public partial class FileSystemProvider : VirtualFileProviderBase
    {
        public static readonly char VolumeSeparatorChar = Path.VolumeSeparatorChar;
        public static readonly char DirectorySeparatorChar = Path.DirectorySeparatorChar;
        public static readonly char AltDirectorySeparatorChar = AltDirectorySeparatorChar == '/' ? '\\' : '/';

        /// <summary>
        /// Base path of this provider (every path will be relative to this one).
        /// </summary>
        private string localBasePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemProvider" /> class with the given base path.
        /// </summary>
        /// <param name="rootPath">The root path of this provider.</param>
        /// <param name="localBasePath">The path to a local directory where this instance will load the files from.</param>
        public FileSystemProvider(string rootPath, string localBasePath) : base(rootPath)
        {
            ChangeBasePath(localBasePath);
        }

        public void ChangeBasePath(string basePath)
        {
            localBasePath = basePath;

            if (localBasePath != null)
                localBasePath = localBasePath.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar);

            // Ensure localBasePath ends with a \
            if (localBasePath != null && !localBasePath.EndsWith(DirectorySeparatorChar.ToString()))
                localBasePath = localBasePath + DirectorySeparatorChar;
        }

        protected virtual string ConvertUrlToFullPath(string url)
        {
            if (localBasePath == null)
                return url;
            return localBasePath + url.Replace(VirtualFileSystem.DirectorySeparatorChar, DirectorySeparatorChar);
        }

        protected virtual string ConvertFullPathToUrl(string path)
        {
            if (localBasePath == null)
                return path;

            if (!path.StartsWith(localBasePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Trying to convert back a path that is not in this file system provider.");

            return path.Substring(localBasePath.Length).Replace(DirectorySeparatorChar, VirtualFileSystem.DirectorySeparatorChar);
        }

        public override bool DirectoryExists(string url)
        {
            var path = ConvertUrlToFullPath(url);
            return Directory.Exists(path);
        }

        /// <inheritdoc/>
        public override void CreateDirectory(string url)
        {
            var path = ConvertUrlToFullPath(url);
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unable to create directory [{0}]".ToFormat(path), ex);
            }
        }

        /// <inheritdoc/>
        public override bool FileExists(string url)
        {
            return File.Exists(ConvertUrlToFullPath(url));
        }

        public override long FileSize(string url)
        {
            var fileInfo = new FileInfo(ConvertUrlToFullPath(url));
            return fileInfo.Length;
        }

        /// <inheritdoc/>
        public override void FileDelete(string url)
        {
            File.Delete(ConvertUrlToFullPath(url));
        }

        /// <inheritdoc/>
        public override void FileMove(string sourceUrl, string destinationUrl)
        {
            File.Move(ConvertUrlToFullPath(sourceUrl), ConvertUrlToFullPath(destinationUrl));
        }

        /// <inheritdoc/>
        public override void FileMove(string sourceUrl, IVirtualFileProvider destinationProvider, string destinationUrl)
        {
            var fsProvider = destinationProvider as FileSystemProvider;
            if (fsProvider != null)
            {
                destinationProvider.CreateDirectory(destinationUrl.Substring(0, destinationUrl.LastIndexOf(VirtualFileSystem.DirectorySeparatorChar)));
                File.Move(ConvertUrlToFullPath(sourceUrl), fsProvider.ConvertUrlToFullPath(destinationUrl));
            }
            else
            {
                using (Stream sourceStream = OpenStream(sourceUrl, VirtualFileMode.Open, VirtualFileAccess.Read),
                    destinationStream = destinationProvider.OpenStream(destinationUrl, VirtualFileMode.CreateNew, VirtualFileAccess.Write))
                {
                    sourceStream.CopyTo(destinationStream);
                }
                File.Delete(sourceUrl);
            }
        }
    }
}
