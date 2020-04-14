// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Runtime.CompilerServices;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Stride.Effects;
using Stride.Starter;

namespace Stride.Particles.Tests
{
    public class Application
    {
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, "AppDelegate");
        }
    }

    [Register("AppDelegate")]
    public class AppDelegate : StrideApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            RuntimeHelpers.RunModuleConstructor(typeof(MaterialKeys).Module.ModuleHandle);

            //Game = new InputTestGame2();

            return base.FinishedLaunching(app, options);
        }
    }
}
