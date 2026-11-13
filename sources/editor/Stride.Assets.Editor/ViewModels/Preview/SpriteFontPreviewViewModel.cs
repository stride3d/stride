// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Assets.Editor.Preview;
using Stride.Core.Presentation.Commands;
using Stride.Editor.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.ViewModels.Preview;

/// <summary>
/// The view model of the font previews: the string the preview renders.
/// </summary>
/// <remarks>
/// It serves the <see cref="SpriteFontPreview"/>; <see cref="PrecompiledSpriteFontPreviewViewModel"/> serves the
/// <see cref="PrecompiledSpriteFontPreview"/>, which shares its view.
/// </remarks>
[AssetPreviewViewModel<SpriteFontPreview>]
public class SpriteFontPreviewViewModel : AssetPreviewViewModel<IFontPreview>
{
    private IFontPreview? spriteFontPreview;
    private string? previewString;

    public SpriteFontPreviewViewModel(ISessionViewModel session)
        : base(session)
    {
        ClearTextCommand = new AnonymousCommand(ServiceProvider, () => PreviewString = string.Empty);
    }

    public string? PreviewString { get { return previewString; } set { SetValue(ref previewString, value, () => spriteFontPreview?.SetPreviewString(value)); } }

    public ICommandBase ClearTextCommand { get; }

    protected override void OnAttachPreview(IFontPreview preview)
    {
        spriteFontPreview = preview;
        spriteFontPreview.SetPreviewString(PreviewString);
    }
}
