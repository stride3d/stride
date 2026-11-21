// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Editor.Avalonia.Behaviors;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Tests;

/// <summary>
/// <see cref="TreeViewFollowSelectionBehavior"/>: the items selected from outside a tree get their ancestors expanded and the
/// last of them in view; a selection made in the tree changes nothing.
/// </summary>
public sealed class TreeViewFollowSelectionTests
{
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnItemSelectedFromOutsideTheTreeHasItsAncestorsExpandedAndItsRowInView(bool expandableViewModels)
    {
        using var tree = await SampleTree.ShowAsync(expandableViewModels);
        var grandchild = tree.Node("Root25/Child/Grandchild");
        Assert.Null(tree.Tree.TreeContainerFromItem(grandchild));

        tree.Selection.Add(grandchild);
        await tree.PumpAsync();

        Assert.True(tree.IsExpanded("Root25"));
        Assert.True(tree.IsExpanded("Root25/Child"));
        // Not the item itself, nor the other items
        Assert.False(tree.IsExpanded("Root25/Child/Grandchild"));
        Assert.False(tree.IsExpanded("Root24"));
        var row = Assert.IsType<TreeViewItem>(tree.Tree.TreeContainerFromItem(grandchild));
        Assert.True(row.IsSelected);
        Assert.True(tree.IsInView(row));
        // The tree doesn't take the focus
        Assert.False(tree.Tree.IsKeyboardFocusWithin);
    }

    [AvaloniaFact]
    public async Task TheViewModelsOfTheAncestorsAreExpandedWhenTheyHaveTheirState()
    {
        using var tree = await SampleTree.ShowAsync(expandableViewModels: true);

        tree.Selection.Add(tree.Node("Root25/Child/Grandchild"));
        await tree.PumpAsync();

        Assert.True(((ExpandableNode)tree.Node("Root25")).IsExpanded);
        Assert.True(((ExpandableNode)tree.Node("Root25/Child")).IsExpanded);
        Assert.False(((ExpandableNode)tree.Node("Root25/Child/Grandchild")).IsExpanded);
    }

    [AvaloniaFact]
    public async Task SeveralItemsSelectedAtOnceAreAllShownAndTheLastOneIsInView()
    {
        using var tree = await SampleTree.ShowAsync(expandableViewModels: true);
        var items = new[] { tree.Node("Root2/Child/Grandchild"), tree.Node("Root14/Child"), tree.Node("Root27/Child/Grandchild") };

        tree.Selection.AddRange(items);
        await tree.PumpAsync();

        Assert.True(tree.IsExpanded("Root2"));
        Assert.True(tree.IsExpanded("Root2/Child"));
        Assert.True(tree.IsExpanded("Root14"));
        Assert.False(tree.IsExpanded("Root14/Child"));
        Assert.True(tree.IsExpanded("Root27"));
        Assert.True(tree.IsExpanded("Root27/Child"));
        Assert.All(items, x => Assert.True(Assert.IsType<TreeViewItem>(tree.Tree.TreeContainerFromItem(x)).IsSelected));
        Assert.True(tree.IsInView((TreeViewItem)tree.Tree.TreeContainerFromItem(items[^1])!));
    }

    [AvaloniaFact]
    public async Task ManyItemsSelectedAtOnceAreAllShown()
    {
        using var tree = await SampleTree.ShowAsync(expandableViewModels: true);
        var items = tree.Roots.Select(x => x.Children[0].Children[0]).ToList();
        // A few hundred items: the grandchildren and their children
        items.AddRange(items.SelectMany(x => x.Children).ToList());
        Assert.True(items.Count >= 300);

        tree.Selection.AddRange(items);
        await tree.PumpAsync();

        Assert.All(tree.Roots, x => Assert.True(((ExpandableNode)x).IsExpanded));
        Assert.All(items, x => Assert.True(((ExpandableNode)x.Parent!).IsExpanded));
        Assert.True(tree.IsInView((TreeViewItem)tree.Tree.TreeContainerFromItem(items[^1])!));
    }

