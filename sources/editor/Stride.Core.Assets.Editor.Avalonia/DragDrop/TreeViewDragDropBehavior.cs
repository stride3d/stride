// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.DragDrop;

/// <summary>
/// Drags the items of a tree, and drops items on it: onto an item (<see cref="IAddChildViewModel"/>), between two items
/// (<see cref="IInsertChildViewModel"/>, with <see cref="CanInsert"/>) or onto the empty area below the items (the data
/// context of the tree, with <see cref="AllowDropOnEmptyArea"/>). Attach it to the tree.
/// </summary>
/// <remarks>
/// <para>
/// As the WPF <c>TreeViewDragDropBehavior</c>: a drag moves the selection when it starts on a selected item, otherwise the
/// item alone, and never starts while an item is being renamed or while both an item and one of its parents are selected.
/// The top and bottom bands of a row insert before and after it (<see cref="DropZoneCalculator"/>), the rest of the row adds
/// to it. An expanded item has no band to insert after it: the gap below the last item of a subtree goes after the item or
/// after one of its ancestors that end there, by the horizontal position of the pointer. A rejected insertion falls back to
/// an addition to the row. Each drop is one call to the target, which makes it one entry of the undo history.
/// </para>
/// <para>
/// The tree shows where the drop goes and the message of the target (<see cref="DragFeedbackAdorner"/>), and scrolls while
/// the pointer stays near its top or bottom edge, as the WPF <c>DragOverAutoScrollBehavior</c>.
/// </para>
/// </remarks>
public sealed class TreeViewDragDropBehavior : StyledElementBehavior<TreeView>
{
    /// <summary>
    /// Identifies the <see cref="CanDrag"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> CanDragProperty = AvaloniaProperty.Register<TreeViewDragDropBehavior, bool>(nameof(CanDrag), true);

    /// <summary>
    /// Identifies the <see cref="CanInsert"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> CanInsertProperty = AvaloniaProperty.Register<TreeViewDragDropBehavior, bool>(nameof(CanInsert));

    /// <summary>
    /// Identifies the <see cref="AllowDropOnEmptyArea"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> AllowDropOnEmptyAreaProperty = AvaloniaProperty.Register<TreeViewDragDropBehavior, bool>(nameof(AllowDropOnEmptyArea));

    private const double DragThreshold = 4.0;
    // The auto-scroll: the height of the bands along the edges, the delay before it starts, and its speed (WPF: 32 px, 0.5 s, 300 px/s)
    private const double ScrollBandHeight = 24.0;
    private static readonly TimeSpan ScrollDelay = TimeSpan.FromSeconds(0.5);
    private static readonly TimeSpan ScrollInterval = TimeSpan.FromMilliseconds(40);
    private const double ScrollStep = 12.0;

    private Point? dragStartPoint;
    private PointerPressedEventArgs? pressedArgs;
    private TreeViewItem? pressedItem;
    // The item a press on a multiple selection selects alone on release, unless the press starts a drag
    private object? deferredSelection;
    private DispatcherTimer? scrollTimer;
    private int scrollDirection;
    private DateTime scrollRequestTime;

    /// <summary>
    /// Gets or sets whether the items of the tree can be dragged.
    /// </summary>
    public bool CanDrag { get => GetValue(CanDragProperty); set => SetValue(CanDragProperty, value); }

    /// <summary>
    /// Gets or sets whether items can be dropped between two items of the tree.
    /// </summary>
    public bool CanInsert { get => GetValue(CanInsertProperty); set => SetValue(CanInsertProperty, value); }

    /// <summary>
    /// Gets or sets whether items can be dropped on the empty area below the items of the tree; the data context of the tree
    /// takes them.
    /// </summary>
    public bool AllowDropOnEmptyArea { get => GetValue(AllowDropOnEmptyAreaProperty); set => SetValue(AllowDropOnEmptyAreaProperty, value); }

    /// <summary>
    /// Gets whether the tree scrolls by itself because the pointer of a drag stays near one of its edges.
    /// </summary>
    public bool IsAutoScrolling => scrollTimer is not null;

