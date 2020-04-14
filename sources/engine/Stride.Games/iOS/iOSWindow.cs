// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_IOS
using OpenTK.Platform.iPhoneOS;
using UIKit;

namespace Stride.Games
{
    /// <summary>
    /// Tuple of 3 elements that an iOS GameContext needs to hold on to.
    /// </summary>
    public struct iOSWindow {
    
        /// <summary>
        /// Initializes current struct with a UIWindow <paramref name="w"/>, a GameView <paramref name="g"/>and a controller <paramref name="c"/>.
        /// </summary>
        public iOSWindow(UIWindow w, iPhoneOSGameView g, StrideGameController c)
        {
            MainWindow = w;
            GameView = g;
            GameViewController = c;
        }

        /// <summary>
        /// The main window of the game.
        /// </summary>
        public readonly UIWindow MainWindow;

        /// <summary>
        /// The view in which the game is rendered.
        /// </summary>
        public readonly iPhoneOSGameView GameView;

        /// <summary>
        /// The controller of the game.
        /// </summary>
        public readonly StrideGameController GameViewController;
    }
}
#endif
