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
using NUnit.Framework.Api;
using NUnit.Framework.Internal;
using NUnitLite;

namespace Android.NUnitLite.UI {
	
	abstract class TestElement : FormattedElement {
		
		string name;
		ITestResult result;
		
		public TestElement (ITest test) : base (String.Empty)
		{
			if (test == null)
				throw new ArgumentNullException ("test");
		
			Test = test;
			name = test.FullName ?? test.Name;
			Caption = GetCaption ();
		}
		
		protected string Name {
			get { return name; }
		}
				
		protected ITestResult Result {
			get {
				AndroidRunner.Results.TryGetValue (name, out result);
				return result;
			}
		}
		
		protected ITest Test { get; private set; }
		
		abstract protected string GetCaption ();
		
		public void Update ()
		{
			SetCaption (GetCaption ());
		}
	}
}
