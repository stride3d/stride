// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Storage;
using Stride.Graphics;

namespace Stride.Editor.Thumbnails
{
    /// <summary>
    /// Byte streams bitmap support for thumbnails.
    /// </summary>
    public sealed class BitmapThumbnailData : ThumbnailData
    {
        private static readonly ObjectCache<ObjectId, Image> Cache = new(512);
        private Stream? thumbnailBitmapStream;

        public BitmapThumbnailData(ObjectId thumbnailId, Stream thumbnailBitmapStream) : base(thumbnailId)
        {
            this.thumbnailBitmapStream = thumbnailBitmapStream;
        }

        /// <inheritdoc />
        protected override Image? BuildImageSource()
        {
            return BuildAsBitmapImage(thumbnailId, thumbnailBitmapStream);
        }

        /// <inheritdoc />
        protected override void FreeBuildingResources()
        {
            thumbnailBitmapStream?.Dispose();
            thumbnailBitmapStream = null;
        }

        private static Image? BuildAsBitmapImage(ObjectId thumbnailId, Stream? thumbnailStream)
        {
            if (thumbnailStream == null)
                return null;

            var stream = thumbnailStream;
            if (!stream.CanRead)
                return null;

            var result = Cache.TryGet(thumbnailId);
            if (result != null)
                return result;

            try
            {
                var image = Image.Load(stream);
                Cache.Cache(thumbnailId, image);
                return image;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
