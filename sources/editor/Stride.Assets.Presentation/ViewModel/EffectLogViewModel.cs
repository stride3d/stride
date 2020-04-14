// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Assets.Effect;

namespace Xenko.Assets.Presentation.ViewModel
{
    [AssetViewModel(typeof(EffectLogAsset))]
    public class EffectLogViewModel : AssetViewModel<EffectLogAsset>
    {
        public EffectLogViewModel(AssetViewModelConstructionParameters parameters) : base(parameters)
        {
        }

        public string Text
        {
            get { return Asset.Text; }
            set { SetValue(Asset.Text != value, () => Asset.Text = value); }
        }
    }
}
