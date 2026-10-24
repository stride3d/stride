// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Storage;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// Creates the <see cref="ThumbnailData"/> that need the images of the UI framework of the editor.
    /// </summary>
    public interface IThumbnailDataFactory
    {
        /// <summary>
        /// Creates a thumbnail that displays an image resource of the view.
        /// </summary>
        /// <param name="thumbnailId">The identifier of the thumbnail.</param>
        /// <param name="resourceKey">The key of the image resource, most likely a string.</param>
        ThumbnailData CreateFromResource(ObjectId thumbnailId, object resourceKey);
    }
}
