// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using System.Collections.Specialized;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// This view model manages updating thumbnails of <see cref="AssetViewModel"/>.
/// </summary>
public sealed class ThumbnailsViewModel : DispatcherViewModel
{
    private readonly ISessionViewModel session;
    private readonly HashSet<PackageViewModel> initialQueue = [];
    private IThumbnailService? thumbnailService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailsViewModel"/> class.
    /// </summary>
    /// <param name="session">The session associated to this view model.</param>
    public ThumbnailsViewModel(ISessionViewModel session)
        : base(session.SafeArgument(nameof(session)).ServiceProvider)
    {
        this.session = session;
        session.ActiveAssetView.FilteredContent.CollectionChanged += VisibleAssetsChanged;
        session.AssetPropertiesChanged += AssetPropertiesChanged;
        ServiceProvider.ServiceRegistered += ServiceRegistered;
    }

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
            var service = thumbnailService;
            session.TrackBackgroundWork(Task.Run(() => service.AddThumbnailAssetItems(assetItems, QueuePosition.First)));
        }
    }

    /// <summary>
    /// Queues the thumbnails of the assets of the given package, once the thumbnail service is there.
    /// </summary>
    /// <returns>The queueing, completed when there is no thumbnail service yet.</returns>
    internal Task StartInitialBuild(PackageViewModel package)
    {
        if (thumbnailService == null)
        {
            // If the thumbnail service is not available yet, defer thumbnail build.
            initialQueue.Add(package);
        }

        // TODO: putting false here makes all thumbnails that are not visible to be incorrectly built
        return RefreshThumbnails(package.Assets, true);
    }

    private Task RefreshThumbnails(IEnumerable<AssetViewModel> assets, bool firstPriority)
    {
        if (thumbnailService == null)
            return Task.CompletedTask;

        var assetItems = new HashSet<AssetItem>(assets.Select(x => x.AssetItem));
        // We run this as a task to prevent dead lock
        var service = thumbnailService;
        return session.TrackBackgroundWork(Task.Run(() => service.AddThumbnailAssetItems(assetItems, firstPriority ? QueuePosition.First : QueuePosition.Last)));
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
        IncreaseThumbnailPriority(session.ActiveAssetView.FilteredContent.OfType<AssetViewModel>());
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
