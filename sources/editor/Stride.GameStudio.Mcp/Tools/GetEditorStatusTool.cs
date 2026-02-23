// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class GetEditorStatusTool
{
    [McpServerTool(Name = "get_editor_status"), Description("Returns the current status of Stride Game Studio, including the loaded project name, solution path, and list of available scenes. Use this tool first to verify the editor is connected and to discover available content.")]
    public static async Task<string> GetEditorStatus(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        CancellationToken cancellationToken)
    {
        var status = await dispatcher.InvokeOnUIThread(() =>
        {
            var currentProject = session.CurrentProject?.Name ?? "(no project)";
            var solutionPath = session.SolutionPath?.ToString() ?? "(none)";

            var packages = session.LocalPackages
                .Select(p => p.Name)
                .ToList();

            var projects = session.LocalPackages
                .OfType<ProjectViewModel>()
                .Select(p => new
                {
                    name = p.Name,
                    type = p.Type.ToString(),
                    platform = p.Platform.ToString(),
                    isCurrentProject = p.IsCurrentProject,
                })
                .ToList();

            var allAssets = session.AllAssets.ToList();
            var scenes = allAssets
                .Where(a => a.AssetType.Name == "SceneAsset")
                .Select(a => new
                {
                    id = a.Id.ToString(),
                    name = a.Name,
                    url = a.Url,
                })
                .ToList();

            var assetCount = allAssets.Count;

            return new
            {
                status = "connected",
                currentProject,
                solutionPath,
                packages,
                projects,
                assetCount,
                scenes,
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true });
    }
}
