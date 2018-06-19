// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// TouchRunner.cs: MonoTouch.Dialog-based driver to run unit tests
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xenko.Graphics.Regression;
#if XAMCORE_2_0
using Foundation;
using ObjCRuntime;
using UIKit;
using Constants = global::ObjCRuntime.Constants;
#else
using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;
using MonoTouch.UIKit;
using Constants = global::MonoTouch.Constants;
#endif

using MonoTouch.Dialog;

using NUnit.Framework.Api;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework.Internal.WorkItems;

namespace Xenko.UnitTesting.UI {
	
	public class TouchRunner : ITestListener {
		
		UIWindow window;
		int passed;
		int failed;
		int ignored;
		int inconclusive;
		TestSuite suite = new TestSuite (String.Empty);
		ITestFilter filter;

		[CLSCompliant (false)]
		public TouchRunner (UIWindow window)
		{
			if (window == null)
				throw new ArgumentNullException ("window");
			
			this.window = window;
			filter = TestFilter.Empty;
		}
		
		public bool AutoStart {
			get { return TouchOptions.Current.AutoStart; }
			set { TouchOptions.Current.AutoStart = value; }
		}
		
		public ITestFilter Filter {
			get { return filter; }
			set { filter = value; }
		}
		
		public bool TerminateAfterExecution {
			get { return TouchOptions.Current.TerminateAfterExecution; }
			set { TouchOptions.Current.TerminateAfterExecution = value; }
		}
		
		[CLSCompliant (false)]
		public UINavigationController NavigationController {
			get { return (UINavigationController) window.RootViewController; }
		}
		
		List<Assembly> assemblies = new List<Assembly> ();
		ManualResetEvent mre = new ManualResetEvent (false);
		
		public void Add (Assembly assembly)
		{
			if (assembly == null)
				throw new ArgumentNullException ("assembly");
			
			assemblies.Add (assembly);
		}
		
		static void TerminateWithSuccess ()
		{
			Selector selector = new Selector ("terminateWithSuccess");
			UIApplication.SharedApplication.PerformSelector (selector, UIApplication.SharedApplication, 0);						
		}
		
		[CLSCompliant (false)]
		public UIViewController GetViewController ()
		{
			var menu = new RootElement ("Test Runner");
            
            var runMode = new Section("Run Mode");
            var interactiveCheckBox = new CheckboxElement("Enable Interactive Mode");
            interactiveCheckBox.Tapped += () => GameTestBase.ForceInteractiveMode = interactiveCheckBox.Value;
            runMode.Add(interactiveCheckBox);
            menu.Add(runMode);

			Section main = new Section ("Loading test suites...");
			menu.Add (main);
			
			Section options = new Section () {
				new StyledStringElement ("Options", Options) { Accessory = UITableViewCellAccessory.DisclosureIndicator },
				new StyledStringElement ("Credits", Credits) { Accessory = UITableViewCellAccessory.DisclosureIndicator }
			};
			menu.Add (options);

			// large unit tests applications can take more time to initialize
			// than what the iOS watchdog will allow them on devices
			ThreadPool.QueueUserWorkItem (delegate {
				foreach (Assembly assembly in assemblies)
					Load (assembly, null);

				window.InvokeOnMainThread (delegate {

                    while (suite.Tests.Count == 1 && (suite.Tests[0] is TestSuite))
                        suite = (TestSuite)suite.Tests[0];

					foreach (TestSuite ts in suite.Tests) {
						main.Add (Setup (ts));
					}
					mre.Set ();
					
					main.Caption = null;
					menu.Reload (main, UITableViewRowAnimation.Fade);
					
					options.Insert (0, new StringElement ("Run Everything", Run));
					menu.Reload (options, UITableViewRowAnimation.Fade);
				});
				assemblies.Clear ();
			});
			
			var dv = new DialogViewController (menu) { Autorotate = true };

			// AutoStart running the tests (with either the supplied 'writer' or the options)
			if (AutoStart) {
				ThreadPool.QueueUserWorkItem (delegate {
					mre.WaitOne ();
					window.BeginInvokeOnMainThread (delegate {
						Run ();	
						// optionally end the process, e.g. click "Touch.Unit" -> log tests results, return to springboard...
						// http://stackoverflow.com/questions/1978695/uiapplication-sharedapplication-terminatewithsuccess-is-not-there
						if (TerminateAfterExecution)
							TerminateWithSuccess ();
					});
				});
			}
			return dv;
		}
		
