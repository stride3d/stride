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
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels;
using Stride.Engine;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class ReparentEntityTool
{
    [McpServerTool(Name = "reparent_entity"), Description("Changes the parent of an entity in the scene hierarchy. The scene must be open in the editor (use open_scene first). Set newParentId to null/empty to move the entity to the root level. This operation supports undo/redo in the editor.")]
    public static async Task<string> ReparentEntity(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the scene containing the entity")] string sceneId,
        [Description("The entity ID to reparent")] string entityId,
        [Description("The new parent entity ID, or omit/null to move to root level")] string? newParentId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(sceneId, out var assetId))
            {
                return new { error = "Invalid scene ID format. Expected a GUID.", reparented = (object?)null };
            }

            if (!Guid.TryParse(entityId, out var entityGuid))
            {
                return new { error = "Invalid entity ID format. Expected a GUID.", reparented = (object?)null };
            }

            var assetVm = session.GetAssetById(assetId);
            if (assetVm is not SceneViewModel sceneVm)
            {
                return new { error = $"Scene not found: {sceneId}", reparented = (object?)null };
            }

            // Get the editor for this scene
            var editorsManager = session.ServiceProvider.Get<IAssetEditorsManager>();
            if (!editorsManager.TryGetAssetEditor<EntityHierarchyEditorViewModel>(sceneVm, out var editor))
            {
                return new { error = $"Scene is not open in the editor. Use open_scene first: {sceneId}", reparented = (object?)null };
            }

            // Find the entity to reparent
            var absoluteId = new AbsoluteId(assetId, entityGuid);
            var partVm = editor.FindPartViewModel(absoluteId);
            if (partVm is not EntityViewModel entityVm)
            {
                return new { error = $"Entity not found in scene: {entityId}", reparented = (object?)null };
            }

            // Resolve new parent (if specified)
            Entity? newParentEntity = null;
            if (!string.IsNullOrEmpty(newParentId))
            {
                if (!Guid.TryParse(newParentId, out var newParentGuid))
                {
                    return new { error = $"Invalid new parent entity ID format: {newParentId}", reparented = (object?)null };
                }

                var newParentAbsId = new AbsoluteId(assetId, newParentGuid);
                var newParentPartVm = editor.FindPartViewModel(newParentAbsId);
                if (newParentPartVm is not EntityViewModel newParentEntityVm)
                {
                    return new { error = $"New parent entity not found: {newParentId}", reparented = (object?)null };
                }

                // Prevent circular parenting: check if the new parent is a descendant of the entity
                var currentParent = newParentEntityVm.TransformParent;
                while (currentParent != null)
                {
                    if (currentParent is EntityViewModel pvm && pvm.AssetSideEntity.Id == entityGuid)
                    {
                        return new { error = "Cannot reparent: new parent is a descendant of the entity (circular reference).", reparented = (object?)null };
                    }
                    currentParent = currentParent.TransformParent;
                }

                newParentEntity = newParentEntityVm.AssetSideEntity;
            }

            var entityName = entityVm.Name;
            var entityDesign = ((IEditorDesignPartViewModel<EntityDesign, Entity>)entityVm).PartDesign;
            var entity = entityVm.AssetSideEntity;

            // Perform reparent: clone → remove → re-add
            var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
            using (var transaction = undoRedoService.CreateTransaction())
            {
                // Clone the sub-hierarchy before removal
                var hierarchy = AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>.CloneSubHierarchies(
                    session.AssetNodeContainer,
                    sceneVm.Asset,
                    new[] { entity.Id },
                    SubHierarchyCloneFlags.None,
                    out _);

                // Remove from current position
                sceneVm.AssetHierarchyPropertyGraph.RemovePartFromAsset(entityDesign);

                // Add to new position
                var movedEntityDesign = hierarchy.Parts[entity.Id];
                int insertIndex = newParentEntity?.Transform.Children.Count
                    ?? sceneVm.Asset.Hierarchy.RootParts.Count;

                sceneVm.AssetHierarchyPropertyGraph.AddPartToAsset(
                    hierarchy.Parts,
                    movedEntityDesign,
                    newParentEntity,
                    insertIndex);

                undoRedoService.SetName(transaction, $"Reparent entity '{entityName}'");
            }

            return new
            {
                error = (string?)null,
                reparented = (object)new
                {
                    id = entityId,
                    name = entityName,
                    newParentId = newParentEntity?.Id.ToString(),
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
