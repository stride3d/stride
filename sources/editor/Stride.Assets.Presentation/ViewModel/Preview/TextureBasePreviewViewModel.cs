// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Assets.Presentation.Preview;
using Stride.Editor.Preview;

namespace Stride.Assets.Presentation.ViewModel.Preview
{
    public abstract class TextureBasePreviewViewModel : AssetPreviewViewModel
    {
        private ITextureBasePreview textureBasePreview;
        private string previewSelectedMipMap;

        private float spriteScale;

        protected TextureBasePreviewViewModel(SessionViewModel session)
            : base(session)
        {
            // Initialize texture preview
            PreviewAvailableMipMaps = new ObservableList<string>();
            previewSelectedMipMap = "Level 0";
            PreviewZoomInCommand = new AnonymousCommand(ServiceProvider, ZoomIn);
            PreviewZoomOutCommand = new AnonymousCommand(ServiceProvider, ZoomOut);
            PreviewFitOnScreenCommand = new AnonymousCommand(ServiceProvider, FitOnScreen);
            PreviewScaleToRealSizeCommand = new AnonymousCommand(ServiceProvider, ScaleToRealSize);
        }
        
        public string PreviewSelectedMipMap
        {
            get { return previewSelectedMipMap; }
            set
            {
                if (value != null)
                {
                    SetValue(ref previewSelectedMipMap, value);
                    textureBasePreview?.DisplayMipMap(ParseMipMapLevel(value));
                }
            }
        }

        public ObservableList<string> PreviewAvailableMipMaps { get; }

        public float SpriteScale { get { return spriteScale; } set { SetValue(ref spriteScale, value); } }

        public ICommandBase PreviewZoomInCommand { get; }

        public ICommandBase PreviewZoomOutCommand { get; }

        public ICommandBase PreviewFitOnScreenCommand { get; }

        public ICommandBase PreviewScaleToRealSizeCommand { get; }

        protected void AttachPreviewTexture(IAssetPreview preview)
        {
            textureBasePreview = (ITextureBasePreview)preview;
            var availableMipMaps = textureBasePreview.GetAvailableMipMaps();
            PreviewAvailableMipMaps.Clear();
            PreviewAvailableMipMaps.AddRange(availableMipMaps.Select(x => $"Level {x}"));
            textureBasePreview.DisplayMipMap(ParseMipMapLevel(PreviewSelectedMipMap));
            textureBasePreview.SpriteScaleChanged += (s, e) => SpriteScale = textureBasePreview.SpriteScale;
        }

        private void ZoomIn()
        {
            textureBasePreview.ZoomIn(null);
        }

        private void ZoomOut()
        {
            textureBasePreview.ZoomOut(null);
        }

        private void FitOnScreen()
        {
            textureBasePreview.FitOnScreen();
        }

        private void ScaleToRealSize()
        {
            textureBasePreview.ScaleToRealSize();
        }

        private static int ParseMipMapLevel(string level)
        {
            if (level == null)
                return 0;

            int result;
            int.TryParse(level.Substring("Level ".Length), out result);
            return result;
        }
    }
}
