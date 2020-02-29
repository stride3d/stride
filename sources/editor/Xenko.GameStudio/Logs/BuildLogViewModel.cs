// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.BuildEngine;
using ServiceWire.NamedPipes;
using Xenko.Core.Diagnostics;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.GameStudio.Logs
{
    public sealed class BuildLogViewModel : LoggerViewModel, IForwardSerializableLogRemote
    {
        private const string BasePipeName = "XenkoCoreAssetsEditor";
        private readonly NpHost host;

        public BuildLogViewModel(IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            PipeName = $"{BasePipeName}.{Guid.NewGuid()}";
            host = new NpHost(PipeName, null, null);
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
