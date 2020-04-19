// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// This view model manages updating thumbnails of <see cref="AssetViewModel"/>.
    /// </summary>
    public class ThumbnailsViewModel : DispatcherViewModel
    {
        private readonly SessionViewModel session;
        private readonly HashSet<PackageViewModel> initialQueue = new HashSet<PackageViewModel>();
        private IThumbnailService thumbnailService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThumbnailsViewModel"/> class.
        /// </summary>
        /// <param name="session">The session associated to this view model.</param>
        public ThumbnailsViewModel(SessionViewModel session)
            : base(session.SafeArgument(nameof(session)).ServiceProvider)
        {
            this.session = session;
            session.ActiveAssetView.FilteredContent.CollectionChanged += VisibleAssetsChanged;
            session.AssetPropertiesChanged += AssetPropertiesChanged;
            ServiceProvider.ServiceRegistered += ServiceRegistered;
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            session.ActiveAssetView.FilteredContent.CollectionChanged -= VisibleAssetsChanged;
            session.AssetPropertiesChanged -= AssetPropertiesChanged;
            ServiceProvider.ServiceRegistered -= ServiceRegistered;

            if (thumbnailService != null)
            {
                thumbnailService.ThumbnailCompleted -= ThumbnailCompleted;
                thumbnailService.Dispose();
            }
            base.Destroy();
        }

        /// <summary>
        /// Increases the priority of thumbnail processing for the given assets, if they are queued for thumbnail processing.
        /// This methods has no effect for assets that are not currently in the thumbnail processing queue.
        /// </summary>
        /// <param name="assets"></param>
        public void IncreaseThumbnailPriority(IEnumerable<AssetViewModel> assets)
        {
            if (thumbnailService != null)
            {
                var thumbnailsToRefresh = new HashSet<AssetItem>();
                thumbnailsToRefresh.AddRange(assets.Select(x => x.AssetItem));
                thumbnailService.IncreaseThumbnailPriority(thumbnailsToRefresh);
            }
        }

        /// <summary>
        /// Starts the first build of thumbnails from the given package. This method should be invoked only once per package, after it and its dependencies have been loaded.
        /// </summary>
        /// <param name="package">The package for which to build thumbnails.</param>
        internal void StartInitialBuild(PackageViewModel package)
        {
            if (thumbnailService == null)
            {
                // If the thumbnail service is not available yet, defer thumbnail build.
                initialQueue.Add(package);
            }

            // TODO: putting false here makes all thumbnails that are not visible to be incorrectly built
            RefreshThumbnails(package.Assets, true);
        }

        /// <summary>
        /// Refreshes the thumbnails of the given assets.
        /// </summary>
        /// <param name="assets">The assets for which to refresh thumbnails.</param>
        /// <param name="firstPriority">If <c>true</c>, the given assets will be put in the front of the thumbnail processing queue.</param>
        private void RefreshThumbnails(IEnumerable<AssetViewModel> assets, bool firstPriority)
        {
            if (thumbnailService != null)
            {
                var assetItems = new HashSet<AssetItem>(assets.Select(x => x.AssetItem));
                // We run this as a task to prevent dead lock
                Task.Run(() => thumbnailService.AddThumbnailAssetItems(assetItems, firstPriority ? QueuePosition.First : QueuePosition.Last));
            }
        }

        public void ForceRefreshThumbnails(IEnumerable<AssetViewModel> assets)
        {
            if (thumbnailService != null)
            {
                var assetItems = new HashSet<AssetItem>(assets.Select(x => x.AssetItem));
                // We run this as a task to prevent dead lock
                Task.Run(() => thumbnailService.AddThumbnailAssetItems(assetItems, QueuePosition.First));
            }
        }

        private void ServiceRegistered(object sender, ServiceRegistrationEventArgs e)
        {
            var service = e.Service as IThumbnailService;
            if (service != null)
            {
                thumbnailService = service;
                thumbnailService.ThumbnailCompleted += ThumbnailCompleted;
                ServiceProvider.ServiceRegistered -= ServiceRegistered;
                foreach (var package in initialQueue)
                {
                    StartInitialBuild(package);
                }
                initialQueue.Clear();
            }
        }

        private void VisibleAssetsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IncreaseThumbnailPriority(session.ActiveAssetView.FilteredContent.OfType<AssetViewModel>());
        }

        private void AssetPropertiesChanged(object sender, AssetChangedEventArgs e)
        {
            var referencers = AssetViewModel.ComputeRecursiveReferencerAssets(e.Assets).ToList();
            RefreshThumbnails(e.Assets.Concat(referencers), true);
        }

        private void ThumbnailCompleted(object sender, ThumbnailCompletedArgs e)
        {
            var asset = session.GetAssetById(e.AssetId);
            if (asset != null)
            {
                Dispatcher.InvokeAsync(async () =>
                {
                    asset.SetThumbnailData(e.Data);
                    await e.Data.PrepareForPresentation(Dispatcher);
                });
            }
        }
    }
}
