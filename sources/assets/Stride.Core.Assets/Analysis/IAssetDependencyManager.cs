// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Analysis
{
    public interface IAssetDependencyManager
    {
        /// <summary>
        /// Computes the dependencies for the specified asset.
        /// </summary>
        /// <param name="assetId">The asset id.</param>
        /// <param name="dependenciesOptions">The dependencies options.</param>
        /// <param name="linkTypes">The type of links to visit while computing the dependencies</param>
        /// <param name="visited">The list of element already visited.</param>
        /// <returns>The dependencies, or <c>null</c> if the object is not tracked.</returns>
        [CanBeNull]
        AssetDependencies ComputeDependencies(AssetId assetId, AssetDependencySearchOptions dependenciesOptions = AssetDependencySearchOptions.All, ContentLinkType linkTypes = ContentLinkType.Reference, HashSet<AssetId> visited = null);
    }
}
