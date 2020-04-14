// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;

namespace Stride.Core.IO
{
    /// <summary>
    /// A virtual file provider, that can returns a Stream for a given path.
    /// </summary>
    public interface IVirtualFileProvider : IDisposable
    {
        /// <summary>
        /// Gets or sets the root path of this provider. See remarks.
        /// </summary>
        /// <value>
        /// The root path.
        /// </value>
        /// <remarks>
        /// All path are relative to the root path.
        /// </remarks>
        string RootPath { get; }

        /// <summary>
        /// Gets the absolute  path for the specified local path from this provider.
        /// </summary>
        /// <param name="path">The path local to this instance.</param>
        /// <returns>An absolute path.</returns>
        string GetAbsolutePath(string path);

        /// <summary>
        /// Gets the absolute path and location if the specified path physically exist on the disk in an uncompressed form (could be inside another file).
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="filePath">The file containing the data.</param>
        /// <param name="start">The start offset in the file.</param>
        /// <param name="end">The end offset in the file (can be -1 if full file).</param>
        /// <returns>True if success, false if not supported and entry is found (note: even when true, the file might not actually exists).</returns>
        bool TryGetFileLocation(string path, out string filePath, out long start, out long end);

        /// <summary>
        /// Opens a Stream from the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="access">The access.</param>
        /// <param name="share">The process sharing mode.</param>
        /// <param name="streamFlags">The type of stream needed</param>
        /// <returns>The opened stream.</returns>
        Stream OpenStream(string path, VirtualFileMode mode, VirtualFileAccess access, VirtualFileShare share = VirtualFileShare.Read, StreamFlags streamFlags = StreamFlags.None);

        /// <summary>
        /// Returns the list of files from the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="searchPattern">The search pattern.</param>
        /// <param name="searchOption">The search option.</param>
        /// <returns>A list of files from the specified path</returns>
        string[] ListFiles(string path, string searchPattern, VirtualSearchOption searchOption);

        /// <summary>
        /// Creates all directories so that url exists.
        /// </summary>
        /// <param name="url">The URL.</param>
        void CreateDirectory(string url);

        /// <summary>
        /// Determines whether the specified path points to an existing directory.
        /// </summary>
        /// <param name="url">The path.</param>
        /// <returns></returns>
        bool DirectoryExists(string url);

        /// <summary>
        /// Determines whether the specified path points to an existing file.
        /// </summary>
        /// <param name="url">The path.</param>
        /// <returns></returns>
        bool FileExists(string url);

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="url">The URL.</param>
        void FileDelete(string url);

        /// <summary>
        /// Move the specified file specified from its sourceUrl to the location pointed by destinationUrl. Do not overwrite, throw IOException if the file can't be moved.
        /// </summary>
        /// <param name="sourceUrl">The source URL of the file</param>
        /// <param name="destinationUrl">The destination URL of the file</param>
        void FileMove(string sourceUrl, string destinationUrl);

        /// <summary>
        /// Move the specified file specified from its sourceUrl to the location pointed by destinationUrl in the destination provider. Do not overwrite, throw IOException if the file can't be moved.
        /// </summary>
        /// <param name="sourceUrl">The source URL.</param>
        /// <param name="destinationProvider">The destination provider.</param>
        /// <param name="destinationUrl">The destination URL, relative to the destination provider.</param>
        void FileMove(string sourceUrl, IVirtualFileProvider destinationProvider, string destinationUrl);

        /// <summary>
        /// Returns the size of the specified file in bytes
        /// </summary>
        /// <param name="url">The file or directory for which to obtain size</param>
        /// <returns>A long value representing the file size in bytes</returns>
        long FileSize(string url);

        /// <summary>
        /// Returns the date and time the specified file or directory was last written to. 
        /// </summary>
        /// <param name="url">The file or directory for which to obtain write date and time information.</param>
        /// <returns>A DateTime structure set to the date and time that the specified file or directory was last written to.</returns>
        DateTime GetLastWriteTime(string url);
    }
}
