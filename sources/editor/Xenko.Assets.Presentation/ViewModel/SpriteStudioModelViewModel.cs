// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Quantum;
using Xenko.SpriteStudio.Offline;

namespace Xenko.Assets.Presentation.ViewModel
{
    // FIXME: this view model should be in the SpriteStudio offline assembly! Can't be done now, because of a circular reference in CompilerApp referencing SpriteStudio, and Editor referencing CompilerApp
    [AssetViewModel(typeof(SpriteStudioModelAsset))]
    public class SpriteStudioModelViewModel : ImportedAssetViewModel<SpriteStudioModelAsset>
    {
        public SpriteStudioModelViewModel(AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
        }

        protected override IAssetImporter GetImporter()
        {
            return AssetRegistry.FindImporterForFile(Asset.Source).FirstOrDefault(x => x.RootAssetTypes.Contains(typeof(SpriteStudioModelAsset)));
        }

        protected override void UpdateAssetFromSource(SpriteStudioModelAsset assetToMerge)
        {
            AssetRootNode[nameof(SpriteStudioModelAsset.NodeNames)].Update(assetToMerge.NodeNames);
        }
    }
}
