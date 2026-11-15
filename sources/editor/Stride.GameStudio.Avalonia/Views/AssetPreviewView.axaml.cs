// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.GameStudio.Avalonia.ViewModels;

namespace Stride.GameStudio.Avalonia.Views;

/// <summary>
/// The Asset preview panel: the view the preview service builds for the selected asset, while the panel is on screen.
/// </summary>
public sealed partial class AssetPreviewView : UserControl
{
    private PreviewViewModel? preview;

    public AssetPreviewView()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        Follow((DataContext as AssetPreviewPanelViewModel)?.Preview);
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Follow((DataContext as AssetPreviewPanelViewModel)?.Preview);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        // The preview's view leaves this view, which another view of the panel may replace
        Follow(null);
    }

    private void Follow(PreviewViewModel? newPreview)
    {
        if (!this.IsAttachedToVisualTree())
            newPreview = null;
        if (preview != newPreview)
        {
            if (preview is not null)
                preview.PropertyChanged -= PreviewPropertyChanged;
            preview = newPreview;
            if (preview is not null)
                preview.PropertyChanged += PreviewPropertyChanged;
        }
        UpdateContent();
    }

    private void PreviewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PreviewViewModel.PreviewObject))
            UpdateContent();
    }

    private void UpdateContent()
    {
        var content = preview?.PreviewObject;
        PreviewContent.Content = content;
        PreviewContent.IsVisible = content is not null;
    }
}
