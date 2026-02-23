// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class SetActiveProjectTool
{
    [McpServerTool(Name = "set_active_project"), Description("Changes which project is active in the editor. The active project determines which project is built and which assets are shown as root. Use get_editor_status to see available projects.")]
    public static async Task<string> SetActiveProject(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("Name of the project to activate (case-insensitive)")] string projectName,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            var projects = session.LocalPackages
                .OfType<ProjectViewModel>()
                .ToList();

            var projectVm = projects
                .FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));

            if (projectVm == null)
            {
                var availableNames = projects.Select(p => p.Name).ToArray();
                return new
                {
                    error = $"Project not found: '{projectName}'. Available projects: {string.Join(", ", availableNames)}",
                    project = (object?)null,
                };
            }

            // Execute the set current project command
            session.SetCurrentProjectCommand.Execute(projectVm);

            return new
            {
                error = (string?)null,
                project = (object)new
                {
                    name = projectVm.Name,
                    type = projectVm.Type.ToString(),
                    platform = projectVm.Platform.ToString(),
                    isCurrentProject = projectVm.IsCurrentProject,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
