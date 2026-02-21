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
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class CaptureViewportTool
{
    [McpServerTool(Name = "capture_viewport"), Description("Captures a PNG screenshot of the 3D viewport for a scene that is open in the editor. The scene must already be open (use open_scene first). Returns the image as a base64-encoded PNG. Use this to visually verify entity placement, lighting, UI layout, and other visual aspects of the scene.")]
    public static async Task<IEnumerable<ContentBlock>> CaptureViewport(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the scene to capture")] string sceneId,
        CancellationToken cancellationToken = default)
    {
        // Get the screenshot service on the UI thread (Controller.GetService requires dispatcher)
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(sceneId, out var assetId))
            {
                return (error: "Invalid scene ID format. Expected a GUID.", service: (IEditorGameScreenshotService?)null);
            }

            var assetVm = session.GetAssetById(assetId);
            if (assetVm is not SceneViewModel sceneVm)
            {
                return (error: $"Scene not found: {sceneId}", service: (IEditorGameScreenshotService?)null);
            }

            var editorsManager = session.ServiceProvider.Get<IAssetEditorsManager>();
            if (!editorsManager.TryGetAssetEditor<EntityHierarchyEditorViewModel>(sceneVm, out var editor))
            {
                return (error: $"Scene is not open in the editor. Use open_scene first: {sceneId}", service: (IEditorGameScreenshotService?)null);
            }

            if (!editor.SceneInitialized)
            {
                return (error: "Scene editor is still initializing. Please wait and try again.", service: (IEditorGameScreenshotService?)null);
            }

            var screenshotService = editor.GetEditorGameService<IEditorGameScreenshotService>();
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