		async void Run ()
		{
			if (!OpenWriter ("Run Everything"))
				return;
			try {
				await Run (suite);
			}
			finally {
				CloseWriter ();
			}
		}
				
		void Options ()
		{
			NavigationController.PushViewController (TouchOptions.Current.GetViewController (), true);				
		}
		
		void Credits ()
		{
			var title = new MultilineElement ("Touch.Unit Runner\nCopyright 2011-2012 Xamarin Inc.\nAll rights reserved.");
			title.Alignment = UITextAlignment.Center;
			
			var root = new RootElement ("Credits") {
				new Section () { title },
				new Section () {
					new HtmlElement ("About Xamarin", "http://www.xamarin.com"),
					new HtmlElement ("About MonoTouch", "http://ios.xamarin.com"),
					new HtmlElement ("About MonoTouch.Dialog", "https://github.com/migueldeicaza/MonoTouch.Dialog"),
					new HtmlElement ("About NUnitLite", "http://www.nunitlite.org"),
					new HtmlElement ("About Font Awesome", "http://fortawesome.github.com/Font-Awesome")
				}
			};
				
			var dv = new DialogViewController (root, true) { Autorotate = true };
			NavigationController.PushViewController (dv, true);				
		}
		
		#region writer
		
		public TestResult Result { get; set; }

		public TextWriter Writer { get; set; }
		
		static string SelectHostName (string[] names, int port)
		{
			if (names.Length == 0)
				return null;
			
			if (names.Length == 1)
				return names [0];
			
			object lock_obj = new object ();
			string result = null;
			int failures = 0;
			
			using (var evt = new ManualResetEvent (false)) {
				for (int i = names.Length - 1; i >= 0; i--) {
					var name = names [i];
					ThreadPool.QueueUserWorkItem ((v) =>
					{
						try {
							var client = new TcpClient (name, port);
							using (var writer = new StreamWriter (client.GetStream ())) {
								writer.WriteLine ("ping");
							}
							lock (lock_obj) {
								if (result == null)
									result = name;
							}
							evt.Set ();
						} catch (Exception) {
							lock (lock_obj) {
								failures++;
								if (failures == names.Length)
									evt.Set ();
							}
						}
					});
				}
				
				// Wait for 1 success or all failures
				evt.WaitOne ();
			}
			
			return result;
		}
		
