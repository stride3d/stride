// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Net;
using Mono.Debugging.Soft;
using MonoDevelop.Core.Execution;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Soft.Stride
{
	public class StrideDebuggerEngine : DebuggerEngineBackend
	{
		public StrideDebuggerEngine ()
		{
		}

        public override bool CanDebugCommand(ExecutionCommand cmd)
	    {
	        var processCmd = cmd as ProcessExecutionCommand;
	        if (processCmd == null)
	            return false;

            return processCmd.Command.StartsWith("StrideDebug");
		}

        public override DebuggerStartInfo CreateDebuggerStartInfo(ExecutionCommand cmd)
	    {
	        var processCmd = cmd as ProcessExecutionCommand;
	        if (processCmd == null)
	            return null;

	        var dsi =
	            processCmd.Command.EndsWith("Client")
	                ? new SoftDebuggerStartInfo(new SoftDebuggerConnectArgs("TestApp", IPAddress.Loopback, 13332))
	                : new SoftDebuggerStartInfo(new SoftDebuggerListenArgs("TestApp", IPAddress.Any, 13332));
			return dsi;
		}

        public override DebuggerSession CreateSession()
        {
			return new StrideRemoteSoftDebuggerSession();
		}
	}
}

