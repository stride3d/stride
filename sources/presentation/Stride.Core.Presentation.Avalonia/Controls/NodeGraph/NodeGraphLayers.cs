// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// The panel of the node containers of a <see cref="NodeGraphControl"/>: it places each container at its position, in the
/// coordinates of the graph; the graph's pan and zoom are its render transform.
/// </summary>
internal sealed class NodeGraphNodesPanel : Panel
{
    private readonly NodeGraphControl graph;

    public NodeGraphNodesPanel(NodeGraphControl graph)
    {
        this.graph = graph;
        RenderTransformOrigin = RelativePoint.TopLeft;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // A node takes the size it wants, wherever it is
        foreach (var child in Children)
            child.Measure(Size.Infinity);
        return default;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // The nodes are measured: the layout can use their size
        graph.EnsureNodesLayout();

        foreach (var child in Children)
        {
            var position = child is NodeGraphNode node ? node.Position : default;
            child.Arrange(new Rect(position, child.DesiredSize));
        }
        return finalSize;
    }
}

/// <summary>
/// The layer of the links of a <see cref="NodeGraphControl"/>, under the nodes, drawn in the coordinates of the graph with the
/// same render transform as <see cref="NodeGraphNodesPanel"/>. It takes no input: the graph tests the links itself.
/// </summary>
internal sealed class NodeGraphLinksLayer : Control
{
    private readonly NodeGraphControl graph;

    public NodeGraphLinksLayer(NodeGraphControl graph)
    {
        this.graph = graph;
        IsHitTestVisible = false;
        RenderTransformOrigin = RelativePoint.TopLeft;
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        graph.RenderLinks(context);
    }
}

/// <summary>
/// The layer over the nodes of a <see cref="NodeGraphControl"/> that draws the selection rectangle, in the coordinates of the
/// graph control.
/// </summary>
internal sealed class NodeGraphOverlayLayer : Control
{
    private readonly NodeGraphControl graph;

    public NodeGraphOverlayLayer(NodeGraphControl graph)
    {
        this.graph = graph;
        IsHitTestVisible = false;
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        graph.RenderOverlay(context);
    }
}
