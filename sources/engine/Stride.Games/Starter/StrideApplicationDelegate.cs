// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_IOS
using System;
using Foundation;
using UIKit;
using Stride.Games;
using Stride.Graphics.SDL;

namespace Stride.Starter
{
    /// <summary>
    /// UIApplicationDelegate base for Stride iOS games. Subclass and assign <see cref="Game"/>
    /// in <c>FinishedLaunching</c> before calling <c>base.FinishedLaunching</c>; the base creates
    /// an SDL window + iOS GameContext and schedules <see cref="GameBase.Run"/> on the next
    /// main-loop tick so launch finishes before the (blocking) game loop takes over.
    /// </summary>
    /// <remarks>
    /// When the Info.plist has a <c>UIApplicationSceneManifest</c>, the game starts when its scene connects.
    /// The build adds an empty one unless the project sets <c>StrideUISceneManifest</c> to <c>false</c>.
    /// </remarks>
    public class StrideApplicationDelegate : UIApplicationDelegate
    {
        protected GameBase Game { get; set; }

        private Window sdlWindow;

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            if (Game == null)
                throw new InvalidOperationException("Game must be assigned before base.FinishedLaunching.");

            // A scene app has no scene yet at this point, and UIKit shows no window outside one.
            if (NSBundle.MainBundle.ObjectForInfoDictionary("UIApplicationSceneManifest") == null)
                StartGame();

            return true;
        }

        public override UISceneConfiguration GetConfiguration(UIApplication application, UISceneSession connectingSceneSession, UISceneConnectionOptions options)
        {
            // The Info.plist's configuration, when it has one: a scene delegate it names starts the game itself.
            var configuration = (UISceneConfiguration)connectingSceneSession.Configuration.Copy();
            if (configuration.DelegateClass is null)
                configuration.DelegateType = typeof(StrideSceneDelegate);
            return configuration;
        }

        /// <summary>
        /// Starts <see cref="Game"/> in a new SDL window; a scene delegate named in the Info.plist calls it when its scene connects.
        /// </summary>
        public void StartGame()
        {
            // One game per app: a second scene doesn't start another.
            if (sdlWindow != null)
                return;

            sdlWindow = new Window("Stride");

            // Game.Run blocks; iOS needs FinishedLaunching (or the scene connection) to return first.
            BeginInvokeOnMainThread(() =>
            {
                try { Game.Run(new GameContextiOS(sdlWindow)); }
                finally { sdlWindow?.Dispose(); sdlWindow = null; }
            });
        }

        public override void WillTerminate(UIApplication application)
        {
            Game?.Exit();
            Game?.Dispose();
            Game = null;
            sdlWindow?.Dispose();
            sdlWindow = null;
        }
    }

    internal sealed class StrideSceneDelegate : UIResponder, IUIWindowSceneDelegate
    {
        [Export("scene:willConnectToSession:options:")]
        public void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
        {
            if (scene is UIWindowScene && UIApplication.SharedApplication.Delegate is StrideApplicationDelegate app)
                app.StartGame();
        }
    }
}
#endif
