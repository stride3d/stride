// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Assets.UI;

namespace Stride.Assets.Presentation.ViewModel
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
