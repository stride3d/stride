// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Diagnostics;

namespace Stride.GameStudio.Logs;

/// <summary>
/// Keeps the warnings and errors of the global log (<see cref="GlobalLogger"/>) from startup, and forwards them
/// to the Editor tab of the Output panel once it exists.
/// </summary>
internal sealed class GlobalLogBuffer
{
    private readonly object syncRoot = new();
    private readonly List<ILogMessage> messages = [];
    private Logger target;

    public static GlobalLogBuffer Instance { get; } = new();

    public void Start() => GlobalLogger.GlobalMessageLogged += OnMessageLogged;

    /// <summary>
    /// Forwards the kept messages, then the new ones, to the given logger.
    /// </summary>
    public void ForwardTo(Logger logger)
    {
        ILogMessage[] past;
        lock (syncRoot)
        {
            past = messages.ToArray();
            target = logger;
        }

        // Outside the lock: Log takes the lock of all loggers, which is held while OnMessageLogged runs
        foreach (var message in past)
            logger.Log(message);
    }

    private void OnMessageLogged(ILogMessage message)
    {
        if (message.Type < LogMessageType.Warning)
            return;

        Logger currentTarget;
        lock (syncRoot)
        {
            messages.Add(message);
            currentTarget = target;
        }
        currentTarget?.Log(message);
    }
}
