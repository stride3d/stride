// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Assets.Presentation.Preview;
using Xenko.Editor.Preview;
using Xenko.Editor.Preview.ViewModel;

namespace Xenko.Assets.Presentation.ViewModel.Preview
{
    [AssetPreviewViewModel(typeof(MaterialPreview))]
    public class MaterialPreviewViewModel : AssetPreviewViewModel
    {
        private MaterialPreview materialPreview;
        private MaterialPreviewPrimitive selectedPrimitive;

        public MaterialPreviewViewModel(SessionViewModel session)
            : base(session)
        {
        }

        public Type PrimitiveTypes => typeof(MaterialPreviewPrimitive);
        
        public MaterialPreviewPrimitive SelectedPrimitive { get { return selectedPrimitive; } set { SetValue(ref selectedPrimitive, value); SetPrimitive(value); } }

        public override void AttachPreview(IAssetPreview preview)
        {
            this.materialPreview = (MaterialPreview)preview;
            SetPrimitive(SelectedPrimitive);
        }

        private void SetPrimitive(MaterialPreviewPrimitive primitive)
        {
            materialPreview.SetPrimitive(primitive);
        }
    }
}
