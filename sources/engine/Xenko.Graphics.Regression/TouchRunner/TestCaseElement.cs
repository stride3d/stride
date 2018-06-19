// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// TestCaseElement.cs: MonoTouch.Dialog element for TestCase
//
// Authors:
//	Sebastien Pouliot  <sebastien@xamarin.com>
//
// Copyright 2011-2013 Xamarin Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Reflection;
using System.Threading.Tasks;
#if XAMCORE_2_0
using UIKit;
#else
using MonoTouch.UIKit;
#endif

using MonoTouch.Dialog;

using NUnit.Framework;
using NUnit.Framework.Internal;
using NUnit.Framework.Api;

namespace Xenko.UnitTesting.UI {
	
	class TestCaseElement : TestElement {
		
		public TestCaseElement (TestMethod testCase, TouchRunner runner)
			: base (testCase, runner)
		{
			Caption = testCase.Name;
			Value = "NotExecuted";
			this.Tapped += async delegate {
				if (!Runner.OpenWriter (Test.FullName))
					return;

				var suite = (testCase.Parent as TestSuite);
				var context = TestExecutionContext.CurrentContext;
				context.TestObject = Reflect.Construct (testCase.Method.ReflectedType, null);

				suite.GetOneTimeSetUpCommand ().Execute (context);
				await Run ();
				suite.GetOneTimeTearDownCommand ().Execute (context);

				Runner.CloseWriter ();
				// display more details on (any) failure (but not when ignored)
				if ((TestCase.RunState == RunState.Runnable) && !Result.IsSuccess ()) {
					var root = new RootElement ("Results") {
						new Section () {
							new TestResultElement (Result)
						}
					};
					var dvc = new DialogViewController (root, true) { Autorotate = true };
					runner.NavigationController.PushViewController (dvc, true);
				} else if (GetContainerTableView () != null) {
					var root = GetImmediateRootElement ();
					root.Reload (this, UITableViewRowAnimation.Fade);
				}
			};
		}
		
		public TestMethod TestCase {
			get { return Test as TestMethod; }
		}
		
		public async Task Run ()
		{
			Update (await Runner.Run (TestCase));
		}
		
		public override void Update ()
		{
			if (Result.IsIgnored ()) {
				Value = Result.GetMessage ();
				DetailColor = UIColor.Orange;
			} else if (Result.IsSuccess () || Result.IsInconclusive ()) {
				int counter = Result.AssertCount;
				Value = String.Format ("{0} {1} ms for {2} assertion{3}",
					Result.IsInconclusive () ? "Inconclusive." : "Success!",
					Result.Duration.TotalMilliseconds, counter,
					counter == 1 ? String.Empty : "s");
				DetailColor = DarkGreen;
			} else if (Result.IsFailure ()) {
				Value = Result.GetMessage ();
				DetailColor = UIColor.Red;
			} else {
				// Assert.Ignore falls into this
				Value = Result.GetMessage ();
			}
		}
	}
}
