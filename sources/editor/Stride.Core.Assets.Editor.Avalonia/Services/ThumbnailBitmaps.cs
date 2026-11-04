// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Media.Imaging;

namespace Stride.Core.Assets.Editor.Avalonia.Services;

/// <summary>
/// Decodes the images of the thumbnails.
/// </summary>
internal static class ThumbnailBitmaps
{
    /// <summary>
    /// Decodes an encoded image (PNG...) from the start of <paramref name="stream"/>.
    /// </summary>
    /// <returns>The bitmap, or <c>null</c> when the stream holds no image Avalonia can decode.</returns>
    public static Bitmap? TryDecode(Stream stream)
    {
        try
        {
            if (stream.CanSeek)
                stream.Position = 0;
            return new Bitmap(stream);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
