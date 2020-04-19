// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Games
{
    /// <summary>
    /// Interface for a game platform (OS, machine dependent).
    /// </summary>
    public interface IGamePlatform
    {
        /// <summary>
        /// Gets the default app directory.
        /// </summary>
        /// <value>The default app directory.</value>
        string DefaultAppDirectory { get; }

        /// <summary>
        /// Gets the main window.
        /// </summary>
        /// <value>The main window.</value>
        GameWindow MainWindow { get; }

        /// <summary>
        /// Creates the a new <see cref="GameWindow"/>. See remarks.
        /// </summary>
        /// <param name="gameContext">The window context. See remarks.</param>
        /// <returns>A new game window.</returns>
        /// <remarks>
        /// This is currently only supported on Windows Desktop. The window context supported on windows is a subclass of System.Windows.Forms.Control (or null and a default GameForm will be created).
        /// </remarks>
        GameWindow CreateWindow(GameContext gameContext = null);
    }
}
