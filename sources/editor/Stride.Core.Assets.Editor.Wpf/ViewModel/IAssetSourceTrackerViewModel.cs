// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.IO;
using Stride.Core.Storage;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// The part of the tracker of asset source files (<see cref="AssetSourceTrackerViewModel"/>) that the session objects use.
    /// </summary>
    public interface IAssetSourceTrackerViewModel
    {
        /// <summary>
        /// Gets the current hash of the given source file.
        /// </summary>
        ObjectId GetCurrentHash(UFile file);

        /// <summary>
        /// Updates the state of the given asset regarding the changes of its source files.
        /// </summary>
        void UpdateAssetStatus(AssetViewModel asset);
    }
}
