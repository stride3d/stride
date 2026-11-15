// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Interactivity;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// A node the user moved in a <see cref="NodeGraphControl"/>, with its positions in the coordinates of the graph.
/// </summary>
/// <param name="Node">The node, an item of <see cref="NodeGraphControl.Nodes"/>.</param>
/// <param name="OldPosition">Where its top left corner was before the drag.</param>
/// <param name="NewPosition">Where its top left corner is now.</param>
[Experimental("STREXP010")]
public readonly record struct NodeGraphNodeMove(object Node, Point OldPosition, Point NewPosition);

/// <summary>
/// The arguments of <see cref="NodeGraphControl.NodesMoved"/>: the nodes the user dragged, once they were dropped.
/// </summary>
[Experimental("STREXP010")]
public sealed class NodeGraphNodesMovedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeGraphNodesMovedEventArgs"/> class.
    /// </summary>
    /// <param name="moves">The nodes that moved.</param>
    public NodeGraphNodesMovedEventArgs(IReadOnlyList<NodeGraphNodeMove> moves)
        : base(NodeGraphControl.NodesMovedEvent)
    {
        ArgumentNullException.ThrowIfNull(moves);
        Moves = moves;
    }

    /// <summary>
    /// The nodes that moved, together, in the order of <see cref="NodeGraphControl.SelectedNodes"/>.
    /// </summary>
    public IReadOnlyList<NodeGraphNodeMove> Moves { get; }
}

/// <summary>
/// The arguments of an event about one node of a <see cref="NodeGraphControl"/>, such as <see cref="NodeGraphControl.NodeDoubleClicked"/>.
/// </summary>
[Experimental("STREXP010")]
public sealed class NodeGraphNodeEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeGraphNodeEventArgs"/> class.
    /// </summary>
    /// <param name="routedEvent">The event.</param>
    /// <param name="node">The node, an item of <see cref="NodeGraphControl.Nodes"/>.</param>
    public NodeGraphNodeEventArgs(RoutedEvent routedEvent, object node)
        : base(routedEvent)
    {
        ArgumentNullException.ThrowIfNull(node);
        Node = node;
    }

    /// <summary>
    /// The node, an item of <see cref="NodeGraphControl.Nodes"/>.
    /// </summary>
    public object Node { get; }
}
