// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Games;

namespace Stride.Audio;

/// <summary>
/// The audio shortcut of a game, <c>Game.Audio</c>; a script reaches it through its <c>Game</c> property.
/// </summary>
public static class AudioExtensions
{
    extension(IGame game)
    {
        /// <summary>
        /// Gets the audio system.
        /// </summary>
        /// <exception cref="ServiceNotFoundException">The game has no audio system.</exception>
        public AudioSystem Audio => game.Services.GetSafeServiceAs<AudioSystem>();
    }
}
