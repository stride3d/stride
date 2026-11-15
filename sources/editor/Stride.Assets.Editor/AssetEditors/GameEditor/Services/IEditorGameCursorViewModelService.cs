// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Editor.AssetEditors.GameEditor.Services;

/// <summary>
/// The game service that shows a cursor over the view of the game and knows when a press of the pointer becomes a drag,
/// for the editor games that handle the pointer themselves (the adorners of the UI editor). Each editor implements it for
/// its view, for the game of every editor.
/// </summary>
/// <remarks>
/// The pointer position over the view is given by <see cref="IEditorGameMousePositionViewModelService"/>.
/// </remarks>
public interface IEditorGameCursorViewModelService : IEditorGameViewModelService
{
    /// <summary>
    /// Gets the distance, in pixels, a pointer moves while pressed before it counts as a drag (any thread).
    /// </summary>
    Vector2 MinimumDragDistance { get; }

    /// <summary>
    /// Changes the cursor shown over the view of the game (any thread).
    /// </summary>
    /// <param name="cursor">The cursor to show; <see cref="EditorGameCursor.Default"/> gives the view its default cursor back.</param>
    void ChangeCursor(EditorGameCursor cursor);
}
