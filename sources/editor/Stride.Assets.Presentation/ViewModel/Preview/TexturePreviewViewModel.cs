// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Commands;
using Stride.Assets.Presentation.Preview;
using Stride.Editor.Preview;
using Stride.Editor.Preview.ViewModel;
using Stride.Graphics;

namespace Stride.Assets.Presentation.ViewModel.Preview
{
    [AssetPreviewViewModel(typeof(TexturePreview))]
    public class TexturePreviewViewModel : TextureBasePreviewViewModel
    {
        private TexturePreview texturePreview;
        private int previewTextureDepth;
        private TextureDimension previewDimension;
        private int previewTextureHeight;
        private int previewTextureWidth;
        private TextureCubePreviewMode selectedCubePreviewMode = TextureCubePreviewMode.Full;
        private float selectedDepth;

        public TexturePreviewViewModel(SessionViewModel session)
            : base(session)
        {
            PreviewPreviousDepthCommand = new AnonymousCommand(ServiceProvider, () => SelectedDepth = (float)(Math.Ceiling(SelectedDepth + PreviewTextureDepth - 1) % PreviewTextureDepth));
            PreviewNextDepthCommand = new AnonymousCommand(ServiceProvider, () => SelectedDepth = (float)(Math.Floor(SelectedDepth + 1) % PreviewTextureDepth));
        }

        public int PreviewTextureDepth { get { return previewTextureDepth; } private set { SetValue(ref previewTextureDepth, value); } }

        public TextureDimension PreviewDimension { get { return previewDimension; } private set { SetValue(ref previewDimension, value); } }

        public int PreviewTextureHeight { get { return previewTextureHeight; } private set { SetValue(ref previewTextureHeight, value); } }

        public int PreviewTextureWidth { get { return previewTextureWidth; } private set { SetValue(ref previewTextureWidth, value); } }
        
        public TextureCubePreviewMode SelectedCubePreviewMode { get { return selectedCubePreviewMode; } set { SetValue(ref selectedCubePreviewMode, value, () => texturePreview.SetCubePreviewMode(value)); } }
        
        public float SelectedDepth { get { return selectedDepth; } set { SetValue(ref selectedDepth, value, () => texturePreview.SetDepthToPreview(value)); } }

        public ICommandBase PreviewPreviousDepthCommand { get; }

        public ICommandBase PreviewNextDepthCommand { get; }

        public override void AttachPreview(IAssetPreview preview)
        {
            texturePreview = (TexturePreview)preview;
            texturePreview.NotifyTextureLoaded += UpdateTextureInfo;
            UpdateTextureInfo();
            AttachPreviewTexture(preview);
        }

        private void UpdateTextureInfo()
        {
            PreviewDimension = texturePreview.Dimension;
            PreviewTextureWidth = texturePreview.TextureWidth;
            PreviewTextureHeight = texturePreview.TextureHeight;
            PreviewTextureDepth = texturePreview.TextureDepth;
        }
    }
}
