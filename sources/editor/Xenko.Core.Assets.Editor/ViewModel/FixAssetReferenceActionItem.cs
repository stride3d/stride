// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Presentation.Dirtiables;

namespace Xenko.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// A <see cref="DirtyingOperation"/> that will fix the references contained in a collection of <see cref="AssetViewModel"/>.
    /// </summary>
    public class FixAssetReferenceOperation : DirtyingOperation
    {
        private readonly bool fixOnUndo;
        private readonly bool fixOnRedo;
        private IReadOnlyCollection<AssetViewModel> assets;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixAssetReferenceOperation"/> class.
        /// </summary>
        /// <param name="assets">The list of assets to fix.</param>
        /// <param name="fixOnUndo">Indicates whether this action item should fix the reference during an Undo operation.</param>
        /// <param name="fixOnRedo">Indicates whether this action item should fix the reference during a Redo operation.</param>
        public FixAssetReferenceOperation(IReadOnlyCollection<AssetViewModel> assets, bool fixOnUndo, bool fixOnRedo)
            : base(assets)
        {
            this.assets = assets;
            this.fixOnUndo = fixOnUndo;
            this.fixOnRedo = fixOnRedo;
        }

        public void FixAssetReferences()
        {
            AssetAnalysis.FixAssetReferences(assets.Select(x => x.AssetItem));
        }

        /// <inheritdoc/>
        protected override void FreezeContent()
        {
            assets = null;
        }

        /// <inheritdoc/>
        protected override void Undo()
        {
            if (!fixOnUndo)
                return;

            FixAssetReferences();
        }

        /// <inheritdoc/>
        protected override void Redo()
        {
            if (!fixOnRedo)
                return;

            FixAssetReferences();
        }
    }
}
