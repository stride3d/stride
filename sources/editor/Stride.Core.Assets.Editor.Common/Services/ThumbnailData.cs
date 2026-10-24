// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Storage;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// The thumbnail of a session object, built in the background and then displayed through <see cref="Presenter"/>.
    /// </summary>
    /// <remarks>The subclasses that build the images of a UI framework live with that framework (see <see cref="IThumbnailDataFactory"/>).</remarks>
    public abstract class ThumbnailData : ViewModelBase
    {
        protected readonly ObjectId thumbnailId;
        private static readonly Dictionary<ObjectId, Task<object>> ComputingThumbnails = new Dictionary<ObjectId, Task<object>>();
        private object presenter;

        protected ThumbnailData(ObjectId thumbnailId)
        {
            this.thumbnailId = thumbnailId;
        }

        public object Presenter { get { return presenter; } set { SetValue(ref presenter, value); } }

        public async Task PrepareForPresentation(IDispatcherService dispatcher)
        {
            Task<object> task;
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
        /// <returns>The object that the view displays, set as <see cref="Presenter"/>.</returns>
        protected abstract object BuildImageSource();

        /// <summary>
        /// Clears the resources required to build the image source.
        /// </summary>
        protected abstract void FreeBuildingResources();
    }
}
