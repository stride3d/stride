// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using System.IO;
using Stride.Core.Storage;

namespace Stride.Core.Assets.Presentation.Services
{
    /// <summary>
    /// Creates the <see cref="ThumbnailData"/>, whose images are those of the UI framework of the editor: the shared services
    /// (thumbnails, folders) ask the factory the host registers, so that each view shows its own image type.
    /// </summary>
    public interface IThumbnailDataFactory
    {
        /// <summary>
        /// Creates a thumbnail that displays an image resource of the view.
        /// </summary>
        /// <param name="thumbnailId">The identifier of the thumbnail.</param>
        /// <param name="resourceKey">The key of the image resource, most likely a string.</param>
        ThumbnailData CreateFromResource(ObjectId thumbnailId, object resourceKey);

        /// <summary>
        /// Creates a thumbnail that displays an encoded image (the PNG that the thumbnail service built).
        /// </summary>
        /// <param name="thumbnailId">The identifier of the thumbnail.</param>
        /// <param name="imageStream">The encoded image. The thumbnail owns the stream and frees it once the image is built.</param>
        ThumbnailData CreateFromStream(ObjectId thumbnailId, Stream imageStream);
    }
}
