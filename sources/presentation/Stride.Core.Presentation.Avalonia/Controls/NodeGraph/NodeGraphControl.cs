// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// A node graph: nodes with input and output slots, and links from an output slot to an input slot. The control knows nothing of
/// the view models it shows: the nodes and the links are any objects, a node is shown by <see cref="NodeTemplate"/>, which places
/// a <see cref="NodeGraphConnector"/> for each slot, and <see cref="LinkResolver"/> gives the slots of a link. The user's changes
/// go through <see cref="LinkCommand"/> and <see cref="UnlinkCommand"/>, and the selection through <see cref="SelectedNodes"/>
/// and <see cref="SelectedLinks"/>.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Left button: a press on an output connector drags a new link, which <see cref="LinkCommand"/> creates when it is released on
/// an input connector the command accepts; a press on an input connector picks up its last link, which is moved to another input
/// connector (<see cref="LinkCommand"/>, with <see cref="NodeGraphLinkRequest.ReplacedLink"/>) or, released away from any input
/// connector, removed (<see cref="UnlinkCommand"/>). A press on a node selects it (Shift: adds it, Ctrl: toggles it) and drags the
/// selected nodes; a click on a node of a selection selects only that node. A press on a link selects it the same way.
/// A press elsewhere draws a selection rectangle (none: replaces the selection, Shift: adds to it, Ctrl: toggles), and a click
/// there clears the selection.</item>
/// <item>Middle button, or right button moved: pans the view. A right click without a move opens the context menu.</item>
/// <item>Wheel: zooms around the pointer.</item>
/// <item>Home: shows every node (<see cref="FitToView"/>). Escape: cancels the drag.</item>
/// </list>
/// With <see cref="AutoLayout"/>, the nodes are laid out as a tree from left to right whenever nodes are added; the user
/// can then move them. Positions aren't saved.
/// </remarks>
public sealed class NodeGraphControl : Control
{
    /// <summary>
    /// The smallest <see cref="Zoom"/>.
    /// </summary>
    public const double MinZoom = 0.1;

    /// <summary>
    /// The largest <see cref="Zoom"/>.
    /// </summary>
    public const double MaxZoom = 4.0;

    /// <summary>
    /// The vertical space between two nodes of a column of the automatic layout.
    /// </summary>
    public const double LayoutVertexGap = 50.0;

    /// <summary>
    /// The horizontal space between two columns of the automatic layout.
    /// </summary>
    public const double LayoutLayerGap = 100.0;

    /// <summary>
    /// The space between the first node and the top left corner of the graph.
    /// </summary>
    public const double LayoutMargin = 20.0;

    /// <summary>
    /// Identifies the <see cref="Nodes"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IEnumerable?> NodesProperty =
        AvaloniaProperty.Register<NodeGraphControl, IEnumerable?>(nameof(Nodes));

    /// <summary>
    /// Identifies the <see cref="Links"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IEnumerable?> LinksProperty =
        AvaloniaProperty.Register<NodeGraphControl, IEnumerable?>(nameof(Links));

    /// <summary>
    /// Identifies the <see cref="LinkResolver"/> styled property.
    /// </summary>
    public static readonly StyledProperty<INodeGraphLinkResolver?> LinkResolverProperty =
        AvaloniaProperty.Register<NodeGraphControl, INodeGraphLinkResolver?>(nameof(LinkResolver));

    /// <summary>
    /// Identifies the <see cref="NodeTemplate"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> NodeTemplateProperty =
        AvaloniaProperty.Register<NodeGraphControl, IDataTemplate?>(nameof(NodeTemplate));

    /// <summary>
    /// Identifies the <see cref="SelectedNodes"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IList?> SelectedNodesProperty =
        AvaloniaProperty.Register<NodeGraphControl, IList?>(nameof(SelectedNodes));

    /// <summary>
    /// Identifies the <see cref="SelectedLinks"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IList?> SelectedLinksProperty =
        AvaloniaProperty.Register<NodeGraphControl, IList?>(nameof(SelectedLinks));

    /// <summary>
    /// Identifies the <see cref="LinkCommand"/> styled property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> LinkCommandProperty =
        AvaloniaProperty.Register<NodeGraphControl, ICommand?>(nameof(LinkCommand));

    /// <summary>
    /// Identifies the <see cref="UnlinkCommand"/> styled property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> UnlinkCommandProperty =
        AvaloniaProperty.Register<NodeGraphControl, ICommand?>(nameof(UnlinkCommand));

    /// <summary>
    /// Identifies the <see cref="AutoLayout"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> AutoLayoutProperty =
        AvaloniaProperty.Register<NodeGraphControl, bool>(nameof(AutoLayout), true);

    /// <summary>
    /// Identifies the <see cref="Zoom"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<NodeGraphControl, double>(nameof(Zoom), 1.0, coerce: CoerceZoom);

    /// <summary>
    /// Identifies the <see cref="Offset"/> styled property.
    /// </summary>
    public static readonly StyledProperty<Vector> OffsetProperty =
        AvaloniaProperty.Register<NodeGraphControl, Vector>(nameof(Offset));

    /// <summary>
    /// Identifies the <see cref="Background"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Panel.BackgroundProperty.AddOwner<NodeGraphControl>();

    /// <summary>
    /// Identifies the <see cref="LinkBrush"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> LinkBrushProperty =
        AvaloniaProperty.Register<NodeGraphControl, IBrush?>(nameof(LinkBrush), Brushes.LightGray);

