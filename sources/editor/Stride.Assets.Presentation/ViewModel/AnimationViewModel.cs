// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;
using Stride.Assets.Models;

namespace Stride.Assets.Presentation.ViewModel
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
