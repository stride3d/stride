// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.BuildEngine;
using ServiceWire.NamedPipes;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.ViewModel;

namespace Stride.GameStudio.Logs
{
    public sealed class BuildLogViewModel : LoggerViewModel, IForwardSerializableLogRemote
    {
        private const string BasePipeName = "StrideCoreAssetsEditor";
        private readonly NpHost host;

        public BuildLogViewModel(IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            PipeName = $"{BasePipeName}.{Guid.NewGuid()}";
            host = new NpHost(PipeName, null, null, new StrideServiceWireSerializer());
            host.AddService<IForwardSerializableLogRemote>(this);
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
