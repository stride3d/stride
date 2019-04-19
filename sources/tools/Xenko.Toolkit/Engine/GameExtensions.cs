using Xenko.Games;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xenko.Toolkit.Engine
{
    /// <summary>
    /// Extensions for <see cref="IGame"/>
    /// </summary>
    public static class GameExtensions
    {
        /// <summary>
        /// Gets the elapsed total seconds since last update.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <returns>Elapsed update time in seconds.</returns>
        /// <exception cref="ArgumentNullException">If the game argument is null.</exception>
        public static float GetDeltaTime(this IGame game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            return (float)game.UpdateTime.Elapsed.TotalSeconds;
        }

        /// <summary>
        /// Exits the game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <exception cref="ArgumentNullException">If the game argument is null.</exception>
        /// /// <exception cref="ArgumentException">If the game argument does not inherit form <see cref="GameBase"/>.</exception>
        public static void Exit(this IGame game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            if (game is GameBase gameBase)
            {
                gameBase.Exit();
            }
            else
            {
                throw new ArgumentException($"The argument {nameof(game)} does not inherit from {nameof(GameBase)}.", nameof(game));
            }
        }

    }
}
