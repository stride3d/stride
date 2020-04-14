// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Engine;
using Stride.Games;

namespace Stride.Audio.Tests.Engine
{
    /// <summary>
    /// Utilities to perform tests in a Game context.
    /// </summary>
    class TestUtilities
    {
        /// <summary>
        /// Create a game instance and run it.
        /// </summary>
        /// <param name="onLoad">The callback that will be called in the <see cref="Game.LoadContent"/> method.</param>
        /// <param name="onBeforeUpdate">The callback that will be called before each <see cref="GameBase.Update"/> calls.</param>
        /// <param name="onAfterUpdate">The callback that will be called after each <see cref="GameBase.Update"/> calls.</param>
        /// <param name="onBeforeDraw">The callback that will be called before each <see cref="GameBase.Draw"/> calls.</param>
        /// <param name="onAfterDraw">The callback that will be called before each <see cref="GameBase.Draw"/> calls.</param>
        static public void CreateAndRunGame(Action<Game> onLoad, Action<Game> onBeforeUpdate, Action<Game> onAfterUpdate = null, Action<Game> onBeforeDraw = null, Action<Game> onAfterDraw = null)
        {
            using (var game = new GameClassForTests())
            {
                game.LoadingContent += onLoad;
                game.BeforeUpdating += onBeforeUpdate;
                game.AfterUpdating += onAfterUpdate;
                game.BeforeDrawing += onBeforeDraw;
                game.AfterDrawing += onAfterDraw;

                game.Run();
            }
        }

        /// <summary>
        /// Utility function to quit the game directly.
        /// </summary>
        /// <remarks>Can be used as parameter for function <see cref="CreateAndRunGame"/>.</remarks>
        /// <param name="game">The <see cref="Game"/> instance.</param>
        static public void ExitGame(Game game)
        {
            game.Exit();
        }

        /// <summary>
        /// Utility function to quit the game after a given time.
        /// </summary>
        /// <remarks>Can be used as parameter for function <see cref="CreateAndRunGame"/>.</remarks>
        /// <param name="sleepTimeMilli">The time to sleep in milliseconds.</param>
        static public Action<Game, int, int> ExitGameAfterSleep(int sleepTimeMilli)
        {
            return (game, dump1, dump2) =>
                {
                    if (game.UpdateTime.Total.TotalMilliseconds > sleepTimeMilli)
                        game.Exit();
                };
        }

        /// <summary>
        /// A utility class which automatically count the number of calls to update in the game and calls the callbacks provided at construction at each Game Update.
        /// </summary>
        class LoopCountClass
        {
            private readonly Action<Game, int, int> oneLoopTurnActionBfrUpdate;
            private readonly Action<Game, int, int> oneLoopTurnActionAftUpdate;

            public LoopCountClass(Action<Game, int, int> oneLoopTurnActionBfrUpdate, Action<Game, int, int> oneLoopTurnActionAftUpdate)
            {
                this.oneLoopTurnActionBfrUpdate = oneLoopTurnActionBfrUpdate;
                this.oneLoopTurnActionAftUpdate = oneLoopTurnActionAftUpdate;
            }

            private int loopCount;
            private int loopCountSum;

            public void OneLoopTurnActionBfrUpdate(Game game)
            {
                oneLoopTurnActionBfrUpdate?.Invoke(game, loopCount, loopCountSum);
            }
            public void OneLoopTurnActionAftUpdate(Game game)
            {
                oneLoopTurnActionAftUpdate?.Invoke(game, loopCount, loopCountSum);

                ++loopCount;
                loopCountSum += loopCount;
            }
        }

        /// <summary>
        /// Create a game instance and run it. 
        /// At each update call, the provided <paramref name="oneLoopTurnActionBfrUpdate"/> and <paramref name="oneLoopTurnActionAftUpdate"/> 
        /// functions will be given as parameters respectly the <see cref="Game"/> instance, the loopCount counter and the loopCount sum.
        /// </summary>
        /// <param name="onLoading">The callback that will be called in the <see cref="Game.LoadContent"/> method.</param>
        /// <param name="oneLoopTurnActionBfrUpdate">The callback that will be called before each <see cref="GameBase.Update"/> calls.</param>
        /// <param name="oneLoopTurnActionAftUpdate">The callback that will be called after each <see cref="GameBase.Update"/> calls.</param>
        static public void ExecuteScriptInUpdateLoop(Action<Game> onLoading, Action<Game, int, int> oneLoopTurnActionBfrUpdate, Action<Game, int, int> oneLoopTurnActionAftUpdate = null)
        {
            var loopCountClass = new LoopCountClass(oneLoopTurnActionBfrUpdate, oneLoopTurnActionAftUpdate);
            CreateAndRunGame(onLoading, loopCountClass.OneLoopTurnActionBfrUpdate, loopCountClass.OneLoopTurnActionAftUpdate);
        }

        /// <summary>
        /// Create a game instance and run it. 
        /// At each draw call, the provided <paramref name="oneLoopTurnActionBfrUpdate"/> and <paramref name="oneLoopTurnActionAftUpdate"/> 
        /// functions will be given as parameters respectly the <see cref="Game"/> instance, the loopCount counter and the loopCount sum.
        /// </summary>
        /// <param name="onLoading">The callback that will be called in the <see cref="Game.LoadContent"/> method.</param>
        /// <param name="oneLoopTurnActionBfrUpdate">The callback that will be called before each <see cref="GameBase.Update"/> calls.</param>
        /// <param name="oneLoopTurnActionAftUpdate">The callback that will be called after each <see cref="GameBase.Update"/> calls.</param>
        static public void ExecuteScriptInDrawLoop(Action<Game> onLoading, Action<Game, int, int> oneLoopTurnActionBfrUpdate, Action<Game, int, int> oneLoopTurnActionAftUpdate = null)
        {
            var loopCountClass = new LoopCountClass(oneLoopTurnActionBfrUpdate, oneLoopTurnActionAftUpdate);
            CreateAndRunGame(onLoading, null, null, loopCountClass.OneLoopTurnActionBfrUpdate, loopCountClass.OneLoopTurnActionAftUpdate);
        }
    }
}
