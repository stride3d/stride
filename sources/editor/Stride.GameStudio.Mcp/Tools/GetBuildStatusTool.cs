// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class GetBuildStatusTool
{
    [McpServerTool(Name = "get_build_status"), Description("Returns the current build status. Use after build_project to check progress. Returns: 'idle' (no build), 'building' (in progress), 'succeeded', or 'failed' with error messages.")]
    public static Task<string> GetBuildStatus(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        CancellationToken cancellationToken = default)
    {
        var (wrapperTask, logger, lastProject, assemblyPath, isCanceled) = BuildProjectTool.GetBuildState();
        var projectFileName = lastProject != null ? Path.GetFileName(lastProject) : null;

        if (wrapperTask == null)
        {
            return Task.FromResult(JsonSerializer.Serialize(new
            {
                status = "idle",
                project = (string?)null,
                errors = (string[]?)null,
                warnings = (string[]?)null,
                assemblyPath = (string?)null,
            }, new JsonSerializerOptions { WriteIndented = true }));
        }

        if (!wrapperTask.IsCompleted)
        {
            return Task.FromResult(JsonSerializer.Serialize(new
            {
                status = "building",
                project = projectFileName,
                errors = (string[]?)null,
                warnings = (string[]?)null,
                assemblyPath = (string?)null,
            }, new JsonSerializerOptions { WriteIndented = true }));
        }

        if (isCanceled)
        {
            return Task.FromResult(JsonSerializer.Serialize(new
            {
                status = "canceled",
                project = projectFileName,
                errors = (string[]?)null,
                warnings = (string[]?)null,
                assemblyPath = (string?)null,
            }, new JsonSerializerOptions { WriteIndented = true }));
        }

        var hasErrors = logger?.HasErrors ?? false;
        var errorMessages = logger?.Messages
            .Where(m => m.Type == LogMessageType.Error || m.Type == LogMessageType.Fatal)
            .Select(m => m.Text)
            .ToArray();
        var warningMessages = logger?.Messages
            .Where(m => m.Type == LogMessageType.Warning)
            .Select(m => m.Text)
            .ToArray();

        return Task.FromResult(JsonSerializer.Serialize(new
        {
            status = hasErrors ? "failed" : "succeeded",
            project = projectFileName,
            errors = errorMessages?.Length > 0 ? errorMessages : null,
            warnings = warningMessages?.Length > 0 ? warningMessages : null,
            assemblyPath = !hasErrors ? assemblyPath : null,
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
