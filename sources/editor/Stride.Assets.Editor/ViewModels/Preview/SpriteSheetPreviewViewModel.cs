// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Preview;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Editor.Annotations;

namespace Stride.Assets.Editor.ViewModels.Preview;

[AssetPreviewViewModel<SpriteSheetPreview>]
public class SpriteSheetPreviewViewModel : TextureBasePreviewViewModel<SpriteSheetPreview>
{
    private SpriteSheetPreview spriteSheetPreview;
    private readonly int previewCurrentFrame = 1;
    private SpriteSheetDisplayMode displayMode;

    public SpriteSheetPreviewViewModel(SessionViewModel session)
        : base(session)
    {
        PreviewPreviousFrameCommand = new AnonymousCommand(ServiceProvider, () => { if (PreviewFrameCount > 0) PreviewCurrentFrame = 1 + (PreviewCurrentFrame + PreviewFrameCount - 2) % PreviewFrameCount; });
        PreviewNextFrameCommand = new AnonymousCommand(ServiceProvider, () => { if (PreviewFrameCount > 0) PreviewCurrentFrame = 1 + (PreviewCurrentFrame + PreviewFrameCount) % PreviewFrameCount; });
        DependentProperties.Add(nameof(DisplayMode), new[] { nameof(PreviewCurrentFrame), nameof(PreviewFrameCount) });
    }

    public int PreviewCurrentFrame { get { return Math.Min(PreviewFrameCount, spriteSheetPreview.CurrentFrame + 1); } set { SetValue(value - 1 != spriteSheetPreview.CurrentFrame, () => spriteSheetPreview.CurrentFrame = value - 1); } }

    public int PreviewFrameCount => spriteSheetPreview.FrameCount;

    public SpriteSheetDisplayMode DisplayMode { get { return displayMode; } set { SetValue(ref displayMode, value, () => { spriteSheetPreview.Mode = value; }); } }

    public CommandBase PreviewPreviousFrameCommand { get; }

    public CommandBase PreviewNextFrameCommand { get; }

    protected override void OnAttachPreview(SpriteSheetPreview preview)
    {
        AttachPreviewTexture(preview);
        spriteSheetPreview = preview;
        DisplayMode = spriteSheetPreview.Mode;
        // Reset the current frame from the view model into the preview
        PreviewCurrentFrame = previewCurrentFrame;
    }
}
