// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Stride.Core.Mathematics;
using Stride.Editor.Preview;
using Stride.Editor.Preview.View;

namespace Stride.Editor.Avalonia.Preview.Views;

/// <summary>
/// The default view of an asset preview: it shows the control the preview game renders in.
/// </summary>
[TemplatePart(PartStrideView, typeof(ContentPresenter))]
public class StridePreviewView : TemplatedControl, IPreviewView
{
    public const string PartStrideView = "PART_StrideView";

    /// <summary>
    /// The views derived from this one share its control theme unless they override this property with their own type.
    /// </summary>
    protected override Type StyleKeyOverride => typeof(StridePreviewView);

    private IAvaloniaPreviewBuilder? builder;
    private IAssetPreview? previewer;
    private ContentPresenter? presenter;

    /// <inheritdoc />
    public void InitializeView(IPreviewBuilder previewBuilder, IAssetPreview assetPreview)
    {
        previewer = assetPreview;
        builder = (IAvaloniaPreviewBuilder)previewBuilder;
        var viewModel = previewer.PreviewViewModel;
        if (viewModel != null)
        {
            viewModel.AttachPreview(previewer);
            DataContext = viewModel;
        }
        UpdateStrideView();

        Loaded += OnLoaded;
    }

    /// <inheritdoc />
    public void UpdateView(IAssetPreview assetPreview)
    {
        var viewModel = previewer?.PreviewViewModel;
        if (viewModel != null)
        {
            viewModel.AttachPreview(previewer!);
            DataContext = viewModel;
        }
        UpdateStrideView();
    }

    /// <inheritdoc />
    public Vector2? GetLaidOutViewSize()
    {
        var view = builder?.GetStrideView();
        if (view == null || view.Bounds.Width <= 0 || view.Bounds.Height <= 0)
            return null;

        var scaling = TopLevel.GetTopLevel(view)?.RenderScaling ?? 1.0;
        return new Vector2((int)(view.Bounds.Width * scaling), (int)(view.Bounds.Height * scaling));
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        presenter = e.NameScope.Find<ContentPresenter>(PartStrideView);
        UpdateStrideView();
    }

    private void OnLoaded(object? sender, RoutedEventArgs routedEventArgs)
    {
        previewer?.OnViewAttached();
    }

    private void UpdateStrideView()
    {
        if (presenter != null && builder != null)
        {
            var strideView = builder.GetStrideView();
            // The previews share the game view: the view of the previous preview (an asset of another type) may still
            // hold it, and a control has a single parent
            if (strideView?.GetVisualParent() is ContentPresenter previous && previous != presenter)
                previous.Content = null;
            presenter.Content = strideView;
        }
    }
}
