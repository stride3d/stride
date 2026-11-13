// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.ViewModels.CurveEditor;
using Stride.Core.Presentation.Avalonia.Drawing;
using Stride.Core.Presentation.Drawing;

namespace Stride.Assets.Editor.Avalonia.Views.CurveEditor;

/// <summary>
/// The pointer input of the curve area of the curve editor. One handler decides once per press what the gesture does, where the
/// WPF view has six behaviors that rely on the order of their events.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Left button: the click selects the key under the pointer, or clears the selection (<see cref="CurveEditorViewModel.PreviewClickCommand"/>);
/// with Alt it then adds a key (<see cref="CurveEditorViewModel.AddPointCommand"/>). A press on a key then drags the selected keys,
/// one undo entry when released; a press elsewhere draws a selection rectangle (none: replace, Shift: add, Ctrl: remove, Alt:
/// nothing, as in WPF).</item>
/// <item>Middle button: pans the view (Ctrl: the keys only, Shift: the values only).</item>
/// <item>Wheel: zooms around the pointer (Ctrl: the keys only, Shift: the values only).</item>
/// <item>Right button: remembers the position, where the context menu's "Add point" adds a key.</item>
/// </list>
/// </remarks>
internal sealed class CurveEditorPointerHandler
{
    /// <summary>
    /// The drag distance when the platform gives none, as the cursor service of the game editors.
    /// </summary>
    private static readonly Size DefaultDragDistance = new(4, 4);

    private readonly Control canvas;
    private readonly InputElement focusTarget;
    private readonly Control selectionRectangle;
    private readonly Func<CurveEditorViewModel?> getEditor;
    private Gesture gesture;
    private Point origin;
    private Point previous;
    private bool isDragging;
    private IResizingTarget? dragTarget;

    /// <summary>
    /// Handles the pointer input of a canvas.
    /// </summary>
    /// <param name="canvas">The canvas that draws the curve; positions are in its coordinates, which are those of the drawing.</param>
    /// <param name="focusTarget">The control that takes the focus on a press, which has the keys of the curve area.</param>
    /// <param name="selectionRectangle">The selection rectangle, shown over the canvas while it is drawn, in the canvas's coordinates.</param>
    /// <param name="getEditor">The editor the input applies to.</param>
    public CurveEditorPointerHandler(Control canvas, InputElement focusTarget, Control selectionRectangle, Func<CurveEditorViewModel?> getEditor)
    {
        this.canvas = canvas;
        this.focusTarget = focusTarget;
        this.selectionRectangle = selectionRectangle;
        this.getEditor = getEditor;
        selectionRectangle.IsVisible = false;
        selectionRectangle.IsHitTestVisible = false;
        canvas.PointerPressed += OnPointerPressed;
        canvas.PointerMoved += OnPointerMoved;
        canvas.PointerReleased += OnPointerReleased;
        canvas.PointerCaptureLost += OnPointerCaptureLost;
        canvas.PointerWheelChanged += OnPointerWheelChanged;
    }

    private enum Gesture
    {
        None,
        Pan,
        Drag,
        Rectangle,
    }

    /// <summary>
    /// The position of the last press of the right button, in the coordinates of the drawing.
    /// </summary>
    public DrawingPoint LastRightClickPosition { get; private set; }

    /// <summary>
    /// The axes a pan or a zoom applies to, from the modifier keys held.
    /// </summary>
    public static CurveAxes GetAxes(KeyModifiers modifiers)
    {
        return CurveAxes.FromModifiers(modifiers.HasFlag(KeyModifiers.Shift), modifiers.HasFlag(KeyModifiers.Control), HasOtherModifiers(modifiers));
    }

    /// <summary>
    /// How a click or a selection rectangle changes the selection, from the modifier keys held.
    /// </summary>
    public static CurveSelectionMode GetSelectionMode(KeyModifiers modifiers)
    {
        return CurveSelectionModes.FromModifiers(modifiers.HasFlag(KeyModifiers.Shift), modifiers.HasFlag(KeyModifiers.Control), HasOtherModifiers(modifiers));
    }

