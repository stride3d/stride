// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Android.App;
using Android.OS;
using Xenko.Starter;

namespace Xenko.Input.Tests
{
    [Activity(Label = "Xenko Input", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : AndroidXenkoActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Game = new InputTestGame2();
            Game.Run(GameContext);
        }
    }
}

