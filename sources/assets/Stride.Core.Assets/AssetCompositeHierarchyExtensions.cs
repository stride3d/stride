// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Extension methods for <see cref="AssetCompositeHierarchy{TAssetPartDesign,TAssetPart}"/> and <see cref="AssetCompositeHierarchyData{TAssetPartDesign,TAssetPart}"/>
    /// </summary>
    public static class AssetCompositeHierarchyExtensions
    {
        /// <summary>
        /// Enumerates the root design parts of this hierarchy.
        /// </summary>
        /// <typeparam name="TAssetPartDesign">The type used for the design information of a part.</typeparam>
        /// <typeparam name="TAssetPart">The type used for the actual parts,</typeparam>
        /// <param name="asset">This hierarchy.</param>
        /// <returns>A sequence containing the root design parts of this hierarchy.</returns>
        [ItemNotNull, NotNull, Pure]
        public static IEnumerable<TAssetPartDesign> EnumerateRootPartDesigns<TAssetPartDesign, TAssetPart>([NotNull] this AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart> asset)
            where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
            where TAssetPart : class, IIdentifiable
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            return asset.RootParts.Select(x => asset.Parts[x.Id]);
        }

        /// <summary>
        /// Merges the <paramref name="other"/> hierarchy into this hierarchy.
        /// </summary>
        /// <remarks>
        /// This method does not check whether the two hierarchies have independent parts and will fail otherwise.
        /// </remarks>
        /// <typeparam name="TAssetPartDesign">The type used for the design information of a part.</typeparam>
        /// <typeparam name="TAssetPart">The type used for the actual parts,</typeparam>
        /// <param name="asset">This hierarchy.</param>
        /// <param name="other">The other hierarchy which parts will added to this hierarchy.</param>
        public static void MergeInto<TAssetPartDesign, TAssetPart>([NotNull] this AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart> asset,
            [NotNull] AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart> other)
            where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
            where TAssetPart : class, IIdentifiable
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            asset.RootParts.AddRange(other.RootParts);
            foreach (var part in other.Parts)
            {
                asset.Parts.Add(part.Value);
            }
        }
    }
}
