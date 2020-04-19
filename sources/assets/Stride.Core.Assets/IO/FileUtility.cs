// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Stride.Core.Assets
{
    /// <summary>
    /// File Utilities methods.
    /// </summary>
    public class FileUtility
    {
        /// <summary>
        /// Determines whether the specified file is locked.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns><c>true</c> if the specified file is locked; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is null</exception>
        public static bool IsFileLocked(string filePath) => IsFileLocked(new FileInfo(filePath));


        /// <summary>
        /// Determines whether the specified file is locked.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns><c>true</c> if the specified file is locked; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="file"/> is null</exception>
        public static bool IsFileLocked(FileInfo file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            try
            {
                using (file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }
            }
            catch (IOException)
            {
                return true;
            }
            return false;            
        }

        /// <summary>
        /// Converts a relative path to an absolute path using the current working directoy.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>An absolute path.</returns>
        public static string GetAbsolutePath(string filePath)
        {
            return filePath == null ? null : Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, filePath));
        }

        /// <summary>
        /// Normalizes the file extension by adding a '.' prefix and making it lowercase.
        /// </summary>
        /// <param name="fileExtension">The file extension.</param>
        /// <returns>A normalized file extension.</returns>
        public static string NormalizeFileExtension(string fileExtension)
        {
            if (String.IsNullOrEmpty(fileExtension))
            {
                return fileExtension;
            }

            fileExtension = fileExtension.ToLowerInvariant();
            if (fileExtension.StartsWith("."))
            {
                return fileExtension;
            }
            return $".{fileExtension}";
        }


        /// <summary>
        /// Gets the file extensions normalized separated by ',' ';'.
        /// </summary>
        /// <param name="fileExtensions">The file extensions separated by ',' ';'.</param>
        /// <returns>An array of file extensions.</returns>
        public static HashSet<string> GetFileExtensionsAsSet(string fileExtensions)
        {
            if (fileExtensions == null) throw new ArgumentNullException(nameof(fileExtensions));
            var fileExtensionArray = fileExtensions.Split(new[] { ',', ';' }).Select(fileExt => fileExt.Trim().ToLowerInvariant()).ToList();
            var filteredExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var fileExtension in fileExtensionArray.Select(NormalizeFileExtension))
            {
                if (fileExtension == string.Empty)
                {
                    continue;
                }
                filteredExtensions.Add(fileExtension);
            }

            return filteredExtensions;
        }


        /// <summary>
        /// Gets the file extensions normalized separated by ',' ';'.
        /// </summary>
        /// <param name="fileExtensions">The file extensions separated by ',' ';'.</param>
        /// <returns>An array of file extensions.</returns>
        public static string[] GetFileExtensions(string fileExtensions)
        {
            return GetFileExtensionsAsSet(fileExtensions).ToArray();
        }

        public static IEnumerable<DirectoryInfo> EnumerateDirectories(string rootDirectory, SearchDirection direction)
        {
            if (rootDirectory == null) throw new ArgumentNullException(nameof(rootDirectory));

            var directory = new DirectoryInfo(rootDirectory);
            if (Directory.Exists(rootDirectory))
            {
                if (direction == SearchDirection.Down)
                {
                    yield return directory;
                    foreach (var subDirectory in directory.EnumerateDirectories("*", SearchOption.AllDirectories))
                    {
                        yield return subDirectory;
                    }
                }
                else
                {
                    do
                    {
                        yield return directory;
                        directory = directory.Parent;
                    }
                    while (directory != null);
                }
            }
        }
    }
}
