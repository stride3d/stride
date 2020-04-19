// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_ANDROID
using System;
using Android.App;
using Android.OS;
using Stride.Engine;
using Stride.Starter;

namespace Stride.Graphics.Regression
{
    [Activity]
    public class AndroidGameTestActivity : AndroidStrideActivity
    {
        public static Game GameToStart;

        public static event EventHandler<EventArgs> Destroyed;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            if (Game == null) // application can be restarted
            {
                Game = GameToStart;
                Game.Exiting += Game_Exiting;
            }

            Game.Run(GameContext);
        }

        public override void OnBackPressed()
        {
            Game.Exit();
            base.OnBackPressed();
        }

        void Game_Exiting(object sender, EventArgs e)
        {
            Finish();
        }

        protected override void OnDestroy()
        {
            Game?.Dispose();

            base.OnDestroy();

            var handler = Destroyed;
            handler?.Invoke(this, EventArgs.Empty);
        }
    }
}
#endif
