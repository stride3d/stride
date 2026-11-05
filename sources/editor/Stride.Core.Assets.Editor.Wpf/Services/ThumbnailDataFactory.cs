// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;
using Stride.Core.Storage;
using Stride.Core.Assets.Presentation.Services;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// Creates the WPF <see cref="ThumbnailData"/>.
    /// </summary>
    public sealed class ThumbnailDataFactory : IThumbnailDataFactory
    {
        /// <inheritdoc/>
        public ThumbnailData CreateFromResource(ObjectId thumbnailId, object resourceKey) => new ResourceThumbnailData(thumbnailId, resourceKey);

        /// <inheritdoc/>
        public ThumbnailData CreateFromStream(ObjectId thumbnailId, Stream imageStream) => new BitmapThumbnailData(thumbnailId, imageStream);
    }
}
