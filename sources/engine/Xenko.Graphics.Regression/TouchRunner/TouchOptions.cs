// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// TouchOptions.cs: MonoTouch.Dialog-based options
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
using Foundation;
using UIKit;
#else
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

using MonoTouch.Dialog;

using Mono.Options;

namespace Xenko.UnitTesting.UI {
	
	public class TouchOptions {

		static public TouchOptions Current = new TouchOptions ();
		
		public TouchOptions ()
		{
			var defaults = NSUserDefaults.StandardUserDefaults;
			EnableNetwork = defaults.BoolForKey ("network.enabled");
			HostName = defaults.StringForKey ("network.host.name");
			HostPort = (int)defaults.IntForKey ("network.host.port");
			SortNames = defaults.BoolForKey ("display.sort");
			
			//var os = new OptionSet () {
			//	{ "autoexit", "If the app should exit once the test run has completed.", v => TerminateAfterExecution = true },
			//	{ "autostart", "If the app should automatically start running the tests.", v => AutoStart = true },
			//	{ "hostname=", "Comma-separated list of host names or IP address to (try to) connect to", v => HostName = v },
			//	{ "hostport=", "TCP port to connect to.", v => HostPort = int.Parse (v) },
			//	{ "enablenetwork", "Enable the network reporter.", v => EnableNetwork = true },
			//};
			//
			//try {
			//	os.Parse (Environment.GetCommandLineArgs ());
			//} catch (OptionException oe) {
			//	Console.WriteLine ("{0} for options '{1}'", oe.Message, oe.OptionName);
			//}
		}
		
		private bool EnableNetwork { get; set; }
		
		public string HostName { get; private set; }
		
		public int HostPort { get; private set; }
		
		public bool AutoStart { get; set; }
		
		public bool TerminateAfterExecution { get; set; }
		
		public bool ShowUseNetworkLogger {
			get { return (EnableNetwork && !String.IsNullOrWhiteSpace (HostName) && (HostPort > 0)); }
		}

		public bool SortNames { get; set; }
		
		[CLSCompliant (false)]
		public UIViewController GetViewController ()
		{
			var network = new BooleanElement ("Enable", EnableNetwork);

			var host = new EntryElement ("Host Name", "name", HostName);
			host.KeyboardType = UIKeyboardType.ASCIICapable;
			
			var port = new EntryElement ("Port", "name", HostPort.ToString ());
			port.KeyboardType = UIKeyboardType.NumberPad;
			
			var sort = new BooleanElement ("Sort Names", SortNames);

			var root = new RootElement ("Options") {
				new Section ("Remote Server") { network, host, port },
				new Section ("Display") { sort }
			};
				
			var dv = new DialogViewController (root, true) { Autorotate = true };
			dv.ViewDisappearing += delegate {
				EnableNetwork = network.Value;
				HostName = host.Value;
				ushort p;
				if (UInt16.TryParse (port.Value, out p))
					HostPort = p;
				else
					HostPort = -1;
				SortNames = sort.Value;
				
				var defaults = NSUserDefaults.StandardUserDefaults;
				defaults.SetBool (EnableNetwork, "network.enabled");
				defaults.SetString (HostName ?? String.Empty, "network.host.name");
				defaults.SetInt (HostPort, "network.host.port");
				defaults.SetBool (SortNames, "display.sort");
			};
			
			return dv;
		}
	}
}
