// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Stride.Core.Diagnostics;

namespace Stride.Engine.Mcp
{
    public sealed class LogRingBuffer : IDisposable
    {
        private readonly LogEntry[] buffer;
        private int writeIndex;
        private int count;
        private readonly object syncLock = new();

        public LogRingBuffer(int capacity = 500)
        {
            buffer = new LogEntry[capacity];
            GlobalLogger.GlobalMessageLogged += OnMessageLogged;
        }

        private void OnMessageLogged(ILogMessage logMessage)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Module = logMessage.Module,
                Level = logMessage.Type,
                Message = logMessage.Text,
                ExceptionInfo = (logMessage as LogMessage)?.Exception?.ToString(),
            };

            lock (syncLock)
            {
                buffer[writeIndex] = entry;
                writeIndex = (writeIndex + 1) % buffer.Length;
                if (count < buffer.Length)
                    count++;
            }
        }

        public LogEntry[] GetEntries(int? maxCount = null, LogMessageType? minLevel = null)
        {
            lock (syncLock)
            {
                var entries = new List<LogEntry>();
                var startIndex = count < buffer.Length ? 0 : writeIndex;

                for (int i = 0; i < count; i++)
                {
                    var idx = (startIndex + i) % buffer.Length;
                    var entry = buffer[idx];
                    if (minLevel.HasValue && entry.Level < minLevel.Value)
                        continue;
                    entries.Add(entry);
                }

                if (maxCount.HasValue && entries.Count > maxCount.Value)
                {
                    entries = entries.GetRange(entries.Count - maxCount.Value, maxCount.Value);
                }

                return entries.ToArray();
            }
        }

        public void Dispose()
        {
            GlobalLogger.GlobalMessageLogged -= OnMessageLogged;
        }
    }

    public readonly struct LogEntry
    {
        public DateTime Timestamp { get; init; }
        public string Module { get; init; }
        public LogMessageType Level { get; init; }
        public string Message { get; init; }
        public string ExceptionInfo { get; init; }
    }
}
