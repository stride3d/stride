// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
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
public sealed class FocusEntityTool
{
    [McpServerTool(Name = "focus_entity"), Description("Focuses the viewport camera on a specific entity, centering it in the scene view. The scene must already be open in the editor (use open_scene first). This also selects the entity. Use this to navigate the 3D viewport to inspect specific objects.")]
    public static async Task<string> FocusEntity(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the scene containing the entity")] string sceneId,
        [Description("The entity ID (GUID from get_scene_tree) to focus on")] string entityId,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(sceneId, out var assetId))
            {
                return new { error = "Invalid scene ID format. Expected a GUID.", focused = (object?)null };
            }

            if (!Guid.TryParse(entityId, out var entityGuid))
            {
                return new { error = "Invalid entity ID format. Expected a GUID.", focused = (object?)null };
            }

            var assetVm = session.GetAssetById(assetId);
            if (assetVm is not SceneViewModel sceneVm)
            {
                return new { error = $"Scene not found: {sceneId}", focused = (object?)null };
            }

            // Get the editor for this scene
            var editorsManager = session.ServiceProvider.Get<IAssetEditorsManager>();
            if (!editorsManager.TryGetAssetEditor<EntityHierarchyEditorViewModel>(sceneVm, out var editor))
            {
                return new { error = $"Scene is not open in the editor. Use open_scene first: {sceneId}", focused = (object?)null };
            }

            // Find the entity view model
            var absoluteId = new AbsoluteId(assetId, entityGuid);
            var partVm = editor.FindPartViewModel(absoluteId);
            if (partVm is not EntityViewModel entityVm)
            {
                return new { error = $"Entity not found in scene: {entityId}", focused = (object?)null };
            }

            // Check if the entity is loaded in the game engine (required for camera focus)
            if (!entityVm.IsLoaded)
            {
                return new { error = $"Entity is not loaded in the viewport. It may be in an unloaded sub-scene: {entityId}", focused = (object?)null };
            }

            // Select the entity first
            editor.ClearSelection();
            editor.SelectedContent.Add(entityVm);

            // Use the entity's built-in FocusOnEntityCommand to center the camera
            // This invokes IEditorGameEntityCameraViewModelService.CenterOnEntity internally
            if (entityVm.FocusOnEntityCommand.IsEnabled)
            {
                entityVm.FocusOnEntityCommand.Execute();
            }
            else
            {
                return new
                {
                    error = (string?)null,
                    focused = (object)new
                    {
                        id = entityVm.AssetSideEntity.Id.ToString(),
                        name = entityVm.Name,
                        warning = "Focus command is not available. Entity was selected but camera was not moved.",
                    },
                };
            }

            return new
            {
                error = (string?)null,
                focused = (object)new
                {
                    id = entityVm.AssetSideEntity.Id.ToString(),
                    name = entityVm.Name,
                    warning = (string?)null,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
