// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// TestElement.cs: MonoTouch.Dialog element for ITest, i.e. TestSuite and TestCase
//
// Authors:
//	Sebastien Pouliot  <sebastien@xamarin.com>
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

#if XAMCORE_2_0
using UIKit;
#else
using MonoTouch.UIKit;
#endif

using MonoTouch.Dialog;

using NUnit.Framework.Internal;
using NUnit.Framework.Api;

namespace Xenko.UnitTesting.UI {

	abstract class TestElement : StyledMultilineElement {
		
		static internal UIColor DarkGreen = UIColor.FromRGB (0x00, 0x77, 0x00);
	
		private TestResult result;
		
		public TestElement (ITest test, TouchRunner runner)
			: base ("?", "?", UITableViewCellStyle.Subtitle)
		{
			if (test == null)
				throw new ArgumentNullException ("test");
			if (runner == null)
				throw new ArgumentNullException ("runner");
		
			Test = test;
			Runner = runner;
		}

		protected TouchRunner Runner { get; private set; }
		
		public TestResult Result {
			get { return result ?? new TestCaseResult (Test as TestMethod); }
			set { result = value; }
		}
		
		protected ITest Test { get; private set; }
		
		public void Update (TestResult result)
		{
			Result = result;
			
			Update ();
		}
		
		abstract public void Update ();
	}
}
