// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Games;

namespace Stride.Editor.Engine;

/// <summary>
/// The view an editor game renders into: the game has no window of its own, the viewport shows the frames it draws
/// and gives it the input of the view.
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
    /// <param name="game">The game, which this viewport becomes the <see cref="EmbeddedGame.Host"/> of.</param>
    /// <returns>The context to run the game with.</returns>
    GameContext Attach(EmbeddedGame game);
}
