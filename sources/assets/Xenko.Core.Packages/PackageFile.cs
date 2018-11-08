// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using NuGet.Packaging;

namespace Xenko.Core.Packages
{
    /// <summary>
    /// Representation of a file in a package.
    /// TODO: Verify if this needs updating for NuGet 3.0
    /// </summary>
    public class PackageFile
    {
        private readonly string packagePath;
        private readonly IPackageFile packageFile;

        /// <summary>
        /// Initializes a new instance of <see cref="PackageFile"/> located in <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Path of the file in the package.</param>
        public PackageFile(string packagePath, string path)
        {
            this.packagePath = packagePath;
            Path = path;
        }

        public PackageFile(IPackageFile x)
        {
            Path = x.Path;
            SourcePath = (x as PhysicalPackageFile)?.SourcePath;
            packageFile = x;
        }

        /// <summary>
        /// Gets the full path of the file on the HDD.
        /// </summary>
        public string FullPath => System.IO.Path.Combine(packagePath, Path);

        /// <summary>
        /// Gets the path of the file inside the package.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the source path of the file on the hard drive (if it has a source).
        /// </summary>
        public string SourcePath { get; }

        /// <summary>
        /// Access to the stream content in read mode.
        /// </summary>
        /// <returns>A new stream reading file pointed by <see cref="Path"/>.</returns>
        public Stream GetStream()
        {
            return packageFile?.GetStream() ?? File.OpenRead(FullPath);
        }
    }
}
