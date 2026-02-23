// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class CaptureViewportTool
{
    [McpServerTool(Name = "capture_viewport"), Description("IMPORTANT: This is the primary way to verify your changes and the only way to see the actual rendered result. Captures a PNG screenshot of the viewport for a scene or UI page that is open in the editor. The asset must already be open (use open_scene or open_ui_page first). Returns the image as a base64-encoded PNG. Use this to visually verify entity placement, lighting, UI layout, model references, and other visual aspects after making modifications.")]
    public static async Task<IEnumerable<ContentBlock>> CaptureViewport(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the scene or UI page to capture")] string assetId,
        CancellationToken cancellationToken = default)
    {
        // Get the screenshot service on the UI thread (Controller.GetService requires dispatcher)
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(assetId, out var id))
            {
                return (error: "Invalid asset ID format. Expected a GUID.", service: (IEditorGameScreenshotService?)null);
            }

            var assetVm = session.GetAssetById(id);
            if (assetVm == null)
            {
                return (error: $"Asset not found: {assetId}", service: (IEditorGameScreenshotService?)null);
            }

            // Verify the asset type supports viewport capture
            if (assetVm is not SceneViewModel && assetVm is not UIPageViewModel)
            {
                return (error: $"Asset type '{assetVm.AssetType.Name}' does not have a viewport. Only scenes and UI pages support viewport capture.", service: (IEditorGameScreenshotService?)null);
            }

            var editorsManager = session.ServiceProvider.Get<IAssetEditorsManager>();
            if (!editorsManager.TryGetAssetEditor<GameEditorViewModel>(assetVm, out var editor))
            {
                var openHint = assetVm is SceneViewModel ? "open_scene" : "open_ui_page";
                return (error: $"Asset is not open in the editor. Use {openHint} first: {assetId}", service: (IEditorGameScreenshotService?)null);
            }

            if (!editor.SceneInitialized)
            {
                return (error: "Editor is still initializing. Please wait and try again.", service: (IEditorGameScreenshotService?)null);
            }

            var screenshotService = editor.GetEditorGameService<IEditorGameScreenshotService>();
            if (screenshotService == null)
            {
                return (error: "Screenshot service is not available for this editor.", service: (IEditorGameScreenshotService?)null);
            }

            return (error: (string?)null, service: (IEditorGameScreenshotService?)screenshotService);
        }, cancellationToken);

        if (result.error != null)
        {
            return [new TextContentBlock { Text = result.error }];
        }

        try
        {
            var pngBytes = await result.service!.CaptureViewportAsync();
            var base64 = Convert.ToBase64String(pngBytes);

            return
            [
                new ImageContentBlock
                {
                    Data = base64,
                    MimeType = "image/png",
                },
            ];
        }
        catch (Exception ex)
        {
            return [new TextContentBlock { Text = $"Failed to capture viewport: {ex.Message}" }];
        }
    }
}
