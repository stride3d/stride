// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Mono.Debugging.Soft;
using MonoDevelop.Debugger.Soft;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Soft.Stride
{
	public class StrideRemoteSoftDebuggerSession : SoftDebuggerSession
	{
		public StrideRemoteSoftDebuggerSession()
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

