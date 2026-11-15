// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// The connector of a slot, a circle that links start from or end at, placed in the node template of a <see cref="NodeGraphControl"/>.
/// The connector registers itself with the graph it is in: the graph draws the links of <see cref="Slot"/> to its center, and a
/// drag from it creates or moves a link.
/// </summary>
/// <remarks>
/// Pseudo-classes: <c>:input</c> or <c>:output</c> after <see cref="Direction"/>, <c>:connected</c> while a link goes to or from
/// the slot, <c>:droptarget</c> while the link the user drags would be accepted here.
/// </remarks>
public sealed class NodeGraphConnector : Control
{
    /// <summary>
    /// The size of a connector whose width and height aren't set.
    /// </summary>
    public const double DefaultSize = 15.0;

    /// <summary>
    /// Identifies the <see cref="Slot"/> styled property.
    /// </summary>
    public static readonly StyledProperty<object?> SlotProperty =
        AvaloniaProperty.Register<NodeGraphConnector, object?>(nameof(Slot));

    /// <summary>
    /// Identifies the <see cref="Direction"/> styled property.
    /// </summary>
    public static readonly StyledProperty<NodeGraphSlotDirection> DirectionProperty =
        AvaloniaProperty.Register<NodeGraphConnector, NodeGraphSlotDirection>(nameof(Direction));

    /// <summary>
    /// Identifies the <see cref="Fill"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<NodeGraphConnector, IBrush?>(nameof(Fill), Brushes.LightGray);

    /// <summary>
    /// Identifies the <see cref="Stroke"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<NodeGraphConnector, IBrush?>(nameof(Stroke), Brushes.LightGray);

    /// <summary>
    /// Identifies the <see cref="StrokeThickness"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<NodeGraphConnector, double>(nameof(StrokeThickness), 3.0);

    /// <summary>
    /// Identifies the <see cref="IsConnected"/> direct property.
    /// </summary>
    public static readonly DirectProperty<NodeGraphConnector, bool> IsConnectedProperty =
        AvaloniaProperty.RegisterDirect<NodeGraphConnector, bool>(nameof(IsConnected), o => o.IsConnected);

    private NodeGraphControl? graph;
    private bool isConnected;

    static NodeGraphConnector()
    {
        AffectsRender<NodeGraphConnector>(FillProperty, StrokeProperty, StrokeThicknessProperty);
        AffectsMeasure<NodeGraphConnector>(StrokeThicknessProperty);
        // A circle, not stretched by its panel
        HorizontalAlignmentProperty.OverrideDefaultValue<NodeGraphConnector>(HorizontalAlignment.Center);
        VerticalAlignmentProperty.OverrideDefaultValue<NodeGraphConnector>(VerticalAlignment.Center);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeGraphConnector"/> class.
    /// </summary>
    public NodeGraphConnector()
    {
        UpdateDirectionPseudoClasses();
    }

    /// <summary>
    /// The slot this connector shows, an object a link resolver returns for the links of this slot.
    /// </summary>
    public object? Slot
    {
        get => GetValue(SlotProperty);
        set => SetValue(SlotProperty, value);
    }

    /// <summary>
    /// Whether links start from this slot or end at it.
    /// </summary>
    public NodeGraphSlotDirection Direction
    {
        get => GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    /// <summary>
    /// The brush of the inside of the circle.
    /// </summary>
    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>
    /// The brush of the circle.
    /// </summary>
    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <summary>
    /// The thickness of the circle.
    /// </summary>
    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    /// <summary>
    /// Whether a link of the graph goes to or from <see cref="Slot"/>. The graph sets it.
    /// </summary>
    public bool IsConnected
    {
        get => isConnected;
        internal set
        {
            if (SetAndRaise(IsConnectedProperty, ref isConnected, value))
                PseudoClasses.Set(":connected", value);
        }
    }

    /// <summary>
    /// The graph this connector is registered with.
    /// </summary>
    internal NodeGraphControl? Graph => graph;

    /// <summary>
    /// Whether the link the user drags would be accepted by this connector. The graph sets it.
    /// </summary>
    internal bool IsDropTarget
    {
        set => PseudoClasses.Set(":droptarget", value);
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var thickness = StrokeThickness;
        var size = Bounds.Size;
        var radius = Math.Max(0, (Math.Min(size.Width, size.Height) - thickness) / 2);
        var center = new Point(size.Width / 2, size.Height / 2);
        var pen = Stroke is { } stroke && thickness > 0 ? new Pen(stroke, thickness) : null;
        // The inside always takes the pointer, even when it shows nothing
        context.DrawEllipse(Fill ?? Brushes.Transparent, pen, center, radius, radius);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize) => new(DefaultSize, DefaultSize);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DirectionProperty)
        {
            UpdateDirectionPseudoClasses();
            graph?.OnConnectorChanged(this);
        }
        else if (change.Property == SlotProperty)
        {
            graph?.OnConnectorChanged(this);
        }
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        graph = this.FindAncestorOfType<NodeGraphControl>();
        graph?.RegisterConnector(this);
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        graph?.UnregisterConnector(this);
        graph = null;
        IsDropTarget = false;
    }

    private void UpdateDirectionPseudoClasses()
    {
        PseudoClasses.Set(":input", Direction == NodeGraphSlotDirection.Input);
        PseudoClasses.Set(":output", Direction == NodeGraphSlotDirection.Output);
    }
}
