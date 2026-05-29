// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_AVFOUNDATION
using System;
using System.IO;

namespace Stride.Audio;

// Copies the offset+length bundle slice to a temp file so AVAsset can take a file URL.
internal static class AVFoundationAssetSliceHelper
{
    public static string ExtractAssetSliceToTempFile(string sourcePath, long startPosition, long length, string suffix)
    {
        var temp = Path.Combine(Path.GetTempPath(), $"stride-{suffix}-{Guid.NewGuid():N}.bin");
        using var src = File.OpenRead(sourcePath);
        using var dst = File.Create(temp);

        src.Seek(startPosition, SeekOrigin.Begin);
        var buffer = new byte[64 * 1024];
        long remaining = length;
        while (remaining > 0)
        {
            int toRead = (int)Math.Min(buffer.Length, remaining);
            int read = src.Read(buffer, 0, toRead);
            if (read <= 0)
                break;
            dst.Write(buffer, 0, read);
            remaining -= read;
        }
        return temp;
    }
}
#endif
