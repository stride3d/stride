// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Services;

public interface IThumbnailService : IDisposable
{
    void AddThumbnailAssetItems(IEnumerable<AssetItem> assetItems, QueuePosition position);

    void IncreaseThumbnailPriority(IEnumerable<AssetItem> assetItems);

    bool HasStaticThumbnail(Type assetType);

    /// <summary>
    /// Stops building thumbnails, once the current one is done.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Gets the number of thumbnails waiting to be built.
    /// </summary>
    int PendingThumbnailCount { get; }

    event EventHandler<ThumbnailCompletedArgs>? ThumbnailCompleted;
}
