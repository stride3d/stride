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
    [McpServerTool(Name = "save_project"), Description("Saves the current project/session to disk. Call this after making any modifications (scenes, entities, components, assets, properties, etc.) to persist changes. Always save before building the project. Returns whether the save was successful.")]
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
