// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.IO
{
    /// <summary>
    /// File mode equivalent of <see cref="System.IO.FileMode"/>.
    /// </summary>
    public enum VirtualFileMode
    {
        /// <summary>
        /// Creates a new file. The function fails if a specified file exists.
        /// </summary>
        CreateNew = 1,

        /// <summary>
        /// Creates a new file, always.
        /// If a file exists, the function overwrites the file, clears the existing attributes, combines the specified file attributes,
        /// and flags with FILE_ATTRIBUTE_ARCHIVE, but does not set the security descriptor that the SECURITY_ATTRIBUTES structure specifies.
        /// </summary>
        Create = 2,

        /// <summary>
        /// Opens a file. The function fails if the file does not exist.
        /// </summary>
        Open = 3,

        /// <summary>
        /// Opens a file, always.
        /// If a file does not exist, the function creates a file as if dwCreationDisposition is CREATE_NEW.
        /// </summary>
        OpenOrCreate = 4,

        /// <summary>
        /// Opens a file and truncates it so that its size is 0 (zero) bytes. The function fails if the file does not exist.
        /// The calling process must open the file with the GENERIC_WRITE access right.
        /// </summary>
        Truncate = 5,

        /// <summary>
        /// Opens a file if it exists and go at the end, otherwise creates a new file.
        /// </summary>
        Append = 6,
    }
}
