// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_PLATFORM_ANDROID
using System;
using Android.App;
using Android.OS;
using Xenko.Engine;
using Xenko.Starter;

namespace Xenko.Graphics.Regression
{
    [Activity]
    public class AndroidGameTestActivity : AndroidXenkoActivity
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
