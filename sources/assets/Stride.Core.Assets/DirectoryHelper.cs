// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Helper class that contains methods to retrieve and manipulate SDK locations.
    /// </summary>
    public static class DirectoryHelper
    {
        private const string StrideSolution = @"build\Stride.sln";
        private const string StrideNuspec = @"stride.nuspec";
        private static string packageDirectoryOverride;

        /// <summary>
        /// Gets the path to the file corresponding to the given package name in the given directory.
        /// </summary>
        /// <param name="directory">The directory where the package file is located.</param>
        /// <param name="packageName">The name of the package.</param>
        /// <returns>The path to the file corresponding to the given package name in the given directory.</returns>
        public static string GetPackageFile(string directory, string packageName)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            return Path.Combine(directory, packageName + Package.PackageFileExtension);
        }

        /// <summary>
        /// Indicates whether the given directory is the root directory of the repository, when executing from a development build. 
        /// </summary>
        /// <param name="directory">The directory to check.</param>
        /// <returns><c>True</c> if the given directory is the root directory of the repository, <c>false</c> otherwise.</returns>
        public static bool IsRootDevDirectory(string directory)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            var strideSolution = Path.Combine(directory, StrideSolution);
            return File.Exists(strideSolution);
        }
    }
}
