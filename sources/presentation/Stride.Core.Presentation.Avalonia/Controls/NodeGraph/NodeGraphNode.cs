// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// The container of a node of a <see cref="NodeGraphControl"/>: its content is the node, shown with the graph's
/// <see cref="NodeGraphControl.NodeTemplate"/> (or a data template found for it), and the graph places it.
/// </summary>
/// <remarks>
/// Pseudo-class: <c>:selected</c> while the node is in the graph's <see cref="NodeGraphControl.SelectedNodes"/>.
/// </remarks>
public sealed class NodeGraphNode : ContentControl
{
    /// <summary>
    /// Identifies the <see cref="IsSelected"/> direct property.
    /// </summary>
    public static readonly DirectProperty<NodeGraphNode, bool> IsSelectedProperty =
        AvaloniaProperty.RegisterDirect<NodeGraphNode, bool>(nameof(IsSelected), o => o.IsSelected);

    private bool isSelected;

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeGraphNode"/> class.
    /// </summary>
    /// <param name="node">The node this container shows.</param>
    internal NodeGraphNode(object node)
    {
        Node = node;
        Content = node;
    }

    /// <summary>
    /// The node this container shows, an item of <see cref="NodeGraphControl.Nodes"/>.
    /// </summary>
    public object Node { get; }

    /// <summary>
    /// Whether the node is selected. The graph sets it.
    /// </summary>
    public bool IsSelected
    {
        get => isSelected;
        internal set
        {
            if (SetAndRaise(IsSelectedProperty, ref isSelected, value))
                PseudoClasses.Set(":selected", value);
        }
    }

    /// <summary>
    /// The position of the top left corner of the node, in the coordinates of the graph.
    /// </summary>
    internal Point Position { get; set; }

    /// <summary>
    /// Whether <see cref="Position"/> was given, by the layout or the user.
    /// </summary>
    internal bool HasPosition { get; set; }
}
