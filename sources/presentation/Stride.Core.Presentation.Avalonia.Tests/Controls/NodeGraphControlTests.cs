// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Presentation.Avalonia.Controls;

namespace Stride.Core.Presentation.Avalonia.Tests.Controls;

public sealed class NodeGraphControlTests
{
    [AvaloniaFact]
    public void LayoutPlacesRootsLeftAndTheNodesTheyReachRight()
    {
        var graph = new TestGraph();
        var a = graph.AddNode("A", 0, 2);
        var b = graph.AddNode("B", 1, 1);
        var c = graph.AddNode("C", 1, 0);
        var d = graph.AddNode("D", 1, 0);
        var lonely = graph.AddNode("Lonely", 0, 0);
        graph.Link(a.Outputs[0], b.Inputs[0]);
        graph.Link(a.Outputs[1], c.Inputs[0]);
        graph.Link(b.Outputs[0], d.Inputs[0]);
        var (window, control) = Show(graph);

        var boundsA = NodeBounds(control, a);
        var boundsB = NodeBounds(control, b);
        var boundsC = NodeBounds(control, c);
        var boundsD = NodeBounds(control, d);
        var boundsLonely = NodeBounds(control, lonely);

        // Columns: the roots, then the nodes they reach, a layer gap apart
        Assert.Equal(NodeGraphControl.LayoutMargin, boundsA.X);
        Assert.Equal(boundsA.X, boundsLonely.X);
        Assert.Equal(Math.Max(boundsA.Right, boundsLonely.Right) + NodeGraphControl.LayoutLayerGap, boundsB.X, 3);
        Assert.Equal(boundsB.X, boundsC.X);
        Assert.True(boundsD.X >= boundsB.Right + NodeGraphControl.LayoutLayerGap);
        // Rows: B above C, a vertex gap apart, A centered on them, the second root under the first tree
        Assert.True(boundsC.Y >= boundsB.Bottom + NodeGraphControl.LayoutVertexGap - 0.01);
        Assert.Equal((boundsB.Y + boundsC.Bottom) / 2, boundsA.Center.Y, 3);
        Assert.Equal(boundsB.Center.Y, boundsD.Center.Y, 3);
        Assert.True(boundsLonely.Y >= Math.Max(boundsA.Bottom, boundsC.Bottom) + NodeGraphControl.LayoutVertexGap - 0.01);
        window.Close();
    }

    [AvaloniaFact]
    public void LayoutRunsAgainWhenANodeIsAdded()
    {
        var graph = new TestGraph();
        var a = graph.AddNode("A", 0, 1);
        var (window, control) = Show(graph);
        control.SetNodePosition(a, new Point(300, 300));

        var b = graph.AddNode("B", 1, 0);
        graph.Link(a.Outputs[0], b.Inputs[0]);
        Dispatcher.UIThread.RunJobs();

        // Adding a node lays out the whole graph again
        Assert.Equal(new Point(NodeGraphControl.LayoutMargin, NodeGraphControl.LayoutMargin), control.GetNodePosition(a));
        Assert.True(NodeBounds(control, b).X > NodeBounds(control, a).Right);
        window.Close();
    }

    [AvaloniaFact]
    public void DraggingFromAnOutputToAnInputLinksTheSlots()
    {
        var graph = new TestGraph();
        var a = graph.AddNode("A", 0, 1);
        var b = graph.AddNode("B", 1, 0);
        var (window, control) = Show(graph);
        var output = control.ConnectorFromSlot(a.Outputs[0], NodeGraphSlotDirection.Output)!;
        var input = control.ConnectorFromSlot(b.Inputs[0], NodeGraphSlotDirection.Input)!;
        Assert.False(output.IsConnected);

        Drag(window, ConnectorCenter(window, output), ConnectorCenter(window, input));

        var link = Assert.Single(graph.Links);
        Assert.Same(a.Outputs[0], link.Source);
        Assert.Same(b.Inputs[0], link.Target);
        Assert.Null(graph.LastRequest!.ReplacedLink);
        Assert.True(output.IsConnected);
        Assert.True(input.IsConnected);
        window.Close();
    }

    [AvaloniaFact]
    public void ALinkTheCommandRefusesOrReleasedElsewhereIsNotCreated()
    {
        var graph = new TestGraph();
        var a = graph.AddNode("A", 1, 1);
        var b = graph.AddNode("B", 1, 0);
        var (window, control) = Show(graph);
        var output = control.ConnectorFromSlot(a.Outputs[0], NodeGraphSlotDirection.Output)!;

        // The test command refuses a link to the same node
        Drag(window, ConnectorCenter(window, output), ConnectorCenter(window, control.ConnectorFromSlot(a.Inputs[0], NodeGraphSlotDirection.Input)!));
        Assert.Empty(graph.Links);

        // Released on the background
        Drag(window, ConnectorCenter(window, output), EmptyPoint(window, control));
        Assert.Empty(graph.Links);
        Assert.Null(graph.LastRequest);
        window.Close();
    }

