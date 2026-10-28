// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Storage;
using Stride.Graphics;

namespace Stride.Editor.Thumbnails;

/// <summary>
/// The thumbnail of an asset built by the thumbnail service, decoded from the PNG stream the build produced into a
/// <see cref="Image"/>. The view of each toolkit converts the <see cref="ThumbnailData.Presenter"/> image to its own bitmap type.
/// </summary>
public sealed class BitmapThumbnailData : ThumbnailData
{
    private static readonly ObjectCache<ObjectId, Image> Cache = new(512);
    private Stream? thumbnailBitmapStream;

    public BitmapThumbnailData(ObjectId thumbnailId, Stream thumbnailBitmapStream)
        : base(thumbnailId)
    {
        this.thumbnailBitmapStream = thumbnailBitmapStream;
    }

    /// <inheritdoc />
    protected override object? BuildImageSource()
    {
        return BuildAsImage(thumbnailId, thumbnailBitmapStream);
    }

    /// <inheritdoc />
    protected override void FreeBuildingResources()
    {
        thumbnailBitmapStream?.Dispose();
        thumbnailBitmapStream = null;
    }

    private static Image? BuildAsImage(ObjectId thumbnailId, Stream? thumbnailStream)
    {
        if (thumbnailStream == null || !thumbnailStream.CanRead)
            return null;

        var result = Cache.TryGet(thumbnailId);
        if (result != null)
            return result;

        try
        {
            thumbnailStream.Position = 0;
            var image = Image.Load(thumbnailStream);
            Cache.Cache(thumbnailId, image);
            return image;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
