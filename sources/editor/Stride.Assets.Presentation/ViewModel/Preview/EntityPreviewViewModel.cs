// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Assets.Presentation.Preview;
using Xenko.Editor.Preview;
using Xenko.Editor.Preview.ViewModel;

namespace Xenko.Assets.Presentation.ViewModel.Preview
{
    [AssetPreviewViewModel(typeof(EntityPreview))]
    public class EntityPreviewViewModel : AssetPreviewViewModel
    {
        public EntityPreviewViewModel(SessionViewModel session)
            : base(session)
        {
        }

        public override void AttachPreview(IAssetPreview preview)
        {
            // Nothing for now
        }
    }
}
