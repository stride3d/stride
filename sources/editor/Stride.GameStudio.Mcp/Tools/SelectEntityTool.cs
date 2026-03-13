// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class SelectEntityTool
{
    [McpServerTool(Name = "select_entity"), Description("Selects one or more entities in the scene editor's hierarchy. The scene must already be open in the editor (use open_scene first). Selecting an entity will highlight it in the scene tree and show its properties in the property grid.")]
    public static async Task<string> SelectEntity(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the scene containing the entity")] string sceneId,
        [Description("The entity ID (GUID from get_scene_tree) to select. For multiple entities, separate IDs with commas.")] string entityId,
        [Description("If true, add to existing selection instead of replacing it (default: false)")] bool addToSelection = false,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(sceneId, out var assetId))
            {
                return new { error = "Invalid scene ID format. Expected a GUID.", selected = (object?)null };
            }

            var assetVm = session.GetAssetById(assetId);
            if (assetVm is not SceneViewModel sceneVm)
            {
                return new { error = $"Scene not found: {sceneId}", selected = (object?)null };
            }

            // Get the editor for this scene
            var editorsManager = session.ServiceProvider.Get<IAssetEditorsManager>();
            if (!editorsManager.TryGetAssetEditor<EntityHierarchyEditorViewModel>(sceneVm, out var editor))
            {
                return new { error = $"Scene is not open in the editor. Use open_scene first: {sceneId}", selected = (object?)null };
            }

            // Parse entity IDs (support comma-separated)
            var entityIdStrings = entityId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var entityViewModels = new System.Collections.Generic.List<EntityViewModel>();
            var errors = new System.Collections.Generic.List<string>();

            foreach (var idStr in entityIdStrings)
            {
                if (!Guid.TryParse(idStr, out var entityGuid))
                {
                    errors.Add($"Invalid entity ID format: {idStr}");
                    continue;
                }

                var absoluteId = new AbsoluteId(assetId, entityGuid);
                var partVm = editor.FindPartViewModel(absoluteId);
                if (partVm is EntityViewModel entityVm)
                {
                    entityViewModels.Add(entityVm);
                }
                else
                {
                    errors.Add($"Entity not found in scene: {idStr}");
                }
            }

            if (entityViewModels.Count == 0)
            {
                var errorMsg = errors.Count > 0
                    ? string.Join("; ", errors)
                    : "No valid entity IDs provided.";
                return new { error = errorMsg, selected = (object?)null };
            }

            // Perform the selection
            if (!addToSelection)
            {
                editor.ClearSelection();
            }

            foreach (var entityVm in entityViewModels)
            {
                editor.SelectedContent.Add(entityVm);
            }

            var selectedInfo = entityViewModels
                .Select(e => new { id = e.AssetSideEntity.Id.ToString(), name = e.Name })
                .ToList();

            return new
            {
                error = (string?)null,
                selected = (object)new
                {
                    count = selectedInfo.Count,
                    entities = selectedInfo,
                    warnings = errors.Count > 0 ? errors : null,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
