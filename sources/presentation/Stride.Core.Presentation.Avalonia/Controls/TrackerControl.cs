// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// Two lines, horizontal and vertical, that cross at <see cref="Position"/>, kept inside <see cref="LineExtents"/>: the tracker
/// of the curve editor. With <see cref="TrackPointer"/>, the position
/// follows the pointer over the parent of the tracker. The tracker takes no input.
/// </summary>
public sealed class TrackerControl : Control
{
    /// <summary>
    /// Identifies the <see cref="LineExtents"/> styled property.
    /// </summary>
    public static readonly StyledProperty<Rect> LineExtentsProperty =
        AvaloniaProperty.Register<TrackerControl, Rect>(nameof(LineExtents));

    /// <summary>
    /// Identifies the <see cref="LineStroke"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> LineStrokeProperty =
        AvaloniaProperty.Register<TrackerControl, IBrush?>(nameof(LineStroke), Brushes.Gray);

    /// <summary>
    /// Identifies the <see cref="LineThickness"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> LineThicknessProperty =
        AvaloniaProperty.Register<TrackerControl, double>(nameof(LineThickness), 1.0);

    /// <summary>
    /// Identifies the <see cref="Position"/> styled property.
    /// </summary>
    public static readonly StyledProperty<Point> PositionProperty =
        AvaloniaProperty.Register<TrackerControl, Point>(nameof(Position));

    /// <summary>
    /// Identifies the <see cref="TrackPointer"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> TrackPointerProperty =
        AvaloniaProperty.Register<TrackerControl, bool>(nameof(TrackPointer));

    private InputElement? trackedParent;

    static TrackerControl()
    {
        IsHitTestVisibleProperty.OverrideDefaultValue<TrackerControl>(false);
        AffectsRender<TrackerControl>(LineExtentsProperty, LineStrokeProperty, LineThicknessProperty, PositionProperty);
    }

    /// <summary>
    /// The area the lines are drawn in; an empty area means the whole control.
    /// </summary>
    public Rect LineExtents { get => GetValue(LineExtentsProperty); set => SetValue(LineExtentsProperty, value); }

    /// <summary>
    /// The brush of the lines.
    /// </summary>
    public IBrush? LineStroke { get => GetValue(LineStrokeProperty); set => SetValue(LineStrokeProperty, value); }

    /// <summary>
    /// The thickness of the lines.
    /// </summary>
    public double LineThickness { get => GetValue(LineThicknessProperty); set => SetValue(LineThicknessProperty, value); }

    /// <summary>
    /// Where the lines cross, in the coordinates of this control.
    /// </summary>
    public Point Position { get => GetValue(PositionProperty); set => SetValue(PositionProperty, value); }

    /// <summary>
    /// Whether <see cref="Position"/> follows the pointer over the parent of this control.
    /// </summary>
    public bool TrackPointer { get => GetValue(TrackPointerProperty); set => SetValue(TrackPointerProperty, value); }

    /// <summary>
    /// The position where the lines cross once kept inside <see cref="LineExtents"/>.
    /// </summary>
    public Point ActualPosition
    {
        get
        {
            var extents = GetActualExtents();
            var position = Position;
            return new Point(Math.Clamp(position.X, extents.Left, Math.Max(extents.Left, extents.Right)),
                             Math.Clamp(position.Y, extents.Top, Math.Max(extents.Top, extents.Bottom)));
        }
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (LineStroke is not { } stroke || LineThickness <= 0)
            return;

        var extents = GetActualExtents();
        var position = ActualPosition;
        var pen = new Pen(stroke, LineThickness);
        context.DrawLine(pen, new Point(extents.Left, position.Y), new Point(extents.Right, position.Y));
        context.DrawLine(pen, new Point(position.X, extents.Top), new Point(position.X, extents.Bottom));
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateTrackedParent();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        SetTrackedParent(null);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TrackPointerProperty)
            UpdateTrackedParent();
    }

    private Rect GetActualExtents()
    {
        var extents = LineExtents;
        return extents.Width > 0 && extents.Height > 0 ? extents : new Rect(Bounds.Size);
    }

    private void UpdateTrackedParent()
    {
        SetTrackedParent(TrackPointer && this.FindAncestorOfType<InputElement>() is { } parent ? parent : null);
    }

    private void SetTrackedParent(InputElement? parent)
    {
        if (ReferenceEquals(trackedParent, parent))
            return;

        trackedParent?.RemoveHandler(PointerMovedEvent, OnParentPointerMoved);
        trackedParent = parent;
        // Handled moves too: the control under the tracker may handle them (the curve editor's canvas while it pans)
        trackedParent?.AddHandler(PointerMovedEvent, OnParentPointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void OnParentPointerMoved(object? sender, PointerEventArgs e)
    {
        Position = e.GetPosition(this);
    }
}
