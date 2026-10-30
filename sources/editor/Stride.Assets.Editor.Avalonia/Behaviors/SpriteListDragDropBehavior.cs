// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Editor.ViewModels.SpriteEditor;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;

namespace Stride.Assets.Editor.Avalonia.Behaviors;

/// <summary>
/// The drag and drop of the sprite list, as the WPF <c>ListBoxDragDropBehavior</c> of the sprite editor: the selected sprites
/// are dragged to another place of the list, and image files dropped on the list add sprites (before or after the sprite under
/// the pointer, or at the end of the sheet).
/// </summary>
/// <remarks>
/// The drop goes through <see cref="IInsertChildViewModel"/> on a sprite and <see cref="IAddChildViewModel"/> on the editor, as in
/// WPF: each drop is one entry of the undo history ("Move images", "Add images").
/// </remarks>
public sealed class SpriteListDragDropBehavior : StyledElementBehavior<ListBox>
{
    /// <summary>
    /// The in-process format of the dragged sprites.
    /// </summary>
    public static readonly DataFormat<SpriteInfoViewModel> SpriteFormat = DataFormat.CreateInProcessFormat<SpriteInfoViewModel>("Stride.SpriteInfoViewModel");

    private const double DragThreshold = 4.0;
    private Point? dragStartPoint;
    private PointerPressedEventArgs? pressedArgs;

    protected override void OnAttached()
    {
        base.OnAttached();
        DragDrop.SetAllowDrop(AssociatedObject!, true);
        AssociatedObject!.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, handledEventsToo: true);
        AssociatedObject!.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, handledEventsToo: true);
        AssociatedObject!.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, handledEventsToo: true);
        AssociatedObject!.AddHandler(DragDrop.DragEnterEvent, OnDragOver);
        AssociatedObject!.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AssociatedObject!.AddHandler(DragDrop.DropEvent, OnDrop);
    }

    protected override void OnDetaching()
    {
        AssociatedObject!.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        AssociatedObject!.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
        AssociatedObject!.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
        AssociatedObject!.RemoveHandler(DragDrop.DragEnterEvent, OnDragOver);
        AssociatedObject!.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
        AssociatedObject!.RemoveHandler(DragDrop.DropEvent, OnDrop);
        base.OnDetaching();
    }

    /// <summary>
    /// Creates the data of a drag of the given sprites.
    /// </summary>
    public static DataTransfer CreateData(IEnumerable<SpriteInfoViewModel> sprites)
    {
        var data = new DataTransfer();
        foreach (var sprite in sprites)
        {
            var item = new DataTransferItem();
            item.Set(SpriteFormat, sprite);
            data.Add(item);
        }
        return data;
    }

    /// <summary>
    /// Drops the given sprites or files at a point of the list (relative to the list), as a drop of the pointer does.
    /// </summary>
    /// <returns><c>true</c> when the list took them.</returns>
    public bool Drop(IReadOnlyCollection<object> children, Point position, KeyModifiers keyModifiers)
    {
        var modifiers = ToAddChildModifiers(keyModifiers);
        switch (GetDropTarget(children, position, modifiers))
        {
            case (IInsertChildViewModel item, var insertPosition):
                item.InsertChildren(children, insertPosition, modifiers);
                return true;
            case (IAddChildViewModel editor, _):
                editor.AddChildren(children, modifiers);
                return true;
            default:
                return false;
        }
    }

    private (object? Target, InsertPosition Position) GetDropTarget(IReadOnlyCollection<object> children, Point position, AddChildModifiers modifiers)
    {
        if (children.Count == 0 || AssociatedObject is not { } list)
            return default;

        // The sprite under the pointer takes the children before or after itself, as the insert adorner of WPF shows
        foreach (var item in list.GetRealizedContainers())
        {
            if (item.DataContext is not IInsertChildViewModel target || item.TranslatePoint(default, list) is not { } origin)
                continue;

            var bounds = new Rect(origin, item.Bounds.Size);
            if (!bounds.Contains(position))
                continue;

            var insertPosition = position.Y < bounds.Center.Y ? InsertPosition.Before : InsertPosition.After;
            return target.CanInsertChildren(children, insertPosition, modifiers, out _) ? (target, insertPosition) : default;
        }

        // Elsewhere, the editor adds the dropped files at the end of the sheet
        if (list.DataContext is IAddChildViewModel editor && editor.CanAddChildren(children, modifiers, out _))
            return (editor, InsertPosition.After);

        return default;
    }

    private static IReadOnlyCollection<object> GetChildren(IDataTransfer? data)
    {
        if (data is null)
            return [];

        if (data.TryGetValues(SpriteFormat) is { Length: > 0 } sprites)
            return sprites.OfType<object>().ToList();

        return data.TryGetFiles()?.Select(x => x.TryGetLocalPath()).OfType<string>().Select(x => (object)new UFile(x)).ToList() ?? [];
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // A drag starts from a sprite, not from the scroll bar or an empty part of the list
        var onItem = (e.Source as Visual)?.FindAncestorOfType<ListBoxItem>(includeSelf: true) is not null;
        if (onItem && e.Source is not TextBox && e.GetCurrentPoint(AssociatedObject).Properties.IsLeftButtonPressed)
        {
            dragStartPoint = e.GetPosition(AssociatedObject);
            pressedArgs = e;
        }
        else
        {
            Reset();
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) => Reset();

    private async void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (dragStartPoint is not { } start || pressedArgs is not { } savedArgs || AssociatedObject is not { } list)
            return;

        var current = e.GetPosition(list);
        if (Math.Abs(current.X - start.X) < DragThreshold && Math.Abs(current.Y - start.Y) < DragThreshold)
            return;

        Reset();
        var sprites = list.SelectedItems?.OfType<SpriteInfoViewModel>().ToList() ?? [];
        if (sprites.Count == 0)
            return;

        try
        {
            await DragDrop.DoDragDropAsync(savedArgs, CreateData(sprites), DragDropEffects.Move);
        }
        catch (Exception)
        {
            // A drag the platform refuses (no drag source on a headless or embedded host) leaves the list as it was
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var children = GetChildren(e.DataTransfer);
        var (target, _) = GetDropTarget(children, e.GetPosition(AssociatedObject), ToAddChildModifiers(e.KeyModifiers));
        e.DragEffects = target is null ? DragDropEffects.None : children.All(x => x is UFile) ? DragDropEffects.Copy : DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var children = GetChildren(e.DataTransfer);
        if (Drop(children, e.GetPosition(AssociatedObject), e.KeyModifiers))
        {
            e.DragEffects = children.All(x => x is UFile) ? DragDropEffects.Copy : DragDropEffects.Move;
            e.Handled = true;
        }
    }

    private void Reset()
    {
        dragStartPoint = null;
        pressedArgs = null;
    }

    private static AddChildModifiers ToAddChildModifiers(KeyModifiers modifiers)
    {
        var result = AddChildModifiers.None;
        if (modifiers.HasFlag(KeyModifiers.Control))
            result |= AddChildModifiers.Ctrl;
        if (modifiers.HasFlag(KeyModifiers.Shift))
            result |= AddChildModifiers.Shift;
        if (modifiers.HasFlag(KeyModifiers.Alt))
            result |= AddChildModifiers.Alt;
        return result;
    }
}
