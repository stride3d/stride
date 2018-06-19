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
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Android.Content;
using NUnit.Framework.Api;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.WorkItems;

namespace Android.NUnitLite {
	
	public class AndroidRunner : ITestListener {
		
		private AndroidRunner ()
		{
		    writer = Console.Out;
		}
		
		public bool AutoStart { get; set; }

		public bool TerminateAfterExecution { get; set; }
		
		#region writer
		
		private TextWriter writer { get; set; }
		
		public bool OpenWriter (string message, Context activity)
		{
			DateTime now = DateTime.Now;

			writer.WriteLine ("[Runner executing:\t{0}]", message);
			// FIXME
			writer.WriteLine ("[M4A Version:\t{0}]", "???");
			
			writer.WriteLine ("[Board:\t\t{0}]", Android.OS.Build.Board);
			writer.WriteLine ("[Bootloader:\t{0}]", Android.OS.Build.Bootloader);
			writer.WriteLine ("[Brand:\t\t{0}]", Android.OS.Build.Brand);
			writer.WriteLine ("[CpuAbi:\t{0} {1}]", Android.OS.Build.CpuAbi, Android.OS.Build.CpuAbi2);
			writer.WriteLine ("[Device:\t{0}]", Android.OS.Build.Device);
			writer.WriteLine ("[Display:\t{0}]", Android.OS.Build.Display);
			writer.WriteLine ("[Fingerprint:\t{0}]", Android.OS.Build.Fingerprint);
			writer.WriteLine ("[Hardware:\t{0}]", Android.OS.Build.Hardware);
			writer.WriteLine ("[Host:\t\t{0}]", Android.OS.Build.Host);
			writer.WriteLine ("[Id:\t\t{0}]", Android.OS.Build.Id);
			writer.WriteLine ("[Manufacturer:\t{0}]", Android.OS.Build.Manufacturer);
			writer.WriteLine ("[Model:\t\t{0}]", Android.OS.Build.Model);
			writer.WriteLine ("[Product:\t{0}]", Android.OS.Build.Product);
			writer.WriteLine ("[Radio:\t\t{0}]", Android.OS.Build.Radio);
			writer.WriteLine ("[Tags:\t\t{0}]", Android.OS.Build.Tags);
			writer.WriteLine ("[Time:\t\t{0}]", Android.OS.Build.Time);
			writer.WriteLine ("[Type:\t\t{0}]", Android.OS.Build.Type);
			writer.WriteLine ("[User:\t\t{0}]", Android.OS.Build.User);
			writer.WriteLine ("[VERSION.Codename:\t{0}]", Android.OS.Build.VERSION.Codename);
			writer.WriteLine ("[VERSION.Incremental:\t{0}]", Android.OS.Build.VERSION.Incremental);
			writer.WriteLine ("[VERSION.Release:\t{0}]", Android.OS.Build.VERSION.Release);
			writer.WriteLine ("[VERSION.Sdk:\t\t{0}]", Android.OS.Build.VERSION.Sdk);
			writer.WriteLine ("[VERSION.SdkInt:\t{0}]", Android.OS.Build.VERSION.SdkInt);
			writer.WriteLine ("[Device Date/Time:\t{0}]", now); // to match earlier C.WL output
			
			// FIXME: add data about how the app was compiled (e.g. ARMvX, LLVM, Linker options)

			return true;
		}
		
		public void CloseWriter ()
		{
		}

		#endregion
		
		public void TestStarted (ITest test)
		{
			if (test is TestSuite) {
				writer.WriteLine ();
				writer.WriteLine (test.Name);
			}
		}

	    public void TestFinished(ITestResult result)
	    {
            AndroidRunner.Results[result.Test.FullName ?? result.Test.Name] = result;

            if (result.Test is TestSuite)
            {
                if (!result.IsFailure() && !result.IsSuccess() && !result.IsInconclusive() && !result.IsIgnored())
                    writer.WriteLine("\t[INFO] {0}", result.Message);

                string name = result.Test.Name;
                if (!String.IsNullOrEmpty(name))
                    writer.WriteLine("{0} : {1} ms", name, result.Duration.TotalMilliseconds);
            }
            else
            {
                if (result.IsSuccess())
                {
                    writer.Write("\t[PASS] ");
                }
                else if (result.IsIgnored())
                {
                    writer.Write("\t[IGNORED] ");
                }
                else if (result.IsFailure())
                {
                    writer.Write("\t[FAIL] ");
                }
                else if (result.IsInconclusive())
                {
                    writer.Write("\t[INCONCLUSIVE] ");
                }
                else
                {
                    writer.Write("\t[INFO] ");
                }
                writer.Write(result.Test.Name);

                string message = result.Message;
                if (!String.IsNullOrEmpty(message))
                {
                    writer.Write(" : {0}", message.Replace("\r\n", "\\r\\n"));
                }
                writer.WriteLine();

                string stacktrace = result.StackTrace;
                if (!String.IsNullOrEmpty(result.StackTrace))
                {
                    string[] lines = stacktrace.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                        writer.WriteLine("\t\t{0}", line);
                }
            }
	    }

	    public void TestOutput(TestOutput testOutput)
	    {
	    }

	    Stack<DateTime> time = new Stack<DateTime> ();
			
		public void TestFinished (TestResult result)
		{
			
		}
		
		static AndroidRunner runner = new AndroidRunner ();
		
		static public AndroidRunner Runner {
			get { return runner; }
		}
		
		static List<TestSuite> top = new List<TestSuite> ();
		static Dictionary<string,TestSuite> suites = new Dictionary<string, TestSuite> ();
		static Dictionary<string,ITestResult> results = new Dictionary<string, ITestResult> ();
		
		static public IList<TestSuite> AssemblyLevel {
			get { return top; }
		}
		
		static public IDictionary<string,TestSuite> Suites {
			get { return suites; }
		}
		
		static public IDictionary<string,ITestResult> Results {
			get { return results; }
		}

        public Task<TestResult> Run(NUnit.Framework.Internal.Test test)
        {
            return Task.Run(() =>
            {
                TestExecutionContext current = TestExecutionContext.CurrentContext;
                current.WorkDirectory = Environment.CurrentDirectory;
                //current.Listener = this; // Internal on Android
                current.GetType().GetField("listener", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(current, this);
                current.TestObject = test is TestSuite ? null : Reflect.Construct((test as TestMethod).Method.ReflectedType, null);
                WorkItem wi = test.CreateWorkItem(TestFilter.Empty);

                wi.Execute(current);
                return wi.Result;
            });
        }
	}
}