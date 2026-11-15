// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// The container of a node of a <see cref="NodeGraphControl"/>: its content and its data context are the node, shown with the
/// graph's <see cref="NodeGraphControl.NodeTemplate"/> (or a data template found for it), and the graph places it at
/// <see cref="Position"/>.
/// </summary>
/// <remarks>
/// <para>Pseudo-class: <c>:selected</c> while the node is in the graph's <see cref="NodeGraphControl.SelectedNodes"/>.</para>
/// <para>A style of the containers, in the graph's styles, can bind their properties to the node, for example
/// <c>&lt;Style Selector="sd|NodeGraphNode"&gt;&lt;Setter Property="Position" Value="{Binding Position}"/&gt;&lt;/Style&gt;</c>
/// to show the positions a view model saved.</para>
/// </remarks>
[Experimental("STREXP010")]
public sealed class NodeGraphNode : ContentControl
{
    /// <summary>
    /// Identifies the <see cref="IsSelected"/> direct property.
    /// </summary>
    public static readonly DirectProperty<NodeGraphNode, bool> IsSelectedProperty =
        AvaloniaProperty.RegisterDirect<NodeGraphNode, bool>(nameof(IsSelected), o => o.IsSelected);

    /// <summary>
    /// Identifies the <see cref="Position"/> styled property.
    /// </summary>
    public static readonly StyledProperty<Point?> PositionProperty =
        AvaloniaProperty.Register<NodeGraphNode, Point?>(nameof(Position));

    private bool isSelected;

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeGraphNode"/> class.
    /// </summary>
    /// <param name="node">The node this container shows.</param>
    internal NodeGraphNode(object node)
    {
        Node = node;
        Content = node;
        DataContext = node;
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
    /// The position of the top left corner of the node, in the coordinates of the graph, or <c>null</c> to let the graph place it.
    /// </summary>
    /// <remarks>
    /// The graph sets it when it places the node and when the user drops the nodes they dragged, after which it raises
    /// <see cref="NodeGraphControl.NodesMoved"/>; it doesn't change while the nodes are dragged. A two-way binding therefore
    /// also receives the positions of the layout.
    /// </remarks>
    public Point? Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    /// <summary>
    /// Where the node is drawn: <see cref="Position"/>, or where the user drags it.
    /// </summary>
    internal Point ShownPosition { get; private set; }

    /// <summary>
    /// Whether the node has a place, given by <see cref="Position"/>, the layout or the user.
    /// </summary>
    internal bool HasPosition { get; private set; }

    /// <summary>
    /// Draws the node somewhere else without changing <see cref="Position"/>, while the user drags it.
    /// </summary>
    internal void ShowAt(Point position)
    {
        ShownPosition = position;
        HasPosition = true;
        (Parent as Layoutable)?.InvalidateArrange();
    }

    /// <summary>
    /// Places the node: draws it there and sets <see cref="Position"/>.
    /// </summary>
    internal void Place(Point position)
    {
        ShowAt(position);
        SetCurrentValue(PositionProperty, position);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PositionProperty)
        {
            var position = change.GetNewValue<Point?>();
            if (position is { } value)
            {
                if (!HasPosition || value != ShownPosition)
                    ShowAt(value);
            }
            else if (HasPosition)
            {
                // The graph places it again at its next arrange
                HasPosition = false;
                (Parent as Layoutable)?.InvalidateArrange();
            }
        }
    }
}