		public bool OpenWriter (string message)
		{
			TouchOptions options = TouchOptions.Current;
			DateTime now = DateTime.Now;
			// let the application provide it's own TextWriter to ease automation with AutoStart property
			if (Writer == null) {
				//if (options.ShowUseNetworkLogger) {
				//	var hostname = SelectHostName (options.HostName.Split (','), options.HostPort);
				//	
				//	if (hostname != null) {
				//		Console.WriteLine ("[{0}] Sending '{1}' results to {2}:{3}", now, message, hostname, options.HostPort);
				//		try {
				//			Writer = new TcpTextWriter (hostname, options.HostPort);
				//		}
				//		catch (SocketException) {
				//			UIAlertView alert = new UIAlertView ("Network Error", 
				//				String.Format ("Cannot connect to {0}:{1}. Continue on console ?", hostname, options.HostPort), 
				//				null, "Cancel", "Continue");
				//			int button = -1;
				//			alert.Clicked += delegate(object sender, UIButtonEventArgs e) {
				//				button = (int)e.ButtonIndex;
				//			};
				//			alert.Show ();
				//			while (button == -1)
				//				NSRunLoop.Current.RunUntil (NSDate.FromTimeIntervalSinceNow (0.5));
				//			Console.WriteLine (button);
				//			Console.WriteLine ("[Host unreachable: {0}]", button == 0 ? "Execution cancelled" : "Switching to console output");
				//			if (button == 0)
				//				return false;
				//			else
				//				Writer = Console.Out;
				//		}
				//	}
				//} else {
					Writer = Console.Out;
				//}
			}
			
			Writer.WriteLine ("[Runner executing:\t{0}]", message);
			Writer.WriteLine ("[MonoTouch Version:\t{0}]", Constants.Version);
			Writer.WriteLine ("[Assembly:\t{0}.dll ({1} bits)]", typeof (NSObject).Assembly.GetName ().Name, IntPtr.Size * 8);
			Writer.WriteLine ("[GC:\t{0}{1}]", GC.MaxGeneration == 0 ? "Boehm": "sgen", 
				NSObject.IsNewRefcountEnabled () ? "+NewRefCount" : String.Empty);
			UIDevice device = UIDevice.CurrentDevice;
			Writer.WriteLine ("[{0}:\t{1} v{2}]", device.Model, device.SystemName, device.SystemVersion);
			Writer.WriteLine ("[Device Name:\t{0}]", device.Name);
			Writer.WriteLine ("[Device UDID:\t{0}]", UniqueIdentifier);
			Writer.WriteLine ("[Device Locale:\t{0}]", NSLocale.CurrentLocale.Identifier);
			Writer.WriteLine ("[Device Date/Time:\t{0}]", now); // to match earlier C.WL output

			Writer.WriteLine ("[Bundle:\t{0}]", NSBundle.MainBundle.BundleIdentifier);
			// FIXME: add data about how the app was compiled (e.g. ARMvX, LLVM, GC and Linker options)
			passed = 0;
			ignored = 0;
			failed = 0;
			inconclusive = 0;
			return true;
		}

		[System.Runtime.InteropServices.DllImport ("/usr/lib/libobjc.dylib")]
		static extern IntPtr objc_msgSend (IntPtr receiver, IntPtr selector);

		// Apple blacklisted `uniqueIdentifier` (for the appstore) but it's still 
		// something useful to have inside the test logs
		static string UniqueIdentifier {
			get {
				IntPtr handle = UIDevice.CurrentDevice.Handle;
				if (UIDevice.CurrentDevice.RespondsToSelector (new Selector ("uniqueIdentifier")))
					return NSString.FromHandle (objc_msgSend (handle, Selector.GetHandle("uniqueIdentifier")));
				return "unknown";
			}
		}
		
		public void CloseWriter ()
		{
			int total = passed + inconclusive + failed; // ignored are *not* run
			Writer.WriteLine ("Tests run: {0} Passed: {1} Inconclusive: {2} Failed: {3} Ignored: {4}", total, passed, inconclusive, failed, ignored);

			Writer.Close ();
			Writer = null;
		}
		
		#endregion
		
		Dictionary<TestSuite, TouchViewController> suites_dvc = new Dictionary<TestSuite, TouchViewController> ();
		Dictionary<TestSuite, TestSuiteElement> suite_elements = new Dictionary<TestSuite, TestSuiteElement> ();
		Dictionary<TestMethod, TestCaseElement> case_elements = new Dictionary<TestMethod, TestCaseElement> ();
		
		public void Show (TestSuite suite)
		{
			NavigationController.PushViewController (suites_dvc [suite], true);
		}
	
		TestSuiteElement Setup (TestSuite suite)
		{
            while (suite.Tests.Count == 1 && (suite.Tests[0] is TestSuite))
                suite = (TestSuite)suite.Tests[0];

			TestSuiteElement tse = new TestSuiteElement (suite, this);
			suite_elements.Add (suite, tse);
			
			var root = new RootElement ("Tests");
		
			Section section = new Section (suite.Name);
			foreach (ITest test in suite.Tests) {
				TestSuite ts = (test as TestSuite);
				if (ts != null) {
					section.Add (Setup (ts));
				} else {
					TestMethod tc = (test as TestMethod);
					if (tc != null) {
						section.Add (Setup (tc));
					} else {
						throw new NotImplementedException (test.GetType ().ToString ());
					}
				}
			}
		
			root.Add (section);
			
			if (section.Count > 1) {
				Section options = new Section () {
					new StringElement ("Run all", async delegate () {
						if (OpenWriter (suite.Name)) {
							await Run (suite);
							CloseWriter ();
							suites_dvc [suite].Filter ();
						}
					})
				};
				root.Add (options);
			}

			suites_dvc.Add (suite, new TouchViewController (root));
			return tse;
		}
		
