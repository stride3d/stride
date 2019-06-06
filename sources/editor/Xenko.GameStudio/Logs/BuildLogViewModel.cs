// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ServiceModel;
using Xenko.Core.BuildEngine;
using Xenko.Core.Diagnostics;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.GameStudio.Logs
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, UseSynchronizationContext = false)]
    public sealed class BuildLogViewModel : LoggerViewModel, IForwardSerializableLogRemote
    {
        private const string BasePipeName = "net.pipe://localhost/Xenko.Core.Assets.Editor";
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
                // print out shader errors first
                if (Xenko.Rendering.EffectSystem.ShaderCompilerErrors.Count > 0) {
                    foreach (string err in Xenko.Rendering.EffectSystem.ShaderCompilerErrors) {
                        logger.Error(err + "\n{{end of shader error history entry}}");
                    }
                    Xenko.Rendering.EffectSystem.ShaderCompilerErrors.Clear();
                }

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
