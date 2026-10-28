// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.Avalonia.DragDrop;

/// <summary>
/// Starts dragging the asset of an item when the pointer moves past a small distance with the left button down. Attach it to the
/// root of the item template of an asset list: its data context is the <see cref="AssetViewModel"/>.
/// </summary>
public sealed class AssetDragBehavior : StyledElementBehavior<Control>
{
    private const double DragThreshold = 4.0;
    private Point? dragStartPoint;
    private PointerPressedEventArgs? pressedArgs;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject!.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, handledEventsToo: true);
        AssociatedObject!.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved);
        AssociatedObject!.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, handledEventsToo: true);
    }

    protected override void OnDetaching()
    {
        AssociatedObject!.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        AssociatedObject!.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
        AssociatedObject!.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
        base.OnDetaching();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(AssociatedObject).Properties.IsLeftButtonPressed)
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
        if (dragStartPoint is not { } start || pressedArgs is not { } savedArgs)
            return;
        if (AssociatedObject?.DataContext is not AssetViewModel asset)
            return;

        var current = e.GetPosition(AssociatedObject);
        if (Math.Abs(current.X - start.X) < DragThreshold && Math.Abs(current.Y - start.Y) < DragThreshold)
            return;

        Reset();
        try
        {
            await global::Avalonia.Input.DragDrop.DoDragDropAsync(savedArgs, AssetDragDropHelper.CreateData([asset]), DragDropEffects.Link);
        }
        catch (Exception)
        {
            // A drag the platform refuses (no drag source on a headless or embedded host) leaves the item as it was
        }
    }

    private void Reset()
    {
        dragStartPoint = null;
        pressedArgs = null;
    }
}
