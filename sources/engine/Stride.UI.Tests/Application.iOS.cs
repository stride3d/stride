// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Stride.Starter;
using Stride.UI.Tests.Rendering;

namespace Stride.UI.Tests
{
    public class ManualApplication
    {
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, "ManualAppDelegate");
        }
    }

    [Register("ManualAppDelegate")]
    public class ManualAppDelegate : StrideApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            //Game = new RenderEditTextTest();
            //Game = new RenderScrollViewerTest();
            //Game = new ComplexLayoutRenderingTest();
            //Game = new RenderScrollViewerTest();
            Game = new RenderStackPanelTest();

            return base.FinishedLaunching(app, options);
        }
    }
}
