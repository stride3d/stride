// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.DragDrop;

/// <summary>
/// Accepts dropped assets (or other dragged items, such as entities) on an element whose data context takes children
/// (<see cref="IAddChildViewModel"/>), such as a folder of the solution explorer or the view of the scene editor. Attach it
/// to the root of the item's template, or to the element.
/// </summary>
/// <remarks>
/// The drop goes through <see cref="IAddChildViewModel.CanAddChildren"/> and
/// <see cref="IAddChildViewModel.AddChildren"/>: a folder moves the assets to itself, one transaction of the undo history
/// ("Move asset ...").
/// While something is dragged over the element, it shows the message of its data context next to the pointer, which tells
/// why a refused drop is refused (<see cref="DragFeedbackAdorner"/>).
/// </remarks>
public sealed class AddChildDropBehavior : StyledElementBehavior<Control>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        global::Avalonia.Input.DragDrop.SetAllowDrop(AssociatedObject!, true);
        AssociatedObject!.AddHandler(global::Avalonia.Input.DragDrop.DragEnterEvent, OnDragOver);
        AssociatedObject!.AddHandler(global::Avalonia.Input.DragDrop.DragOverEvent, OnDragOver);
        AssociatedObject!.AddHandler(global::Avalonia.Input.DragDrop.DragLeaveEvent, OnDragLeave);
        AssociatedObject!.AddHandler(global::Avalonia.Input.DragDrop.DropEvent, OnDrop);
    }

    protected override void OnDetaching()
    {
        AssociatedObject!.RemoveHandler(global::Avalonia.Input.DragDrop.DragEnterEvent, OnDragOver);
        AssociatedObject!.RemoveHandler(global::Avalonia.Input.DragDrop.DragOverEvent, OnDragOver);
        AssociatedObject!.RemoveHandler(global::Avalonia.Input.DragDrop.DragLeaveEvent, OnDragLeave);
        AssociatedObject!.RemoveHandler(global::Avalonia.Input.DragDrop.DropEvent, OnDrop);
        DragFeedbackAdorner.Clear(AssociatedObject);
        base.OnDetaching();
    }

    /// <summary>
    /// Whether the given assets can be dropped on the item of this element, with the feedback message of the item.
    /// </summary>
    /// <returns>The item when it takes the assets, otherwise <c>null</c>.</returns>
    public IAddChildViewModel? GetDropTarget(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        message = string.Empty;
        if (children.Count == 0 || AssociatedObject?.DataContext is not IAddChildViewModel target)
            return null;

        return target.CanAddChildren(children, modifiers, out message) ? target : null;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var items = AssetDragDropHelper.GetItems(e.DataTransfer);
        var target = GetDropTarget(items, AssetDragDropHelper.ToAddChildModifiers(e.KeyModifiers), out var message);
        if (items.Count > 0 && AssociatedObject is { } element)
        {
            if (target is null && string.IsNullOrEmpty(message))
                message = DragDropMessages.InvalidDropArea;
            var acceptance = target is not null ? DropAcceptance.Accepted : DropAcceptance.Rejected;
            DragFeedbackAdorner.Show(element, e.GetPosition(element), acceptance, message, new Rect(element.Bounds.Size));
        }
        e.DragEffects = target is not null ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        // Moving over a child of the element leaves the child; only leaving the element ends the feedback
        if (AssociatedObject is { } element && !new Rect(element.Bounds.Size).Contains(e.GetPosition(element)))
            DragFeedbackAdorner.Clear(element);
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (AssociatedObject is { } element)
            DragFeedbackAdorner.Clear(element);
        var items = AssetDragDropHelper.GetItems(e.DataTransfer);
        var modifiers = AssetDragDropHelper.ToAddChildModifiers(e.KeyModifiers);
        if (GetDropTarget(items, modifiers, out _) is not { } target)
            return;

        target.AddChildren(items, modifiers);
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }
}
