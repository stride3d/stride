// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ServiceModel;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.VisualStudio.Commands;

namespace Stride.VisualStudio.BuildEngine
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class PackageBuildMonitorRemote : IForwardSerializableLogRemote
    {
        private string logPipeUrl;
        private IBuildMonitorCallback buildMonitorCallback;

        public PackageBuildMonitorRemote(IBuildMonitorCallback buildMonitorCallback, string logPipeUrl)
        {
            this.buildMonitorCallback = buildMonitorCallback;
            this.logPipeUrl = logPipeUrl;

            // Listen to pipe with this as listener
            var host = new ServiceHost(this);
            host.AddServiceEndpoint(typeof(IForwardSerializableLogRemote), new NetNamedPipeBinding(NetNamedPipeSecurityMode.None) { MaxReceivedMessageSize = int.MaxValue }, this.logPipeUrl);
            host.Open();
        }

        public void ForwardSerializableLog(SerializableLogMessage message)
        {
            buildMonitorCallback.Message(message.Type.ToString(), message.Module, message.Text);
        }
    }
}