    [AvaloniaFact]
    public void ALinkDraggedOffItsInputIsUnlinkedOrMoved()
    {
        var graph = new TestGraph();
        var a = graph.AddNode("A", 0, 1);
        var b = graph.AddNode("B", 1, 0);
        var c = graph.AddNode("C", 1, 0);
        var link = graph.Link(a.Outputs[0], b.Inputs[0]);
        var (window, control) = Show(graph);
        var inputB = control.ConnectorFromSlot(b.Inputs[0], NodeGraphSlotDirection.Input)!;
        var inputC = control.ConnectorFromSlot(c.Inputs[0], NodeGraphSlotDirection.Input)!;

        // Moved to another input: one request that replaces the link
        Drag(window, ConnectorCenter(window, inputB), ConnectorCenter(window, inputC));
        var moved = Assert.Single(graph.Links);
        Assert.Same(c.Inputs[0], moved.Target);
        Assert.Same(link, graph.LastRequest!.ReplacedLink);
        Assert.False(inputB.IsConnected);
        Assert.True(inputC.IsConnected);

        // Released away from any input: removed
        Drag(window, ConnectorCenter(window, inputC), EmptyPoint(window, control));
        Assert.Empty(graph.Links);
        Assert.Same(moved, graph.LastUnlinked);
        Assert.False(inputC.IsConnected);
        window.Close();
    }

    [AvaloniaFact]
    public void ClicksSelectNodesAndLinks()
    {
        var graph = new TestGraph();
        var a = graph.AddNode("A", 0, 1);
        var b = graph.AddNode("B", 1, 0);
        var link = graph.Link(a.Outputs[0], b.Inputs[0]);
        var selectedNodes = new ObservableCollection<object>();
        var selectedLinks = new ObservableCollection<object>();
        var (window, control) = Show(graph, x =>
        {
            x.SelectedNodes = selectedNodes;
            x.SelectedLinks = selectedLinks;
        });

        Click(window, NodeTitle(window, control, a));
        Assert.Equal([a], selectedNodes);
        Assert.True(control.ContainerFromNode(a)!.IsSelected);

        Click(window, NodeTitle(window, control, b), RawInputModifiers.Control);
        Assert.Equal([a, b], selectedNodes);

        // A click on a node of the selection selects only that node
        Click(window, NodeTitle(window, control, b));
        Assert.Equal([b], selectedNodes);
        Assert.False(control.ContainerFromNode(a)!.IsSelected);

        // The middle of a link is the middle of its ends
        var start = ConnectorCenter(window, control.ConnectorFromSlot(a.Outputs[0], NodeGraphSlotDirection.Output)!);
        var end = ConnectorCenter(window, control.ConnectorFromSlot(b.Inputs[0], NodeGraphSlotDirection.Input)!);
        Click(window, new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2));
        Assert.Equal([link], selectedLinks);
        Assert.Empty(selectedNodes);

