// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright 2011-2012 Xamarin Inc.
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

using Android.App;
using Android.OS;
using Android.Widget;

using MonoDroid.Dialog;
using NUnit.Framework.Api;
using NUnit.Framework.Internal;

namespace Android.NUnitLite.UI {

	[Activity (Label = "Tests")]			
	public class TestSuiteActivity : Activity {
		
		string test_suite;
		TestSuite suite;
		Section main;
		
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			test_suite = Intent.GetStringExtra ("TestSuite");
			suite = AndroidRunner.Suites [test_suite];

			var menu = new RootElement (String.Empty);

            while (suite.Tests.Count == 1 && (suite.Tests[0] is TestSuite))
                suite = (TestSuite)suite.Tests[0];

            main = new Section(suite.FullName ?? suite.Name);
			foreach (ITest test in suite.Tests) {
				TestSuite ts = test as TestSuite;
				if (ts != null)
					main.Add (new TestSuiteElement (ts));
				else
					main.Add (new TestCaseElement (test));
			}
			menu.Add (main);

			Section options = new Section () {
				new ActionElement ("Run all", Run),
			};
			menu.Add (options);

			var da = new DialogAdapter (this, menu);
			var lv = new ListView (this) {
				Adapter = da
			};
			SetContentView (lv);
		}
		
		public async void Run ()
		{
			AndroidRunner runner = AndroidRunner.Runner;
			if (!runner.OpenWriter ("Run " + test_suite, this))
				return;
			
			try {
				foreach (NUnit.Framework.Internal.Test test in suite.Tests)
				{
				    await runner.Run(test);
				}
			}
			finally {
				runner.CloseWriter ();
			}
			
			foreach (TestElement te in main) {
				te.Update ();
			}
		}
	}
}
