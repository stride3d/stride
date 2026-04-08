// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.ViewModel;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Services;
using Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels;
using Stride.Engine;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class DeleteEntityTool
{
    [McpServerTool(Name = "delete_entity"), Description("Deletes an entity from a scene. The scene must be open in the editor (use open_scene first). This also deletes all child entities. This operation supports undo/redo in the editor.")]
    public static async Task<string> DeleteEntity(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the scene containing the entity")] string sceneId,
        [Description("The entity ID to delete")] string entityId,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(sceneId, out var assetId))
            {
                return new { error = "Invalid scene ID format. Expected a GUID.", deleted = (object?)null };
            }

            if (!Guid.TryParse(entityId, out var entityGuid))
            {
                return new { error = "Invalid entity ID format. Expected a GUID.", deleted = (object?)null };
            }

            var assetVm = session.GetAssetById(assetId);
            if (assetVm is not SceneViewModel sceneVm)
            {
                return new { error = $"Scene not found: {sceneId}", deleted = (object?)null };
            }

            // Get the editor for this scene
            var editorsManager = session.ServiceProvider.Get<IAssetEditorsManager>();
            if (!editorsManager.TryGetAssetEditor<EntityHierarchyEditorViewModel>(sceneVm, out var editor))
            {
                return new { error = $"Scene is not open in the editor. Use open_scene first: {sceneId}", deleted = (object?)null };
            }

            // Find the entity to delete
            var absoluteId = new AbsoluteId(assetId, entityGuid);
            var partVm = editor.FindPartViewModel(absoluteId);
            if (partVm is not EntityViewModel entityVm)
            {
                return new { error = $"Entity not found in scene: {entityId}", deleted = (object?)null };
            }

            var entityName = entityVm.Name;
            var entityDesign = ((IEditorDesignPartViewModel<EntityDesign, Entity>)entityVm).PartDesign;

            // Clear selection to avoid stale references
            editor.ClearSelection();

            // Delete via the property graph with undo/redo support
            var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
            using (var transaction = undoRedoService.CreateTransaction())
            {
                sceneVm.AssetHierarchyPropertyGraph.DeleteParts(
                    new[] { entityDesign },
                    out var mapping);

                var operation = new DeletedPartsTrackingOperation<EntityDesign, Entity>(sceneVm, mapping);
                undoRedoService.PushOperation(operation);

                undoRedoService.SetName(transaction, $"Delete entity '{entityName}'");
            }

            return new
            {
                error = (string?)null,
                deleted = (object)new
                {
                    id = entityId,
                    name = entityName,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
