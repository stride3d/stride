// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Xenko.Core;
using Xenko.Core.Storage;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.Services
{
    public sealed class ThumbnailData : ViewModelBase
    {
        private static readonly ObjectCache<ObjectId, BitmapImage> Cache = new ObjectCache<ObjectId, BitmapImage>(512);
        private static readonly Dictionary<ObjectId, Task<BitmapImage>> ComputingThumbnails = new Dictionary<ObjectId, Task<BitmapImage>>();
        private readonly ObjectId thumbnailId;
        private Stream thumbnailStream;
        private object presenter;

        public ThumbnailData(ObjectId thumbnailId, Stream thumbnailStream)
        {
            this.thumbnailId = thumbnailId;
            this.thumbnailStream = thumbnailStream;
        }

        public object Presenter { get { return presenter; } set { SetValue(ref presenter, value); } }

        public async Task PrepareForPresentation(IDispatcherService dispatcher)
        {
            Task<BitmapImage> task;
            lock (ComputingThumbnails)
            {
                if (!ComputingThumbnails.TryGetValue(thumbnailId, out task))
                {
                    task = Task.Run(() => BuildBitmapImage(thumbnailId, thumbnailStream));
                    ComputingThumbnails.Add(thumbnailId, task);
                }
            }

            var result = await task;
            dispatcher.Invoke(() => Presenter = result);
            thumbnailStream = null;

            lock (ComputingThumbnails)
            {
                ComputingThumbnails.Remove(thumbnailId);
            }
        }

        private static BitmapImage BuildBitmapImage(ObjectId thumbnailId, Stream thumbnailStream)
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
