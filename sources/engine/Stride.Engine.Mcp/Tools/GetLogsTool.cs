// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Diagnostics;

namespace Stride.Engine.Mcp.Tools
{
    [McpServerToolType]
    public sealed class GetLogsTool
    {
        [McpServerTool(Name = "get_logs"), Description("Returns recent log entries from the game. Can filter by minimum log level and limit the number of entries returned.")]
        public static Task<string> GetLogs(
            LogRingBuffer logRingBuffer,
            [Description("Maximum number of log entries to return (default: 100)")] int maxCount = 100,
            [Description("Minimum log level filter: Debug, Info, Warning, Error, or Fatal")] string minLevel = null,
            CancellationToken cancellationToken = default)
        {
            LogMessageType? minLevelParsed = null;
            if (!string.IsNullOrEmpty(minLevel))
            {
                if (Enum.TryParse<LogMessageType>(minLevel, ignoreCase: true, out var parsed))
                {
                    minLevelParsed = parsed;
                }
                else
                {
                    return Task.FromResult(JsonSerializer.Serialize(new { error = $"Invalid log level: {minLevel}. Valid values: Debug, Info, Warning, Error, Fatal" }));
                }
            }

            var entries = logRingBuffer.GetEntries(maxCount, minLevelParsed);

            var result = entries.Select(e => new
            {
                timestamp = e.Timestamp.ToString("O"),
                module = e.Module,
                level = e.Level.ToString(),
                message = e.Message,
                exceptionInfo = e.ExceptionInfo,
            }).ToArray();

            return Task.FromResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
