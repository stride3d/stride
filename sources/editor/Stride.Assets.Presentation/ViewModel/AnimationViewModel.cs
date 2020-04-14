// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Reflection;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Quantum;
using Xenko.Assets.Models;

namespace Xenko.Assets.Presentation.ViewModel
{
    [AssetViewModel(typeof(AnimationAsset))]
    public class AnimationViewModel : ImportedAssetViewModel<AnimationAsset>
    {
        public AnimationViewModel(AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
        }

        protected override IAssetImporter GetImporter()
        {
            return AssetRegistry.FindImporterForFile(Asset.Source).OfType<ModelAssetImporter>().FirstOrDefault();
        }

        protected override void UpdateAssetFromSource(AnimationAsset assetToMerge)
        {
            AssetRootNode[nameof(AnimationAsset.AnimationTimeMaximum)].Update(assetToMerge.AnimationTimeMaximum);
            AssetRootNode[nameof(AnimationAsset.AnimationTimeMinimum)].Update(assetToMerge.AnimationTimeMinimum);
        }
    }
}
