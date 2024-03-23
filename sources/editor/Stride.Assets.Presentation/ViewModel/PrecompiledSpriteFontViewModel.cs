// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.SpriteFont;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Assets.Presentation.ViewModel
{
    [AssetViewModel<PrecompiledSpriteFontAsset>]
    public class PrecompiledSpriteFontViewModel : AssetViewModel<PrecompiledSpriteFontAsset>
    {
        public PrecompiledSpriteFontViewModel(AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
        }
    }
}
