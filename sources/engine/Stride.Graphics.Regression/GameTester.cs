// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
#if STRIDE_PLATFORM_IOS
using UIKit;
using Stride.Starter;
#endif
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

namespace Stride.Graphics.Regression
{
    public class GameTester
    {
        public static readonly Logger Logger = GlobalLogger.GetLogger("GameTester");

        private static object uniThreadLock = new object();

        public static void RunGameTest(Game game)
        {
#if STRIDE_PLATFORM_WINDOWS_DESKTOP

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
                    // Notify waiter that game has exited
                    Logger.Info("Game finished.");
                    tcs.TrySetResult(true);
                };

                EventHandler<GameUnhandledExceptionEventArgs> exceptionhandler = (sender, e) =>
                {
                    Logger.Info($"Game finished with exception ={e}.");
                    tcs.TrySetException((Exception)e.ExceptionObject);
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

                        // create the stride game view 
                        var bounds = UIScreen.MainScreen.Bounds;
                        var strideGameView = new iOSStrideView((System.Drawing.RectangleF)bounds) { ContentScaleFactor = UIScreen.MainScreen.Scale };

                        // create the view controller used to display the stride game
                        var strideGameController = new iOSGameTestController(game) { View = strideGameView };

                        // create the game context
                        var gameContext = new GameContextiOS(new iOSWindow(window, strideGameView, strideGameController));

                        // push view
                        rootNavigationController.PushViewController(gameContext.Control.GameViewController, false);

                        // launch the game
                        game.Run(gameContext);
                    });
#elif STRIDE_PLATFORM_ANDROID
                    // Start activity
                    AndroidGameTestActivity.GameToStart = game;
                    AndroidGameTestActivity.Destroyed += gameFinishedCallback;
                    PlatformAndroid.Context.StartActivity(typeof(AndroidGameTestActivity));
#endif
                    // Wait for completion of task
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
                    // iOS Cleanup
                    UIApplication.SharedApplication.InvokeOnMainThread(() =>
                    {
                        var window = UIApplication.SharedApplication.KeyWindow;
                        var rootNavigationController = (UINavigationController)window.RootViewController;

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
