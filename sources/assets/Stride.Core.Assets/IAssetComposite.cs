// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Stride.Core.Assets
{
    /// <summary>
    /// An interface that defines the composition declared by an asset inheriting from another asset.
    /// </summary>
    public interface IAssetComposite
    {
        /// <summary>
        /// Collects the part assets.
        /// </summary>
        IEnumerable<AssetPart> CollectParts();

        /// <summary>
        /// Checks if this <see cref="AssetPart"/> container contains the part with the specified id.
        /// </summary>
        /// <param name="id">Unique identifier of the asset part</param>
        /// <returns><c>true</c> if this asset contains the part with the specified id; otherwise <c>false</c></returns>
        bool ContainsPart(Guid id);
    }
}
