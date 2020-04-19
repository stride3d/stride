// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using Stride.Core.Assets;
using Stride.Core.IO;
using Stride.Core.Storage;

namespace Stride.Editor.Thumbnails
{
    /// <summary>
    /// This enum describes the result of a thumbnail build operation.
    /// </summary>
    public enum ThumbnailBuildResult
    {
        /// <summary>
        /// The build has either failed, or not been triggered due to previous failures
        /// </summary>
        Failed,
        /// <summary>
        /// The build has either been successfully executed, or already up-to-date
        /// </summary>
        Succeeded,
        /// <summary>
        /// The build has been cancelled.
        /// </summary>
        Cancelled,
    }

    /// <summary>
    /// An event arguments class containing information about a thumbnail creation.
    /// </summary>
    public class ThumbnailBuiltEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the id of the asset whose thumbnail has been built.
        /// </summary>
        public AssetId AssetId { get; internal set; }

        /// <summary>
        /// Gets the url of the asset whose thumbnail has been built.
        /// </summary>
        public UFile Url { get; internal set; }

        /// <summary>
        /// Gets the value indicating if the result of the build
        /// </summary>
        public ThumbnailBuildResult Result { get; internal set; }

        /// <summary>
        /// Gets the value indicating if the built thumbnail is different from its previous version.
        /// </summary>
        public bool ThumbnailChanged { get; internal set; }

        /// <summary>
        /// Gets the stream to the thumbnail PNG file corresponding to the item.
        /// </summary>
        /// <remarks>This property is null if <see cref="Result"/> is not <see cref="ThumbnailBuildResult.Succeeded"/>.</remarks>
        public Stream ThumbnailStream { get; internal set; }

        /// <summary>
        /// Gets the hash of the stream to the thumbnail PNG file corresponding to the item.
        /// </summary>
        /// <remarks>This property is equal to <see cref="ObjectId.Empty"/> if <see cref="Result"/> is not <see cref="ThumbnailBuildResult.Succeeded"/>.</remarks>
        public ObjectId ThumbnailId { get; internal set; }
    }
}
