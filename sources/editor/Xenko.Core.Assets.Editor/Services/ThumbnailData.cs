// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xenko.Core.Storage;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.Services
{
    public abstract class ThumbnailData : ViewModelBase
    {
        protected readonly ObjectId thumbnailId;
        private static readonly Dictionary<ObjectId, Task<ImageSource>> ComputingThumbnails = new Dictionary<ObjectId, Task<ImageSource>>();
        private object presenter;

        protected ThumbnailData(ObjectId thumbnailId)
        {
            this.thumbnailId = thumbnailId;
        }

        public object Presenter { get { return presenter; } set { SetValue(ref presenter, value); } }

        public async Task PrepareForPresentation(IDispatcherService dispatcher)
        {
            Task<ImageSource> task;
            lock (ComputingThumbnails)
            {
                if (!ComputingThumbnails.TryGetValue(thumbnailId, out task))
                {
                    task = Task.Run(() => BuildImageSource());
                    ComputingThumbnails.Add(thumbnailId, task);
                }
            }

            var result = await task;
            dispatcher.Invoke(() => Presenter = result);
            FreeBuildingResources();

            lock (ComputingThumbnails)
            {
                ComputingThumbnails.Remove(thumbnailId);
            }
        }

        /// <summary>
        /// Fetches and prepare the image source instance to be displayed.
        /// </summary>
        /// <returns></returns>
        protected abstract ImageSource BuildImageSource();

        /// <summary>
        /// Clears the resources required to build the image source.
        /// </summary>
        protected abstract void FreeBuildingResources();
    }

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
