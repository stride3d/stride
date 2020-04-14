// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Core.IO;

namespace Stride.Core.Assets
{
    public interface IAssetFinder
    {
        /// <summary>
        /// Finds an asset by its identifier.
        /// </summary>
        /// <param name="assetId">The identifier of the asset.</param>
        /// <returns>The corresponding <see cref="AssetItem" /> if found; otherwise, <c>null</c>.</returns>
        [CanBeNull]
        AssetItem FindAsset(AssetId assetId);

        /// <summary>
        /// Finds an asset by its location.
        /// </summary>
        /// <param name="location">The location of the asset.</param>
        /// <returns>The corresponding <see cref="AssetItem" /> if found; otherwise, <c>null</c>.</returns>
        [CanBeNull]
        AssetItem FindAsset([NotNull] UFile location);

        /// <summary>
        /// Finds an asset from a proxy object.
        /// </summary>
        /// <param name="proxyObject">The proxy object which is represent the targeted asset.</param>
        /// <returns>The corresponding <see cref="AssetItem" /> if found; otherwise, <c>null</c>.</returns>
        [CanBeNull]
        AssetItem FindAssetFromProxyObject([CanBeNull] object proxyObject);
    }
}
