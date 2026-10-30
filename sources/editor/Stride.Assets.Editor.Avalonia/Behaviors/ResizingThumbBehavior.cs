// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Editor.ViewModels.SpriteEditor;

namespace Stride.Assets.Editor.Avalonia.Behaviors;

/// <summary>
/// Forwards the drag of a <see cref="Thumb"/> to a view model that it resizes or moves (<see cref="IResizingTarget"/>): a handle of
/// the region, borders or center of a sprite. The cross-platform counterpart of the WPF <c>ThumbBehavior</c>.
/// </summary>
public sealed class ResizingThumbBehavior : StyledElementBehavior<Thumb>
{
    public static readonly StyledProperty<ResizingDirection> DirectionProperty =
        AvaloniaProperty.Register<ResizingThumbBehavior, ResizingDirection>(nameof(Direction));

    public static readonly StyledProperty<IResizingTarget?> TargetProperty =
        AvaloniaProperty.Register<ResizingThumbBehavior, IResizingTarget?>(nameof(Target));

    private IResizingTarget? draggedTarget;

    /// <summary>
    /// The direction of the resizing.
    /// </summary>
    public ResizingDirection Direction
    {
        get => GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    /// <summary>
    /// The target of the resizing.
    /// </summary>
    public IResizingTarget? Target
    {
        get => GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    /// <summary>
    /// Whether a drag of the thumb is in progress.
    /// </summary>
    public bool IsDragging => draggedTarget is not null;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject!.DragStarted += OnDragStarted;
        AssociatedObject.DragDelta += OnDragDelta;
        AssociatedObject.DragCompleted += OnDragCompleted;
    }

    protected override void OnDetaching()
    {
        AssociatedObject!.DragCompleted -= OnDragCompleted;
        AssociatedObject.DragDelta -= OnDragDelta;
        AssociatedObject.DragStarted -= OnDragStarted;
        // A drag that the view ends (the editor closes, the tool changes) still closes its entry of the undo history
        EndDrag(default);
        base.OnDetaching();
    }

    private void OnDragStarted(object? sender, VectorEventArgs e)
    {
        if (draggedTarget is not null || Target is not { } target)
            return;

        draggedTarget = target;
        target.OnResizingStarted(Direction);
    }

    private void OnDragDelta(object? sender, VectorEventArgs e)
    {
        draggedTarget?.OnResizingDelta(Direction, e.Vector.X, e.Vector.Y);
    }

    private void OnDragCompleted(object? sender, VectorEventArgs e)
    {
        EndDrag(e.Vector);
    }

    private void EndDrag(Vector change)
    {
        var target = draggedTarget;
        draggedTarget = null;
        target?.OnResizingCompleted(Direction, change.X, change.Y);
    }
}