		TestCaseElement Setup (TestMethod test)
		{
			TestCaseElement tce = new TestCaseElement (test, this);
			case_elements.Add (test, tce);
			return tce;
		}
				
		public void TestStarted (ITest test)
		{
			if (test is TestSuite) {
				Writer.WriteLine ();
				Writer.WriteLine (test.Name);
			}
		}

	    public void TestFinished(ITestResult r)
	    {
	        UIApplication.SharedApplication.InvokeOnMainThread(() => UpdateTestResult(r));
	    }

	    public void UpdateTestResult(ITestResult r)
		{
			TestResult result = r as TestResult;
			TestSuite ts = result.Test as TestSuite;
			if (ts != null) {
				TestSuiteElement tse;
				if (suite_elements.TryGetValue (ts, out tse))
					tse.Update (result);
			} else {
				TestMethod tc = result.Test as TestMethod;
				if (tc != null)
					case_elements [tc].Update (result);
			}
			
			if (result.Test is TestSuite) {
				if (!result.IsFailure () && !result.IsSuccess () && !result.IsInconclusive () && !result.IsIgnored ())
					Writer.WriteLine ("\t[INFO] {0}", result.Message);

				string name = result.Test.Name;
				if (!String.IsNullOrEmpty (name))
					Writer.WriteLine ("{0} : {1} ms", name, result.Duration.TotalMilliseconds);
			} else {
				if (result.IsSuccess ()) {
					Writer.Write ("\t[PASS] ");
					passed++;
				} else if (result.IsIgnored ()) {
					Writer.Write ("\t[IGNORED] ");
					ignored++;
				} else if (result.IsFailure ()) {
					Writer.Write ("\t[FAIL] ");
					failed++;
				} else if (result.IsInconclusive ()) {
					Writer.Write ("\t[INCONCLUSIVE] ");
					inconclusive++;
				} else {
					Writer.Write ("\t[INFO] ");
				}
				Writer.Write (result.Test.Name);
				
				string message = result.Message;
				if (!String.IsNullOrEmpty (message)) {
					Writer.Write (" : {0}", message.Replace ("\r\n", "\\r\\n"));
				}
				Writer.WriteLine ();
						
				string stacktrace = result.StackTrace;
				if (!String.IsNullOrEmpty (result.StackTrace)) {
					string[] lines = stacktrace.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string line in lines)
						Writer.WriteLine ("\t\t{0}", line);
				}
			}
		}

        NamespaceAssemblyBuilder builder = new NamespaceAssemblyBuilder(new NUnitLiteTestAssemblyBuilder());
		Dictionary<string, object> empty = new Dictionary<string, object> ();

		public bool Load (string assemblyName, IDictionary settings)
		{
			return AddSuite (builder.Build (assemblyName, settings ?? empty));
		}

		public bool Load (Assembly assembly, IDictionary settings)
		{
			return AddSuite (builder.Build (assembly, settings ?? empty));
		}

		bool AddSuite (TestSuite ts)
		{
			if (ts == null)
				return false;
			suite.Add (ts);
			return true;
		}

		public Task<TestResult> Run (Test test)
		{
		    return Task.Run(() =>
		    {
		        Result = null;
		        TestExecutionContext current = TestExecutionContext.CurrentContext;
		        current.WorkDirectory = Environment.CurrentDirectory;
		        //current.Listener = this; // Internal on Android
		        current.GetType().GetField("listener", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(current, this);
                current.TestObject = test is TestSuite ? null : Reflect.Construct((test as TestMethod).Method.ReflectedType, null);
		        WorkItem wi = test.CreateWorkItem(filter, new FinallyDelegate());
		        wi.Execute(current);
		        Result = wi.Result;
		        return Result;
		    });
		}

		public ITest LoadedTest {
			get {
				return suite;
			}
		}

		public void TestOutput (TestOutput testOutput)
		{
		}
	}
}
