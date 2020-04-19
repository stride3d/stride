// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Quantum;
using Stride.SpriteStudio.Offline;

namespace Stride.Assets.Presentation.ViewModel
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