        Click(window, EmptyPoint(window, control));
        Assert.Empty(selectedLinks);
        Assert.Empty(selectedNodes);
        window.Close();
    }

    [AvaloniaFact]
    public void ARectangleSelectsTheNodesItTouches()
    {
        var graph = new TestGraph();
        var a = graph.AddNode("A", 0, 1);
        var b = graph.AddNode("B", 0, 0);
        var c = graph.AddNode("C", 0, 0);
        var selectedNodes = new ObservableCollection<object>();
        var (window, control) = Show(graph, x => x.SelectedNodes = selectedNodes);
        // One column: A, B, C from top to bottom
        var boundsB = control.TranslatePoint(control.GraphToView(control.GetNodePosition(b)!.Value), window)!.Value;

        Drag(window, new Point(2, 2), new Point(boundsB.X + 5, boundsB.Y + 5));
        Assert.Equal([a, b], selectedNodes);
        Assert.False(control.ContainerFromNode(c)!.IsSelected);

        // Ctrl toggles: B leaves, C joins
        var boundsC = control.TranslatePoint(control.GraphToView(control.GetNodePosition(c)!.Value), window)!.Value;
        Drag(window, new Point(boundsB.X - 10, boundsB.Y + 5), new Point(boundsC.X + 5, boundsC.Y + 5), RawInputModifiers.Control);
        Assert.Equal([a, c], selectedNodes);
        window.Close();
    }

    [AvaloniaFact]
    public void DraggingANodeMovesTheSelectedNodes()
    {
        var graph = new TestGraph();
        var a = graph.AddNode("A", 0, 0);
        var b = graph.AddNode("B", 0, 0);
        var selectedNodes = new ObservableCollection<object> { a, b };
        var (window, control) = Show(graph, x =>
        {
            x.SelectedNodes = selectedNodes;
            x.Zoom = 2;
        });
        var positionA = control.GetNodePosition(a)!.Value;
        var positionB = control.GetNodePosition(b)!.Value;

        var title = NodeTitle(window, control, a);
        Drag(window, title, title + new Point(40, 20));

        // The move is in the coordinates of the control: half of it in the graph at a zoom of 2
        Assert.Equal(positionA + new Point(20, 10), control.GetNodePosition(a));
        Assert.Equal(positionB + new Point(20, 10), control.GetNodePosition(b));
        Assert.Equal([a, b], selectedNodes);
        window.Close();
    }

    [AvaloniaFact]
    public void TheWheelZoomsAroundThePointerAndTheMiddleButtonPans()
    {
        var graph = new TestGraph();
        graph.AddNode("A", 0, 0);
        var (window, control) = Show(graph);
        var pointer = new Point(200, 150);
        var graphPoint = control.ViewToGraph(pointer);

        window.MouseWheel(pointer, new Vector(0, 2));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1.21, control.Zoom, 6);
        var after = control.ViewToGraph(pointer);
        Assert.Equal(graphPoint.X, after.X, 6);
        Assert.Equal(graphPoint.Y, after.Y, 6);

        var offset = control.Offset;
        window.MouseDown(pointer, MouseButton.Middle);
        window.MouseMove(pointer + new Point(30, -20));
        window.MouseUp(pointer + new Point(30, -20), MouseButton.Middle);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(offset + new Vector(30, -20), control.Offset);

        // The zoom stays in its range
        control.Zoom = 100;
        Assert.Equal(NodeGraphControl.MaxZoom, control.Zoom);
        window.Close();
    }

    [AvaloniaFact]
    public void HomeShowsEveryNode()
    {
        var graph = new TestGraph();
        var a = graph.AddNode("A", 0, 1);
        var b = graph.AddNode("B", 1, 0);
        graph.Link(a.Outputs[0], b.Inputs[0]);
        var (window, control) = Show(graph, x =>
        {
            x.Zoom = 3;
            x.Offset = new Vector(-2000, 500);
        });

        control.Focus();
        window.KeyPressQwerty(PhysicalKey.Home, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1.0, control.Zoom);
        var view = new Rect(control.Bounds.Size);
        foreach (var node in new[] { a, b })
        {
            var bounds = NodeBounds(control, node);
            Assert.True(view.Contains(new Rect(control.GraphToView(bounds.TopLeft), control.GraphToView(bounds.BottomRight))));
        }
        window.Close();
    }

    [AvaloniaFact]
    public void TheGraphFollowsItsCollections()
    {
        var graph = new TestGraph();
        var a = graph.AddNode("A", 0, 1);
        var b = graph.AddNode("B", 1, 0);
        var selectedNodes = new ObservableCollection<object>();
        var (window, control) = Show(graph, x => x.SelectedNodes = selectedNodes);

        // A selection made elsewhere shows
        selectedNodes.Add(b);
        Assert.True(control.ContainerFromNode(b)!.IsSelected);

        // A link added elsewhere connects its slots
        var link = graph.Link(a.Outputs[0], b.Inputs[0]);
        Assert.True(control.ConnectorFromSlot(a.Outputs[0], NodeGraphSlotDirection.Output)!.IsConnected);

        // A removed node takes its container, its connectors and its selection
        graph.Links.Remove(link);
        graph.Nodes.Remove(b);
        Dispatcher.UIThread.RunJobs();
        Assert.Null(control.ContainerFromNode(b));
        Assert.Null(control.ConnectorFromSlot(b.Inputs[0], NodeGraphSlotDirection.Input));
        Assert.Empty(selectedNodes);
        Assert.False(control.ConnectorFromSlot(a.Outputs[0], NodeGraphSlotDirection.Output)!.IsConnected);
        window.Close();
    }

    private static (Window Window, NodeGraphControl Control) Show(TestGraph graph, Action<NodeGraphControl>? setup = null)
    {
        var control = new NodeGraphControl
        {
            Nodes = graph.Nodes,
            Links = graph.Links,
            LinkResolver = graph,
            LinkCommand = graph.LinkCommand,
            UnlinkCommand = graph.UnlinkCommand,
            NodeTemplate = NodeTemplate,
        };
        setup?.Invoke(control);
        var window = new Window { Width = 800, Height = 600, Content = control };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, control);
    }

    private static IDataTemplate NodeTemplate { get; } = new FuncDataTemplate<TestNode>((node, _) =>
    {
        var inputs = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        foreach (var slot in node.Inputs)
            inputs.Children.Add(new NodeGraphConnector { Slot = slot, Direction = NodeGraphSlotDirection.Input });
        var outputs = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        foreach (var slot in node.Outputs)
            outputs.Children.Add(new NodeGraphConnector { Slot = slot, Direction = NodeGraphSlotDirection.Output });
        var title = new TextBlock { Name = "Title", Text = node.Name, Margin = new Thickness(20, 8) };
        var panel = new DockPanel();
        DockPanel.SetDock(inputs, Dock.Left);
        DockPanel.SetDock(outputs, Dock.Right);
        panel.Children.Add(inputs);
        panel.Children.Add(outputs);
        panel.Children.Add(title);
        return panel;
    });

    private static Rect NodeBounds(NodeGraphControl control, TestNode node)
    {
        var container = control.ContainerFromNode(node)!;
        return new Rect(control.GetNodePosition(node)!.Value, container.Bounds.Size);
    }

    private static Point ConnectorCenter(Window window, NodeGraphConnector connector)
        => connector.TranslatePoint(new Point(connector.Bounds.Width / 2, connector.Bounds.Height / 2), window)!.Value;

    private static Point NodeTitle(Window window, NodeGraphControl control, TestNode node)
    {
        var title = control.ContainerFromNode(node)!.GetVisualDescendants().OfType<TextBlock>().First(x => x.Name == "Title");
        return title.TranslatePoint(new Point(title.Bounds.Width / 2, title.Bounds.Height / 2), window)!.Value;
    }

    private static Point EmptyPoint(Window window, NodeGraphControl control)
        => control.TranslatePoint(new Point(control.Bounds.Width - 10, control.Bounds.Height - 10), window)!.Value;

    private static void Click(Window window, Point point, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.MouseDown(point, MouseButton.Left, modifiers);
        window.MouseUp(point, MouseButton.Left, modifiers);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Drag(Window window, Point from, Point to, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.MouseDown(from, MouseButton.Left, modifiers);
        window.MouseMove(from + (to - from) / 2, modifiers);
        window.MouseMove(to, modifiers);
        window.MouseUp(to, MouseButton.Left, modifiers);
        Dispatcher.UIThread.RunJobs();
    }

    private sealed class TestNode(string name)
    {
        public string Name { get; } = name;

        public List<TestSlot> Inputs { get; } = [];

        public List<TestSlot> Outputs { get; } = [];

        public override string ToString() => Name;
    }

    private sealed class TestSlot(TestNode node, bool isInput)
    {
        public TestNode Node { get; } = node;

        public bool IsInput { get; } = isInput;
    }

    private sealed class TestLink(TestSlot source, TestSlot target)
    {
        public TestSlot Source { get; } = source;

        public TestSlot Target { get; } = target;
    }

    /// <summary>
    /// A graph of nodes with slots, whose commands link an output to an input of another node.
    /// </summary>
    private sealed class TestGraph : INodeGraphLinkResolver
    {
        public TestGraph()
        {
            LinkCommand = new TestCommand(
                x => x is NodeGraphLinkRequest { SourceSlot: TestSlot { IsInput: false } source, TargetSlot: TestSlot { IsInput: true } target } && source.Node != target.Node,
                x =>
                {
                    var request = (NodeGraphLinkRequest)x!;
                    LastRequest = request;
                    if (request.ReplacedLink is TestLink replaced)
                        Links.Remove(replaced);
                    Link((TestSlot)request.SourceSlot, (TestSlot)request.TargetSlot);
                });
            UnlinkCommand = new TestCommand(x => x is TestLink, x =>
            {
                LastUnlinked = (TestLink)x!;
                Links.Remove(LastUnlinked);
            });
        }

        public ObservableCollection<TestNode> Nodes { get; } = [];

        public ObservableCollection<TestLink> Links { get; } = [];

        public ICommand LinkCommand { get; }

        public ICommand UnlinkCommand { get; }

        public NodeGraphLinkRequest? LastRequest { get; private set; }

        public TestLink? LastUnlinked { get; private set; }

        public TestNode AddNode(string name, int inputCount, int outputCount)
        {
            var node = new TestNode(name);
            for (var i = 0; i < inputCount; ++i)
                node.Inputs.Add(new TestSlot(node, true));
            for (var i = 0; i < outputCount; ++i)
                node.Outputs.Add(new TestSlot(node, false));
            Nodes.Add(node);
            return node;
        }

        public TestLink Link(TestSlot source, TestSlot target)
        {
            var link = new TestLink(source, target);
            Links.Add(link);
            return link;
        }

        public object? GetSourceSlot(object link) => ((TestLink)link).Source;

        public object? GetTargetSlot(object link) => ((TestLink)link).Target;
    }

    private sealed class TestCommand(Func<object?, bool> canExecute, Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => canExecute(parameter);

        public void Execute(object? parameter) => execute(parameter);
    }
}
