// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Presentation.Commands;
using Xenko.Assets.Presentation.Preview;
using Xenko.Editor.Preview;
using Xenko.Editor.Preview.ViewModel;

namespace Xenko.Assets.Presentation.ViewModel.Preview
{
    [AssetPreviewViewModel(typeof(ProceduralModelPreview))]
    public class ProceduralModelPreviewViewModel : AssetPreviewViewModel
    {
        private ProceduralModelPreview modelPreview;

        public ProceduralModelPreviewViewModel(SessionViewModel session)
            : base(session)
        {
            ResetModelCommand = new AnonymousCommand(ServiceProvider, ResetModel);
        }

        public ICommandBase ResetModelCommand { get; }

        public override void AttachPreview(IAssetPreview preview)
        {
            modelPreview = (ProceduralModelPreview)preview;
        }

        private void ResetModel()
        {
            modelPreview?.ResetCamera();
        }
    }
}
