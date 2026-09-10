// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

namespace Stride.Core.Diagnostics;

/// <summary>
/// A <see cref="LogListener"/> implementation redirecting its output to a <see cref="Debug"/>.
/// </summary>
public class DebugLogListener : LogListener
{
    /// <inheritdoc/>
    protected override bool IsEnabled(ILogMessage logMessage)
    {
#if DEBUG
        return base.IsEnabled(logMessage);
#else
        // Debug.WriteLine is compiled out in this build, so nothing can consume the message.
        return false;
#endif
    }

    protected override void OnLog(ILogMessage logMessage)
    {
        Debug.WriteLine(GetDefaultText(logMessage));
        var exceptionMsg = GetExceptionText(logMessage);
        if (!string.IsNullOrEmpty(exceptionMsg))
        {
            Debug.WriteLine(exceptionMsg);
        }
    }
}
