// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Android.App;
using Android.OS;
using Stride.Starter;
using Stride.UI.Tests.Rendering;

namespace Stride.UI.Tests
{
    [Activity(Label = "Stride UI", MainLauncher = true, Icon = "@drawable/icon")]
    public class AndroidActivity : AndroidStrideActivity
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

