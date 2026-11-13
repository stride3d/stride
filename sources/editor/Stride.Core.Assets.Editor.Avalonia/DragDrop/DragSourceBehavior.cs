// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace Stride.Core.Assets.Editor.Avalonia.DragDrop;

/// <summary>
/// Starts a drag of the data context of its element when the pointer moves past a small distance with the left button
/// down. Attach a derived behavior to the root of the item template of a list.
/// </summary>
public abstract class DragSourceBehavior : StyledElementBehavior<Control>
{
    private const double DragThreshold = 4.0;
    private Point? dragStartPoint;
    private PointerPressedEventArgs? pressedArgs;

    /// <summary>
    /// The effects that the drop targets may apply.
    /// </summary>
    protected abstract DragDropEffects AllowedEffects { get; }

    /// <summary>
    /// Creates the data of a drag of the given data context.
    /// </summary>
    /// <returns>The data, or <c>null</c> when this data context is not dragged.</returns>
    protected abstract IDataTransfer? CreateData(object? dataContext);

    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject!.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, handledEventsToo: true);
        AssociatedObject!.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved);
        AssociatedObject!.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, handledEventsToo: true);
    }

    /// <inheritdoc />
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

        var current = e.GetPosition(AssociatedObject);
        if (Math.Abs(current.X - start.X) < DragThreshold && Math.Abs(current.Y - start.Y) < DragThreshold)
            return;

        Reset();
        if (CreateData(AssociatedObject?.DataContext) is not { } data)
            return;

        try
        {
            await global::Avalonia.Input.DragDrop.DoDragDropAsync(savedArgs, data, AllowedEffects);
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
