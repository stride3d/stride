// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_IOS
using System;
using CoreGraphics;
using Foundation;
using OpenTK;
using UIKit;
using Stride.Games;

namespace Stride.Starter
{
    public class StrideApplicationDelegate : UIApplicationDelegate
    {
        /// <summary>
        /// The instance of the game to run.
        /// </summary>
	    protected GameBase Game;
		
		/// <summary>
		/// The main windows of the application.
		/// </summary>
        protected UIWindow MainWindow { get; private set; }
		
        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
			if (Game == null)
				throw new InvalidOperationException("Please set 'Game' to a valid instance of Game before calling this method.");
				
            var bounds = UIScreen.MainScreen.Bounds;

            // create the game main windows
            MainWindow = new UIWindow(bounds);

            var strideGameView = CreateView(bounds);

            var strideGameController = CreateViewController(strideGameView);

            // create the game context
            var gameContext = new GameContextiOS(new iOSWindow(MainWindow, strideGameView, strideGameController));

            // Force fullscreen
            UIApplication.SharedApplication.SetStatusBarHidden(true, false);

            // Added UINavigationController to switch between UIViewController because the game is killed if the FinishedLaunching (in the AppDelegate) method doesn't return true in 10 sec.
            var navigationController = new UINavigationController {NavigationBarHidden = true};
            navigationController.PushViewController(gameContext.Control.GameViewController, false);
            MainWindow.RootViewController = navigationController;

            // launch the main window
            MainWindow.MakeKeyAndVisible();

            // launch the game
            Game.Run(gameContext);

            return Game.IsRunning;
        }

        protected virtual iOSStrideView CreateView(CGRect bounds, nfloat? contentScaleFactor = null)
        {
            // create the stride game view 
            var rect = new System.Drawing.RectangleF((float)bounds.X, (float)bounds.Y, (float)bounds.Height, (float)bounds.Width);
            return new iOSStrideView(rect) { ContentScaleFactor = contentScaleFactor ?? UIScreen.MainScreen.Scale };
        }

        protected virtual StrideGameController CreateViewController(iOSStrideView strideGameView)
        {
            // create the view controller used to display the stride game
            return new StrideGameController { View = strideGameView };
        }
    }
}

#endif
