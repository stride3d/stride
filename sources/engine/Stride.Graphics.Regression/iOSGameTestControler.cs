// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_IOS

using Xenko.Games;

namespace Xenko.Graphics.Regression
{
    public class iOSGameTestController : XenkoGameController
    {
        private readonly GameBase game;

        public iOSGameTestController(GameBase game)
        {
            this.game = game;
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            if (game != null)
                game.Dispose();
        }
    }
}

#endif
