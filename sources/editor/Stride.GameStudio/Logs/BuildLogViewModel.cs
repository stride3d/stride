// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ServiceModel;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.ViewModel;

namespace Stride.GameStudio.Logs
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, UseSynchronizationContext = false)]
    public sealed class BuildLogViewModel : LoggerViewModel, IForwardSerializableLogRemote
    {
        private const string BasePipeName = "net.pipe://localhost/Stride.Core.Assets.Editor";
        private readonly ServiceHost host;

        public BuildLogViewModel(IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            PipeName = $"{BasePipeName}.{Guid.NewGuid()}";
            host = new ServiceHost(this);
            host.AddServiceEndpoint(typeof(IForwardSerializableLogRemote), new NetNamedPipeBinding(NetNamedPipeSecurityMode.None) { MaxReceivedMessageSize = int.MaxValue }, PipeName);
            host.Open();
        }
        
        public string PipeName { get; }

        public void ForwardSerializableLog(SerializableLogMessage message)
        {
            foreach (var logger in Loggers.Keys)
            {
                logger.Log(message);
            }
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            host.Close();
            base.Destroy();
        }
    }
}
