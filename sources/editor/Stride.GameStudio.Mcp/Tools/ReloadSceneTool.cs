// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class ReloadSceneTool
{
    [McpServerTool(Name = "reload_scene"), Description("Closes and reopens a scene editor tab to refresh its in-memory state. Useful after build_project completes (to pick up new script component types) or when the scene editor appears stale. WARNING: Any unsaved in-editor changes to this scene will be discarded. Save first with save_project if you want to keep changes.")]
    public static async Task<string> ReloadScene(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the scene to reload")] string sceneId,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeTaskOnUIThread(async () =>
        {
            if (!AssetId.TryParse(sceneId, out var assetId))
            {
                return new { error = "Invalid scene ID format. Expected a GUID.", result = (object?)null };
            }

            var assetVm = session.GetAssetById(assetId);
            if (assetVm is not SceneViewModel sceneVm)
            {
                return new { error = $"Scene not found: {sceneId}", result = (object?)null };
            }

            var editorsManager = session.ServiceProvider.Get<IAssetEditorsManager>();

            // Close the scene editor without saving
            var closed = editorsManager.CloseAssetEditorWindow(sceneVm, save: false);
            if (!closed)
            {
                return new { error = "Failed to close the scene editor tab.", result = (object?)null };
            }

            // Reopen the scene editor
            await editorsManager.OpenAssetEditorWindow(sceneVm);

            return new
            {
                error = (string?)null,
                result = (object)new
                {
                    status = "reloaded",
                    sceneId = sceneVm.Id.ToString(),
                    sceneName = sceneVm.Name,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
