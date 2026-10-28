// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

public sealed class ThumbnailsViewModel : DispatcherViewModel
{
    private readonly SessionViewModel session;
    private readonly HashSet<PackageViewModel> initialQueue = [];
    private IThumbnailService? thumbnailService;

    public ThumbnailsViewModel(SessionViewModel session)
        : base(session.SafeArgument(nameof(session)).ServiceProvider)
    {
        this.session = session;
        session.AssetCollection.FilteredContent.CollectionChanged += VisibleAssetsChanged;
        session.AssetPropertiesChanged += AssetPropertiesChanged;
        ServiceProvider.ServiceRegistered += ServiceRegistered;
    }

    public override void Destroy()
    {
        session.AssetCollection.FilteredContent.CollectionChanged -= VisibleAssetsChanged;
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
    /// Stops building thumbnails, once the current one is done.
    /// </summary>
    public Task StopAsync()
    {
        return thumbnailService?.StopAsync() ?? Task.CompletedTask;
    }

    public void IncreaseThumbnailPriority(IEnumerable<AssetViewModel> assets)
    {
        if (thumbnailService != null)
        {
            var thumbnailsToRefresh = new HashSet<AssetItem>();
            thumbnailsToRefresh.AddRange(assets.Select(x => x.AssetItem));
            thumbnailService.IncreaseThumbnailPriority(thumbnailsToRefresh);
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

    private void RefreshThumbnails(IEnumerable<AssetViewModel> assets, bool firstPriority)
    {
        if (thumbnailService != null)
        {
            var assetItems = new HashSet<AssetItem>(assets.Select(x => x.AssetItem));
            // We run this as a task to prevent dead lock
            Task.Run(() => thumbnailService.AddThumbnailAssetItems(assetItems, firstPriority ? QueuePosition.First : QueuePosition.Last));
        }
    }

    private void ServiceRegistered(object? sender, ServiceRegistrationEventArgs e)
    {
        if (e.Service is IThumbnailService service)
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

    private void VisibleAssetsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        IncreaseThumbnailPriority(session.AssetCollection.FilteredContent.OfType<AssetViewModel>());
    }

    private void AssetPropertiesChanged(object? sender, AssetChangedEventArgs e)
    {
        var referencers = AssetViewModel.ComputeRecursiveReferencerAssets(e.Assets).ToList();
        RefreshThumbnails(e.Assets.Concat(referencers), true);
    }

    private void ThumbnailCompleted(object? sender, ThumbnailCompletedArgs e)
    {
        var asset = session.GetAssetById(e.AssetId);
        if (asset != null)
        {
            // InvokeTask: InvokeAsync would take the async lambda as a Func<Task> result and drop the inner task with its exceptions
            _ = Dispatcher.InvokeTask(async () =>
            {
                asset.SetThumbnailData(e.Data);
                await e.Data.PrepareForPresentation(Dispatcher);
            });
        }
    }
}
