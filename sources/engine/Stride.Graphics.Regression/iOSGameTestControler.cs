// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_IOS

using Stride.Games;

namespace Stride.Graphics.Regression
{
    public class iOSGameTestController : StrideGameController
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
