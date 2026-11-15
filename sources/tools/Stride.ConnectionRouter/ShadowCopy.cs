// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;

namespace Stride.ConnectionRouter
{
    /// <summary>
    /// Runs the router from a copy of its package folder, in <see cref="Folder"/>.
    /// Builds and package refreshes can then overwrite the package files while it keeps running.
    /// </summary>
    /// <remarks>
    /// Only one router runs at a time, so there is a single copy. It is replaced when a router of other files starts, once the previous router stopped.
    /// </remarks>
    internal static class ShadowCopy
    {
        private const string SourceFileName = ".source";

        /// <summary>
        /// Gets the folder of the copy. All its content is replaced with the copy, so nothing else may be kept there.
        /// </summary>
        public static string Folder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "stride", "router-shadowcopy");

        private static string SourceFile => Path.Combine(Folder, SourceFileName);

        /// <summary>
        /// Describes the files of an executable's folder: the path of the executable, the newest write time and the total size of the folder's files.
        /// It changes when the files change.
        /// </summary>
        /// <param name="executablePath">The path of the executable.</param>
        /// <returns>The description, which <see cref="IsCopyOf"/> compares.</returns>
        public static string GetSource(string executablePath)
        {
            var newestWriteTime = DateTime.MinValue;
            var totalSize = 0L;
            foreach (var file in new DirectoryInfo(Path.GetDirectoryName(executablePath)).EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if (file.LastWriteTimeUtc > newestWriteTime)
                    newestWriteTime = file.LastWriteTimeUtc;
                totalSize += file.Length;
            }
            return $"{Path.GetFullPath(executablePath)}|{newestWriteTime.Ticks:x}|{totalSize:x}";
        }

        /// <summary>
        /// Checks whether the copy holds the files described by <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The description of the files, from <see cref="GetSource"/>.</param>
        public static bool IsCopyOf(string source)
        {
            try
            {
                return File.ReadAllText(SourceFile) == source;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks whether a file is in the copy.
        /// </summary>
        public static bool IsInCopy(string filePath)
        {
            return string.Equals(Path.GetDirectoryName(Path.GetFullPath(filePath)), Folder, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Makes the copy hold the folder of an executable, unless it already does.
        /// No process may run from the copy, as its files are replaced.
        /// </summary>
        /// <param name="executablePath">The path of the executable.</param>
        /// <param name="source">The description of the files, from <see cref="GetSource"/>.</param>
        /// <returns>The path of the executable in the copy.</returns>
        public static string GetOrCreateCopy(string executablePath, string source)
        {
            if (!IsCopyOf(source))
            {
                // The description is removed first and written last, so that a copy stopped half-way is replaced next time
                Directory.CreateDirectory(Folder);
                File.Delete(SourceFile);
                foreach (var file in Directory.EnumerateFiles(Folder))
                    File.Delete(file);
                foreach (var folder in Directory.EnumerateDirectories(Folder))
                    Directory.Delete(folder, true);

                CopyFolder(Path.GetDirectoryName(executablePath), Folder);
                File.WriteAllText(SourceFile, source);
            }

            return Path.Combine(Folder, Path.GetFileName(executablePath));
        }

        private static void CopyFolder(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (var file in Directory.EnumerateFiles(source))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
            foreach (var folder in Directory.EnumerateDirectories(source))
                CopyFolder(folder, Path.Combine(target, Path.GetFileName(folder)));
        }
    }
}
