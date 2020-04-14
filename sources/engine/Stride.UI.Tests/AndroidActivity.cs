// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Android.App;
using Android.OS;
using Xenko.Starter;
using Xenko.UI.Tests.Rendering;

namespace Xenko.UI.Tests
{
    [Activity(Label = "Xenko UI", MainLauncher = true, Icon = "@drawable/icon")]
    public class AndroidActivity : AndroidXenkoActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            
            //Program.Main();
            //return;
            
            //Game = new ComplexLayoutRenderingTest();
            //Game = new RenderBorderImageTest();
            //Game = new RenderButtonTest();
            //Game = new RenderImageTest();
            //Game = new RenderTextBlockTest();
            //Game = new RenderEditTextTest();
            //Game = new SeparateAlphaTest();
            //Game = new RenderScrollViewerTest();
            Game = new RenderStackPanelTest();
            Game.Run(GameContext);
        }


    }
}

