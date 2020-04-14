// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics;

namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// A <see cref="LoggerResult"/> that also forwards messages to another <see cref="ILogger"/>.
    /// </summary>
    [DebuggerDisplay("HasErrors: {HasErrors} Messages: [{Messages.Count}]")]
    public class ForwardingLoggerResult : LoggerResult
    {
        private readonly ILogger loggerToForward;

        public ForwardingLoggerResult(ILogger loggerToForward)
        {
            this.loggerToForward = loggerToForward;
        }

        protected override void LogRaw(ILogMessage logMessage)
        {
            base.LogRaw(logMessage);
            loggerToForward?.Log(logMessage);
        }
    }
}
