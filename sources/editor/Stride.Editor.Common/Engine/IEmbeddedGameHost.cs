// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Editor.Engine
{
    /// <summary>
    /// The host of an <see cref="EmbeddedGame"/> that has no window of its own: it shows the frames the game draws, and
    /// gives the game the input it receives.
    /// </summary>
    /// <remarks>
    /// Every member is called on the game thread.
    /// </remarks>
    /// <seealso cref="EmbeddedGame.Host"/>
    public interface IEmbeddedGameHost
    {
        /// <summary>
        /// Called once the game is initialized, before its first frame.
        /// </summary>
        void Initialize(EmbeddedGame game);

        /// <summary>
        /// Called at the beginning of each frame the game draws, before the game prepares its graphics context.
        /// </summary>
        void BeginDraw(EmbeddedGame game);

        /// <summary>
        /// Called at the end of each frame, before the game submits the command list of the frame.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="present"><c>true</c> when the frame was drawn, <c>false</c> when the game skipped the draw.</param>
        void EndDraw(EmbeddedGame game, bool present);

        /// <summary>
        /// Called once the game submitted the command list of the frame.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="present"><c>true</c> when the frame was drawn, <c>false</c> when the game skipped the draw.</param>
        void AfterEndDraw(EmbeddedGame game, bool present);
    }
}
