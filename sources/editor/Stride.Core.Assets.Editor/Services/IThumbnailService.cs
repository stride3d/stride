// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// An interface that is capable of enqueuing thumbnail compilation orders and notify thumbnail compilation completion.
    /// </summary>
    public interface IThumbnailService : IDisposable
    {
        /// <summary>
        /// Adds the given asset items to the queue.
        /// </summary>
        /// <param name="assetItems">The asset items to add to the queue.</param>
        /// <param name="position">The position in the queue from which to start insertion.</param>
        /// <remarks> If an asset item is already in the queue, it might be moved to upper priority if <see cref="position"/> is <see cref="QueuePosition.First"/>.</remarks>
        void AddThumbnailAssetItems(IEnumerable<AssetItem> assetItems, QueuePosition position);

        /// <summary>
        /// Increases the priority of the thumbnail compilation of the given asset items, if they are in the queue. If an asset item is not in the queue, it is not added.
        /// </summary>
        /// <param name="assetItems">The asset items to increase the priority.</param>
        /// <remarks>This method is equivalent to <see cref="AddThumbnailAssetItems"/> with <see cref="QueuePosition.First"/> except that it won't add asset items that are not already in the queue.</remarks>
        void IncreaseThumbnailPriority(IEnumerable<AssetItem> assetItems);

        /// <summary>
        /// Indicates whether the given asset type has static thumbnails
        /// </summary>
        /// <param name="assetType">The asset type.</param>
        /// <remarks>A type has static thumbnails if the thumbnail image does not depend on the asset properties.</remarks>
        /// <returns><c>True</c> if the asset type has static thumbnails, <c>False</c> otherwise.</returns>
        bool HasStaticThumbnail(Type assetType);

        /// <summary>
        /// Raised when a thumbnail is successfully compiled.
        /// </summary>
        event EventHandler<ThumbnailCompletedArgs> ThumbnailCompleted;
    }
}
