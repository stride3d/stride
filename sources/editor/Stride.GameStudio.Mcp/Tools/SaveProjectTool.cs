// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class SaveProjectTool
{
    [McpServerTool(Name = "save_project"), Description("Saves all pending changes (scenes, entities, components, assets, etc.) to disk. Use this to persist your work — changes made through MCP tools are held in memory until saved. Note: build_project and restart_game_studio auto-save, so you don't need to call this before those. Use save_project when you want to checkpoint your work, or before external tools need to read the on-disk files. WARNING: This writes the editor's in-memory state to disk and will overwrite any external changes made to asset files outside of Game Studio.")]
    public static async Task<string> SaveProject(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        CancellationToken cancellationToken = default)
    {
        var success = await dispatcher.InvokeTaskOnUIThread(async () =>
        {
            return await session.SaveSession();
        }, cancellationToken);

        var result = new
        {
            error = success ? (string?)null : "Save failed. Check the editor log for details.",
            result = new
            {
                status = success ? "saved" : "failed",
            },
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
