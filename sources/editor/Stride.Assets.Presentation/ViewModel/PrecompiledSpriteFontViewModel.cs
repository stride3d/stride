// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Assets.SpriteFont;

namespace Stride.Assets.Presentation.ViewModel
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
