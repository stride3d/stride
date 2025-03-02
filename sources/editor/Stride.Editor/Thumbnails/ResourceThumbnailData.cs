// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Storage;
using Stride.Editor.Resources;
using Stride.Graphics;

namespace Stride.Editor.Thumbnails
{
    /// <summary>
    /// Generic Image resources, DrawingImage vectors, etc. support for thumbnails.
    /// </summary>
    public sealed class ResourceThumbnailData : ThumbnailData
    {
        private string? resourceKey;

        /// <param name="resourceKey">The key used to fetch the resource, most likely a string.</param>
        public ResourceThumbnailData(ObjectId thumbnailId, object resourceKey)
            : base(thumbnailId)
        {
            this.resourceKey = resourceKey.ToString();
        }

        /// <inheritdoc />
        protected override Image? BuildImageSource()
        {
            if (resourceKey == null)
                return null;

            try
            {
                return EmbeddedResourceReader.GetImage(resourceKey);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <inheritdoc />
        protected override void FreeBuildingResources()
        {
            resourceKey = null;
        }
    }
}
