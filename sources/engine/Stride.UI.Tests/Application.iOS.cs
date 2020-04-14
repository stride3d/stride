// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Xenko.Starter;
using Xenko.UI.Tests.Rendering;

namespace Xenko.UI.Tests
{
    public class ManualApplication
    {
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, "ManualAppDelegate");
        }
    }

    [Register("ManualAppDelegate")]
    public class ManualAppDelegate : XenkoApplicationDelegate
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
