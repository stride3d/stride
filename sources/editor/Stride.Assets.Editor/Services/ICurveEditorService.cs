// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Assets.Editor.Services;

/// <summary>
/// Shows the curve editor of a session. Each editor (WPF, Avalonia) implements it with its own window or panel, around one
/// <see cref="ViewModels.CurveEditor.CurveEditorViewModel"/> per session.
/// </summary>
/// <remarks>
/// The "Edit curve" command of the property grid (<see cref="Quantum.NodePresenters.Commands.EditCurveCommand"/>) looks it up in
/// the session's services, and does nothing in an editor that has none.
/// </remarks>
public interface ICurveEditorService
{
    /// <summary>
    /// Shows the curve editor with the given curve: the curve is added to its list and selected, or only selected if it is
    /// already listed.
    /// </summary>
    /// <param name="curve">The curve, an <c>IComputeCurve&lt;T&gt;</c> of a type the editor supports (<see cref="ViewModels.CurveEditor.CurveEditorViewModel.CanEdit"/>).</param>
    /// <param name="name">The name of the curve in the list.</param>
    void Open(object curve, string name);

    /// <summary>
    /// Closes the curve editor, if it is open, and empties its list.
    /// </summary>
    void Close();
}
