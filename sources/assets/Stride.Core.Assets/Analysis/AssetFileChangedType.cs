// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Assets.Analysis
{
    /// <summary>
    /// Type of a change event for an asset.
    /// </summary>
    [Flags]
    public enum AssetFileChangedType
    {
        /// <summary>
        /// An asset was added to the disk
        /// </summary>
        Added = 1,

        /// <summary>
        /// The asset was deleted from the disk
        /// </summary>
        Deleted = 2,

        /// <summary>
        /// The asset is updated on the disk
        /// </summary>
        Updated = 4,

        /// <summary>
        /// The asset event mask (Added | Deleted | Updated).
        /// </summary>
        AssetEventMask = Added | Deleted | Updated,

        /// <summary>
        /// The asset import was modified on the disk
        /// </summary>
        SourceUpdated = 8,

        /// <summary>
        /// The asset import was deleted from the disk
        /// </summary>
        SourceDeleted = 16,

        /// <summary>
        /// The source event mask (SourceUpdated | SourceDeleted).
        /// </summary>
        SourceEventMask = SourceUpdated | SourceDeleted,
    }
}
