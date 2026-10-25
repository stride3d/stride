// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Editors;

/// <summary>
/// The state of a file on disk (existence, length and last write time), which tells an editor that another program changed
/// the file without watching the file system.
/// </summary>
/// <param name="Exists">Whether the file existed when the signature was captured.</param>
/// <param name="Length">The length of the file in bytes, or <c>0</c> when it did not exist.</param>
/// <param name="LastWriteTimeUtc">The last write time of the file in UTC, or <see langword="default"/> when it did not exist.</param>
public readonly record struct FileContentSignature(bool Exists, long Length, DateTime LastWriteTimeUtc)
{
    /// <summary>
    /// Captures the current signature of a file.
    /// </summary>
    /// <param name="path">The OS path of the file, or <c>null</c>.</param>
    /// <returns>
    /// The signature of the file, or the default signature (<see cref="Exists"/> is <c>false</c>) when <paramref name="path"/>
    /// is <c>null</c> or empty or the file does not exist.
    /// </returns>
    public static FileContentSignature Capture(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return default;

        var info = new FileInfo(path);
        return new FileContentSignature(true, info.Length, info.LastWriteTimeUtc);
    }
}
