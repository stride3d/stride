// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xenko.Core.Annotations;

namespace Xenko.Core.IO
{
    /// <summary>
    /// Base class that describes a uniform path and provides method to manipulate them. Concrete class are <see cref="UFile"/> and <see cref="UDirectory"/>.
    /// This class is immutable and its descendants are immutable. See remarks.
    /// </summary>
    /// <remarks>
    /// <para>A uniform path contains only characters '/' to separate directories and doesn't contain any successive
    /// '/' or './'. This class is used to represent a path, relative or absolute to a directory or filename.</para>
    /// <para>This class can be used to represent uniforms paths both on windows or unix platforms</para>
    /// TODO Provide more documentation on how to use this class
    /// </remarks>
    public abstract class UPath : IEquatable<UPath>, IComparable
    {
        private static readonly HashSet<char> InvalidFileNameChars = new HashSet<char>(Path.GetInvalidFileNameChars());

        private readonly int hashCode;

        protected readonly StringSpan DriveSpan;

        protected readonly StringSpan DirectorySpan;

        protected readonly StringSpan NameSpan;

        protected readonly StringSpan ExtensionSpan;

        /// <summary>
        /// The directory separator char '/' used to separate directory in an url.
        /// </summary>
        public const char DirectorySeparatorChar = '/';

        /// <summary>
        /// The directory separator char '\' used to separate directory in an url.
        /// </summary>
        public const char DirectorySeparatorCharAlt = '\\';

        /// <summary>
        /// The directory separator string '/' used to separate directory in an url.
        /// </summary>
        public const string DirectorySeparatorString = "/";

        /// <summary>
        /// The directory separator string '\' used to separate directory in an url.
        /// </summary>
        public const string DirectorySeparatorStringAlt = "\\";

        /// <summary>
        /// Initializes a new instance of the <see cref="UPath" /> class from a file path.
        /// </summary>
        /// <param name="filePath">The full path to a file.</param>
        /// <param name="isDirectory">if set to <c>true</c> the filePath is considered as a directory and not a filename.</param>
        internal UPath(string filePath, bool isDirectory)
        {
            if (!isDirectory && filePath != null && (filePath.EndsWith(DirectorySeparatorString) || filePath.EndsWith(DirectorySeparatorStringAlt) || filePath.EndsWith(Path.VolumeSeparatorChar)))
            {
                throw new ArgumentException("A file path cannot end with with directory char '\\' or '/', or a volume separator ':'.");
            }

            FullPath = Decode(filePath, isDirectory, out DriveSpan, out DirectorySpan, out NameSpan, out ExtensionSpan);
            hashCode = ComputeStringHashCodeCaseInsensitive(FullPath);
        }

        protected UPath([NotNull] string fullPath, StringSpan driveSpan, StringSpan directorySpan)
        {
            FullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
            hashCode = ComputeStringHashCodeCaseInsensitive(fullPath);
            DriveSpan = driveSpan;
            DirectorySpan = directorySpan;
        }

