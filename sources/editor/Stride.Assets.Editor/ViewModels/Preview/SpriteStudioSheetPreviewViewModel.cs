// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Preview;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Editor.Annotations;

namespace Stride.Assets.Editor.ViewModels.Preview;

// FIXME: this view model should be in the SpriteStudio offline assembly! Can't be done now, because of a circular reference in CompilerApp referencing SpriteStudio, and Editor referencing CompilerApp
[AssetPreviewViewModel<SpriteStudioSheetPreview>]
public class SpriteStudioSheetPreviewViewModel : AssetPreviewViewModel<SpriteStudioSheetPreview>
{
    private SpriteStudioSheetPreview? spriteStudioSheetPreview;

    public SpriteStudioSheetPreviewViewModel(ISessionViewModel session)
        : base(session)
    {
        ResetModelCommand = new AnonymousCommand(ServiceProvider, ResetModel);
    }

    public ICommandBase ResetModelCommand { get; }

    protected override void OnAttachPreview(SpriteStudioSheetPreview preview)
    {
        spriteStudioSheetPreview = preview;
    }

    private void ResetModel()
    {
        spriteStudioSheetPreview?.ResetCamera();
    }
}
