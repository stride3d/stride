// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Preview;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Editor.Annotations;

namespace Stride.Assets.Editor.ViewModels.Preview;

[AssetPreviewViewModel<HeightmapPreview>]
public sealed class HeightmapPreviewViewModel : TextureBasePreviewViewModel<HeightmapPreview>
{
    private HeightmapPreview heightmapPreview;
    private int previewHeightmapLength;
    private int previewHeightmapWidth;

    public HeightmapPreviewViewModel(ISessionViewModel session)
        : base(session)
    {
    }

    public int PreviewHeightmapLength { get { return previewHeightmapLength; } private set { SetValue(ref previewHeightmapLength, value); } }

    public int PreviewHeightmapWidth { get { return previewHeightmapWidth; } private set { SetValue(ref previewHeightmapWidth, value); } }

    protected override void OnAttachPreview(HeightmapPreview preview)
    {
        heightmapPreview = preview;
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
