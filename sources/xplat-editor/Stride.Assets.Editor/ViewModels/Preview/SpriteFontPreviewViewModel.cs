// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Preview;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Editor.Annotations;

namespace Stride.Assets.Editor.ViewModels.Preview;

[AssetPreviewViewModel<SpriteFontPreview>]
public class SpriteFontPreviewViewModel : AssetPreviewViewModel<SpriteFontPreview>
{
    private SpriteFontPreview? spriteFontPreview;
    private string? previewString;

    public SpriteFontPreviewViewModel(ISessionViewModel session)
        : base(session)
    {
        ClearTextCommand = new AnonymousCommand(ServiceProvider, () => PreviewString = string.Empty);
    }

    public string? PreviewString { get { return previewString; } set { SetValue(ref previewString, value, () => spriteFontPreview?.SetPreviewString(value)); } }

    public ICommandBase ClearTextCommand { get; }

    protected override void OnAttachPreview(SpriteFontPreview preview)
    {
        spriteFontPreview = preview;
        spriteFontPreview.SetPreviewString(PreviewString);
    }
}
