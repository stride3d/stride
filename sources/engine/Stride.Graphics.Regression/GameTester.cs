// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_ANDROID || STRIDE_PLATFORM_IOS
using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

#if STRIDE_PLATFORM_IOS
using UIKit;
#endif

using Stride.Games;
#endif

using Stride.Core.Diagnostics;
using Stride.Engine;

namespace Stride.Graphics.Regression
{
    /// <summary>
    ///   Provides functionality to execute game tests in a platform-specific environment.
    /// </summary>
    public class GameTester
    {
        public static readonly Logger Logger = GlobalLogger.GetLogger("GameTester");

#if STRIDE_PLATFORM_ANDROID || STRIDE_PLATFORM_IOS
        private static object uniThreadLock = new();
#endif

        /// <summary>
        ///   Runs the specified game test in a platform-specific environment.
        /// </summary>
        /// <param name="game">The <see cref="Game"/> instance to run. This parameter cannot be <see langword="null"/>.</param>
        /// <remarks>
        ///   This method handles the execution of the <paramref name="game"/> differently depending
        ///   on the target platform:
        ///   <list type="bullet">
        ///     <item>
        ///       On desktop platforms, the game is executed in a blocking manner.
        ///     </item>
        ///     <item>
        ///       On iOS and Android platforms, the game is executed within a platform-specific activity or view,
        ///       with proper handling for game completion and exceptions.
        ///       It also uses synchronization mechanisms to ensure thread safety and waits for the game to
        ///       complete before returning.
        ///     </item>
        ///   </list>
        /// </remarks>
        public static void RunGameTest(Game game)
        {
#if STRIDE_PLATFORM_DESKTOP

            using (game)
            {
                game.Run();
            }

#elif STRIDE_PLATFORM_UWP

            throw new NotImplementedException();

#elif STRIDE_PLATFORM_IOS || STRIDE_PLATFORM_ANDROID

            lock(uniThreadLock)
            {
                // Prepare finish callback
                var tcs = new TaskCompletionSource<bool>();
                EventHandler<EventArgs> gameFinishedCallback = (sender, e) =>
                {
                    // Notify waiters that the Game has exited
                    Logger.Info("Game finished.");
                    tcs.TrySetResult(true);
                };

                EventHandler<GameUnhandledExceptionEventArgs> exceptionhandler = (sender, e) =>
                {
                    // Notify waiters that the Game has thrown an exception
                    Logger.Error($"Game finished with exception = {e}.");
                    tcs.TrySetException((Exception) e.ExceptionObject);
                };

                // Transmit data to activity
                // TODO: Avoid static with string intent + Dictionary?
                try
                {
                    game.UnhandledException += exceptionhandler;

                    Logger.Info(@"Starting activity");
#if STRIDE_PLATFORM_IOS
                    game.Exiting += gameFinishedCallback;

                    UIApplication.SharedApplication.InvokeOnMainThread(() =>
                    {
                        var window = UIApplication.SharedApplication.KeyWindow;
                        var rootNavigationController = (UINavigationController)window.RootViewController;

                        /*// Create the Stride Game View
                        var bounds = UIScreen.MainScreen.Bounds;
                        var strideGameView = new iOSStrideView((System.Drawing.RectangleF)bounds) { ContentScaleFactor = UIScreen.MainScreen.Scale };

                        // Create the view controller used to display the Stride Game
                        var strideGameController = new iOSGameTestController(game) { View = strideGameView };

                        // Create the Game Context
                        var gameContext = new GameContextiOS(new iOSWindow(window, strideGameView, strideGameController));

                        // Push view
                        rootNavigationController.PushViewController(gameContext.Control.GameViewController, false);

                        // Launch the game
                        game.Run(gameContext);*/
                        throw new NotImplementedException();

                    });
#elif STRIDE_PLATFORM_ANDROID
                    // Start activity
                    AndroidGameTestActivity.GameToStart = game;
                    AndroidGameTestActivity.Destroyed += gameFinishedCallback;
                    PlatformAndroid.Context.StartActivity(typeof(AndroidGameTestActivity));
#endif
                    // Wait for completion
                    // TODO: Should we put a timeout and issue a Game.Exit() in main thread if too long?
                    tcs.Task.Wait();

                    Logger.Info(@"Activity ended");
                }
                catch (AggregateException e)
                {
                    // Unwrap aggregate exceptions
                    if (e.InnerExceptions.Count == 1)
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                }
                finally
                {
#if STRIDE_PLATFORM_IOS
                    // iOS cleanup
                    UIApplication.SharedApplication.InvokeOnMainThread(() =>
                    {
                        var window = UIApplication.SharedApplication.KeyWindow;
                        var rootNavigationController = (UINavigationController) window.RootViewController;

                        rootNavigationController.PopViewController(false);
                    });
#elif STRIDE_PLATFORM_ANDROID
                    AndroidGameTestActivity.Destroyed -= gameFinishedCallback;
                    AndroidGameTestActivity.GameToStart = null;
#endif
                }
            }
#endif
        }
    }
}
