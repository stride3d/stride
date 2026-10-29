// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.Avalonia.DragDrop;

/// <summary>
/// Accepts dropped assets on an item whose data context takes children (<see cref="IAddChildViewModel"/>), such as a folder of
/// the solution explorer. Attach it to the root of the item's template.
/// </summary>
/// <remarks>
/// As the drop on the WPF solution explorer, the drop goes through <see cref="IAddChildViewModel.CanAddChildren"/> and
/// <see cref="IAddChildViewModel.AddChildren"/>: a folder moves the assets to itself, one transaction of the undo history
/// ("Move asset ...").
/// </remarks>
public sealed class AddChildDropBehavior : StyledElementBehavior<Control>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        global::Avalonia.Input.DragDrop.SetAllowDrop(AssociatedObject!, true);
        AssociatedObject!.AddHandler(global::Avalonia.Input.DragDrop.DragEnterEvent, OnDragOver);
        AssociatedObject!.AddHandler(global::Avalonia.Input.DragDrop.DragOverEvent, OnDragOver);
        AssociatedObject!.AddHandler(global::Avalonia.Input.DragDrop.DropEvent, OnDrop);
    }

    protected override void OnDetaching()
    {
        AssociatedObject!.RemoveHandler(global::Avalonia.Input.DragDrop.DragEnterEvent, OnDragOver);
        AssociatedObject!.RemoveHandler(global::Avalonia.Input.DragDrop.DragOverEvent, OnDragOver);
        AssociatedObject!.RemoveHandler(global::Avalonia.Input.DragDrop.DropEvent, OnDrop);
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
        var assets = AssetDragDropHelper.GetAssets(e.DataTransfer);
        var target = GetDropTarget(assets, AssetDragDropHelper.ToAddChildModifiers(e.KeyModifiers), out _);
        e.DragEffects = target is not null ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var assets = AssetDragDropHelper.GetAssets(e.DataTransfer);
        var modifiers = AssetDragDropHelper.ToAddChildModifiers(e.KeyModifiers);
        if (GetDropTarget(assets, modifiers, out _) is not { } target)
            return;

        target.AddChildren(assets, modifiers);
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }
}