    protected override void OnAttached()
    {
        base.OnAttached();
        var tree = AssociatedObject!;
        global::Avalonia.Input.DragDrop.SetAllowDrop(tree, true);
        tree.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        tree.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        tree.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        tree.AddHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost, handledEventsToo: true);
        tree.AddHandler(global::Avalonia.Input.DragDrop.DragEnterEvent, OnDragOver);
        tree.AddHandler(global::Avalonia.Input.DragDrop.DragOverEvent, OnDragOver);
        tree.AddHandler(global::Avalonia.Input.DragDrop.DragLeaveEvent, OnDragLeave);
        tree.AddHandler(global::Avalonia.Input.DragDrop.DropEvent, OnDrop);
    }

    protected override void OnDetaching()
    {
        var tree = AssociatedObject!;
        tree.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        tree.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
        tree.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
        tree.RemoveHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost);
        tree.RemoveHandler(global::Avalonia.Input.DragDrop.DragEnterEvent, OnDragOver);
        tree.RemoveHandler(global::Avalonia.Input.DragDrop.DragOverEvent, OnDragOver);
        tree.RemoveHandler(global::Avalonia.Input.DragDrop.DragLeaveEvent, OnDragLeave);
        tree.RemoveHandler(global::Avalonia.Input.DragDrop.DropEvent, OnDrop);
        EndDragOver();
        ResetPress();
        base.OnDetaching();
    }

    /// <summary>
    /// Gets the items that a drag starting on <paramref name="item"/> moves: the selection when the item is selected,
    /// otherwise the item alone; none when the drag can't start (an item is being renamed, or both an item and one of its
    /// parents are selected).
    /// </summary>
    public IReadOnlyList<object> GetItemsToDrag(TreeViewItem item)
    {
        if (AssociatedObject is not { } tree || item.DataContext is not { } data)
            return [];

        var selection = tree.SelectedItems?.Cast<object>().ToList() ?? [];
        IReadOnlyList<object> items = selection.Contains(data) ? selection : [data];
        if (items.Any(x => x is IIsEditableViewModel { IsEditing: true }))
            return [];

        // As WPF: a selection that holds an item and one of its parents can't be dragged
        var containers = items.Select(x => tree.TreeContainerFromItem(x)).OfType<TreeViewItem>().ToList();
        if (containers.Any(x => GetParentItems(x).Any(p => items.Contains(p.DataContext))))
            return [];

        return items;
    }

    /// <summary>
    /// Finds where the given items would go if they were dropped at <paramref name="position"/>, without dropping them.
    /// </summary>
    /// <param name="items">The dropped items.</param>
    /// <param name="position">The position of the pointer, relative to the tree.</param>
    /// <param name="modifiers">The modifier keys.</param>
    public TreeViewDropTarget GetDropTarget(IReadOnlyCollection<object> items, Point position, AddChildModifiers modifiers)
    {
        if (AssociatedObject is not { } tree || items.Count == 0)
            return TreeViewDropTarget.None;

        var hit = tree.InputHitTest(position) as Visual;
        var item = hit?.GetSelfAndVisualAncestors().OfType<TreeViewItem>().FirstOrDefault(x => IsItemOf(tree, x));
        if (item is null)
        {
            // The empty area below the items: the data context of the tree takes them (the WPF tree panel has the data context of the tree)
            if (hit is null || !AllowDropOnEmptyArea || tree.DataContext is not IAddChildViewModel area)
                return TreeViewDropTarget.None;
            var areaAcceptance = area.GetAddChildrenAcceptance(items, modifiers, out var areaMessage);
            return new TreeViewDropTarget(TreeViewDropKind.Add, area, null, InsertPosition.Before, areaAcceptance, areaMessage, null, null);
        }

        var header = GetHeaderBounds(tree, item);
        if (CanInsert)
        {
            var allowAfter = !(item.IsExpanded && item.ItemCount > 0);
            if (DropZoneCalculator.GetInsertPosition(position.Y - header.Y, header.Height, allowAfter) is { } insertPosition)
            {
                var targetItem = insertPosition == InsertPosition.After ? ResolveInsertAfterTarget(tree, item, position.X) : item;
                // A rejected insertion falls through to the addition, so that the item itself becomes the target. A no-op stops
                // here, otherwise the row would answer a gesture that points between two rows.
                if (targetItem.DataContext is IInsertChildViewModel insertTarget)
                {
                    var insertAcceptance = insertTarget.GetInsertChildrenAcceptance(items, insertPosition, modifiers, out var insertMessage);
                    if (insertAcceptance != DropAcceptance.Rejected)
                    {
                        var y = insertPosition == InsertPosition.Before ? header.Top : header.Bottom;
                        var line = (new Point(GetIndent(tree, targetItem), y), new Point(header.Right, y));
                        return new TreeViewDropTarget(TreeViewDropKind.Insert, insertTarget, targetItem, insertPosition, insertAcceptance, insertMessage, null, line);
                    }
                }
            }
        }

        if (item.DataContext is not IAddChildViewModel addTarget)
            return TreeViewDropTarget.None with { TargetBounds = header };

        var acceptance = addTarget.GetAddChildrenAcceptance(items, modifiers, out var message);
        return new TreeViewDropTarget(TreeViewDropKind.Add, addTarget, item, InsertPosition.Before, acceptance, message, header, null);
    }

    /// <summary>
    /// Drops the given items at <paramref name="position"/>, as a drop of the pointer does.
    /// </summary>
    /// <param name="items">The dropped items.</param>
    /// <param name="position">The position of the pointer, relative to the tree.</param>
    /// <param name="modifiers">The modifier keys.</param>
    /// <returns><c>true</c> when the target took the items.</returns>
    public bool Drop(IReadOnlyCollection<object> items, Point position, AddChildModifiers modifiers)
    {
        var target = GetDropTarget(items, position, modifiers);
        // Only an accepted drop changes the asset. A no-op leaves it untouched and creates no undo entry.
        if (target.Acceptance != DropAcceptance.Accepted)
            return false;

        switch (target.Kind)
        {
            case TreeViewDropKind.Insert:
                ((IInsertChildViewModel)target.Target!).InsertChildren(items, target.InsertPosition, modifiers);
                return true;
            case TreeViewDropKind.Add:
                ((IAddChildViewModel)target.Target!).AddChildren(items, modifiers);
                return true;
            default:
                return false;
        }
    }

    private static bool IsItemOf(TreeView tree, TreeViewItem item) => ReferenceEquals(item.FindAncestorOfType<TreeView>(), tree);

    private static IEnumerable<TreeViewItem> GetParentItems(TreeViewItem item)
    {
        return item.GetVisualAncestors().TakeWhile(x => x is not TreeView).OfType<TreeViewItem>();
    }

    /// <summary>
    /// Gets the row of <paramref name="item"/> without its children, relative to the tree.
    /// </summary>
    private static Rect GetHeaderBounds(TreeView tree, TreeViewItem item)
    {
        var header = item.GetVisualDescendants().OfType<Control>().FirstOrDefault(x => x.Name == "PART_Header" && ReferenceEquals(x.TemplatedParent, item));
        var element = (Control?)header ?? item;
        var origin = element.TranslatePoint(default, tree) ?? default;
        var height = header?.Bounds.Height ?? item.Bounds.Height;
        // The row spans the width of the tree, whatever the indentation of the item
        var left = item.TranslatePoint(default, tree)?.X ?? 0;
        return new Rect(left, origin.Y, Math.Max(0, tree.Bounds.Width - left), height);
    }

    /// <summary>
    /// Gets where the content of <paramref name="item"/> starts, relative to the tree: the indentation of its level.
    /// </summary>
    private static double GetIndent(TreeView tree, TreeViewItem item)
    {
        var presenter = item.GetVisualDescendants().OfType<Control>().FirstOrDefault(x => x.Name == "PART_HeaderPresenter" && ReferenceEquals(x.TemplatedParent, item));
        return (presenter ?? item).TranslatePoint(default, tree)?.X ?? 0;
    }

    /// <summary>
    /// Finds the item after which the dragged items go, from the horizontal position of the pointer.
    /// </summary>
    /// <remarks>
    /// The gap below the last item of a subtree is also the gap below each ancestor that ends there. The pointer then selects
    /// one of these items by its indentation: moving left selects an ancestor. Therefore an item can go after an expanded
    /// ancestor, which has no gap of its own.
    /// </remarks>
    private static TreeViewItem ResolveInsertAfterTarget(TreeView tree, TreeViewItem item, double pointerX)
    {
        var target = item;
        var child = item;
        var parent = GetParentItems(child).FirstOrDefault();
        while (parent is not null && IsLastChild(child, parent))
        {
            // The deepest item whose indentation the pointer has passed wins
            if (pointerX < GetIndent(tree, target))
                target = parent;
            child = parent;
            parent = GetParentItems(child).FirstOrDefault();
        }
        return target;
    }

    private static bool IsLastChild(TreeViewItem child, TreeViewItem parent)
    {
        var count = parent.ItemCount;
        return count > 0 && Equals(parent.Items[count - 1], child.DataContext);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ResetPress();
        if (!CanDrag || AssociatedObject is not { } tree || !e.GetCurrentPoint(tree).Properties.IsLeftButtonPressed)
            return;

        // A drag starts from the row of an item, not from its buttons, its expander or the box that renames it
        var source = e.Source as Visual;
        var item = source?.GetSelfAndVisualAncestors().OfType<TreeViewItem>().FirstOrDefault(x => IsItemOf(tree, x));
        if (item is null || source!.GetSelfAndVisualAncestors().TakeWhile(x => !ReferenceEquals(x, item)).Any(x => x is Button or TextBox))
            return;

        dragStartPoint = e.GetPosition(tree);
        pressedArgs = e;
        pressedItem = item;

        // A press on an item of a multiple selection keeps the selection, so that a drag moves all of it; a release without
        // drag selects the item alone, as a click does
        var selection = tree.SelectedItems;
        if (e.ClickCount == 1 && (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Meta)) == 0
            && selection is { Count: > 1 } && selection.Contains(item.DataContext))
        {
            deferredSelection = item.DataContext;
            item.Focus(NavigationMethod.Pointer);
            e.Handled = true;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (deferredSelection is { } item && AssociatedObject is { } tree)
            tree.SelectedItem = item;
        ResetPress();
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) => ResetPress();

    private async void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (dragStartPoint is not { } start || pressedArgs is not { } savedArgs || pressedItem is not { } item || AssociatedObject is not { } tree)
            return;
        if (!e.GetCurrentPoint(tree).Properties.IsLeftButtonPressed)
        {
            ResetPress();
            return;
        }

        var current = e.GetPosition(tree);
        if (Math.Abs(current.X - start.X) < DragThreshold && Math.Abs(current.Y - start.Y) < DragThreshold)
            return;

        ResetPress();
        var items = GetItemsToDrag(item);
        if (items.Count == 0)
            return;

        try
        {
            await global::Avalonia.Input.DragDrop.DoDragDropAsync(savedArgs, AssetDragDropHelper.CreateItemData(items), DragDropEffects.Move);
        }
        catch (Exception)
        {
            // A drag the platform refuses (no drag source on a headless or embedded host) leaves the tree as it was
        }
        finally
        {
            EndDragOver();
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (AssociatedObject is not { } tree)
            return;

        var position = e.GetPosition(tree);
        var items = AssetDragDropHelper.GetItems(e.DataTransfer);
        var target = GetDropTarget(items, position, AssetDragDropHelper.ToAddChildModifiers(e.KeyModifiers));
        var message = items.Count == 0 ? string.Empty : target.Target is null ? DragDropMessages.InvalidDropArea : target.Message;
        DragFeedbackAdorner.Show(tree, position, target.Acceptance, message, target.TargetBounds, target.InsertLine);
        UpdateAutoScroll(tree, position);

        e.DragEffects = target.Acceptance != DropAcceptance.Rejected ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        // Moving from one row to another leaves the first row; only leaving the tree ends the feedback
        if (AssociatedObject is { } tree && !new Rect(tree.Bounds.Size).Contains(e.GetPosition(tree)))
            EndDragOver();
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (AssociatedObject is not { } tree)
            return;

        EndDragOver();
        var items = AssetDragDropHelper.GetItems(e.DataTransfer);
        var modifiers = AssetDragDropHelper.ToAddChildModifiers(e.KeyModifiers);
        var dropped = Drop(items, e.GetPosition(tree), modifiers);
        e.DragEffects = dropped ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void UpdateAutoScroll(TreeView tree, Point position)
    {
        var direction = position.Y < ScrollBandHeight ? -1 : position.Y > tree.Bounds.Height - ScrollBandHeight ? 1 : 0;
        if (direction == 0)
        {
            StopAutoScroll();
            return;
        }

        if (direction != scrollDirection || scrollTimer is null)
        {
            StopAutoScroll();
            scrollDirection = direction;
            scrollRequestTime = DateTime.UtcNow;
            scrollTimer = new DispatcherTimer(ScrollInterval, DispatcherPriority.Background, (_, _) => AutoScroll(tree));
            scrollTimer.Start();
        }
    }

    private void AutoScroll(TreeView tree)
    {
        if (DateTime.UtcNow - scrollRequestTime < ScrollDelay || tree.FindDescendantOfType<ScrollViewer>() is not { } scrollViewer)
            return;

        var offset = scrollViewer.Offset;
        var maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        scrollViewer.Offset = offset.WithY(Math.Clamp(offset.Y + scrollDirection * ScrollStep, 0, maxY));
    }

    private void StopAutoScroll()
    {
        scrollTimer?.Stop();
        scrollTimer = null;
        scrollDirection = 0;
    }

    private void EndDragOver()
    {
        StopAutoScroll();
        if (AssociatedObject is { } tree)
            DragFeedbackAdorner.Clear(tree);
    }

    private void ResetPress()
    {
        dragStartPoint = null;
        pressedArgs = null;
        pressedItem = null;
        deferredSelection = null;
    }
}

