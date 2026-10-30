// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Assets.Editor.ViewModels.SpriteEditor;

/// <summary>
/// The side or corner of a region that a handle of the sprite editor drags, or <see cref="Center"/> for the handle that moves it.
/// </summary>
/// <remarks>A copy of the WPF <c>Stride.Assets.Presentation.ViewModel.ResizingDirection</c>, until the consolidation.</remarks>
[Flags]
public enum ResizingDirection
{
    Center = 1,
    Left = 2,
    Top = 4,
    Right = 8,
    Bottom = 16,
    TopLeft = Left | Top,
    TopRight = Right | Top,
    BottomLeft = Left | Bottom,
    BottomRight = Right | Bottom
}

/// <summary>
/// A view model that a handle of the view resizes or moves by dragging: the start, the changes and the end of the drag, which
/// the target makes one entry of the undo history.
/// </summary>
/// <remarks>A copy of the WPF <c>Stride.Assets.Presentation.ViewModel.IResizingTarget</c>, until the consolidation.</remarks>
public interface IResizingTarget
{
    /// <summary>
    /// Notify the target once the resizing operation is completed.
    /// </summary>
    /// <param name="direction">The direction of the resizing.</param>
    /// <param name="horizontalChange">The total horizontal change during the dragging.</param>
    /// <param name="verticalChange">The total vertical change during the dragging.</param>
    void OnResizingCompleted(ResizingDirection direction, double horizontalChange, double verticalChange);

    /// <summary>
    /// Called on the target during the resizing operation.
    /// </summary>
    /// <param name="direction">The direction of the resizing.</param>
    /// <param name="horizontalChange">The horizontal delta of the resizing.</param>
    /// <param name="verticalChange">The vertical delta of the resizing.</param>
    void OnResizingDelta(ResizingDirection direction, double horizontalChange, double verticalChange);

    /// <summary>
    /// Notify the target that the resizing operation started.
    /// </summary>
    /// <param name="direction">The direction of the resizing.</param>
    void OnResizingStarted(ResizingDirection direction);
}
