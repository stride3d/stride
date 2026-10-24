// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Presentation.ViewModels;

namespace Stride.GameStudio.Logs
{
    public sealed class BuildLogViewModel : LoggerViewModel, IForwardSerializableLogRemote
    {
        public BuildLogViewModel(IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
        
        /// <summary>
        /// Shows what <paramref name="logger"/> logs from now on, unless it is shown already.
        /// </summary>
        public void EnsureLogger(Logger logger)
        {
            if (!Loggers.ContainsKey(logger))
                AddLogger(logger);
        }

        /// <summary>
        /// Shows what <paramref name="logger"/> logged and logs from now on, unless it is shown already.
        /// </summary>
        public void EnsureLoggerWithPast(LoggerResult logger)
        {
            if (!Loggers.ContainsKey(logger))
                AddLoggerWithPast(logger);
        }

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
            base.Destroy();
        }
    }
}
