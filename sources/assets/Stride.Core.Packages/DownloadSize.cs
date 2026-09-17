// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Packages;

/// <summary>Formats package download sizes the same way wherever they are shown (the stride CLI, Game Studio).</summary>
public static class DownloadSize
{
    /// <summary>The bytes downloaded so far, e.g. "45.2 MB". No total: it is not known before the downloads start.</summary>
    public static string Format(long downloadedBytes) => $"{Megabytes(downloadedBytes)} MB";

    /// <summary>
    /// "45.2 / 131.9 MB", for a download whose size is known (<paramref name="totalBytes"/> above 0), else as
    /// <see cref="Format(long)"/>.
    /// </summary>
    public static string Format(long downloadedBytes, long totalBytes)
        => totalBytes > 0 ? $"{Megabytes(downloadedBytes)} / {Megabytes(totalBytes)} MB" : Format(downloadedBytes);

    private static string Megabytes(long bytes) => $"{bytes / 1048576.0:0.0}";
}
