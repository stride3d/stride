// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Runtime.CompilerServices;
using Android.App;
using Android.OS;
using Xenko.Effects;
using Xenko.Starter;

namespace Xenko.Graphics.Tests
{
    [Activity(Label = "Xenko Graphics", MainLauncher = true, Icon = "@drawable/icon")]
    public class ActivityAndroid : AndroidXenkoActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            RuntimeHelpers.RunModuleConstructor(typeof(MaterialKeys).Module.ModuleHandle);

            base.OnCreate(bundle);
            
            //Game = new TestDrawQuad();
            //Game = new TestGeometricPrimitives();
            //Game = new TestRenderToTexture();
            //Game = new TestSpriteBatch();
            //Game = new TestImageLoad();
            //Game = new TestStaticSpriteFont();
            //Game = new TestDynamicSpriteFont();
            //Game = new TestDynamicSpriteFontJapanese();
            Game = new TestDynamicSpriteFontVarious();

            Game.Run(GameContext);
        }
    }
}
