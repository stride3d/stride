// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.IO;

namespace Stride.Core.Assets.Tracking
{
    /// <summary>
    /// Data structure for the <see cref="AssetSourceTracker.SourceFileChanged"/> block.
    /// </summary>
    public struct SourceFileChangedData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SourceFileChangedData"/> structure.
        /// </summary>
        /// <param name="type">The type of change that occurred.</param>
        /// <param name="assetId">The id of the asset affected by this change.</param>
        /// <param name="files">The list of files that changed.</param>
        /// <param name="needUpdate">Indicate whether the asset needs to be updated from its sources due to this change.</param>
        public SourceFileChangedData(SourceFileChangeType type, AssetId assetId, IReadOnlyList<UFile> files, bool needUpdate)
        {
            Type = type;
            AssetId = assetId;
            Files = files;
            NeedUpdate = needUpdate;
        }

        /// <summary>
        /// Gets the type of change that occurred.
        /// </summary>
        public SourceFileChangeType Type { get; }

        /// <summary>
        /// Gets the id of the asset affected by this change.
        /// </summary>
        public AssetId AssetId { get; }

        /// <summary>
        /// Gets the list of files that changed
        /// </summary>
        public IReadOnlyList<UFile> Files { get; }

        /// <summary>
        /// Gets whether the asset needs to be updated from its sources due to this change.
        /// </summary>
        public bool NeedUpdate { get; }
    }
}
