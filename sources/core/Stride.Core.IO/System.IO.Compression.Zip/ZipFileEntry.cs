// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ZipFileEntry.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   Represents an entry in Zip file directory
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace System.IO.Compression.Zip
{
    /// <summary>
    /// Represents an entry in Zip file directory
    /// </summary>
    public class ZipFileEntry
    {
        #region Public Properties

        /// <summary>
        /// Gets the user comment for file.
        /// </summary>
        public string Comment { get; internal set; }

        /// <summary>
        /// Gets the compressed file size.
        /// </summary>
        public uint CompressedSize { get; internal set; }

        /// <summary>
        /// Gets the 32-bit checksum of entire file.
        /// </summary>
        public uint Crc32 { get; internal set; }

        /// <summary>
        /// Gets the offset of file inside Zip storage.
        /// </summary>
        public uint FileOffset { get; internal set; }

        /// <summary>
        /// Gets the original file size.
        /// </summary>
        public uint FileSize { get; internal set; }

        /// <summary>
        /// Gets the full path and filename as stored in Zip.
        /// </summary>
        public string FilenameInZip { get; internal set; }

        /// <summary>
        /// Gets the offset of header information inside Zip storage.
        /// </summary>
        public uint HeaderOffset { get; internal set; }

        /// <summary>
        /// Gets the size of header information.
        /// </summary>
        public uint HeaderSize { get; internal set; }

        /// <summary>
        /// Gets the compression method.
        /// </summary>
        public Compression Method { get; internal set; }

        /// <summary>
        /// Gets the last modification time of file.
        /// </summary>
        public DateTime ModifyTime { get; internal set; }

        /// <summary>
        /// Gets the full path and filename of the containing zip file.
        /// </summary>
        public string ZipFileName { get; internal set; }

        #endregion
    }
}
