// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Drawing;

namespace Stride.Assets.Editor.ViewModels.CurveEditor;

/// <summary>
/// A click on the curve area, the parameter of <see cref="CurveEditorViewModel.PreviewClickCommand"/>.
/// </summary>
/// <param name="Position">The position of the pointer, in the coordinates of the drawing.</param>
/// <param name="Mode">How the click changes the selection, from the modifier keys held.</param>
public readonly record struct CurveEditorClick(DrawingPoint Position, CurveSelectionMode Mode);

/// <summary>
/// A selection rectangle drawn on the curve area, the parameter of <see cref="CurveEditorViewModel.SelectCommand"/>.
/// </summary>
/// <param name="Rectangle">The rectangle, in the coordinates of the drawing.</param>
/// <param name="Mode">How the rectangle changes the selection, from the modifier keys held.</param>
public readonly record struct CurveEditorSelection(DrawingRect Rectangle, CurveSelectionMode Mode);