    /// <summary>
    /// Identifies the <see cref="SelectedLinkBrush"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> SelectedLinkBrushProperty =
        AvaloniaProperty.Register<NodeGraphControl, IBrush?>(nameof(SelectedLinkBrush), Brushes.LightGreen);

    /// <summary>
    /// Identifies the <see cref="LinkPreviewBrush"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> LinkPreviewBrushProperty =
        AvaloniaProperty.Register<NodeGraphControl, IBrush?>(nameof(LinkPreviewBrush), Brushes.White);

    /// <summary>
    /// Identifies the <see cref="LinkThickness"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> LinkThicknessProperty =
        AvaloniaProperty.Register<NodeGraphControl, double>(nameof(LinkThickness), 3.0);

    private const double ZoomStep = 1.1;
    private const double DragThreshold = 4.0;
    private const double LinkHitTolerance = 4.0;
    private const double ConnectorHitTolerance = 4.0;
    private const int BezierSegmentCount = 24;
    private const double ArrowSize = 9.0;

    private static readonly IBrush SelectionRectangleFill = new ImmutableSolidColorBrush(Color.FromArgb(0x40, 0x36, 0x99, 0xB8));
    private static readonly IPen SelectionRectanglePen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x36, 0x99, 0xB8)), 1);
    private static readonly IDashStyle LinkPreviewDashStyle = new ImmutableDashStyle([2, 1], 0);

    private readonly NodeGraphLinksLayer linksLayer;
    private readonly NodeGraphNodesPanel nodesPanel;
    private readonly NodeGraphOverlayLayer overlayLayer;
    private readonly Dictionary<object, NodeGraphNode> containers = new(ReferenceEqualityComparer.Instance);
    private readonly List<NodeGraphConnector> connectors = [];
    private readonly ObservableCollection<object> ownSelectedNodes = [];
    private readonly ObservableCollection<object> ownSelectedLinks = [];
    private INotifyCollectionChanged? observedNodes;
    private INotifyCollectionChanged? observedLinks;
    private INotifyCollectionChanged? observedSelectedNodes;
    private INotifyCollectionChanged? observedSelectedLinks;
    private bool isLayoutPending;

    // The gesture of the current press
    private Gesture gesture;
    private MouseButton gestureButton;
    private Point pressPosition;
    private Point lastPosition;
    private Point currentPosition;
    private KeyModifiers pressModifiers;
    private bool hasMoved;
    private NodeGraphNode? pressedNode;
    private bool selectOnlyPressedNodeOnClick;
    private readonly List<(NodeGraphNode Node, Point Start)> draggedNodes = [];
    private object? linkSourceSlot;
    private object? replacedLink;
    private Point linkPreviewEnd;
    private NodeGraphConnector? hoveredInputConnector;
    private NodeGraphConnector? dropTarget;

    static NodeGraphControl()
    {
        FocusableProperty.OverrideDefaultValue<NodeGraphControl>(true);
        ClipToBoundsProperty.OverrideDefaultValue<NodeGraphControl>(true);
        BackgroundProperty.OverrideDefaultValue<NodeGraphControl>(new ImmutableSolidColorBrush(Color.FromRgb(0x49, 0x49, 0x4E)));
        AffectsRender<NodeGraphControl>(BackgroundProperty);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeGraphControl"/> class.
    /// </summary>
    public NodeGraphControl()
    {
        linksLayer = new NodeGraphLinksLayer(this);
        nodesPanel = new NodeGraphNodesPanel(this);
        overlayLayer = new NodeGraphOverlayLayer(this);
        VisualChildren.Add(linksLayer);
        VisualChildren.Add(nodesPanel);
        VisualChildren.Add(overlayLayer);
        // The nodes are in the logical tree, for their styles, resources and data templates
        LogicalChildren.Add(nodesPanel);
        UpdateTransforms();
        Observe(ref observedSelectedNodes, ownSelectedNodes, OnSelectedNodesCollectionChanged);
        Observe(ref observedSelectedLinks, ownSelectedLinks, OnSelectedLinksCollectionChanged);
        // A node that changes its size or its slots moves the ends of its links
        LayoutUpdated += (_, _) => linksLayer.InvalidateVisual();
    }

    /// <summary>
    /// The nodes of the graph. A collection that notifies its changes updates the graph.
    /// </summary>
    public IEnumerable? Nodes
    {
        get => GetValue(NodesProperty);
        set => SetValue(NodesProperty, value);
    }

    /// <summary>
    /// The links of the graph, whose slots <see cref="LinkResolver"/> gives. A collection that notifies its changes updates the graph.
    /// </summary>
    public IEnumerable? Links
    {
        get => GetValue(LinksProperty);
        set => SetValue(LinksProperty, value);
    }

    /// <summary>
    /// Gives the slots of the links.
    /// </summary>
    public INodeGraphLinkResolver? LinkResolver
    {
        get => GetValue(LinkResolverProperty);
        set => SetValue(LinkResolverProperty, value);
    }

    /// <summary>
    /// The template of the nodes, which places a <see cref="NodeGraphConnector"/> for each slot. Without it, a data template found
    /// for the node is used.
    /// </summary>
    public IDataTemplate? NodeTemplate
    {
        get => GetValue(NodeTemplateProperty);
        set => SetValue(NodeTemplateProperty, value);
    }

    /// <summary>
    /// The selected nodes. The graph adds and removes the nodes the user selects, and shows the changes made by others when the
    /// collection notifies them. Without it, the graph keeps the selection itself.
    /// </summary>
    public IList? SelectedNodes
    {
        get => GetValue(SelectedNodesProperty);
        set => SetValue(SelectedNodesProperty, value);
    }

    /// <summary>
    /// The selected links, as <see cref="SelectedNodes"/>.
    /// </summary>
    public IList? SelectedLinks
    {
        get => GetValue(SelectedLinksProperty);
        set => SetValue(SelectedLinksProperty, value);
    }

    /// <summary>
    /// The command that links two slots, with a <see cref="NodeGraphLinkRequest"/>. A connector takes a dragged link only when the
    /// command can execute the request.
    /// </summary>
    public ICommand? LinkCommand
    {
        get => GetValue(LinkCommandProperty);
        set => SetValue(LinkCommandProperty, value);
    }

    /// <summary>
    /// The command that removes a link, with the link, when the user drags it off its input connector and releases it elsewhere.
    /// </summary>
    public ICommand? UnlinkCommand
    {
        get => GetValue(UnlinkCommandProperty);
        set => SetValue(UnlinkCommandProperty, value);
    }

    /// <summary>
    /// Whether the graph lays out its nodes as a tree whenever nodes are added. Otherwise a new node goes near the top left corner.
    /// </summary>
    public bool AutoLayout
    {
        get => GetValue(AutoLayoutProperty);
        set => SetValue(AutoLayoutProperty, value);
    }

    /// <summary>
    /// The scale of the view, between <see cref="MinZoom"/> and <see cref="MaxZoom"/>.
    /// </summary>
    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    /// <summary>
    /// Where the origin of the graph is in the control: a point of the graph is at <c>point * Zoom + Offset</c>.
    /// </summary>
    public Vector Offset
    {
        get => GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    /// <summary>
    /// The brush drawn under the graph.
    /// </summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>
    /// The brush of the links.
    /// </summary>
    public IBrush? LinkBrush
    {
        get => GetValue(LinkBrushProperty);
        set => SetValue(LinkBrushProperty, value);
    }

    /// <summary>
    /// The brush of the selected links.
    /// </summary>
    public IBrush? SelectedLinkBrush
    {
        get => GetValue(SelectedLinkBrushProperty);
        set => SetValue(SelectedLinkBrushProperty, value);
    }

    /// <summary>
    /// The brush of the link the user drags.
    /// </summary>
    public IBrush? LinkPreviewBrush
    {
        get => GetValue(LinkPreviewBrushProperty);
        set => SetValue(LinkPreviewBrushProperty, value);
    }

    /// <summary>
    /// The thickness of the links, in the coordinates of the graph.
    /// </summary>
    public double LinkThickness
    {
        get => GetValue(LinkThicknessProperty);
        set => SetValue(LinkThicknessProperty, value);
    }

    private IList EffectiveSelectedNodes => SelectedNodes ?? ownSelectedNodes;

    private IList EffectiveSelectedLinks => SelectedLinks ?? ownSelectedLinks;

    /// <summary>
    /// Gets the container of a node.
    /// </summary>
    /// <param name="node">A node of <see cref="Nodes"/>.</param>
    /// <returns>The container, or <c>null</c> if the node isn't in the graph.</returns>
    public NodeGraphNode? ContainerFromNode(object node) => containers.GetValueOrDefault(node);

    /// <summary>
    /// Gets the connector of a slot.
    /// </summary>
    /// <param name="slot">The slot.</param>
    /// <param name="direction">The side of the connector.</param>
    /// <returns>The connector, or <c>null</c> if no node of the graph shows the slot on that side.</returns>
    public NodeGraphConnector? ConnectorFromSlot(object slot, NodeGraphSlotDirection direction)
    {
        for (var i = connectors.Count - 1; i >= 0; --i)
        {
            var connector = connectors[i];
            if (connector.Direction == direction && Equals(connector.Slot, slot))
                return connector;
        }
        return null;
    }

    /// <summary>
    /// Gets the position of a node.
    /// </summary>
    /// <param name="node">A node of <see cref="Nodes"/>.</param>
    /// <returns>The position of its top left corner in the coordinates of the graph, or <c>null</c> if the node isn't in the graph
    /// or has no position yet (the graph places a new node at its next arrange).</returns>
    public Point? GetNodePosition(object node)
        => containers.TryGetValue(node, out var container) && container.HasPosition ? container.Position : null;

    /// <summary>
    /// Moves a node.
    /// </summary>
    /// <param name="node">A node of <see cref="Nodes"/>.</param>
    /// <param name="position">The position of its top left corner in the coordinates of the graph.</param>
    public void SetNodePosition(object node, Point position)
    {
        if (!containers.TryGetValue(node, out var container))
            return;

        container.Position = position;
        container.HasPosition = true;
        nodesPanel.InvalidateArrange();
    }

    /// <summary>
    /// Lays out the nodes as a tree, at the next arrange.
    /// </summary>
    public void LayoutNodes()
    {
        isLayoutPending = true;
        foreach (var container in containers.Values)
            container.HasPosition = false;
        nodesPanel.InvalidateArrange();
    }

    /// <summary>
    /// Zooms and pans the view so that it shows every node, at a zoom of 1 at most.
    /// </summary>
    public void FitToView()
    {
        var placed = containers.Values.Where(x => x.HasPosition).ToList();
        var size = Bounds.Size;
        if (placed.Count == 0 || size.Width <= 0 || size.Height <= 0)
            return;

        var area = placed.Select(x => new Rect(x.Position, x.Bounds.Size)).Aggregate((a, b) => a.Union(b)).Inflate(LayoutMargin);
        Zoom = Math.Min(1.0, Math.Min(size.Width / area.Width, size.Height / area.Height));
        Offset = new Vector((size.Width - area.Width * Zoom) / 2 - area.X * Zoom, (size.Height - area.Height * Zoom) / 2 - area.Y * Zoom);
    }

    /// <summary>
    /// Converts a point of the control to the coordinates of the graph.
    /// </summary>
    public Point ViewToGraph(Point point) => new((point.X - Offset.X) / Zoom, (point.Y - Offset.Y) / Zoom);

    /// <summary>
    /// Converts a point of the graph to the coordinates of the control.
    /// </summary>
    public Point GraphToView(Point point) => new(point.X * Zoom + Offset.X, point.Y * Zoom + Offset.Y);

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        // The whole area takes the pointer, including where nothing is drawn
        context.FillRectangle(Background ?? Brushes.Transparent, new Rect(Bounds.Size));
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        linksLayer.Measure(availableSize);
        nodesPanel.Measure(availableSize);
        overlayLayer.Measure(availableSize);
        return default;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var bounds = new Rect(finalSize);
        linksLayer.Arrange(bounds);
        nodesPanel.Arrange(bounds);
        overlayLayer.Arrange(bounds);
        return finalSize;
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == NodesProperty)
        {
            Observe(ref observedNodes, change.NewValue as INotifyCollectionChanged, OnNodesCollectionChanged);
            ResetContainers();
        }
        else if (change.Property == LinksProperty)
        {
            Observe(ref observedLinks, change.NewValue as INotifyCollectionChanged, OnLinksCollectionChanged);
            OnLinksChanged();
        }
        else if (change.Property == LinkResolverProperty)
        {
            OnLinksChanged();
        }
        else if (change.Property == NodeTemplateProperty)
        {
            foreach (var container in containers.Values)
                container.ContentTemplate = NodeTemplate;
        }
        else if (change.Property == SelectedNodesProperty)
        {
            Observe(ref observedSelectedNodes, EffectiveSelectedNodes as INotifyCollectionChanged, OnSelectedNodesCollectionChanged);
            UpdateNodeSelectionStates();
        }
        else if (change.Property == SelectedLinksProperty)
        {
            Observe(ref observedSelectedLinks, EffectiveSelectedLinks as INotifyCollectionChanged, OnSelectedLinksCollectionChanged);
            linksLayer.InvalidateVisual();
        }
        else if (change.Property == ZoomProperty || change.Property == OffsetProperty)
        {
            UpdateTransforms();
        }
        else if (change.Property == LinkBrushProperty || change.Property == SelectedLinkBrushProperty || change.Property == LinkPreviewBrushProperty || change.Property == LinkThicknessProperty)
        {
            linksLayer.InvalidateVisual();
        }
        else if (change.Property == AutoLayoutProperty && change.GetNewValue<bool>())
        {
            LayoutNodes();
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (gesture != Gesture.None)
            return;

        var point = e.GetCurrentPoint(this);
        var position = point.Position;
        Focus();
        pressPosition = lastPosition = currentPosition = position;
        pressModifiers = e.KeyModifiers;
        hasMoved = false;

        switch (point.Properties.PointerUpdateKind)
        {
            case PointerUpdateKind.MiddleButtonPressed:
                StartGesture(Gesture.Pan, MouseButton.Middle, e);
                return;

            case PointerUpdateKind.RightButtonPressed:
                // A pan once it moves; otherwise the release opens the context menu
                StartGesture(Gesture.PendingPan, MouseButton.Right, e);
                return;

            case PointerUpdateKind.LeftButtonPressed:
                break;

            default:
                return;
        }

        var source = e.Source as Visual;
        var connector = source?.GetSelfAndVisualAncestors().OfType<NodeGraphConnector>().FirstOrDefault(x => x.Graph == this);
        if (connector?.Slot is { } slot)
        {
            if (connector.Direction == NodeGraphSlotDirection.Output)
            {
                StartLinkDrag(slot, null, position, e);
                return;
            }

            // An input connector picks up its last link, to move it or remove it
            if (FindLastLinkTo(slot) is { } link && LinkResolver?.GetSourceSlot(link) is { } linkSource)
            {
                StartLinkDrag(linkSource, link, position, e);
                return;
            }
        }

        var container = source?.GetSelfAndVisualAncestors().OfType<NodeGraphNode>().FirstOrDefault(x => ReferenceEquals(x.GetVisualParent(), nodesPanel));
        if (container is not null)
        {
            PressNode(container, e);
            return;
        }

        if (HitTestLink(ViewToGraph(position)) is { } hitLink)
        {
            ClickItem(hitLink, EffectiveSelectedLinks, pressModifiers);
            e.Handled = true;
            return;
        }

        StartGesture(Gesture.Rectangle, MouseButton.Left, e);
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (gesture == Gesture.None)
            return;

        var position = e.GetPosition(this);
        currentPosition = position;
        if (!hasMoved && Distance(position, pressPosition) > DragThreshold)
            hasMoved = true;

        switch (gesture)
        {
            case Gesture.PendingPan:
                if (!hasMoved)
                    return;

                gesture = Gesture.Pan;
                goto case Gesture.Pan;

            case Gesture.Pan:
                Offset += position - lastPosition;
                lastPosition = position;
                break;

            case Gesture.NodeDrag:
                if (!hasMoved)
                    return;

                var delta = (position - pressPosition) / Zoom;
                foreach (var (node, start) in draggedNodes)
                {
                    node.Position = start + delta;
                    node.HasPosition = true;
                }
                nodesPanel.InvalidateArrange();
                break;

            case Gesture.LinkDrag:
                linkPreviewEnd = ViewToGraph(position);
                UpdateDropTarget(linkPreviewEnd);
                linksLayer.InvalidateVisual();
                break;

            case Gesture.Rectangle:
                overlayLayer.InvalidateVisual();
                break;
        }
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (gesture != Gesture.None && e.InitialPressMouseButton == gestureButton)
        {
            var endedGesture = gesture;
            // Ended before the commands run, so that they see the graph as it will stay
            var source = linkSourceSlot;
            var link = replacedLink;
            var target = dropTarget?.Slot;
            var isOverInput = hoveredInputConnector is not null;
            var node = pressedNode;
            var selectOnlyNode = selectOnlyPressedNodeOnClick && !hasMoved;
            var moved = hasMoved;
            EndGesture(e.Pointer);

            switch (endedGesture)
            {
                case Gesture.Pan:
                    // A moved right button doesn't open the context menu
                    e.Handled = true;
                    break;

                case Gesture.NodeDrag:
                    if (selectOnlyNode && node is not null)
                        SelectOnly(node.Node);
                    e.Handled = true;
                    break;

                case Gesture.LinkDrag:
                    if (source is not null && target is not null)
                    {
                        var request = new NodeGraphLinkRequest(source, target, link);
                        if (LinkCommand?.CanExecute(request) == true)
                            LinkCommand.Execute(request);
                    }
                    else if (link is not null && !isOverInput && UnlinkCommand?.CanExecute(link) == true)
                    {
                        UnlinkCommand.Execute(link);
                    }
                    e.Handled = true;
                    break;

                case Gesture.Rectangle:
                    SelectInRectangle(moved);
                    e.Handled = true;
                    break;
            }
        }
        base.OnPointerReleased(e);
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        // Another control took the pointer: moved nodes stay where they went, a link or a rectangle does nothing
        if (gesture != Gesture.None)
            EndGesture(null);
    }

    /// <inheritdoc/>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (e.Delta.Y == 0)
            return;

        // Around the pointer: the point of the graph under it stays there
        var position = e.GetPosition(this);
        var graphPosition = ViewToGraph(position);
        Zoom *= Math.Pow(ZoomStep, e.Delta.Y);
        Offset = new Vector(position.X - graphPosition.X * Zoom, position.Y - graphPosition.Y * Zoom);
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
            return;

        if (e.Key == Key.Home && e.KeyModifiers == KeyModifiers.None && gesture == Gesture.None)
        {
            FitToView();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && gesture != Gesture.None)
        {
            if (gesture == Gesture.NodeDrag)
            {
                // The nodes go back where they were
                foreach (var (node, start) in draggedNodes)
                    node.Position = start;
                nodesPanel.InvalidateArrange();
            }
            EndGesture(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Lays out the nodes that need it, once they are measured.
    /// </summary>
    internal void EnsureNodesLayout()
    {
        var unplaced = containers.Values.Where(x => !x.HasPosition).ToList();
        if (!isLayoutPending && unplaced.Count == 0)
            return;

        if (AutoLayout)
        {
            var nodes = OrderedContainers().Select(x => x.Node).ToList();
            var positions = NodeGraphTreeLayout.Compute(nodes, x => containers[x].DesiredSize, GetNodeLinks(), new Point(LayoutMargin, LayoutMargin), LayoutVertexGap, LayoutLayerGap);
            foreach (var (node, position) in positions)
            {
                var container = containers[node];
                container.Position = position;
                container.HasPosition = true;
            }
        }
        else
        {
            // Near the top left corner, each new node a bit lower and to the right of the previous one
            var cascade = 0;
            foreach (var container in unplaced)
            {
                container.Position = new Point(LayoutMargin + cascade, LayoutMargin + cascade);
                container.HasPosition = true;
                cascade += 20;
            }
        }
        isLayoutPending = false;
    }

    /// <summary>
    /// Draws the links, in the coordinates of the graph.
    /// </summary>
    internal void RenderLinks(DrawingContext context)
    {
        var thickness = LinkThickness;
        var selectedLinks = EffectiveSelectedLinks;
        IPen? linkPen = LinkBrush is { } linkBrush ? new Pen(linkBrush, thickness) : null;
        IPen? selectedLinkPen = SelectedLinkBrush is { } selectedBrush ? new Pen(selectedBrush, thickness) : null;
        foreach (var link in EnumerateLinks())
        {
            // The link the user moves is drawn as the preview
            if (gesture == Gesture.LinkDrag && ReferenceEquals(link, replacedLink))
                continue;

            if (GetLinkEnds(link) is not { } ends)
                continue;

            var isSelected = selectedLinks.Contains(link);
            var pen = isSelected ? selectedLinkPen : linkPen;
            if (pen is not null)
                DrawLink(context, pen, isSelected ? SelectedLinkBrush : LinkBrush, ends.Start, ends.End);
        }

        if (gesture == Gesture.LinkDrag && linkSourceSlot is not null && LinkPreviewBrush is { } previewBrush
            && GetConnectorCenter(ConnectorFromSlot(linkSourceSlot, NodeGraphSlotDirection.Output)) is { } start)
        {
            if (GetInputEnd(dropTarget) is { } end)
                DrawLink(context, new Pen(previewBrush, thickness), previewBrush, start, end);
            else
                context.DrawGeometry(null, new Pen(previewBrush, thickness, LinkPreviewDashStyle), CreateLinkGeometry(start, linkPreviewEnd));
        }
    }

    /// <summary>
    /// Draws the selection rectangle, in the coordinates of the control.
    /// </summary>
    internal void RenderOverlay(DrawingContext context)
    {
        if (gesture == Gesture.Rectangle && hasMoved)
            context.DrawRectangle(SelectionRectangleFill, SelectionRectanglePen, new Rect(pressPosition, currentPosition).Normalize());
    }

    internal void RegisterConnector(NodeGraphConnector connector)
    {
        if (!connectors.Contains(connector))
            connectors.Add(connector);
        UpdateConnectorState(connector, GetConnectedSlots());
        linksLayer.InvalidateVisual();
    }

    internal void UnregisterConnector(NodeGraphConnector connector)
    {
        connectors.Remove(connector);
        if (ReferenceEquals(dropTarget, connector))
            dropTarget = null;
        if (ReferenceEquals(hoveredInputConnector, connector))
            hoveredInputConnector = null;
        linksLayer.InvalidateVisual();
    }

    internal void OnConnectorChanged(NodeGraphConnector connector)
    {
        UpdateConnectorState(connector, GetConnectedSlots());
        linksLayer.InvalidateVisual();
    }

    private static double CoerceZoom(AvaloniaObject sender, double value)
        => double.IsNaN(value) ? 1.0 : Math.Clamp(value, MinZoom, MaxZoom);

    private static double Distance(Point a, Point b)
    {
        var delta = a - b;
        return Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
    }

    private static void Observe(ref INotifyCollectionChanged? observed, INotifyCollectionChanged? collection, NotifyCollectionChangedEventHandler handler)
    {
        if (observed is not null)
            observed.CollectionChanged -= handler;
        observed = collection;
        if (observed is not null)
            observed.CollectionChanged += handler;
    }

    private static void DrawLink(DrawingContext context, IPen pen, IBrush? arrowBrush, Point start, Point end)
    {
        context.DrawGeometry(null, pen, CreateLinkGeometry(start, end));
        // The arrow: the link always enters its input from the left
        var size = Math.Max(ArrowSize, pen.Thickness * 2);
        var arrow = new StreamGeometry();
        using (var arrowContext = arrow.Open())
        {
            arrowContext.BeginFigure(end, true);
            arrowContext.LineTo(new Point(end.X - size, end.Y - size / 2));
            arrowContext.LineTo(new Point(end.X - size, end.Y + size / 2));
            arrowContext.EndFigure(true);
        }
        context.DrawGeometry(arrowBrush, null, arrow);
    }

    private static Geometry CreateLinkGeometry(Point start, Point end)
    {
        var (control1, control2) = GetControlPoints(start, end);
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(start, false);
            context.CubicBezierTo(control1, control2, end);
            context.EndFigure(false);
        }
        return geometry;
    }

    private static (Point Control1, Point Control2) GetControlPoints(Point start, Point end)
    {
        // Even a link going backward leaves its output to the right and enters its input from the left
        var curvature = Math.Max(Math.Abs(end.X - start.X), 120.0) * 0.4;
        return (new Point(start.X + curvature, start.Y), new Point(end.X - curvature, end.Y));
    }

    private static Point Bezier(Point p0, Point p1, Point p2, Point p3, double t)
    {
        var u = 1 - t;
        var a = u * u * u;
        var b = 3 * u * u * t;
        var c = 3 * u * t * t;
        var d = t * t * t;
        return new Point(a * p0.X + b * p1.X + c * p2.X + d * p3.X, a * p0.Y + b * p1.Y + c * p2.Y + d * p3.Y);
    }

    private static double DistanceToSegment(Point point, Point a, Point b)
    {
        var ab = b - a;
        var lengthSquared = ab.X * ab.X + ab.Y * ab.Y;
        var t = lengthSquared > 0 ? Math.Clamp(((point.X - a.X) * ab.X + (point.Y - a.Y) * ab.Y) / lengthSquared, 0, 1) : 0;
        return Distance(point, a + ab * t);
    }

    private void UpdateTransforms()
    {
        var matrix = Matrix.CreateScale(Zoom, Zoom) * Matrix.CreateTranslation(Offset.X, Offset.Y);
        nodesPanel.RenderTransform = new MatrixTransform(matrix);
        linksLayer.RenderTransform = new MatrixTransform(matrix);
    }

    private IEnumerable<NodeGraphNode> OrderedContainers()
    {
        var seen = new HashSet<NodeGraphNode>();
        if (Nodes is { } nodes)
        {
            foreach (var node in nodes)
            {
                if (node is not null && containers.TryGetValue(node, out var container) && seen.Add(container))
                    yield return container;
            }
        }
    }

    private IEnumerable<object> EnumerateLinks()
    {
        if (Links is { } links)
        {
            foreach (var link in links)
            {
                if (link is not null)
                    yield return link;
            }
        }
    }

    private IEnumerable<(object Source, object Target)> GetNodeLinks()
    {
        foreach (var link in EnumerateLinks())
        {
            var sourceNode = FindNodeOfSlot(LinkResolver?.GetSourceSlot(link), NodeGraphSlotDirection.Output);
            var targetNode = FindNodeOfSlot(LinkResolver?.GetTargetSlot(link), NodeGraphSlotDirection.Input);
            if (sourceNode is not null && targetNode is not null)
                yield return (sourceNode, targetNode);
        }
    }

    private object? FindNodeOfSlot(object? slot, NodeGraphSlotDirection direction)
        => slot is null ? null : ConnectorFromSlot(slot, direction)?.FindAncestorOfType<NodeGraphNode>()?.Node;

    private (Point Start, Point End)? GetLinkEnds(object link)
    {
        if (LinkResolver is not { } resolver || resolver.GetSourceSlot(link) is not { } source || resolver.GetTargetSlot(link) is not { } target)
            return null;

        var start = GetConnectorCenter(ConnectorFromSlot(source, NodeGraphSlotDirection.Output));
        var end = GetInputEnd(ConnectorFromSlot(target, NodeGraphSlotDirection.Input));
        return start is not null && end is not null ? (start.Value, end.Value) : null;
    }

    /// <summary>
    /// Where a link ends at an input connector: the tip of its arrow, at the height of the connector on the left side of its node
    /// (the nodes are drawn over the links).
    /// </summary>
    private Point? GetInputEnd(NodeGraphConnector? connector)
    {
        if (connector?.TranslatePoint(new Point(0, connector.Bounds.Height / 2), nodesPanel) is not { } point)
            return null;

        return connector.FindAncestorOfType<NodeGraphNode>() is { } container && ReferenceEquals(container.GetVisualParent(), nodesPanel)
            ? new Point(Math.Min(point.X, container.Position.X), point.Y)
            : point;
    }

    private Point? GetConnectorCenter(NodeGraphConnector? connector)
        => connector?.TranslatePoint(new Point(connector.Bounds.Width / 2, connector.Bounds.Height / 2), nodesPanel);

    private object? HitTestLink(Point graphPosition)
    {
        var tolerance = LinkThickness / 2 + LinkHitTolerance / Zoom;
        // The last link is drawn over the others
        foreach (var link in EnumerateLinks().Reverse())
        {
            if (GetLinkEnds(link) is not { } ends)
                continue;

            var (control1, control2) = GetControlPoints(ends.Start, ends.End);
            var previous = ends.Start;
            for (var i = 1; i <= BezierSegmentCount; ++i)
            {
                var next = Bezier(ends.Start, control1, control2, ends.End, (double)i / BezierSegmentCount);
                if (DistanceToSegment(graphPosition, previous, next) <= tolerance)
                    return link;
                previous = next;
            }
        }
        return null;
    }

    private object? FindLastLinkTo(object slot)
    {
        object? found = null;
        foreach (var link in EnumerateLinks())
        {
            if (Equals(LinkResolver?.GetTargetSlot(link), slot))
                found = link;
        }
        return found;
    }

    private void StartGesture(Gesture newGesture, MouseButton button, PointerPressedEventArgs e)
    {
        gesture = newGesture;
        gestureButton = button;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void StartLinkDrag(object sourceSlot, object? link, Point position, PointerPressedEventArgs e)
    {
        linkSourceSlot = sourceSlot;
        replacedLink = link;
        linkPreviewEnd = ViewToGraph(position);
        StartGesture(Gesture.LinkDrag, MouseButton.Left, e);
        UpdateDropTarget(linkPreviewEnd);
        linksLayer.InvalidateVisual();
    }

    private void PressNode(NodeGraphNode container, PointerPressedEventArgs e)
    {
        var selectedNodes = EffectiveSelectedNodes;
        var wasSelected = selectedNodes.Contains(container.Node);
        if (pressModifiers.HasFlag(KeyModifiers.Control))
        {
            // Toggles the node, without a drag
            ClickItem(container.Node, selectedNodes, pressModifiers);
            e.Handled = true;
            return;
        }

        if (!wasSelected)
            ClickItem(container.Node, selectedNodes, pressModifiers);

        // The selected nodes move together; a click on a node of a selection then selects only that node
        pressedNode = container;
        selectOnlyPressedNodeOnClick = wasSelected && pressModifiers == KeyModifiers.None;
        draggedNodes.Clear();
        foreach (var node in selectedNodes.Cast<object?>().ToList())
        {
            if (node is not null && containers.TryGetValue(node, out var selected))
                draggedNodes.Add((selected, selected.Position));
        }
        StartGesture(Gesture.NodeDrag, MouseButton.Left, e);
    }

    private void EndGesture(IPointer? pointer)
    {
        var endedGesture = gesture;
        gesture = Gesture.None;
        pressedNode = null;
        selectOnlyPressedNodeOnClick = false;
        draggedNodes.Clear();
        linkSourceSlot = null;
        replacedLink = null;
        SetDropTarget(null, null);
        // Releasing the capture raises the capture lost event, which has nothing left to stop
        if (pointer?.Captured == this)
            pointer.Capture(null);

        if (endedGesture == Gesture.LinkDrag)
            linksLayer.InvalidateVisual();
        else if (endedGesture == Gesture.Rectangle)
            overlayLayer.InvalidateVisual();
    }

    private void UpdateDropTarget(Point graphPosition)
    {
        NodeGraphConnector? hovered = null;
        for (var i = connectors.Count - 1; i >= 0; --i)
        {
            var connector = connectors[i];
            if (connector.Direction != NodeGraphSlotDirection.Input || connector.Slot is null)
                continue;

            if (connector.TranslatePoint(default, nodesPanel) is not { } topLeft)
                continue;

            var bounds = new Rect(topLeft, connector.Bounds.Size).Inflate(ConnectorHitTolerance / Zoom);
            if (bounds.Contains(graphPosition))
            {
                hovered = connector;
                break;
            }
        }

        NodeGraphConnector? target = null;
        if (hovered?.Slot is { } slot && linkSourceSlot is not null)
        {
            // Dropping a link back where it was changes nothing
            var isSameTarget = replacedLink is not null && Equals(LinkResolver?.GetTargetSlot(replacedLink), slot);
            if (!isSameTarget && LinkCommand?.CanExecute(new NodeGraphLinkRequest(linkSourceSlot, slot, replacedLink)) == true)
                target = hovered;
        }
        SetDropTarget(hovered, target);
    }

    private void SetDropTarget(NodeGraphConnector? hovered, NodeGraphConnector? target)
    {
        hoveredInputConnector = hovered;
        if (ReferenceEquals(dropTarget, target))
            return;

        if (dropTarget is not null)
            dropTarget.IsDropTarget = false;
        dropTarget = target;
        if (dropTarget is not null)
            dropTarget.IsDropTarget = true;
    }

    private void ClickItem(object item, IList selection, KeyModifiers modifiers)
    {
        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            if (selection.Contains(item))
                selection.Remove(item);
            else
                selection.Add(item);
        }
        else if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            if (!selection.Contains(item))
                selection.Add(item);
        }
        else if (!selection.Contains(item))
        {
            ClearSelection();
            selection.Add(item);
        }
    }

    private void SelectOnly(object node)
    {
        ClearSelection();
        EffectiveSelectedNodes.Add(node);
    }

    private void ClearSelection()
    {
        // The links first, then the nodes
        if (EffectiveSelectedLinks.Count > 0)
            EffectiveSelectedLinks.Clear();
        if (EffectiveSelectedNodes.Count > 0)
            EffectiveSelectedNodes.Clear();
    }

    private void SelectInRectangle(bool moved)
    {
        if (!moved)
        {
            // A click on the background clears the selection
            if (pressModifiers == KeyModifiers.None)
                ClearSelection();
            return;
        }

        var area = new Rect(ViewToGraph(pressPosition), ViewToGraph(currentPosition)).Normalize();
        var hits = OrderedContainers().Where(x => new Rect(x.Position, x.Bounds.Size).Intersects(area)).Select(x => x.Node).ToList();
        var selection = EffectiveSelectedNodes;
        if (pressModifiers.HasFlag(KeyModifiers.Control))
        {
            foreach (var node in hits)
            {
                if (selection.Contains(node))
                    selection.Remove(node);
                else
                    selection.Add(node);
            }
        }
        else
        {
            if (!pressModifiers.HasFlag(KeyModifiers.Shift))
                ClearSelection();
            foreach (var node in hits)
            {
                if (!selection.Contains(node))
                    selection.Add(node);
            }
        }
    }

    private void ResetContainers()
    {
        foreach (var container in containers.Values.ToList())
            RemoveContainer(container.Node);
        if (Nodes is { } nodes)
        {
            foreach (var node in nodes)
            {
                if (node is not null)
                    AddContainer(node);
            }
        }
    }

    private void AddContainer(object node)
    {
        if (containers.ContainsKey(node))
            return;

        var container = new NodeGraphNode(node) { ContentTemplate = NodeTemplate, IsSelected = EffectiveSelectedNodes.Contains(node) };
        containers.Add(node, container);
        nodesPanel.Children.Add(container);
        // Adding nodes lays out the whole graph again
        if (AutoLayout)
            isLayoutPending = true;
    }

    private void RemoveContainer(object node)
    {
        if (!containers.Remove(node, out var container))
            return;

        nodesPanel.Children.Remove(container);
        if (ReferenceEquals(pressedNode, container) || draggedNodes.Any(x => ReferenceEquals(x.Node, container)))
            EndGesture(null);
        // A removed node doesn't stay selected
        if (EffectiveSelectedNodes.Contains(node))
            EffectiveSelectedNodes.Remove(node);
    }

    private void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Remove:
            case NotifyCollectionChangedAction.Replace:
                foreach (var node in e.OldItems ?? Array.Empty<object>())
                {
                    if (node is not null)
                        RemoveContainer(node);
                }
                foreach (var node in e.NewItems ?? Array.Empty<object>())
                {
                    if (node is not null)
                        AddContainer(node);
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                ResetContainers();
                break;
        }
        nodesPanel.InvalidateArrange();
    }

    private void OnLinksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset || e.OldItems is not null)
        {
            var links = EnumerateLinks().ToHashSet(ReferenceEqualityComparer.Instance);
            // A removed link doesn't stay selected, and the user can't move it any more
            foreach (var link in EffectiveSelectedLinks.Cast<object?>().Where(x => x is not null && !links.Contains(x)).ToList())
                EffectiveSelectedLinks.Remove(link);
            if (replacedLink is not null && !links.Contains(replacedLink))
                EndGesture(null);
        }
        OnLinksChanged();
    }

    private void OnLinksChanged()
    {
        var connectedSlots = GetConnectedSlots();
        foreach (var connector in connectors)
            UpdateConnectorState(connector, connectedSlots);
        linksLayer.InvalidateVisual();
    }

    private HashSet<object> GetConnectedSlots()
    {
        var slots = new HashSet<object>();
        if (LinkResolver is { } resolver)
        {
            foreach (var link in EnumerateLinks())
            {
                if (resolver.GetSourceSlot(link) is { } source)
                    slots.Add(source);
                if (resolver.GetTargetSlot(link) is { } target)
                    slots.Add(target);
            }
        }
        return slots;
    }

    private static void UpdateConnectorState(NodeGraphConnector connector, HashSet<object> connectedSlots)
        => connector.IsConnected = connector.Slot is { } slot && connectedSlots.Contains(slot);

    private void OnSelectedNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateNodeSelectionStates();

    private void OnSelectedLinksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => linksLayer.InvalidateVisual();

    private void UpdateNodeSelectionStates()
    {
        var selection = EffectiveSelectedNodes;
        foreach (var container in containers.Values)
            container.IsSelected = selection.Contains(container.Node);
    }

    private enum Gesture
    {
        None,
        PendingPan,
        Pan,
        NodeDrag,
        LinkDrag,
        Rectangle,
    }
}
