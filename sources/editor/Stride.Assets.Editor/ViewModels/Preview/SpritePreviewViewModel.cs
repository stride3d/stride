// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.Preview;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Editor.Annotations;
using Stride.Editor.Preview;

namespace Stride.Assets.Editor.ViewModels.Preview;

/// <summary>
/// The view model of any <see cref="PreviewFromSpriteBatch{T}"/> that declares no view model of its own.
/// </summary>
[AssetPreviewViewModel(typeof(PreviewFromSpriteBatch<>))]
public class SpritePreviewViewModel : TextureBasePreviewViewModel<IAssetPreview>
{
    private ITextureBasePreview? spritePreview;
    private int previewWidth;
    private int previewHeight;

    public SpritePreviewViewModel(ISessionViewModel session)
        : base(session)
    {
    }

    public int PreviewWidth { get => previewWidth; private set => SetValue(ref previewWidth, value); }

    public int PreviewHeight { get => previewHeight; private set => SetValue(ref previewHeight, value); }

    /// <inheritdoc />
    protected override void OnAttachPreview(IAssetPreview preview)
    {
        spritePreview = (ITextureBasePreview)preview;
        preview.ContentLoaded += (sender, e) => UpdateSize();
        UpdateSize();
        AttachPreviewTexture(preview);
    }

    private void UpdateSize()
    {
        if (spritePreview == null)
            return;
        PreviewWidth = (int)spritePreview.SpriteSize.X;
        PreviewHeight = (int)spritePreview.SpriteSize.Y;
    }
}
