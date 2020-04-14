// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Annotations;
using Xenko.Assets.UI;

namespace Xenko.Assets.Presentation.ViewModel
{
    /// <summary>
    /// View model for <see cref="UIPageAsset"/>.
    /// </summary>
    [AssetViewModel(typeof(UIPageAsset))]
    public class UIPageViewModel : UIBaseViewModel, IAssetViewModel<UIPageAsset>
    {
        public UIPageViewModel([NotNull] AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {

        }

        /// <inheritdoc />
        public new UIPageAsset Asset => (UIPageAsset)base.Asset;
    }
}