/// <summary>
/// How a drop on a tree would take the dropped items.
/// </summary>
public enum TreeViewDropKind
{
    /// <summary>
    /// Nothing under the pointer takes items.
    /// </summary>
    None,

    /// <summary>
    /// The item under the pointer, or the data context of the tree on its empty area, adds them as children
    /// (<see cref="IAddChildViewModel"/>).
    /// </summary>
    Add,

    /// <summary>
    /// They go before or after an item (<see cref="IInsertChildViewModel"/>).
    /// </summary>
    Insert,
}

/// <summary>
/// Where a drop on a tree would go.
/// </summary>
/// <param name="Kind">How the target takes the items.</param>
/// <param name="Target">The <see cref="IAddChildViewModel"/> or <see cref="IInsertChildViewModel"/> that takes them.</param>
/// <param name="Item">The item of the tree that takes them, or before or after which they go; <c>null</c> on the empty area.</param>
/// <param name="InsertPosition">Before or after <paramref name="Item"/>, for an insertion.</param>
/// <param name="Acceptance">Whether the target accepts them.</param>
/// <param name="Message">The message of the target: what the drop does, or why it does nothing.</param>
/// <param name="TargetBounds">The row that takes them, relative to the tree, for an addition to an item.</param>
/// <param name="InsertLine">The line between two rows where they go, relative to the tree, for an insertion.</param>
public sealed record TreeViewDropTarget(TreeViewDropKind Kind, object? Target, TreeViewItem? Item, InsertPosition InsertPosition, DropAcceptance Acceptance, string Message, Rect? TargetBounds, (Point Start, Point End)? InsertLine)
{
    /// <summary>
    /// No target: the drop is refused.
    /// </summary>
    public static readonly TreeViewDropTarget None = new(TreeViewDropKind.None, null, null, InsertPosition.Before, DropAcceptance.Rejected, DragDropMessages.InvalidDropArea, null, null);
}