    [AvaloniaFact]
    public async Task ALevelThatTheTreeDoesNotShowIsPassedOver()
    {
        // The packages of the asset picker show the folders of their assets, not the folder that holds them
        using var tree = await SampleTree.ShowAsync(expandableViewModels: false, skipFirstLevel: true);
        var item = tree.Node("Root25/Child/Grandchild/Leaf");

        tree.Selection.Add(item);
        await tree.PumpAsync();

        Assert.True(tree.IsExpanded("Root25"));
        Assert.True(tree.IsExpanded("Root25/Child/Grandchild"));
        Assert.True(tree.IsInView(Assert.IsType<TreeViewItem>(tree.Tree.TreeContainerFromItem(item))));
    }

    [AvaloniaFact]
    public async Task ASelectionMadeByClickingInTheTreeExpandsAndScrollsNothing()
    {
        using var tree = await SampleTree.ShowAsync(expandableViewModels: true);
        var root = tree.Node("Root3");
        var offset = tree.ScrollViewer.Offset;

        tree.Window.MouseDown(tree.RowCenter(root), MouseButton.Left);
        tree.Window.MouseUp(tree.RowCenter(root), MouseButton.Left);
        await tree.PumpAsync();

        Assert.Equal([root], tree.Selection);
        Assert.All(tree.Roots, x => Assert.False(((ExpandableNode)x).IsExpanded));
        Assert.Equal(offset, tree.ScrollViewer.Offset);
    }

    [AvaloniaFact]
    public async Task ASelectionMadeWithTheKeyboardInTheTreeExpandsNothing()
    {
        using var tree = await SampleTree.ShowAsync(expandableViewModels: true);
        tree.Window.MouseDown(tree.RowCenter(tree.Node("Root0")), MouseButton.Left);
        tree.Window.MouseUp(tree.RowCenter(tree.Node("Root0")), MouseButton.Left);
        await tree.PumpAsync();

        // Down, then select all
        tree.Window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        tree.Window.KeyReleaseQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        tree.Window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.Control);
        tree.Window.KeyReleaseQwerty(PhysicalKey.A, RawInputModifiers.Control);
        await tree.PumpAsync();

