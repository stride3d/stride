// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.DragDrop;

/// <summary>
/// Accepts dropped assets on a reference property of the property grid. Attach it to the root of the property's template: its
/// data context is the <see cref="NodeViewModel"/>.
/// </summary>
/// <remarks>
/// As the WPF <c>ReferenceHostDragDropBehavior</c>, the drop goes through the reference handler that the node updaters attach
/// to the node (<see cref="ReferenceData"/>; <c>ContentReferenceViewModel</c> for an asset reference): it checks that the asset
/// fits the property and sets it with the <c>SetContentReference</c> command, one transaction of the undo history.
/// </remarks>
public sealed class ContentReferenceDropBehavior : StyledElementBehavior<Control>
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
    /// Whether the given assets can be dropped on the node of this element, with the feedback message of the reference handler.
    /// </summary>
    /// <returns>The reference handler of the node when it accepts the assets, otherwise <c>null</c>.</returns>
    public IAddReferenceViewModel? GetDropTarget(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        message = string.Empty;
        if (children.Count == 0 || AssociatedObject?.DataContext is not NodeViewModel { IsReadOnly: false } node)
            return null;
        if (node.GetAssociatedData(ReferenceData.AddReferenceViewModel) is not IAddReferenceViewModel reference)
            return null;

        reference.SetTargetNode(node);
        return reference.CanAddChildren(children, modifiers, out message) ? reference : null;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var assets = AssetDragDropHelper.GetAssets(e.DataTransfer);
        var target = GetDropTarget(assets, AssetDragDropHelper.ToAddChildModifiers(e.KeyModifiers), out _);
        e.DragEffects = target is not null ? DragDropEffects.Link : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var assets = AssetDragDropHelper.GetAssets(e.DataTransfer);
        var modifiers = AssetDragDropHelper.ToAddChildModifiers(e.KeyModifiers);
        if (GetDropTarget(assets, modifiers, out _) is not { } target)
            return;

        target.AddChildren(assets, modifiers);
        e.DragEffects = DragDropEffects.Link;
        e.Handled = true;
    }
}
