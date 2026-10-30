// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Editor.ViewModels.SpriteEditor;

namespace Stride.Assets.Editor.Avalonia.Behaviors;

/// <summary>
/// Binds a <see cref="ScrollViewer"/> to the <see cref="ViewportViewModel"/> of the sprite sheet editor: the size of the viewport
/// and the scroll offsets both ways, and the wheel with Ctrl zooms around the mouse. The cross-platform counterpart of the WPF
/// <c>BindActualSizeBehavior</c>, <c>BindScrollOffsetBehavior</c> and <c>ViewportZoomBehavior</c> of the sprite editor.
/// </summary>
public sealed class SpriteViewportBehavior : StyledElementBehavior<ScrollViewer>
{
    public static readonly StyledProperty<ViewportViewModel?> ViewportProperty =
        AvaloniaProperty.Register<SpriteViewportBehavior, ViewportViewModel?>(nameof(Viewport));

    public static readonly StyledProperty<KeyModifiers> ModifiersProperty =
        AvaloniaProperty.Register<SpriteViewportBehavior, KeyModifiers>(nameof(Modifiers), KeyModifiers.Control);

    private ViewportViewModel? viewport;
    private bool offsetPending;
    private bool updatingViewModel;

    /// <summary>
    /// The viewport of the editor.
    /// </summary>
    public ViewportViewModel? Viewport
    {
        get => GetValue(ViewportProperty);
        set => SetValue(ViewportProperty, value);
    }

    /// <summary>
    /// The keys to hold for the wheel to zoom rather than scroll (Ctrl by default).
    /// </summary>
    public KeyModifiers Modifiers
    {
        get => GetValue(ModifiersProperty);
        set => SetValue(ModifiersProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject!.PropertyChanged += OnScrollViewerPropertyChanged;
        // Before the scroll viewer scrolls with the wheel
        AssociatedObject.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        SetViewport(Viewport);
    }

    protected override void OnDetaching()
    {
        SetViewport(null);
        AssociatedObject!.PropertyChanged -= OnScrollViewerPropertyChanged;
        AssociatedObject.RemoveHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged);
        base.OnDetaching();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ViewportProperty && AssociatedObject is not null)
            SetViewport(Viewport);
    }

    private void SetViewport(ViewportViewModel? value)
    {
        if (viewport is not null)
            viewport.PropertyChanged -= OnViewportPropertyChanged;
        viewport = value;
        if (viewport is not null && AssociatedObject is { } scrollViewer)
        {
            viewport.PropertyChanged += OnViewportPropertyChanged;
            UpdateViewModelSize(scrollViewer);
            ApplyOffset();
        }
    }

    private void OnScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (viewport is null || sender is not ScrollViewer scrollViewer)
            return;

        if (e.Property == ScrollViewer.ViewportProperty)
        {
            UpdateViewModelSize(scrollViewer);
        }
        else if (e.Property == ScrollViewer.OffsetProperty && !offsetPending)
        {
            // The scroll bars moved: the view model follows. While an offset of the view model waits for the layout (a zoom
            // changes the extent), the scroll viewer only clamps its old offset.
            updatingViewModel = true;
            try
            {
                viewport.HorizontalOffset = scrollViewer.Offset.X;
                viewport.VerticalOffset = scrollViewer.Offset.Y;
            }
            finally
            {
                updatingViewModel = false;
            }
        }
    }

    private void UpdateViewModelSize(ScrollViewer scrollViewer)
    {
        viewport!.ViewportWidth = scrollViewer.Viewport.Width;
        viewport.ViewportHeight = scrollViewer.Viewport.Height;
    }

    private void OnViewportPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (updatingViewModel || e.PropertyName is not (nameof(ViewportViewModel.HorizontalOffset) or nameof(ViewportViewModel.VerticalOffset)))
            return;

        // The view model sets its offsets before its zoom: the scroll viewer gets them once the layout has the new size of
        // the image, as the WPF scroll viewer did
        if (!offsetPending)
        {
            offsetPending = true;
            Dispatcher.UIThread.Post(ApplyOffset, DispatcherPriority.Loaded);
        }
    }

    private void ApplyOffset()
    {
        offsetPending = false;
        if (viewport is null || AssociatedObject is not { } scrollViewer)
            return;

        scrollViewer.UpdateLayout();
        scrollViewer.Offset = new Vector(Math.Max(0, viewport.HorizontalOffset), Math.Max(0, viewport.VerticalOffset));
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (viewport is null || AssociatedObject is not { } scrollViewer || (e.KeyModifiers & Modifiers) != Modifiers || e.Delta.Y == 0)
            return;

        var position = e.GetPosition(scrollViewer);
        if (e.Delta.Y > 0)
            viewport.ZoomIn(position.X, position.Y);
        else
            viewport.ZoomOut(position.X, position.Y);
        e.Handled = true;
    }
}
