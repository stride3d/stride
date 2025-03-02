// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Stride.Core.IO;

/// <summary>
/// Defines a normalized file path. See <see cref="UPath"/> for details. This class cannot be inherited.
/// </summary>
[DataContract("file")]
[TypeConverter(typeof(UFileTypeConverter))]
public sealed class UFile : UPath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UFile"/> class.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    public UFile(string? filePath)
        : base(filePath, false)
    {
    }

    /// <summary>
    /// Gets the name of the file with its extension. Can be null.
    /// </summary>
    /// <returns>The name.</returns>
    public string? GetFileName()
    {
        var span = NameSpan;
        if (ExtensionSpan.IsValid)
        {
            span.Length = ExtensionSpan.Next - span.Start;
        }
        return span.IsValid ? FullPath.Substring(span) : null;
    }

    /// <summary>
    /// Gets the name of the file without its extension.
    /// </summary>
    /// <value>The name of file.</value>
    public string? GetFileNameWithoutExtension()
    {
        return NameSpan.IsValid ? FullPath.Substring(NameSpan) : null;
    }

    /// <summary>
    /// Gets the file path (<see cref="UPath.GetDirectory()"/> + '/' + <see cref="GetFileName()"/>) with the extension or drive. Can be an null if no filepath.
    /// </summary>
    /// <returns>The path.</returns>
    public string? GetDirectoryAndFileName()
    {
        var span = DirectorySpan;
        if (ExtensionSpan.IsValid)
        {
            span.Length = ExtensionSpan.Next - span.Start;
        }
        else if (NameSpan.IsValid)
        {
            span.Length = NameSpan.Next - span.Start;
        }
        return span.IsValid ? FullPath.Substring(span) : null;
    }

    /// <summary>
    /// Gets the file path (<see cref="UPath.GetDirectory()"/> + '/' + <see cref="GetFileName()"/>) without the extension or drive. Can be an null if no filepath.
    /// </summary>
    /// <returns>The path.</returns>
    public string? GetDirectoryAndFileNameWithoutExtension()
    {
        var span = DirectorySpan;
        if (NameSpan.IsValid)
        {
            span.Length = NameSpan.Next - span.Start;
        }
        return span.IsValid ? FullPath.Substring(span) : null;
    }

    /// <summary>
    /// Gets the extension of the file. Can be null.
    /// </summary>
    /// <returns>The extension.</returns>
    public string? GetFileExtension()
    {
        return ExtensionSpan.IsValid ? FullPath.Substring(ExtensionSpan) : null;
    }

    /// <summary>
    /// Gets the name of the file with its extension.
    /// </summary>
    /// <value>The name of file.</value>
    public string? GetFullPathWithoutExtension()
    {
        var span = new StringSpan(0, FullPath.Length);
        if (NameSpan.IsValid)
        {
            span.Length = NameSpan.Next;
        }
        return span.IsValid ? FullPath.Substring(span) : null;
    }

    /// <summary>
    /// Combines the specified left uniform location and right location and return a new <see cref="UFile"/>
    /// </summary>
    /// <param name="leftPath">The left path.</param>
    /// <param name="rightPath">The right path.</param>
    /// <returns>The combination of both paths.</returns>
    public static UFile Combine(UDirectory leftPath, UFile rightPath)
    {
        return UPath.Combine(leftPath, rightPath);
    }

    /// <summary>
    /// Makes this instance relative to the specified anchor directory.
    /// </summary>
    /// <param name="anchorDirectory">The anchor directory.</param>
    /// <returns>A relative path of this instance to the anchor directory.</returns>
    public new UFile MakeRelative(UDirectory anchorDirectory)
    {
        return (UFile)base.MakeRelative(anchorDirectory);
    }

    /// <summary>
    /// Determines whether the specified path is a valid <see cref="UFile"/>
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns><c>true</c> if the specified path is a valid <see cref="UFile"/>; otherwise, <c>false</c>.</returns>
    public static new bool IsValid(string path)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(path);
#else
        if (path is null) throw new ArgumentNullException(nameof(path));
#endif
        if (!UPath.IsValid(path))
        {
            return false;
        }
        if (path.Length > 0 && path.EndsWith(DirectorySeparatorChar, DirectorySeparatorCharAlt))
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="string"/> to <see cref="UPath"/>.
    /// </summary>
    /// <param name="fullPath">The full path.</param>
    /// <returns>The result of the conversion.</returns>
    [return: NotNullIfNotNull(nameof(fullPath))]
    public static implicit operator UFile?(string? fullPath)
    {
        return fullPath != null ? new UFile(fullPath) : null;
    }
}
