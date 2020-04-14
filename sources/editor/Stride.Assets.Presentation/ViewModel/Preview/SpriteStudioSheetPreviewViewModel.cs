// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Commands;
using Stride.Assets.Presentation.Preview;
using Stride.Editor.Preview;
using Stride.Editor.Preview.ViewModel;

namespace Stride.Assets.Presentation.ViewModel.Preview
{
    // FIXME: this view model should be in the SpriteStudio offline assembly! Can't be done now, because of a circular reference in CompilerApp referencing SpriteStudio, and Editor referencing CompilerApp
    [AssetPreviewViewModel(typeof(SpriteStudioSheetPreview))]
    public class SpriteStudioSheetPreviewViewModel : AssetPreviewViewModel
    {
        private SpriteStudioSheetPreview spriteStudioSheetPreview;

        public SpriteStudioSheetPreviewViewModel(SessionViewModel session)
            : base(session)
        {
            ResetModelCommand = new AnonymousCommand(ServiceProvider, ResetModel);
        }

        public ICommandBase ResetModelCommand { get; }

        public override void AttachPreview(IAssetPreview preview)
        {
            spriteStudioSheetPreview = (SpriteStudioSheetPreview)preview;
        }

        private void ResetModel()
        {
            spriteStudioSheetPreview.ResetCamera();
        }
    }
}
