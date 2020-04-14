// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Presentation.Commands;
using Xenko.Assets.Presentation.Preview;
using Xenko.Editor.Preview;
using Xenko.Editor.Preview.ViewModel;

namespace Xenko.Assets.Presentation.ViewModel.Preview
{
    [AssetPreviewViewModel(typeof(SpriteSheetPreview))]
    public class SpriteSheetPreviewViewModel : TextureBasePreviewViewModel
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

        public override void AttachPreview(IAssetPreview preview)
        {
            AttachPreviewTexture(preview);
            spriteSheetPreview = (SpriteSheetPreview)preview;
            DisplayMode = spriteSheetPreview.Mode;
            // Reset the current frame from the view model into the preview
            PreviewCurrentFrame = previewCurrentFrame;
        }
    }
}
