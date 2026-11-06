// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Panels;

namespace Stride.GameStudio.Avalonia.ViewModels;

/// <summary>
/// The Asset preview panel: the preview game renders only while the panel is shown (visible and selected in its group), as
/// WPF's preview pane.
/// </summary>
internal sealed class AssetPreviewPanelViewModel : PanelViewModel
{
    public AssetPreviewPanelViewModel(PanelContext context, PreviewViewModel preview)
        : base(context)
    {
        Preview = preview ?? throw new ArgumentNullException(nameof(preview));
        Preview.IsPreviewVisible = IsShown;
    }

    /// <summary>
    /// The preview of the session.
    /// </summary>
    public PreviewViewModel Preview { get; }

    /// <inheritdoc />
    protected override void OnIsShownChanged()
    {
        Preview.IsPreviewVisible = IsShown;
    }
}
