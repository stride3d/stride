// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Stride.Core.IO
{
    /// <summary>
    /// Exposes the whole file system through folder similar to Cygwin.
    /// As an example, "/c/Program Files/Test/Data.dat" would work.
    /// </summary>
    public class DriveFileProvider : FileSystemProvider
    {
        public static string DefaultRootPath = "/drive";

        public DriveFileProvider(string rootPath) : base(rootPath, null)
        {
        }

        /// <summary>
        /// Resolves the VFS URL from a given file path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public string GetLocalPath(string filePath)
        {
            filePath = Path.GetFullPath(filePath);

            var resolveProviderResult = VirtualFileSystem.ResolveProvider(RootPath, true);
            var provider = resolveProviderResult.Provider as DriveFileProvider;

            if (provider == null)
                throw new InvalidOperationException();

            return provider.ConvertFullPathToUrl(filePath);
        }

        /// <inheritdoc/>
        protected override string ConvertUrlToFullPath(string url)
        {
            // Linux style: keep as is
            if (VolumeSeparatorChar == '/')
                return url;

            // TODO: Support more complex URL such as UNC or devices
            // Windows style: reprocess URL like cygwin
            var result = new StringBuilder(url.Length + 1);
            int separatorIndex = 0;

            foreach (var c in url.ToCharArray())
            {
                if (c == VirtualFileSystem.DirectorySeparatorChar || c == VirtualFileSystem.AltDirectorySeparatorChar)
                {
                    if (separatorIndex == 1)
                    {
                        // Add volume separator on second /
                        result.Append(VolumeSeparatorChar);
                    }

                    // Ignore first separator (before volume)
                    if (separatorIndex >= 1)
                    {
                        result.Append(DirectorySeparatorChar);
                    }

                    separatorIndex++;
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        /// <inheritdoc/>
        protected override string ConvertFullPathToUrl(string path)
        {
            // Linux style: keep as is
            if (VolumeSeparatorChar == '/')
                return path;

            // TODO: Support more complex URL such as UNC or devices
            // Windows style: reprocess URL like cygwin
            var result = new StringBuilder(path.Length + 1);

            result.Append(VirtualFileSystem.DirectorySeparatorChar);

            foreach (var c in path.ToCharArray())
            {
                if (c == VolumeSeparatorChar)
                {
                    // TODO: More advanced validation, i.e. is there no directory separator before volume separator, etc...
                }
                else if (c == DirectorySeparatorChar)
                {
                    result.Append(VirtualFileSystem.DirectorySeparatorChar);
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }
    }
}
