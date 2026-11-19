// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Assets.Editor.EditorGame.Tools;

/// <summary>
/// How a rectangle drawn in the view changes the selection.
/// </summary>
public enum RectangleSelectionMode
{
    /// <summary>
    /// The entities in the rectangle become the selection (no key held).
    /// </summary>
    Replace,

    /// <summary>
    /// The entities in the rectangle are added to the selection (Shift).
    /// </summary>
    Add,

    /// <summary>
    /// The entities in the rectangle that are selected leave the selection, the others join it, as Ctrl+click does (Ctrl).
    /// </summary>
    Toggle,
}

/// <summary>
/// The rectangle selection of a view: which entities a rectangle drawn in the view holds, and how they change the selection.
/// </summary>
/// <remarks>
/// The rectangles are in the normalized coordinates of the view, as the mouse position of the game: (0, 0) is the top left
/// corner, (1, 1) the bottom right one.
/// </remarks>
internal static class RectangleSelection
{
    /// <summary>
    /// Gets the rectangle between two corners, in any order.
    /// </summary>
    public static RectangleF FromCorners(Vector2 a, Vector2 b)
    {
        var min = Vector2.Min(a, b);
        var max = Vector2.Max(a, b);
        return new RectangleF(min.X, min.Y, max.X - min.X, max.Y - min.Y);
    }

    /// <summary>
    /// Gets the mode of a rectangle from the keys held when it is released: Ctrl toggles, as Ctrl+click does; Shift adds.
    /// </summary>
    public static RectangleSelectionMode ModeOf(bool isCtrlDown, bool isShiftDown)
    {
        return isCtrlDown ? RectangleSelectionMode.Toggle : isShiftDown ? RectangleSelectionMode.Add : RectangleSelectionMode.Replace;
    }

    /// <summary>
    /// Indicates whether a box of the world is entirely in a rectangle of the view: its eight corners are in front of the
    /// camera and inside the rectangle. A floor or a wall that the rectangle only crosses is not selected.
    /// </summary>
    /// <param name="rectangle">The rectangle, in normalized view coordinates.</param>
    /// <param name="bounds">The box, in world space.</param>
    /// <param name="viewProjection">The view and projection matrices of the camera of the view.</param>
    public static bool Contains(RectangleF rectangle, BoundingBox bounds, Matrix viewProjection)
    {
        foreach (var corner in bounds.GetCorners())
        {
            if (!Contains(rectangle, corner, viewProjection))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Indicates whether a point of the world is in a rectangle of the view: in front of the camera, and inside the
    /// rectangle once projected.
    /// </summary>
    /// <param name="rectangle">The rectangle, in normalized view coordinates.</param>
    /// <param name="point">The point, in world space.</param>
    /// <param name="viewProjection">The view and projection matrices of the camera of the view.</param>
    public static bool Contains(RectangleF rectangle, Vector3 point, Matrix viewProjection)
    {
        var clip = Vector4.Transform(new Vector4(point, 1.0f), viewProjection);
        if (clip.W <= MathUtil.ZeroTolerance)
            return false;

        var x = (clip.X / clip.W + 1.0f) * 0.5f;
        var y = (1.0f - clip.Y / clip.W) * 0.5f;
        return x >= rectangle.Left && x <= rectangle.Right && y >= rectangle.Top && y <= rectangle.Bottom;
    }

    /// <summary>
    /// Changes a selection with the items a rectangle holds.
    /// </summary>
    /// <param name="selection">The selection to change.</param>
    /// <param name="items">The items in the rectangle.</param>
    /// <param name="mode">How the rectangle changes the selection.</param>
    /// <returns><c>true</c> when the selection changed.</returns>
    public static bool Apply(ICollection<object> selection, IReadOnlyCollection<object> items, RectangleSelectionMode mode)
    {
        var changed = false;
        switch (mode)
        {
            case RectangleSelectionMode.Replace:
                foreach (var item in selection.Where(x => !items.Contains(x)).ToList())
                    changed |= selection.Remove(item);
                foreach (var item in items.Where(x => !selection.Contains(x)))
                {
                    selection.Add(item);
                    changed = true;
                }
                break;
            case RectangleSelectionMode.Add:
                foreach (var item in items.Where(x => !selection.Contains(x)))
                {
                    selection.Add(item);
                    changed = true;
                }
                break;
            case RectangleSelectionMode.Toggle:
                foreach (var item in items)
                {
                    if (!selection.Remove(item))
                        selection.Add(item);
                    changed = true;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
        return changed;
    }
}
