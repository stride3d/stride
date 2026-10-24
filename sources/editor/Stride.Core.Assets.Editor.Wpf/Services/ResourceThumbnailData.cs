// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows.Media;
using Stride.Core.Storage;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// Generic ImageSource resources, DrawingImage vectors, etc. support for thumbnails.
    /// </summary>
    public sealed class ResourceThumbnailData : ThumbnailData
    {
        object resourceKey;
        
        /// <param name="resourceKey">The key used to fetch the resource, most likely a string.</param>
        public ResourceThumbnailData(ObjectId thumbnailId, object resourceKey) : base(thumbnailId)
        {
            this.resourceKey = resourceKey;
        }

        /// <inheritdoc />
        protected override ImageSource BuildImageSource()
        {
            if (resourceKey == null)
                return null;
            try
            {
                return System.Windows.Application.Current.TryFindResource(resourceKey) as ImageSource;
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
