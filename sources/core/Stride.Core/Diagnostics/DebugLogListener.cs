// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

namespace Stride.Core.Diagnostics;

/// <summary>
/// A <see cref="LogListener"/> implementation redirecting its output to a <see cref="Debug"/>.
/// </summary>
public class DebugLogListener : LogListener
{
    /// <summary>
    /// Minimum severity captured; messages below this level are dropped. Defaults to
    /// <see cref="LogMessageType.Verbose"/> so the listener catches everything unless the host
    /// raises the threshold (e.g. <see cref="LogMessageType.Warning"/> in a hot path where
    /// Info/Verbose chatter would slow the debugger).
    /// </summary>
    public LogMessageType MinimumLevel { get; set; } = LogMessageType.Verbose;

    protected override void OnLog(ILogMessage logMessage)
    {
        if (logMessage.Type < MinimumLevel)
            return;

        Debug.WriteLine(GetDefaultText(logMessage));
        var exceptionMsg = GetExceptionText(logMessage);
        if (!string.IsNullOrEmpty(exceptionMsg))
        {
            Debug.WriteLine(exceptionMsg);
        }
    }
}
