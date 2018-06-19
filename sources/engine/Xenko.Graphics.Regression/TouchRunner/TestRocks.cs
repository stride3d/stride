// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// TestRocks.cs: Helpers
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

using NUnit.Framework;
using NUnit.Framework.Internal;
using NUnit.Framework.Api;

namespace Xenko.UnitTesting {
	
	static class TestRock {
		
		const string NUnitFrameworkExceptionPrefix = "NUnit.Framework.";

		static public bool IsIgnored (this TestResult result)
		{
			return (result.ResultState.Status == TestStatus.Skipped);
		}
		
		static public bool IsSuccess (this TestResult result)
		{
			return (result.ResultState.Status == TestStatus.Passed);
		}
		
		static public bool IsFailure (this TestResult result)
		{
			return (result.ResultState.Status == TestStatus.Failed);
		}
		
		static public bool IsInconclusive (this TestResult result)
		{
			return (result.ResultState.Status == TestStatus.Inconclusive);
		}

		// remove the nunit exception message from the "real" message
		static public string GetMessage (this TestResult result)
		{
			string m = result.Message;
			if (m == null)
				return "Unknown error";
			if (!m.StartsWith (NUnitFrameworkExceptionPrefix))
				return m;
			return m.Substring (m.IndexOf (" : ") + 3);
		}
	}
}
