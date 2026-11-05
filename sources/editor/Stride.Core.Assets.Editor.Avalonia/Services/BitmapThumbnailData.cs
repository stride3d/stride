// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Media.Imaging;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Storage;
using Stride.Core.Assets.Presentation.Services;

namespace Stride.Core.Assets.Editor.Avalonia.Services;

/// <summary>
/// The thumbnail of an asset built by the thumbnail service: the PNG stream of the build, decoded into an Avalonia
/// <see cref="Bitmap"/> that the views show as is.
/// </summary>
public sealed class BitmapThumbnailData : ThumbnailData
{
    private static readonly ObjectCache<ObjectId, Bitmap> Cache = new(512);
    private Stream? thumbnailBitmapStream;

    /// <param name="thumbnailId">The identifier of the thumbnail.</param>
    /// <param name="thumbnailBitmapStream">The encoded image (a PNG).</param>
    public BitmapThumbnailData(ObjectId thumbnailId, Stream thumbnailBitmapStream)
        : base(thumbnailId)
    {
        this.thumbnailBitmapStream = thumbnailBitmapStream;
    }

    /// <inheritdoc />
    protected override object? BuildImageSource()
    {
        if (thumbnailBitmapStream is not { CanRead: true } stream)
            return null;

        var result = Cache.TryGet(thumbnailId);
        if (result != null)
            return result;

        if (ThumbnailBitmaps.TryDecode(stream) is not { } bitmap)
            return null;

        Cache.Cache(thumbnailId, bitmap);
        return bitmap;
    }

    /// <inheritdoc />
    protected override void FreeBuildingResources()
    {
        thumbnailBitmapStream?.Dispose();
        thumbnailBitmapStream = null;
    }
}
