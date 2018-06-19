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
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using Android.App;
using Android.OS;
using Android.Widget;
using MonoDroid.Dialog;
using NUnit.Framework.Api;
using NUnit.Framework.Internal;
using Xenko.Graphics.Regression;

namespace Android.NUnitLite.UI {

    public class RunnerActivity : Activity {
		
		Section main;
		
		public RunnerActivity ()
		{
			Initialized = (AndroidRunner.AssemblyLevel.Count > 0);
		}
		
		public bool Initialized {
			get; private set;
		}
		
		public AndroidRunner Runner {
			get { return AndroidRunner.Runner; }
		}

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

			var menu = new RootElement ("Test Runner");

            var runMode = new Section("Run Mode");
            var interactiveCheckBox = new CheckboxElement("Enable Interactive Mode");
            interactiveCheckBox.ValueChanged += (sender , args) => GameTestBase.ForceInteractiveMode = interactiveCheckBox.Value;
            runMode.Add(interactiveCheckBox);
            menu.Add(runMode);

			main = new Section ("Test Suites");

            IList<ITest> suites = new List<ITest>(AndroidRunner.AssemblyLevel);
            while (suites.Count == 1 && (suites[0] is TestSuite))
                suites = suites[0].Tests;

            foreach (var test in suites)
            {
                var ts = test as TestSuite;
                if (ts != null)
                    main.Add(new TestSuiteElement(ts));
                else
                    main.Add(new TestCaseElement(test));
			}
			menu.Add (main);

			Section options = new Section () {
				new ActionElement ("Run Everything", Run),
				new ActivityElement ("Credits", typeof (CreditsActivity))
			};
			menu.Add (options);

			var lv = new ListView (this) {
				Adapter = new DialogAdapter (this, menu)
			};
			SetContentView (lv);

			// AutoStart running the tests (with either the supplied 'writer' or the options)
			if (Runner.AutoStart) {
				string msg = String.Format ("Automatically running tests{0}...", 
					Runner.TerminateAfterExecution ? " and closing application" : String.Empty);
				Toast.MakeText (this, msg, ToastLength.Long).Show ();
				ThreadPool.QueueUserWorkItem (delegate {
					RunOnUiThread (delegate {
						Run ();	
						// optionally end the process, e.g. click "Andr.Unit" -> log tests results, return to springboard...
						if (Runner.TerminateAfterExecution)
							Finish ();
					});
				});
			}
		}

        NamespaceAssemblyBuilder builder = new NamespaceAssemblyBuilder(new NUnitLiteTestAssemblyBuilder());

		public void Add (Assembly assembly)
		{
			if (assembly == null)
				throw new ArgumentNullException ("assembly");

			// this can be called many times but we only want to load them
			// once since we need to share them across most activities
			if (!Initialized)
			{
                var ts = builder.Build(assembly, new Dictionary<string, object>());

                if (ts == null) return;

				// TestLoader.Load always return a TestSuite so we can avoid casting many times
				AndroidRunner.AssemblyLevel.Add (ts);
				Add (ts);
			}
		}
		
		void Add (TestSuite suite)
		{
		    var suiteName = suite.FullName ?? suite.Name;

            // add a suffix to the name if a test with the same name already exists
		    var count = 2;
		    while (AndroidRunner.Suites.ContainsKey(suiteName))
		    {
		        suiteName = (suite.FullName ?? suite.Name) + count;
		        ++count;
		    }
		    suite.FullName = suite.Name = suiteName;

		    AndroidRunner.Suites.Add(suiteName, suite);
			foreach (ITest test in suite.Tests) 
            {
				TestSuite ts = (test as TestSuite);
				if (ts != null)
					Add (ts);
			}
		}

		async void Run ()
		{
			if (!Runner.OpenWriter ("Run Everything", this))
				return;
			
			try {
				foreach (TestSuite suite in AndroidRunner.AssemblyLevel)
				{
				    await Runner.Run(suite);
				}
			}
			finally {
				Runner.CloseWriter ();
			}
			
			foreach (TestElement te in main) {
				te.Update ();
			}
		}
    }
}
