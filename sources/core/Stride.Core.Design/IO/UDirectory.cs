// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Stride.Core.IO;

/// <summary>
/// Defines a normalized directory path. See <see cref="UPath"/> for details. This class cannot be inherited.
/// </summary>
[DataContract("dir")]
[TypeConverter(typeof(UDirectoryTypeConverter))]
public sealed class UDirectory : UPath
{
    /// <summary>
    /// An empty directory.
    /// </summary>
    public static readonly UDirectory Empty = new(string.Empty);

    /// <summary>
    /// A this '.' directory.
    /// </summary>
    public static readonly UDirectory This = new(".");

    /// <summary>
    /// Initializes a new instance of the <see cref="UDirectory"/> class.
    /// </summary>
    /// <param name="directoryPath">The directory path.</param>
    public UDirectory(string? directoryPath) : base(directoryPath, true)
    {
    }

    internal UDirectory(string fullPath, StringSpan driveSpan, StringSpan directorySpan) : base(fullPath, driveSpan, directorySpan)
    {
    }

    /// <summary>
    /// Gets the name of the directory.
    /// </summary>
    /// <returns>The name of the directory.</returns>
    public string GetDirectoryName()
    {
        var index = FullPath.IndexOfReverse(DirectorySeparatorChar);
        return index >= 0 ? FullPath[(index + 1)..] : string.Empty;
    }

    /// <summary>
    /// Makes this instance relative to the specified anchor directory.
    /// </summary>
    /// <param name="anchorDirectory">The anchor directory.</param>
    /// <returns>A relative path of this instance to the anchor directory.</returns>
    public new UDirectory MakeRelative(UDirectory anchorDirectory)
    {
        return (UDirectory)base.MakeRelative(anchorDirectory);
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="string"/> to <see cref="UPath"/>.
    /// </summary>
    /// <param name="fullPath">The full path.</param>
    /// <returns>The result of the conversion.</returns>
    [return: NotNullIfNotNull(nameof(fullPath))]
    public static implicit operator UDirectory?(string? fullPath)
    {
        return fullPath != null ? new UDirectory(fullPath) : null;
    }

    /// <summary>
    /// Combines the specified left uniform location and right location and return a new <see cref="UDirectory"/>
    /// </summary>
    /// <param name="leftPath">The left path.</param>
    /// <param name="rightPath">The right path.</param>
    /// <returns>The combination of both paths.</returns>
    public static UDirectory Combine(UDirectory leftPath, UDirectory rightPath)
    {
        return UPath.Combine(leftPath, rightPath);
    }

    /// <summary>
    /// Determines whether this directory contains the specified path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns><c>true</c> if this directory contains the specified path; otherwise, <c>false</c>.</returns>
    public bool Contains(UPath path)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(path);
#else
        if (path is null) throw new ArgumentNullException(nameof(path));
#endif
        return path.FullPath.StartsWith(FullPath, StringComparison.OrdinalIgnoreCase) && path.FullPath.Length > FullPath.Length && path.FullPath[FullPath.Length] == DirectorySeparatorChar;
    }
}
