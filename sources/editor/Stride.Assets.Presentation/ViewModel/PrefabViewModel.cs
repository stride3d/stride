// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Annotations;
using Xenko.Core.Quantum;
using Xenko.Assets.Entities;

namespace Xenko.Assets.Presentation.ViewModel
{
    [AssetViewModel(typeof(PrefabAsset))]
    public class PrefabViewModel : EntityHierarchyViewModel, IAssetViewModel<PrefabAsset>
    {
        public PrefabViewModel([NotNull] AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
            
        }

        /// <inheritdoc />
        public new PrefabAsset Asset => (PrefabAsset)base.Asset;

        protected override IObjectNode GetPropertiesRootNode()
        {
            // We don't use CanProvideProperties because we still want the button to open in editor. But we don't want to display any property directly
            return null;
        }
    }
}
