// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Linq;
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
public sealed class OpenSceneTool
{
    [McpServerTool(Name = "open_scene"), Description("Opens a scene asset in the editor. This will open the scene editor tab for the specified scene, allowing you to inspect and modify its entities. If the scene is already open, it will be activated/focused. Use get_editor_status or query_assets to find scene IDs first.")]
    public static async Task<string> OpenScene(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the scene to open (GUID from get_editor_status or query_assets)")] string sceneId,
        CancellationToken cancellationToken = default)
    {
        // Validate and resolve the scene asset on the UI thread
        var resolveResult = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(sceneId, out var assetId))
            {
                return (Error: "Invalid scene ID format. Expected a GUID.", Asset: (SceneViewModel?)null);
            }

            var assetVm = session.GetAssetById(assetId);
            if (assetVm is not SceneViewModel sceneVm)
            {
                return (Error: $"Scene not found or asset is not a scene: {sceneId}", Asset: (SceneViewModel?)null);
            }

            return (Error: (string?)null, Asset: sceneVm);
        }, cancellationToken);

        if (resolveResult.Error != null)
        {
            return JsonSerializer.Serialize(new { error = resolveResult.Error }, new JsonSerializerOptions { WriteIndented = true });
        }

        var sceneVm = resolveResult.Asset!;

        // Open the asset editor window — this must happen on the UI thread
        // OpenAssetEditorWindow is an async method that waits for the editor to load
        await dispatcher.InvokeTaskOnUIThread(async () =>
        {
            var editorsManager = session.ServiceProvider.Get<IAssetEditorsManager>();
            await editorsManager.OpenAssetEditorWindow(sceneVm);
        }, cancellationToken);

        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            return new
            {
                status = "opened",
                sceneId,
                sceneName = sceneVm.Name,
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
