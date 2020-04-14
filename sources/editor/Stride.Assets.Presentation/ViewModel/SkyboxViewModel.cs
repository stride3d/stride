// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Editor.Extensions;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Assets.Skyboxes;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Core.Quantum;

namespace Xenko.Assets.Presentation.ViewModel
{
    [AssetViewModel(typeof(SkyboxAsset))]
    public class SkyboxViewModel : AssetViewModel<SkyboxAsset>
    {
        public SkyboxViewModel(AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
        }
    }
}
