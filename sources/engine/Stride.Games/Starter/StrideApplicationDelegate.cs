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
    public class StrideApplicationDelegate : UIApplicationDelegate
    {
        protected GameBase Game { get; set; }

        private Window sdlWindow;

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            if (Game == null)
                throw new InvalidOperationException("Game must be assigned before base.FinishedLaunching.");

            sdlWindow = new Window("Stride");

            // Game.Run blocks; iOS needs FinishedLaunching to return for launch to complete.
            BeginInvokeOnMainThread(() =>
            {
                try { Game.Run(new GameContextiOS(sdlWindow)); }
                finally { sdlWindow?.Dispose(); sdlWindow = null; }
            });

            return true;
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
}
#endif
