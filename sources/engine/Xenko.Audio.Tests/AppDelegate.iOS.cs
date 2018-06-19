// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Xenko.Audio.Tests
{
    [Register("AppDelegateiOS")]
    public class AppDelegate : UIApplicationDelegate
    {
        UIWindow window;
        MainViewController viewController;

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            window = new UIWindow(UIScreen.MainScreen.Bounds);

            viewController = new MainViewController();
            window.RootViewController = viewController;

            window.MakeKeyAndVisible();

            return true;
        }
    }
}

