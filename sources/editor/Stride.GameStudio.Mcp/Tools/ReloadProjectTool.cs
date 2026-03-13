// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Commands;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class ReloadProjectTool
{
    [McpServerTool(Name = "reload_project"), Description("Triggers a full GameStudio restart to reload the entire project from disk. This is equivalent to File > Reload project in the editor. Use this when external tools have modified .csproj, .sln, or other project-level files that GameStudio needs to re-read. WARNING: The MCP connection will be lost when GameStudio restarts — the client must reconnect to the new instance. If there are unsaved changes, GameStudio will show a Save/Don't Save/Cancel dialog to the user.")]
    public static async Task<string> ReloadProject(
        DispatcherBridge dispatcher,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            var editorVm = EditorViewModel.Instance;
            if (editorVm == null)
            {
                return new { error = "EditorViewModel is not available.", result = (object?)null };
            }

            // GameStudioViewModel has a ReloadSessionCommand property — access via reflection
            // since we don't directly reference the Stride.GameStudio assembly
            var reloadProp = editorVm.GetType().GetProperty("ReloadSessionCommand");
            if (reloadProp == null)
            {
                return new { error = "ReloadSessionCommand not found on the editor view model.", result = (object?)null };
            }

            var command = reloadProp.GetValue(editorVm) as ICommandBase;
            if (command == null)
            {
                return new { error = "ReloadSessionCommand is not an ICommandBase.", result = (object?)null };
            }

            // Fire the reload command — this will trigger the close/restart sequence asynchronously.
            // GameStudio will show a save dialog if there are unsaved changes, then restart.
            command.Execute();

            return new
            {
                error = (string?)null,
                result = (object)new
                {
                    status = "reload_initiated",
                    message = "GameStudio is restarting. The MCP connection will be lost. Reconnect to the new instance after restart.",
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
