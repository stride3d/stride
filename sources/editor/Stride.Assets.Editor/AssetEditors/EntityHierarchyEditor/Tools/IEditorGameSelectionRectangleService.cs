// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;

/// <summary>
/// The rectangle selection of the view of a scene or prefab editor: a left drag that a click there would give to the
/// selection draws a rectangle, and its release selects the entities the rectangle holds (Shift adds them, Ctrl toggles
/// them). The user interface that draws the rectangle over the view turns it on.
/// </summary>
public interface IEditorGameSelectionRectangleService : IEditorGameViewModelService
{
    /// <summary>
    /// Gets or sets whether a left drag draws a selection rectangle (any thread). Off by default.
    /// </summary>
    bool IsRectangleSelectionEnabled { get; set; }

    /// <summary>
    /// Raised when the rectangle being drawn changes (game thread): the rectangle in normalized view coordinates ((0, 0) top
    /// left, (1, 1) bottom right) of the active view, or <c>null</c> once it is released or canceled.
    /// </summary>
    event EventHandler<RectangleF?>? SelectionRectangleChanged;
}
