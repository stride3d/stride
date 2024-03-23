// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.UI;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;

namespace Stride.Assets.Presentation.ViewModel
{
    /// <summary>
    /// View model for <see cref="UIPageAsset"/>.
    /// </summary>
    [AssetViewModel<UIPageAsset>]
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
