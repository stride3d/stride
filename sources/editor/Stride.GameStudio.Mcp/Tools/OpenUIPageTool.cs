// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
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
public sealed class OpenUIPageTool
{
    [McpServerTool(Name = "open_ui_page"), Description("Opens a UI page asset in the editor. This will open the UI page editor tab, allowing you to inspect and modify its elements. If the page is already open, it will be activated/focused. Use query_assets with type 'UIPageAsset' to find UI page IDs.")]
    public static async Task<string> OpenUIPage(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the UI page to open (GUID from query_assets)")] string assetId,
        CancellationToken cancellationToken = default)
    {
        var resolveResult = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(assetId, out var id))
            {
                return (Error: "Invalid asset ID format. Expected a GUID.", Asset: (UIPageViewModel?)null);
            }

            var assetVm = session.GetAssetById(id);
            if (assetVm is not UIPageViewModel uiPageVm)
            {
                return (Error: $"Asset not found or is not a UI page: {assetId}", Asset: (UIPageViewModel?)null);
            }

            return (Error: (string?)null, Asset: uiPageVm);
        }, cancellationToken);

        if (resolveResult.Error != null)
        {
            return JsonSerializer.Serialize(new { error = resolveResult.Error }, new JsonSerializerOptions { WriteIndented = true });
        }

        var uiPageVm = resolveResult.Asset!;

        await dispatcher.InvokeTaskOnUIThread(async () =>
        {
            var editorsManager = session.ServiceProvider.Get<IAssetEditorsManager>();
            await editorsManager.OpenAssetEditorWindow(uiPageVm);
        }, cancellationToken);

        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            return new
            {
                status = "opened",
                assetId,
                name = uiPageVm.Name,
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