        Assert.Contains(tree.Node("Root1"), tree.Selection);
        Assert.All(tree.Roots, x => Assert.False(((ExpandableNode)x).IsExpanded));
    }

    [AvaloniaFact]
    public async Task AnItemSelectedByAMenuOfTheTreeIsShown()
    {
        using var tree = await SampleTree.ShowAsync(expandableViewModels: true);
        var grandchild = tree.Node("Root25/Child/Grandchild");
        var item = new MenuItem { Header = "Select" };
        item.Click += (_, _) => tree.Selection.Add(grandchild);
        var menu = new ContextMenu { Items = { item } };
        tree.Tree.ContextMenu = menu;
        menu.Open(tree.Tree);
        await tree.PumpAsync();

        var popupRoot = TopLevel.GetTopLevel(item) ?? throw new InvalidOperationException("The menu is not open.");
        var center = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), popupRoot)!.Value;
        popupRoot.MouseDown(center, MouseButton.Left);
        popupRoot.MouseUp(center, MouseButton.Left);
        await tree.PumpAsync();

        Assert.Equal([grandchild], tree.Selection);
        Assert.True(tree.IsExpanded("Root25/Child"));
        Assert.True(tree.IsInView(Assert.IsType<TreeViewItem>(tree.Tree.TreeContainerFromItem(grandchild))));
    }

    [AvaloniaFact]
    public async Task ASelectionRemovedBeforeTheTreeUpdatesExpandsNothing()
    {
        using var tree = await SampleTree.ShowAsync(expandableViewModels: true);
        var grandchild = tree.Node("Root25/Child/Grandchild");

        tree.Selection.Add(grandchild);
        tree.Selection.Clear();
        await tree.PumpAsync();

        Assert.False(tree.IsExpanded("Root25"));
    }

    private abstract class Node(string name, Node? parent) : IChildViewModel, INotifyPropertyChanged
    {
        public string Name { get; } = name;

        public Node? Parent { get; } = parent;

        public ObservableCollection<Node> Children { get; } = [];

        public string Path => Parent is null ? Name : $"{Parent.Path}/{Name}";

        public event PropertyChangedEventHandler? PropertyChanged;

        public string GetName() => Name;

        public IChildViewModel? GetParent() => Parent;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class ExpandableNode(string name, Node? parent) : Node(name, parent), IExpandableViewModel
    {
        public bool IsExpanded
        {
            get;
            set
            {
                if (field == value)
                    return;
                field = value;
                OnPropertyChanged();
            }
        }
    }

    private sealed class PlainNode(string name, Node? parent) : Node(name, parent);

    /// <summary>
    /// A tree of 30 roots, each with a child, a grandchild and 10 leaves, all collapsed, in a window where a few roots show.
    /// </summary>
    private sealed class SampleTree : IDisposable
    {
        private SampleTree(Window window, TreeView tree, List<Node> roots, ObservableList<object> selection)
        {
            Window = window;
            Tree = tree;
            Roots = roots;
            Selection = selection;
        }

        public Window Window { get; }

        public TreeView Tree { get; }

        public List<Node> Roots { get; }

        public ObservableList<object> Selection { get; }

        public ScrollViewer ScrollViewer => Tree.GetVisualDescendants().OfType<ScrollViewer>().First();

        /// <param name="expandableViewModels">Whether the items keep their expanded state, which their rows bind.</param>
        /// <param name="skipFirstLevel">Whether a root shows the children of its child rather than its child.</param>
        public static async Task<SampleTree> ShowAsync(bool expandableViewModels, bool skipFirstLevel = false)
        {
            Node Create(string name, Node? parent) => expandableViewModels ? new ExpandableNode(name, parent) : new PlainNode(name, parent);

            var roots = new List<Node>();
            for (var i = 0; i < 30; ++i)
            {
                var root = Create($"Root{i}", null);
                var child = Create("Child", root);
                var grandchild = Create("Grandchild", child);
                for (var j = 0; j < 10; ++j)
                    grandchild.Children.Add(Create(j == 0 ? "Leaf" : $"Leaf{j}", grandchild));
                child.Children.Add(grandchild);
                root.Children.Add(child);
                roots.Add(root);
            }

            var selection = new ObservableList<object>();
            var tree = new TreeView
            {
                SelectionMode = SelectionMode.Multiple,
                ItemsSource = roots,
                SelectedItems = selection,
                ItemTemplate = new FuncTreeDataTemplate<Node>((node, _) => new TextBlock { Text = node.Name },
                    node => skipFirstLevel && node.Parent is null ? node.Children[0].Children : node.Children),
            };
            if (expandableViewModels)
            {
                tree.Styles.Add(new Style(x => x.OfType<TreeViewItem>())
                {
                    Setters = { new Setter(TreeViewItem.IsExpandedProperty, new Binding(nameof(ExpandableNode.IsExpanded)) { Mode = BindingMode.TwoWay }) },
                });
            }
            Interaction.GetBehaviors(tree).Add(new TreeViewFollowSelectionBehavior());
            var window = new Window { Width = 400, Height = 200, Content = tree };
            window.Show();
            var sample = new SampleTree(window, tree, roots, selection);
            await sample.PumpAsync();
            return sample;
        }

        public Node Node(string path)
        {
            var names = path.Split('/');
            var node = Roots.Single(x => x.Name == names[0]);
            foreach (var name in names.Skip(1))
                node = node.Children.Single(x => x.Name == name);
            return node;
        }

        /// <summary>
        /// Gets whether the row of an item is expanded; an item without a row is not.
        /// </summary>
        public bool IsExpanded(string path) => Tree.TreeContainerFromItem(Node(path)) is TreeViewItem { IsExpanded: true };

        /// <summary>
        /// Gets whether the row of an item, without its children, is inside the visible part of the tree.
        /// </summary>
        public bool IsInView(TreeViewItem row)
        {
            var presenter = Tree.GetVisualDescendants().OfType<ScrollContentPresenter>().First();
            var header = row.GetVisualDescendants().OfType<Control>().First(x => x.Name == "PART_Header" && ReferenceEquals(x.TemplatedParent, row));
            var top = header.TranslatePoint(default, presenter)!.Value.Y;
            return top >= 0 && top + header.Bounds.Height <= presenter.Bounds.Height;
        }

        public Point RowCenter(Node node)
        {
            var row = (TreeViewItem)Tree.TreeContainerFromItem(node)!;
            var text = row.GetVisualDescendants().OfType<TextBlock>().First(x => x.Text == node.Name);
            return text.TranslatePoint(new Point(text.Bounds.Width / 2, text.Bounds.Height / 2), Window)!.Value;
        }

        public async Task PumpAsync()
        {
            for (var i = 0; i < 3; ++i)
            {
                await DispatcherPump.PumpAsync();
                Dispatcher.UIThread.RunJobs();
            }
        }

        public void Dispose() => Window.Close();
    }
}
