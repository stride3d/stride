// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Mono.Debugging.Soft;
using MonoDevelop.Debugger.Soft;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Soft.Xenko
{
	public class XenkoRemoteSoftDebuggerSession : SoftDebuggerSession
	{
		public XenkoRemoteSoftDebuggerSession()
		{
			
		}
		
		protected override void OnRun(DebuggerStartInfo startInfo)
		{
			var dsi = (SoftDebuggerStartInfo)startInfo;
            if (dsi.StartArgs is SoftDebuggerConnectArgs)
			    base.StartConnecting(dsi);
            else if (dsi.StartArgs is SoftDebuggerListenArgs)
                base.StartListening(dsi);
            else
                throw new NotImplementedException();
		}
	}
}

