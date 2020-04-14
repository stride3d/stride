// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Quantum
{
    /// <summary>
    /// Arguments of the <see cref="AssetCompositeHierarchyPropertyGraph{TAssetPartDesign, TAssetPart}.PartAdded"/> and <see cref="AssetCompositeHierarchyPropertyGraph{TAssetPartDesign, TAssetPart}.PartRemoved"/> events.
    /// </summary>
    public class AssetPartChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetPartChangeEventArgs"/> class.
        /// </summary>
        /// <param name="assetItem">The asset that was modified.</param>
        /// <param name="partId">The id of the part that has been added or removed.</param> 
        public AssetPartChangeEventArgs([NotNull] AssetItem assetItem, Guid partId)
        {
            if (!(assetItem.Asset is AssetComposite))
                throw new ArgumentException($@"The given assetItem does not hold an {nameof(AssetComposite)}.", nameof(assetItem));
            AssetItem = assetItem;
            PartId = partId;
        }

        /// <summary>
        /// Gets the asset item of the asset that was modified.
        /// </summary>
        public AssetItem AssetItem { get; }

        /// <summary>
        /// Gets the asset that was modified.
        /// </summary>
        [NotNull]
        public AssetComposite Asset => (AssetComposite)AssetItem.Asset;

        /// <summary>
        /// Gets the id of the part that has been added or removed.
        /// </summary>
        public Guid PartId { get; }
    }
}
