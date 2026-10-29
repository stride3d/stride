// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Behaviors;

/// <summary>
/// Shows the value of a slider in the scene while it is dragged, and applies it to the node of the slider once on
/// release, so the drag is a single undo step from the start value to the end value. A drag that loses the pointer
/// capture restores the start value without editing. Keys apply their value at once.
/// </summary>
/// <remarks>
/// The cross-platform counterpart of the WPF <c>LiveSliderEditBehavior</c>. Only user input applies a value: a value
/// set from code, such as the property grid refreshing the slider or the slider clamping a weight beyond its range, is
/// not an edit and is not written back, so the slider is bound one way to the node value.
/// The editor sends each change to the scene game one after another, waiting for the game to apply it, which lags
/// behind a drag: <see cref="Preview"/> writes the dragged values to the scene directly instead.
/// </remarks>
public sealed class LiveSliderEditBehavior : StyledElementBehavior<Slider>
{
    public static readonly StyledProperty<Action<double>?> PreviewProperty =
        AvaloniaProperty.Register<LiveSliderEditBehavior, Action<double>?>(nameof(Preview));

    private bool dragging;
    private double dragStartValue;

    /// <summary>
    /// Shows a dragged value in the scene without applying it.
    /// </summary>
    public Action<double>? Preview
    {
        get => GetValue(PreviewProperty);
        set => SetValue(PreviewProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject!.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, true);
        AssociatedObject.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
        AssociatedObject.AddHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost, RoutingStrategies.Direct | RoutingStrategies.Bubble, true);
        AssociatedObject.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Bubble, true);
        AssociatedObject.PropertyChanged += OnSliderPropertyChanged;
    }

    protected override void OnDetaching()
    {
        AssociatedObject!.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        AssociatedObject.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
        AssociatedObject.RemoveHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost);
        AssociatedObject.RemoveHandler(InputElement.KeyUpEvent, OnKeyUp);
        AssociatedObject.PropertyChanged -= OnSliderPropertyChanged;
        dragging = false;
        base.OnDetaching();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(AssociatedObject).Properties.IsLeftButtonPressed)
            return;

        dragStartValue = AssociatedObject!.Value;
        dragging = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!dragging)
            return;

        dragging = false;
        Apply();
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        // A release ends the drag before the capture is lost: a capture lost during a drag cancels it
        if (!dragging)
            return;

        dragging = false;
        AssociatedObject!.SetCurrentValue(RangeBase.ValueProperty, dragStartValue);
        Preview?.Invoke(dragStartValue);
    }

    private void OnSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (dragging && e.Property == RangeBase.ValueProperty)
            Preview?.Invoke(AssociatedObject!.Value);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Up or Key.PageUp or Key.Down or Key.PageDown or Key.Left or Key.Right or Key.Home or Key.End)
            Apply();
    }

    private void Apply()
    {
        if (AssociatedObject!.DataContext is not NodeViewModel node)
            return;

        var value = AssociatedObject.Value;
        // A click on the thumb that does not move it is not an edit
        if (node.NodeValue is IConvertible current && current.ToDouble(null) == value)
            return;

        node.NodeValue = value;
    }
}
