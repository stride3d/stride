// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Games;

#if STRIDE_PLATFORM_ANDROID || STRIDE_PLATFORM_IOS
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Games;
#endif

#if STRIDE_PLATFORM_IOS
using UIKit;
#endif

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
                try
                {
                    // Use headless context when not in interactive mode (no window/display needed)
                    var context = game is GameTestBase { ScreenShotAutomationEnabled: true }
                        ? GameContextFactory.NewGameContext(AppContextType.Headless)
                        : null; // null = auto-detect (creates a window)
                    game.Run(context);
                }
                finally
                {
                    // End/Discard RenderDoc capture while the device is still alive (before Dispose/Destroy)
                    if (game is GameTestBase testGame)
                        testGame.EndOrDiscardRenderDocCapture();
                }
            }

            // Log process memory for diagnostics (helps detect resource leaks across tests)
            using var process = Process.GetCurrentProcess();
            Logger.Info($"Process memory: working set={process.WorkingSet64 / 1024 / 1024}MB, private={process.PrivateMemorySize64 / 1024 / 1024}MB, GC={GC.GetTotalMemory(false) / 1024 / 1024}MB");

#elif STRIDE_PLATFORM_UWP

            throw new NotImplementedException();

#elif STRIDE_PLATFORM_IOS || STRIDE_PLATFORM_ANDROID

#if STRIDE_PLATFORM_ANDROID || STRIDE_PLATFORM_IOS
            // Headless path mirrors desktop: when ScreenShotAutomationEnabled (set from
            // !ForceInteractiveMode), run offscreen in-process — no separate Activity (Android)
            // or SDL-over-Avalonia window switch (iOS), no per-test process. Interactive runs
            // fall through to the SDL-window path below for visual inspection.
            if (game is GameTestBase { ScreenShotAutomationEnabled: true })
            {
                try
                {
                    var context = GameContextFactory.NewGameContext(AppContextType.Headless);
                    game.Run(context);
                }
                finally
                {
                    // Swallow Dispose so the original Run exception (if any) propagates.
                    try { game.Dispose(); } catch { }
                }
                return;
            }
#endif
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
                    // Marshal to the iOS main thread: SDL_Init/SDL_CreateWindow + UIKit gesture
                    // recognizer setup must run on the main thread. InvokeOnMainThread blocks
                    // the calling (test) thread until the lambda returns, and the lambda contains
                    // the blocking game.Run call — so test-thread synchronization is implicit.
                    // game.Exit() (called by the test or by the edge-swipe gesture handler) ends
                    // the game loop, the lambda returns, control flows back to the test thread.
                    // Avalonia.iOS's run loop is paused while we hold the main thread; that's fine
                    // for an interactive test session (the user is looking at the game, not the
                    // runner UI). When the SDL window is disposed, iOS makes Avalonia's window key
                    // again automatically.
                    Exception runError = null;
                    UIApplication.SharedApplication.InvokeOnMainThread(() =>
                    {
                        Stride.Graphics.SDL.Window sdlWindow = null;
                        UIWindow keyWindow = null;
                        UIScreenEdgePanGestureRecognizer swipe = null;
                        try
                        {
                            // SDL2 detects the already-running UIApplication (Avalonia.iOS) and
                            // creates only a new UIWindow rather than installing its own delegate.
                            sdlWindow = new Stride.Graphics.SDL.Window("Stride Tests");

                            // iOS-standard left-edge swipe to exit (mirrors Android's system back
                            // gesture). Attach to the now-key window — that's SDL's window since
                            // SDL_CreateWindow makes the new window key on creation.
                            keyWindow = UIApplication.SharedApplication.KeyWindow;
                            if (keyWindow != null)
                            {
                                swipe = new UIScreenEdgePanGestureRecognizer(g =>
                                {
                                    if (g.State == UIGestureRecognizerState.Ended) game.Exit();
                                }) { Edges = UIRectEdge.Left };
                                keyWindow.AddGestureRecognizer(swipe);
                            }

                            var gameContext = new GameContextiOS(sdlWindow);
                            game.Run(gameContext);  // blocks main thread; returns on game.Exit()
                        }
                        catch (Exception ex) { runError = ex; }
                        finally
                        {
                            if (keyWindow != null && swipe != null) keyWindow.RemoveGestureRecognizer(swipe);
                            sdlWindow?.Dispose();
                        }
                    });
                    if (runError != null) ExceptionDispatchInfo.Capture(runError).Throw();
                    // The Android path below uses tcs.Task.Wait — iOS doesn't need it because
                    // InvokeOnMainThread already blocked us until game.Run completed.
#elif STRIDE_PLATFORM_ANDROID
                    // Start activity
                    AndroidGameTestActivity.GameToStart = game;
                    AndroidGameTestActivity.Destroyed += gameFinishedCallback;
                    PlatformAndroid.Context.StartActivity(typeof(AndroidGameTestActivity));
                    // Wait for completion
                    // TODO: Should we put a timeout and issue a Game.Exit() in main thread if too long?
                    tcs.Task.Wait();
#endif

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
#if STRIDE_PLATFORM_ANDROID
                    AndroidGameTestActivity.Destroyed -= gameFinishedCallback;
                    AndroidGameTestActivity.GameToStart = null;
#endif
                }
            }
#endif
        }
    }
}
