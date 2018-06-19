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
using Android.Content;
using Android.OS;
using Android.Widget;

using MonoDroid.Dialog;
using NUnit.Framework.Api;
using NUnit.Framework.Internal;
using NUnitLite;

namespace Android.NUnitLite.UI {
	
	[Activity (Label = "Results")]			
	public class TestResultActivity : Activity {
		
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			string test_case = Intent.GetStringExtra ("TestCase");
			
			ITestResult result = AndroidRunner.Results [test_case];

			string error = String.Format ("<b>{0}<b><br><font color='grey'>{1}</font>", 
				result.Message, result.StackTrace.Replace (System.Environment.NewLine, "<br>"));
			
			var menu = new RootElement (String.Empty) {
				new Section (test_case) {
					new FormattedElement (error)
				}
			};
			
			var da = new DialogAdapter (this, menu);
			var lv = new ListView (this) {
				Adapter = da
			};
			SetContentView (lv);
		}
	}
}
