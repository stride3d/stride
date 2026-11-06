// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Stride.Core.Storage;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// Byte streams bitmap support for thumbnails.
    /// </summary>
    public sealed class BitmapThumbnailData : ThumbnailData
    {
        private static readonly ObjectCache<ObjectId, ImageSource> Cache = new ObjectCache<ObjectId, ImageSource>(512);
        private Stream thumbnailBitmapStream;

        public BitmapThumbnailData(ObjectId thumbnailId, Stream thumbnailBitmapStream) : base(thumbnailId)
        {
            this.thumbnailBitmapStream = thumbnailBitmapStream;
        }

        /// <inheritdoc />
        protected override ImageSource BuildImageSource()
        {
            return BuildAsBitmapImage(thumbnailId, thumbnailBitmapStream);
        }

        /// <inheritdoc />
        protected override void FreeBuildingResources()
        {
            thumbnailBitmapStream = null;
        }

        private static ImageSource BuildAsBitmapImage(ObjectId thumbnailId, Stream thumbnailStream)
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
                var bitmap = new BitmapImage();
                stream.Position = 0;
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                thumbnailStream.Close();
                Cache.Cache(thumbnailId, bitmap);
                return bitmap;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
