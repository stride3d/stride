// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.ViewModel.Preview;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Commands;
using Stride.Editor.Annotations;
using Stride.Editor.Preview;

namespace Stride.SpriteStudio.Editor;

[AssetPreviewViewModel<SpriteStudioSheetPreview>]
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
