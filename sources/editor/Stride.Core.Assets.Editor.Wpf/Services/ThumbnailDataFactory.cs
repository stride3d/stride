// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Storage;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// Creates the WPF <see cref="ThumbnailData"/>.
    /// </summary>
    public sealed class ThumbnailDataFactory : IThumbnailDataFactory
    {
        /// <inheritdoc/>
        public ThumbnailData CreateFromResource(ObjectId thumbnailId, object resourceKey) => new ResourceThumbnailData(thumbnailId, resourceKey);
    }
}
