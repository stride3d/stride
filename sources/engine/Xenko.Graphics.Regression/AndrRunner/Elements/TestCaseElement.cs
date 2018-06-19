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
using Android.Content;
using Android.Views;
using NUnit.Framework.Api;
using NUnit.Framework.Internal;

namespace Android.NUnitLite.UI {
	
	class TestCaseElement : TestElement {
		
		public TestCaseElement (ITest test) : base (test)
		{
			if (test.RunState == RunState.Runnable)
				Indicator = "..."; // hint there's more
		}
		
		protected override string GetCaption ()
		{
		    string color, message;

            if (Result == null)
            {
                color = "white";
                message = "Not Executed";
            }
            else if (Result.IsIgnored())
            {
                color = "#FF7700";
                message = Result.GetMessage();
            }
            else if (Result.IsSuccess() || Result.IsInconclusive())
            {
                message = String.Format("{0} for {1} assertion{2}",
                                        Result.IsInconclusive() ? "Inconclusive." : "Success!",
                                        Result.AssertCount,
                                        Result.AssertCount == 1 ? String.Empty : "s");
                color = "green";
            }
            else if (Result.IsFailure())
            {
                message = Result.GetMessage();
                color = "red";
            }
            else
            {
                message = Result.GetMessage();
                color = "grey";
            }

            return string.Format("<b>{0}</b><br><font color='{1}'>{2}</font>", Result == null ? Test.Name : Result.Name, color, message);
		}
		
		public TestMethod TestCase {
			get { return Test as TestMethod; }
		}
		
		public override View GetView (Context context, View convertView, ViewGroup parent)
		{
			View view = base.GetView (context, convertView, parent);
			view.Click += async delegate {
				if (TestCase.RunState != RunState.Runnable)
					return;
								
				AndroidRunner runner = AndroidRunner.Runner;
				if (!runner.OpenWriter ("Run " + TestCase.FullName, context))
					return;
				
				try
				{
				    await runner.Run(TestCase);
				}
				finally {
					runner.CloseWriter ();
                    Update();
				}

				if (!Result.IsSuccess()) {
					Intent intent = new Intent (context, typeof (TestResultActivity));
					intent.PutExtra ("TestCase", Name);
					intent.AddFlags (ActivityFlags.NewTask);			
					context.StartActivity (intent);
				}
			};
			return view;
		}
	}
}