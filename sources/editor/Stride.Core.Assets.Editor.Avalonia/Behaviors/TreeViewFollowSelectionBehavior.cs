// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Behaviors;

/// <summary>
/// Shows the items that are selected from outside a tree (a click in a scene view, the selection history, a new item...):
/// expands their ancestors, not the items themselves, and brings the last of them into view. A selection made in the tree
/// with the pointer or the keyboard changes nothing, as its rows are already shown. The tree doesn't take the focus.
/// </summary>
/// <remarks>
/// The parents of the items come from <see cref="IChildViewModel.GetParent"/>. An ancestor that is an
/// <see cref="IExpandableViewModel"/> is expanded through its view model, which its row follows, even before the row exists;
/// the others through their rows. An ancestor without a row (a level that the tree doesn't show) is passed over. The items
/// of a new collection of selected items (the view model's, once bound) count as selected from outside the tree. While attached, the behavior does the
/// scrolling to the selection: the tree's own <see cref="TreeView.AutoScrollToSelectedItem"/>, which brings the first
/// selected item into view once its row exists, is off.
/// </remarks>
public sealed class TreeViewFollowSelectionBehavior : StyledElementBehavior<TreeView>
{
    // The items selected from outside the tree since the last update, in the order of their selection
    private readonly List<object> pendingItems = [];
    // The collection of selected items whose items were followed: the tree doesn't say when it gets a new one
    private IList? followedItems;
    private bool isTreeInput;
    private bool isUpdatePosted;
    private bool treeAutoScroll;

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is not { } tree)
            return;

        treeAutoScroll = tree.AutoScrollToSelectedItem;
        tree.AutoScrollToSelectedItem = false;
        tree.SelectionChanged += OnTreeSelectionChanged;
        tree.Loaded += OnTreeLoaded;
        // The selection changes while the tree handles a click or a key are the tree's own
        tree.AddHandler(InputElement.PointerPressedEvent, OnInputStarted, RoutingStrategies.Tunnel, handledEventsToo: true);
        tree.AddHandler(InputElement.PointerPressedEvent, OnInputEnded, RoutingStrategies.Bubble, handledEventsToo: true);
        tree.AddHandler(InputElement.PointerReleasedEvent, OnInputStarted, RoutingStrategies.Tunnel, handledEventsToo: true);
        tree.AddHandler(InputElement.PointerReleasedEvent, OnInputEnded, RoutingStrategies.Bubble, handledEventsToo: true);
        tree.AddHandler(InputElement.KeyDownEvent, OnInputStarted, RoutingStrategies.Tunnel, handledEventsToo: true);
        tree.AddHandler(InputElement.KeyDownEvent, OnInputEnded, RoutingStrategies.Bubble, handledEventsToo: true);
        FollowNewCollection(tree);
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        if (AssociatedObject is { } tree)
        {
            tree.SelectionChanged -= OnTreeSelectionChanged;
            tree.Loaded -= OnTreeLoaded;
            tree.RemoveHandler(InputElement.PointerPressedEvent, OnInputStarted);
            tree.RemoveHandler(InputElement.PointerPressedEvent, OnInputEnded);
            tree.RemoveHandler(InputElement.PointerReleasedEvent, OnInputStarted);
            tree.RemoveHandler(InputElement.PointerReleasedEvent, OnInputEnded);
            tree.RemoveHandler(InputElement.KeyDownEvent, OnInputStarted);
            tree.RemoveHandler(InputElement.KeyDownEvent, OnInputEnded);
            tree.AutoScrollToSelectedItem = treeAutoScroll;
        }
        followedItems = null;
        pendingItems.Clear();
        isTreeInput = false;
        base.OnDetaching();
    }

    private void OnInputStarted(object? sender, RoutedEventArgs e) => isTreeInput = true;

    private void OnInputEnded(object? sender, RoutedEventArgs e) => isTreeInput = false;

    private void OnTreeLoaded(object? sender, RoutedEventArgs e)
    {
        if (AssociatedObject is { } tree)
            FollowNewCollection(tree);
        // A selection made while the tree was not shown (a panel that opens with the session) shows once it is
        PostUpdate();
    }

    private void OnTreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Not the lists and boxes in the rows
        if (AssociatedObject is not { } tree || e.Source != tree || isTreeInput)
            return;

        FollowNewCollection(tree);
        AddPendingItems(e.AddedItems);
    }

    /// <summary>
    /// Follows the items of the collection of selected items of the tree when it is a new one (the view model's, once bound).
    /// </summary>
    private void FollowNewCollection(TreeView tree)
    {
        var items = tree.SelectedItems;
        if (ReferenceEquals(items, followedItems))
            return;

        followedItems = items;
        AddPendingItems(items);
    }

    private void AddPendingItems(IList items)
    {
        if (items.Count == 0)
            return;

        foreach (var item in items)
        {
            if (item is not null)
                pendingItems.Add(item);
        }
        PostUpdate();
    }

    private void PostUpdate()
    {
        // Once, after the whole change of the selection (a clear then an add, the items of a rectangle one by one...)
        if (pendingItems.Count == 0 || isUpdatePosted)
            return;

        isUpdatePosted = true;
        Dispatcher.UIThread.Post(Update, DispatcherPriority.Background);
    }

    private void Update()
    {
        isUpdatePosted = false;
        if (AssociatedObject is not { SelectedItems: { } selectedItems } tree)
        {
            pendingItems.Clear();
            return;
        }
        if (!tree.IsLoaded)
            return;

        // The rows of the first level
        tree.UpdateLayout();
        var selected = new HashSet<object>(selectedItems.Cast<object>());
        // Each ancestor once, with where the rows of its children are
        var expanded = new Dictionary<object, ItemsControl>();
        object? last = null;
        foreach (var item in pendingItems)
        {
            if (!selected.Contains(item))
                continue;

            ExpandAncestors(tree, item, expanded);
            last = item;
        }
        pendingItems.Clear();
        if (last is null)
            return;

        // The rows of the expanded items exist after a layout
        tree.UpdateLayout();
        BringIntoView(tree, last);
    }

    /// <summary>
    /// Expands the ancestors of an item in a tree, from the root.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="item">The item.</param>
    /// <param name="expanded">The ancestors expanded so far, with where the rows of their children are; filled.</param>
    internal static void ExpandAncestors(TreeView tree, object item, Dictionary<object, ItemsControl> expanded)
    {
        // The ancestors left to expand, from the parent up, and where the rows of the first of them are
        var ancestors = new List<object>();
        ItemsControl owner = tree;
        for (var parent = (item as IChildViewModel)?.GetParent(); parent is not null; parent = parent.GetParent())
        {
            if (expanded.TryGetValue(parent, out var parentOwner))
            {
                owner = parentOwner;
                break;
            }
            ancestors.Add(parent);
        }

        for (var i = ancestors.Count - 1; i >= 0; --i)
        {
            var ancestor = ancestors[i];
            if (ancestor is IExpandableViewModel viewModel)
                viewModel.IsExpanded = true;
            if (owner.ContainerFromItem(ancestor) is TreeViewItem container)
            {
                if (!container.IsExpanded)
                    container.SetCurrentValue(TreeViewItem.IsExpandedProperty, true);
                // The next ancestor is looked for in the rows of this one, which a view model can't expand
                if (i > 0 && ancestors[i - 1] is not IExpandableViewModel && container.ContainerFromItem(ancestors[i - 1]) is null)
                    container.UpdateLayout();
                owner = container;
            }
            expanded[ancestor] = owner;
        }
    }

    /// <summary>
    /// Brings the row of an item into view, without its children.
    /// </summary>
    private static void BringIntoView(TreeView tree, object item)
    {
        if (tree.TreeContainerFromItem(item) is not TreeViewItem container)
            return;

        var header = container.GetVisualDescendants().OfType<Control>().FirstOrDefault(x => x.Name == "PART_Header" && ReferenceEquals(x.TemplatedParent, container));
        (header ?? container).BringIntoView();
    }
}
