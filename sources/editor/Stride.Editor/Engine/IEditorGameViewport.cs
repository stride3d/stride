// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using Stride.Core.Mathematics;
using Stride.Games;

namespace Stride.Editor.Engine;

/// <summary>
/// The view an editor game renders into. In the Avalonia editor the game has no window of its own: the viewport shows
/// the frames it draws and gives it the input of the view. In the WPF editor the game renders into a WinForms form
/// that the viewport shows.
/// </summary>
/// <remarks>
/// A viewport serves one game. It is created on the UI thread (<see cref="IEditorGameViewportService.CreateViewport"/>),
/// then attached to its game and disposed on the game thread.
/// </remarks>
public interface IEditorGameViewport : IEmbeddedGameHost, IDisposable
{
    /// <summary>
    /// Gets the control of the UI toolkit that shows the game.
    /// </summary>
    object View { get; }

    /// <summary>
    /// Prepares the game to render into this viewport (game thread, before the game runs and creates its device).
    /// </summary>
    /// <param name="game">The game, which this viewport becomes the <see cref="EmbeddedGame.Host"/> of when the game has no window.</param>
    /// <returns>The context to run the game with.</returns>
    GameContext Attach(EmbeddedGame game);

    /// <summary>
    /// Releases the control that shows the game (UI thread), when the controller of the game is destroyed, before the
    /// game exits. Does nothing by default.
    /// </summary>
    void ReleaseView()
    {
    }

    /// <summary>
    /// Gets where the pointer is over the view, as a fraction of the size of the view from its top left corner (UI
    /// thread), or <c>null</c> when the view doesn't know it. A pointer outside the view gives a point outside 0 to 1.
    /// </summary>
    /// <remarks>
    /// The editors place what is dropped or pasted on the view under it (see <c>IEditorGameMousePositionViewModelService</c>).
    /// <c>null</c> by default.
    /// </remarks>
    Vector2? PointerPosition => null;

    /// <summary>
    /// Gets where the last right click on the view was, which opened the menu of the view, as a fraction of the size of
    /// the view from its top left corner (UI thread), or <c>null</c> when there was none. <c>null</c> by default.
    /// </summary>
    Vector2? LastRightClickPosition => null;
}
