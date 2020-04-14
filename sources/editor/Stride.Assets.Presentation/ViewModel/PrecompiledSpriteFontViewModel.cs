// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Assets.SpriteFont;

namespace Xenko.Assets.Presentation.ViewModel
{
    [AssetViewModel(typeof(PrecompiledSpriteFontAsset))]
    public class PrecompiledSpriteFontViewModel : AssetViewModel<PrecompiledSpriteFontAsset>
    {
        public PrecompiledSpriteFontViewModel(AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
        }
    }
}
