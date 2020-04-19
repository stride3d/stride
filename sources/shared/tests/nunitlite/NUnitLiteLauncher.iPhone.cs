// ***********************************************************************
// Copyright (c) 2009 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Reflection;
using Foundation;
using UIKit;
using Stride.UnitTesting.UI;

namespace Stride.Core.Tests
{
    public class Application
    {
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, "AppDelegate");
        }
    }

    [Register("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate
    {
        UIWindow window;
        TouchRunner runner;

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            window = new UIWindow(UIScreen.MainScreen.Bounds);
            runner = new TouchRunner(window);

            // register every tests included in the main application/assembly
            runner.Add(System.Reflection.Assembly.GetExecutingAssembly());

            window.RootViewController = new UINavigationController(runner.GetViewController());
            window.MakeKeyAndVisible();

            return true;
        }
    }
}