        /// <summary>
        /// Gets the full path ((drive?)(directory?/)(name.ext?)). An empty path is an empty string.
        /// </summary>
        /// <value>The full path.</value>
        /// <remarks>This property cannot be null.</remarks>
        [NotNull]
        public string FullPath { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has a <see cref="GetDrive"/> != null.
        /// </summary>
        /// <value><c>true</c> if this instance has drive; otherwise, <c>false</c>.</value>
        public bool HasDrive => DriveSpan.IsValid;

        /// <summary>
        /// Gets a value indicating whether this instance has a <see cref="GetDirectory()"/> != null;
        /// </summary>
        /// <value><c>true</c> if this instance has directory; otherwise, <c>false</c>.</value>
        public bool HasDirectory => !IsFile || NameSpan.Start > 0;

        /// <summary>
        /// Gets a value indicating whether this location is a relative location.
        /// </summary>
        /// <value><c>true</c> if this instance is relative; otherwise, <c>false</c>.</value>
        public bool IsRelative => !IsAbsolute;

        /// <summary>
        /// Determines whether this instance is absolute.
        /// </summary>
        /// <returns><c>true</c> if this instance is absolute; otherwise, <c>false</c>.</returns>
        public bool IsAbsolute => HasDrive || (DirectorySpan.IsValid && FullPath[DirectorySpan.Start] == DirectorySeparatorChar);

        /// <summary>
        /// Gets a value indicating whether this instance is a location to a file. Can be null.
        /// </summary>
        /// <value><c>true</c> if this instance is file; otherwise, <c>false</c>.</value>
        public bool IsFile => NameSpan.IsValid || ExtensionSpan.IsValid;

        /// <summary>
        /// Gets the type of the path (absolute or relative).
        /// </summary>
        /// <value>The type of the path.</value>
        public UPathType PathType => IsAbsolute ? UPathType.Absolute : UPathType.Relative;

        /// <summary>
        /// Indicates whether the specified <see cref="UPath"/> is null or empty.
        /// </summary>
        /// <param name="path">The path to test</param>
        /// <returns><c>true</c> if the value parameter is null or empty, otherwise <c>false</c>.</returns>
        public static bool IsNullOrEmpty(UPath path)
        {
            return string.IsNullOrEmpty(path?.FullPath);
        }

        /// <summary>
        /// Gets the drive (contains the ':' if any), can be null.
        /// </summary>
        /// <returns>The drive.</returns>
        [CanBeNull]
        public string GetDrive()
        {
            return DriveSpan.IsValid ? FullPath.Substring(DriveSpan) : null;
        }

        /// <summary>
        /// Gets the directory. Can be null. It won't contain the drive if one is specified.
        /// </summary>
        /// <returns>The directory.</returns>
        [CanBeNull]
        [Obsolete("This method is obsolete. Use GetFullDirectory")]
        public string GetDirectory()
        {
            if (DirectorySpan.IsValid)
            {
                // Case if we just have a directory without trailing '/' or just a '/', we keep it as is.
                if ((FullPath[DirectorySpan.End] != DirectorySeparatorChar) || (DirectorySpan.Length == 1))
                {
                    return FullPath.Substring(DirectorySpan);
                }
                return FullPath.Substring(DirectorySpan.Start, DirectorySpan.Length - 1);
            }
            if (DriveSpan.IsValid & (NameSpan.IsValid || ExtensionSpan.IsValid))
            {
                return "/";
            }
            return null;
        }

        /// <summary>
        /// Gets the parent directory of this instance. For a file, this is the directory directly containing the file.
        /// For a directory, this is the parent directory.
        /// </summary>
        /// <returns>The parent directory or <see cref="UDirectory.Empty"/> if no directory found.</returns>
        public UDirectory GetParent()
        {
            if (DirectorySpan.IsValid)
            {
                // Find last index of '/' in this instance. When it has a File we know where the '/', so no need
                // to look it up.
                var index = IsFile ? DirectorySpan.End : FullPath.IndexOfReverse(DirectorySeparatorChar);
                if (index >= 0)
                {
                    // We cannot remove the trailing '/' of a parent which is 'C:/' or '/'.
                    index = (index == (DriveSpan.IsValid ? DriveSpan.Next : 0) ? index + 1 : index);
                    return new UDirectory(FullPath.Substring(0, index), DriveSpan, new StringSpan(DirectorySpan.Start, index - DirectorySpan.Start));
                }
            }
            return UDirectory.Empty;
        }

        /// <summary>
        /// Decomposition of this instance in its subcomponents which are made of the drive if any,
        /// the directories and the filename (including its extension).
        /// </summary>
        /// <returns>An IEnumerable of all the components of this instance.</returns>
        [NotNull]
        public IReadOnlyCollection<string> GetComponents()
        {
            var list = new List<string>(FullPath.Count(pathItem => pathItem == DirectorySeparatorChar) + 1);
            if (DriveSpan.IsValid)
            {
                list.Add(FullPath.Substring(DriveSpan));
            }

            if (DirectorySpan.IsValid && (DirectorySpan.Length >= 1))
            {
                foreach (var s in FullPath.Substring(DirectorySpan.Start, DirectorySpan.Length).Split(new char[1] { DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
                {
                    list.Add(s);
                }
            }

            var file = this as UFile;
            var fileName = file?.GetFileName();
            if (fileName != null)
            {
                list.Add(fileName);
            }

            return list;
        }

        /// <summary>
        /// Gets the full directory with <see cref="GetDrive()"/> + <see cref="GetDirectory()"/> or empty directory.
        /// </summary>
        /// <returns>System.String.</returns>
        [NotNull]
        public UDirectory GetFullDirectory()
        {
            if (IsFile)
            {
                // No directory in this path
                if (NameSpan.Start == 0)
                    return new UDirectory(null);
                // This path only contains a leading '/', we should return it
                if (NameSpan.Start == 1)
                    return new UDirectory("/", DriveSpan, DirectorySpan);
                // This path contains only 'c:/somefile', we should return 'c:/'
                if (DriveSpan.IsValid && (DriveSpan.Next == NameSpan.Start - 1))
                    return new UDirectory(FullPath.Substring(0, NameSpan.Start), DriveSpan, DirectorySpan);

                // Return the path until the name, excluding the last '/'
                return new UDirectory(FullPath.Substring(0, NameSpan.Start - 1), DriveSpan, DirectorySpan);
            }
            // Either a directory or a null path
            return this as UDirectory ?? new UDirectory(null);
        }

        public bool Equals(UPath other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(FullPath, other.FullPath, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals(obj as UPath);
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        private static int ComputeStringHashCodeCaseInsensitive([NotNull] string text)
        {
            return text.Aggregate(0, (current, t) => (current * 397) ^ char.ToLowerInvariant(t));
        }

        public int CompareTo(object obj)
        {
            if (obj is UPath uPath)
            {
                return string.Compare(FullPath, uPath.FullPath, StringComparison.OrdinalIgnoreCase);
            }
            return 0;
        }

        public override string ToString()
        {
            return FullPath;
        }

        /// <summary>
        /// Converts this path to a Windows path (/ replaced by \)
        /// </summary>
        /// <returns>A string representation of this path in windows form.</returns>
        [NotNull]
        public string ToWindowsPath()
        {
            return FullPath.Replace('/', '\\');
        }

        /// <summary>
        /// Implements the ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(UPath left, UPath right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Implements the !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(UPath left, UPath right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Combines the specified left uniform location and right location and return a new <see cref="UPath"/>
        /// </summary>
        /// <param name="leftPath">The left path.</param>
        /// <param name="rightPath">The right path.</param>
        /// <returns>The combination of both paths.</returns>
        [NotNull]
        public static T Combine<T>([NotNull] UDirectory leftPath, [NotNull] T rightPath) where T : UPath
        {
            if (leftPath == null) throw new ArgumentNullException(nameof(leftPath));
            if (rightPath == null) throw new ArgumentNullException(nameof(rightPath));

            // If right path is absolute, return it directly
            if (rightPath.IsAbsolute)
            {
                return rightPath;
            }

            var separator = string.IsNullOrEmpty(leftPath.FullPath) || string.IsNullOrEmpty(rightPath.FullPath) ? string.Empty : DirectorySeparatorString;
            var path = $"{leftPath.FullPath}{separator}{rightPath.FullPath}";
            return rightPath is UFile ? (T)(object)new UFile(path) : (T)(object)new UDirectory(path);
        }

        /// <summary>
        /// Makes this instance relative to the specified anchor directory.
        /// </summary>
        /// <param name="anchorDirectory">The anchor directory.</param>
        /// <returns>A relative path of this instance to the anchor directory.</returns>
        public UPath MakeRelative([NotNull] UDirectory anchorDirectory)
        {
            if (anchorDirectory == null) throw new ArgumentNullException(nameof(anchorDirectory));

            // If the toRelativize path is already relative, don't bother
            if (IsRelative)
            {
                return this;
            }

            // If anchor directory is not absolute directory, throw an error
            if (!anchorDirectory.IsAbsolute)
            {
                throw new ArgumentException(@"Expecting an absolute directory", nameof(anchorDirectory));
            }

            if (anchorDirectory.HasDrive != HasDrive)
            {
                throw new InvalidOperationException("Path should have no drive information/or both drive information simultaneously");
            }

            // Return a "." when the directory is the same
            if (this is UDirectory && anchorDirectory == this)
            {
                return UDirectory.This;
            }

            // Get the full path of the anchor directory
            var anchorPath = anchorDirectory.FullPath;

            // Builds an absolute path for the toRelative path (directory-only)
            var absoluteFile = Combine(anchorDirectory, this);
            var absolutePath = absoluteFile.GetFullDirectory().FullPath;

            var relativePath = new StringBuilder();

            var index = anchorPath.Length;
            var foundCommonRoot = false;
            for (; index >= 0; index--)
            {
                // Need to be a directory separator or end of string
                if (!((index == anchorPath.Length || anchorPath[index] == DirectorySeparatorChar)))
                    continue;

                // Absolute path needs to also have a directory separator at the same location (or end of string)
                if (index == absolutePath.Length || (index < absolutePath.Length && absolutePath[index] == DirectorySeparatorChar))
                {
                    if (string.Compare(anchorPath, 0, absolutePath, 0, index, true) == 0)
                    {
                        foundCommonRoot = true;
                        break;
                    }
                }

                relativePath.Append("..").Append(DirectorySeparatorChar);
            }

            if (!foundCommonRoot)
            {
                return this;
            }

            if (index < absolutePath.Length && absolutePath[index] == DirectorySeparatorChar)
            {
                index++;
            }

            relativePath.Append(absolutePath.Substring(index));
            if (absoluteFile is UFile file)
            {
                // If not empty, add a separator
                if (relativePath.Length > 0)
                    relativePath.Append(DirectorySeparatorChar);

                // Add filename
                relativePath.Append(file.GetFileName());
            }
            var newPath = relativePath.ToString();
            return !IsFile ? (UPath)new UDirectory(newPath) : new UFile(newPath);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="UPath"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>The result of the conversion.</returns>
        [CanBeNull]
        public static implicit operator string(UPath url)
        {
            return url?.FullPath;
        }

        /// <summary>
        /// Determines whether the specified path contains some directory characeters '\' or '/'
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if the specified path contains some directory characeters '\' or '/'; otherwise, <c>false</c>.</returns>
        public static bool HasDirectoryChars(string path)
        {
            return (path != null && (path.Contains(DirectorySeparatorChar) || path.Contains(DirectorySeparatorCharAlt)));
        }

        /// <summary>
        /// Determines whether the specified path is a valid <see cref="UPath"/>
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if the specified path is valid; otherwise, <c>false</c>.</returns>
        public static bool IsValid(string path)
        {
            string error;
            Normalize(path, out error);
            return error == null;
        }

        /// <summary>
        /// Normalize a path by replacing '\' by '/' and transforming relative '..' or current path '.' to an absolute path. See remarks.
        /// </summary>
        /// <param name="pathToNormalize">The path automatic normalize.</param>
        /// <returns>A normalized path.</returns>
        /// <exception cref="System.ArgumentException">If path is invalid</exception>
        /// <remarks>Unlike <see cref="System.IO.Path" /> , this doesn't make a path absolute to the actual file system.</remarks>
        [NotNull]
        public static string Normalize(string pathToNormalize)
        {
            string error;
            var result = Normalize(pathToNormalize, out error);
            if (error != null)
            {
                throw new ArgumentException(error, nameof(pathToNormalize));
            }
            return result.ToString();
        }

        /// <summary>
        /// Normalize a path by replacing '\' by '/' and transforming relative '..' or current path '.' to an absolute path. See remarks.
        /// </summary>
        /// <param name="pathToNormalize">The path automatic normalize.</param>
        /// <param name="error">The error or null if no errors.</param>
        /// <returns>A normalized path or null if there is an error.</returns>
        /// <remarks>Unlike <see cref="System.IO.Path" /> , this doesn't make a path absolute to the actual file system.</remarks>
        [CanBeNull]
        public static StringBuilder Normalize(string pathToNormalize, out string error)
        {
            StringSpan drive;
            StringSpan directoryOrFileName;
            StringSpan fileName;
            return Normalize(pathToNormalize, out drive, out directoryOrFileName, out fileName, out error);
        }

        /// <summary>
        /// Possible state when normalizing a path.
        /// </summary>
        private enum NormalizationState
        {
            StartComponent,
            InComponent,
            VolumeSeparator,
            DirectorySeparator,
        }

        /// <summary>
        /// Normalize a path by replacing '\' by '/' and transforming relative '..' or current path '.' to an absolute path. See remarks.
        /// </summary>
        /// <param name="pathToNormalize">The path automatic normalize.</param>
        /// <param name="drive">The drive character region.</param>
        /// <param name="directoryOrFileName">The directory.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="error">The error or null if no errors.</param>
        /// <returns>A normalized path or null if there is an error.</returns>
        /// <remarks>Unlike <see cref="System.IO.Path" /> , this doesn't make a path absolute to the actual file system.</remarks>
        [CanBeNull]
        public static unsafe StringBuilder Normalize(string pathToNormalize, out StringSpan drive, out StringSpan directoryOrFileName, out StringSpan fileName, out string error)
        {
            drive = new StringSpan();
            directoryOrFileName = new StringSpan();
            fileName = new StringSpan();
            error = null;
            var path = pathToNormalize;
            if (path == null)
            {
                return null;
            }
            var countDirectories = pathToNormalize.Count(pathItem => pathItem == DirectorySeparatorChar ||
                                                                     pathItem == DirectorySeparatorCharAlt ||
                                                                     pathItem == Path.VolumeSeparatorChar);

            // Safeguard if count directories is going wild
            if (countDirectories > 1024)
            {
                error = "Path contains too many directory '/' separator or ':'";
                return null;
            }

            // Optimize the code by using stack alloc in order to avoid allocation of a List<StringSpan>()
            var currentPath = -1;
            var state = NormalizationState.StartComponent;
            var hasDriveSpan = false;
            var paths = stackalloc StringSpan[countDirectories + 1];
            var builder = new StringBuilder(pathToNormalize.Length);

            // Iterate on all chars on original path
            foreach (var pathItem in pathToNormalize)
            {
                // Check if we have a directory separator
                if (pathItem == DirectorySeparatorChar || pathItem == DirectorySeparatorCharAlt)
                {
                    // Add only non consecutive '/'
                    if (state != NormalizationState.DirectorySeparator)
                    {
                        // Special case where path is starting with "/" or with "X:/", we will create
                        // an entry just for the "/".
                        if ((state == NormalizationState.StartComponent) || (state == NormalizationState.VolumeSeparator))
                        {
                            currentPath++;
                            paths[currentPath] = new StringSpan(builder.Length, 1);
                        }
                        else
                        {
                            paths[currentPath].Length++;
                        }
                        builder.Append(DirectorySeparatorChar);

                        // We are either reading more directory separator or reading a new component.
                        state = NormalizationState.DirectorySeparator;
                    }
                }
                else if (pathItem == Path.VolumeSeparatorChar)
                {
                    // Check in case of volume separator ':'
                    if (hasDriveSpan)
                    {
                        error = "Path contains more than one drive ':' separator";
                        return null;
                    }

                    if (state == NormalizationState.DirectorySeparator)
                    {
                        error = "Path cannot contain a drive ':' separator after a backslash";
                        return null;
                    }

                    if (state == NormalizationState.StartComponent)
                    {
                        error = "Path cannot start with a drive ':' separator";
                        return null;
                    }

                    // Append the volume ':'
                    builder.Append(pathItem);
                    paths[currentPath].Length++;
                    hasDriveSpan = true;

                    state = NormalizationState.VolumeSeparator; // We are expecting to read a directory separator now
                }
                else if (!InvalidFileNameChars.Contains(pathItem))
                {
                    if (state == NormalizationState.VolumeSeparator)
                    {
                        error = @"Path must contain a separator '/' or '\' after the volume separator ':'";
                        return null;
                    }
                    if ((state == NormalizationState.StartComponent) || (state == NormalizationState.DirectorySeparator))
                    {
                        // We are starting a new component. Check if previous one is either '..' or '.', in which case
                        // we can simplify
                        TrimParentAndSelfPath(builder, ref currentPath, paths, hasDriveSpan, false);
                        currentPath++;
                        paths[currentPath] = new StringSpan(builder.Length, 0);
                    }
                    builder.Append(pathItem);
                    paths[currentPath].Length++;
                    state = NormalizationState.InComponent; // We are expecting to read either a character, a separator or a volume separator;
                }
                else
                {
                    // Else the character is invalid
                    error = "Invalid character [{0}] found in path [{1}]".ToFormat(pathItem, pathToNormalize);
                    return null;
                }
            }

            // Remove trailing '..' or '.'
            TrimParentAndSelfPath(builder, ref currentPath, paths, hasDriveSpan, true);
            // Remove trailing if and only if the path content is not "/" or "c:/".
            if ((builder.Length > (hasDriveSpan ? paths[0].Next + 1 : 1)) && (builder[builder.Length - 1] == DirectorySeparatorChar))
            {
                builder.Length = builder.Length - 1;
                paths[currentPath].Length--;
            }

            // Go back to upper path if current is not vaid
            if (currentPath > 0 && !paths[currentPath].IsValid)
            {
                currentPath--;
            }

            // Copy the drive, directory, filename information to the output
            var startDirectory = 0;
            if (hasDriveSpan)
            {
                drive = paths[0];
                startDirectory = 1;
            }

            // If there is any directory information, process it
            if (startDirectory <= currentPath)
            {
                directoryOrFileName.Start = paths[startDirectory].Start;
                if (currentPath == startDirectory)
                {
                    directoryOrFileName.Length = paths[startDirectory].Length;
                }
                else
                {
                    directoryOrFileName.Length = paths[currentPath - 1].Next - directoryOrFileName.Start;

                    if (paths[currentPath].IsValid)
                    {
                        // In case last path is a parent '..' don't include it in fileName
                        if (IsParentComponentPath(builder, paths[currentPath]))
                        {
                            directoryOrFileName.Length += paths[currentPath].Length;
                        }
                        else
                        {
                            fileName.Start = paths[currentPath].Start;
                            fileName.Length = builder.Length - fileName.Start;
                        }
                    }
                }
            }

            return builder;
        }

        /// <summary>
        /// Does `builder.Substring(path)` represent either '..' or '../'?
        /// </summary>
        /// <param name="builder">String holding path.</param>
        /// <param name="path">Span of component to compare against.</param>
        /// <returns>True if it represents a parent directory.</returns>
        private static bool IsParentComponentPath(StringBuilder builder, StringSpan path)
        {
            if (((path.Length == 2) || (path.Length == 3)) && (builder[path.Start] == '.') && (builder[path.Start + 1] == '.'))
            {
                return (path.Length == 2) || (builder[path.Start + 2] == DirectorySeparatorChar);
            }
            return false;
        }

        /// <summary>
        /// Does `builder.Substring(path)` represent either '.' or './'?
        /// </summary>
        /// <param name="builder">String holding path.</param>
        /// <param name="path">Span of component to compare against.</param>
        /// <returns>True if it represents a parent directory.</returns>
        private static bool IsRelativeCurrentComponentPath(StringBuilder builder, StringSpan path)
        {
            if (((path.Length == 1) || (path.Length == 2)) && (builder[path.Start] == '.'))
            {
                return (path.Length == 1) || (builder[path.Start + 1] == DirectorySeparatorChar);
            }
            return false;
        }

        /// <summary>
        /// Trims the path by removing unecessary '..' and '.' path items.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="currentPath">The current path.</param>
        /// <param name="paths">The paths.</param>
        /// <param name="hasDrivePath">Does path has a drive letter in it?</param>
        /// <param name="isLastTrim">if set to <c>true</c> is last trim to occur.</param>
        private static unsafe void TrimParentAndSelfPath(StringBuilder builder, ref int currentPath, StringSpan* paths, bool hasDrivePath, bool isLastTrim)
        {
            if (currentPath < 0)
                return;

            var path = paths[currentPath];
            if (IsParentComponentPath(builder, path))
            {
                // If we have 2 or more components we can remove them but only if the
                // previous path is not already a relative path.
                if ((currentPath > 0) && !IsParentComponentPath(builder, paths[currentPath - 1]))
                {
                    if (currentPath == 1)
                    {
                        // Case of just 'a/../' or '/../'
                        if (paths[0].Length == 1)
                        {
                            currentPath = 0;
                            paths[0].Length = 1;
                        }
                        else
                        {
                            // We are back to an empty path.
                            currentPath = -1;
                            builder.Length = 0;
                            return;
                        }
                    }
                    else if ((currentPath == 2) && hasDrivePath)
                    {
                        // Case of just 'c:/..' which becomes 'c:/'.
                        currentPath--;
                    }
                    else
                    {
                        // Case of something like '.../a/b/../' => '.../a/'
                        currentPath = currentPath - 2;
                    }
                    // The new length is where the last removed component started
                    builder.Length = paths[currentPath + 1].Start;
                }
            }
            else if (IsRelativeCurrentComponentPath(builder, path) && ((isLastTrim && currentPath > 0) || !isLastTrim))
            {
                // We do not need the current component, we starts from the parent if any (or no parent if !isLastTrim)
                currentPath--;
                // The new length is where the last removed component started
                builder.Length = paths[currentPath + 1].Start;
            }
        }

        [NotNull]
        private static string Decode(string pathToNormalize, bool isPathDirectory, out StringSpan drive, out StringSpan directory, out StringSpan fileName, out StringSpan fileExtension)
        {
            drive = new StringSpan();
            directory = new StringSpan();
            fileName = new StringSpan();
            fileExtension = new StringSpan();

            if (string.IsNullOrWhiteSpace(pathToNormalize))
            {
                return string.Empty;
            }

            // Normalize path
            // TODO handle network path/http/file path
            string error;
            var path = Normalize(pathToNormalize, out drive, out directory, out fileName, out error);
            if (error != null)
            {
                throw new ArgumentException(error);
            }

            if (isPathDirectory)
            {
                // If we are expecting a directory, merge the fileName with the directory
                if (fileName.IsValid)
                {
                    if (directory.IsValid)
                    {
                        // Case of '../file'
                        directory.Length += fileName.Length;
                    }
                    else if (drive.IsValid)
                    {
                        // case of 'C:/file'
                        directory.Start = drive.Next;
                        directory.Length = fileName.Length + 1;
                    }
                    else
                    {
                        // Case of just a file 'file', make sure to include the leading '/' if there is one,
                        // which is why we don't just do 'directory = fileName'.
                        directory.Start = 0;
                        directory.Length = fileName.Next;
                    }
                    fileName = new StringSpan();
                }
                else if (drive.IsValid && !directory.IsValid)
                {
                    // Case of just C:, we need to add a '/' to be a valid directory
                    path.Append(DirectorySeparatorChar);
                    directory.Start = drive.Next;
                    directory.Length = 1;
                }
            }
            else
            {
                // In case this is only a directory name and we are expecting a filename, gets the directory name as a filename
                if (directory.IsValid && !fileName.IsValid)
                {
                    fileName = directory;
                    directory = new StringSpan();
                }

                if (fileName.IsValid)
                {
                    var extensionIndex = path.LastIndexOf('.', fileName.Start);
                    if (extensionIndex >= 0)
                    {
                        fileName.Length = extensionIndex - fileName.Start;
                        fileExtension.Start = extensionIndex;
                        fileExtension.Length = path.Length - extensionIndex;
                    }
                }
            }

            return path.ToString();
        }
    }
}