    private static bool HasOtherModifiers(KeyModifiers modifiers) => (modifiers & (KeyModifiers.Alt | KeyModifiers.Meta)) != 0;

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(canvas);
        var position = point.Position;
        if (point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
        {
            // Not handled: the context menu opens on the release
            LastRightClickPosition = position.ToDrawing();
            return;
        }

        if (gesture != Gesture.None || getEditor() is not { SelectedCurve: not null } editor)
            return;

        switch (point.Properties.PointerUpdateKind)
        {
            case PointerUpdateKind.MiddleButtonPressed when GetAxes(e.KeyModifiers).Any:
                Start(Gesture.Pan, e, position);
                break;

            case PointerUpdateKind.LeftButtonPressed:
                // The click first: it selects the key under the pointer and says whether there is one (WPF's preview click)
                editor.PreviewClickCommand.Execute(new CurveEditorClick(position.ToDrawing(), GetSelectionMode(e.KeyModifiers)));
                if (e.KeyModifiers == KeyModifiers.Alt)
                    editor.AddPointCommand.Execute(position.ToDrawing());

                if (editor.IsControlPointHovered && editor.SelectedCurve is IResizingTarget target)
                {
                    dragTarget = target;
                    target.OnResizingStarted(ResizingDirection.Center);
                    Start(Gesture.Drag, e, position);
                }
                else
                {
                    Start(Gesture.Rectangle, e, position);
                }
                break;

            default:
                return;
        }
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (gesture == Gesture.None)
            return;

        var point = e.GetCurrentPoint(canvas);
        var position = point.Position;
        var buttonPressed = gesture == Gesture.Pan ? point.Properties.IsMiddleButtonPressed : point.Properties.IsLeftButtonPressed;
        if (!buttonPressed)
        {
            // The release went elsewhere
            Stop(e.Pointer, complete: true, e.KeyModifiers);
            return;
        }

        if (!isDragging)
        {
            var dragDistance = canvas.GetPlatformSettings()?.GetTapSize(PointerType.Mouse) ?? DefaultDragDistance;
            var delta = position - origin;
            isDragging = gesture == Gesture.Pan || Math.Abs(delta.X) > dragDistance.Width || Math.Abs(delta.Y) > dragDistance.Height;
        }

        if (isDragging)
        {
            switch (gesture)
            {
                case Gesture.Pan:
                    getEditor()?.Pan(previous.ToDrawing(), position.ToDrawing(), GetAxes(e.KeyModifiers));
                    break;

                case Gesture.Drag:
                    var movement = position - previous;
                    if (movement.X != 0 || movement.Y != 0)
                        dragTarget?.OnResizingDelta(ResizingDirection.Center, movement.X, movement.Y);
                    break;

                case Gesture.Rectangle:
                    ShowRectangle(new Rect(origin, position).Normalize());
                    break;
            }
            previous = position;
        }
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var button = gesture == Gesture.Pan ? MouseButton.Middle : MouseButton.Left;
        if (gesture == Gesture.None || e.InitialPressMouseButton != button)
            return;

        if (isDragging && gesture != Gesture.Pan)
            previous = e.GetPosition(canvas);
        Stop(e.Pointer, complete: true, e.KeyModifiers);
        e.Handled = true;
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        // Another control took the pointer: a drag keeps where it went, as WPF's, and a rectangle selects nothing
        if (gesture != Gesture.None)
            Stop(null, complete: false, KeyModifiers.None);
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var axes = GetAxes(e.KeyModifiers);
        if (getEditor() is not { SelectedCurve: not null } editor || !axes.Any || e.Delta.Y == 0)
            return;

        editor.Zoom(e.GetPosition(canvas).ToDrawing(), e.Delta.Y > 0, axes);
        e.Handled = true;
    }

    private void Start(Gesture started, PointerPressedEventArgs e, Point position)
    {
        // The curve area takes the focus, for Delete and the navigation keys
        focusTarget.Focus(NavigationMethod.Pointer);
        gesture = started;
        origin = position;
        previous = position;
        isDragging = false;
        e.Pointer.Capture(canvas);
    }

    private void Stop(IPointer? pointer, bool complete, KeyModifiers modifiers)
    {
        var stopped = gesture;
        var dragged = isDragging;
        var target = dragTarget;
        gesture = Gesture.None;
        isDragging = false;
        dragTarget = null;
        selectionRectangle.IsVisible = false;
        // Releasing the capture raises the capture lost event, which has nothing left to stop
        pointer?.Capture(null);

        switch (stopped)
        {
            case Gesture.Drag when dragged:
                var total = previous - origin;
                target?.OnResizingCompleted(ResizingDirection.Center, total.X, total.Y);
                break;

            case Gesture.Rectangle when dragged && complete:
                getEditor()?.SelectCommand.Execute(new CurveEditorSelection(new Rect(origin, previous).Normalize().ToDrawing(), GetSelectionMode(modifiers)));
                break;
        }
    }

    private void ShowRectangle(Rect rectangle)
    {
        Canvas.SetLeft(selectionRectangle, rectangle.X);
        Canvas.SetTop(selectionRectangle, rectangle.Y);
        selectionRectangle.Width = rectangle.Width;
        selectionRectangle.Height = rectangle.Height;
        selectionRectangle.IsVisible = true;
    }
}
