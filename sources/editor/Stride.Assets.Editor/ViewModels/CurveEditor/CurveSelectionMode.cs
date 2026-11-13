// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Assets.Editor.ViewModels.CurveEditor;

/// <summary>
/// How a click or a selection rectangle of the curve editor changes the selected control points. The view chooses it from the
/// modifier keys held (<see cref="FromModifiers"/>).
/// </summary>
public enum CurveSelectionMode
{
    /// <summary>
    /// The points replace the selection; a click on empty space clears it (no modifier key).
    /// </summary>
    Replace,

    /// <summary>
    /// The points are added to the selection (Shift).
    /// </summary>
    Add,

    /// <summary>
    /// The points are removed from the selection (Ctrl).
    /// </summary>
    Remove,

    /// <summary>
    /// The selection doesn't change (any other combination of modifier keys, for example Alt to add a point).
    /// </summary>
    None,
}

/// <summary>
/// Helpers for <see cref="CurveSelectionMode"/>.
/// </summary>
public static class CurveSelectionModes
{
    /// <summary>
    /// The selection mode of the modifier keys held, as the WPF curve editor read them: none replaces, Shift alone adds, Ctrl alone
    /// removes, anything else keeps the selection.
    /// </summary>
    /// <param name="shift">Whether Shift is held.</param>
    /// <param name="control">Whether Ctrl is held.</param>
    /// <param name="other">Whether any other modifier key (Alt, the Windows or Command key) is held.</param>
    public static CurveSelectionMode FromModifiers(bool shift, bool control, bool other)
    {
        if (other)
            return CurveSelectionMode.None;

        return (shift, control) switch
        {
            (false, false) => CurveSelectionMode.Replace,
            (true, false) => CurveSelectionMode.Add,
            (false, true) => CurveSelectionMode.Remove,
            _ => CurveSelectionMode.None,
        };
    }
}
