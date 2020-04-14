// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Android.App;
using Android.OS;
using Stride.Starter;

namespace Stride.Particles.Tests
{
    [Activity(Label = "Stride Particles", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : AndroidStrideActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            //Game = new InputTestGame2();
            //Game.Run(GameContext);
        }
    }
}

