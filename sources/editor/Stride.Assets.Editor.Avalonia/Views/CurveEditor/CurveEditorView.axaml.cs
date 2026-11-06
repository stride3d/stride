// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Stride.Assets.Editor.ViewModels.CurveEditor;
using Stride.Core.Presentation.Avalonia.Drawing;
using Stride.Core.Presentation.Drawing;

namespace Stride.Assets.Editor.Avalonia.Views.CurveEditor;

/// <summary>
/// The view of the curve editor panel: the curves opened, and the selected one drawn with its axes in a
/// <see cref="Core.Presentation.Avalonia.Controls.DrawingCanvas"/>. The middle button pans and the wheel zooms, both axes, or
/// the keys only with Control, or the values only with Shift, as in WPF.
/// </summary>
public sealed partial class CurveEditorView : UserControl
{
    /// <summary>
    /// The display modes of a rotation curve, for its combo box.
    /// </summary>
    public static IReadOnlyList<RotationDisplayMode> RotationDisplayModes { get; } = Enum.GetValues<RotationDisplayMode>();

    /// <summary>
    /// Converts the curve area of the editor to the extents of the tracker.
    /// </summary>
    public static IValueConverter DrawingRectToRect { get; } = new FuncValueConverter<DrawingRect, Rect>(x => x.ToAvalonia());

    private Point? panPosition;

    public CurveEditorView()
    {
        AvaloniaXamlLoader.Load(this);
        var canvas = this.Get<Control>("Canvas");
        canvas.PointerPressed += OnCanvasPointerPressed;
        canvas.PointerMoved += OnCanvasPointerMoved;
        canvas.PointerReleased += OnCanvasPointerReleased;
        canvas.PointerCaptureLost += (_, _) => panPosition = null;
        canvas.PointerWheelChanged += OnCanvasPointerWheelChanged;
    }

    private CurveEditorViewModel? Editor => DataContext as CurveEditorViewModel;

    /// <summary>
    /// The axes a pan or a zoom applies to, from the modifier keys held.
    /// </summary>
    public static CurveAxes GetAxes(KeyModifiers modifiers)
    {
        return CurveAxes.FromModifiers(modifiers.HasFlag(KeyModifiers.Shift), modifiers.HasFlag(KeyModifiers.Control),
            (modifiers & (KeyModifiers.Alt | KeyModifiers.Meta)) != 0);
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var canvas = (Control)sender!;
        var point = e.GetCurrentPoint(canvas);
        if (!point.Properties.IsMiddleButtonPressed || Editor?.SelectedCurve is null || !GetAxes(e.KeyModifiers).Any)
            return;

        // Middle drag pans the view
        Focus();
        panPosition = point.Position;
        e.Pointer.Capture(canvas);
        e.Handled = true;
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (panPosition is not { } previous)
            return;

        var canvas = (Control)sender!;
        var point = e.GetCurrentPoint(canvas);
        if (!point.Properties.IsMiddleButtonPressed)
        {
            StopPanning(e.Pointer);
            return;
        }

        Editor?.Pan(previous.ToDrawing(), point.Position.ToDrawing(), GetAxes(e.KeyModifiers));
        panPosition = point.Position;
        e.Handled = true;
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (panPosition is null || e.InitialPressMouseButton != MouseButton.Middle)
            return;

        StopPanning(e.Pointer);
        e.Handled = true;
    }

    private void OnCanvasPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var axes = GetAxes(e.KeyModifiers);
        if (Editor is not { SelectedCurve: not null } editor || !axes.Any || e.Delta.Y == 0)
            return;

        editor.Zoom(e.GetPosition((Control)sender!).ToDrawing(), e.Delta.Y > 0, axes);
        e.Handled = true;
    }

    private void StopPanning(IPointer pointer)
    {
        panPosition = null;
        pointer.Capture(null);
    }
}
