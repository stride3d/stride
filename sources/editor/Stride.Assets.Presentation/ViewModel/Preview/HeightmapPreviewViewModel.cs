// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Assets.Presentation.Preview;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Editor.Preview;
using Stride.Editor.Preview.ViewModel;

namespace Stride.Assets.Presentation.ViewModel.Preview
{
    [AssetPreviewViewModel(typeof(HeightmapPreview))]
    public class HeightmapPreviewViewModel : TextureBasePreviewViewModel
    {
        private HeightmapPreview heightmapPreview;
        private int previewHeightmapLength;
        private int previewHeightmapWidth;

        public HeightmapPreviewViewModel(SessionViewModel session)
            : base(session)
        {
        }

        public int PreviewHeightmapLength { get { return previewHeightmapLength; } private set { SetValue(ref previewHeightmapLength, value); } }

        public int PreviewHeightmapWidth { get { return previewHeightmapWidth; } private set { SetValue(ref previewHeightmapWidth, value); } }

        public override void AttachPreview(IAssetPreview preview)
        {
            heightmapPreview = (HeightmapPreview)preview;
            heightmapPreview.NotifyHeightmapLoaded += UpdateHeightmapInfo;
            UpdateHeightmapInfo();
            AttachPreviewTexture(preview);
        }

        private void UpdateHeightmapInfo()
        {
            PreviewHeightmapWidth = heightmapPreview.Width;
            PreviewHeightmapLength = heightmapPreview.Length;
        }
    }
}
