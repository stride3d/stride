// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Annotations;
using Xenko.Assets.UI;

namespace Xenko.Assets.Presentation.ViewModel
{
    /// <summary>
    /// View model for <see cref="UILibraryAsset"/>.
    /// </summary>
    [AssetViewModel(typeof(UILibraryAsset))]
    public class UILibraryViewModel : UIBaseViewModel, IAssetViewModel<UILibraryAsset>
    {
        public UILibraryViewModel([NotNull] AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {

        }

        /// <inheritdoc />
        public new UILibraryAsset Asset => (UILibraryAsset)base.Asset;
    }
}
