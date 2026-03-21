// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
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
using Stride.Engine;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class CreateEntityTool
{
    [McpServerTool(Name = "create_entity"), Description("Creates a new entity in a scene. The scene must be open in the editor (use open_scene first). The new entity gets a TransformComponent by default. Returns the new entity's ID. This operation supports undo/redo in the editor.")]
    public static async Task<string> CreateEntity(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the scene to add the entity to")] string sceneId,
        [Description("Name for the new entity")] string name,
        [Description("Optional parent entity ID. If omitted, entity is added at the root level.")] string? parentId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(sceneId, out var assetId))
            {
                return new { error = "Invalid scene ID format. Expected a GUID.", entity = (object?)null };
            }

            var assetVm = session.GetAssetById(assetId);
            if (assetVm is not SceneViewModel sceneVm)
            {
                return new { error = $"Scene not found: {sceneId}", entity = (object?)null };
            }

            // Get the editor for this scene
            var editorsManager = session.ServiceProvider.Get<IAssetEditorsManager>();
            if (!editorsManager.TryGetAssetEditor<EntityHierarchyEditorViewModel>(sceneVm, out var editor))
            {
                return new { error = $"Scene is not open in the editor. Use open_scene first: {sceneId}", entity = (object?)null };
            }

            // Resolve the parent entity (if specified)
            Entity? parentEntity = null;
            if (!string.IsNullOrEmpty(parentId))
            {
                if (!Guid.TryParse(parentId, out var parentGuid))
                {
                    return new { error = $"Invalid parent entity ID format: {parentId}", entity = (object?)null };
                }

                var parentAbsId = new AbsoluteId(assetId, parentGuid);
                var parentVm = editor.FindPartViewModel(parentAbsId);
                if (parentVm is not EntityViewModel parentEntityVm)
                {
                    return new { error = $"Parent entity not found: {parentId}", entity = (object?)null };
                }
                parentEntity = parentEntityVm.AssetSideEntity;
            }

            // Create the new entity
            var newEntity = new Entity { Name = name ?? "Entity" };

            // Generate collection item IDs required by the asset system
            AssetCollectionItemIdHelper.GenerateMissingItemIds(newEntity);

            // Wrap in EntityDesign and add to the scene via property graph
            var collection = new AssetPartCollection<EntityDesign, Entity>
            {
                new EntityDesign(newEntity, "")
            };

            var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
            using (var transaction = undoRedoService.CreateTransaction())
            {
                int insertIndex = parentEntity?.Transform.Children.Count
                    ?? sceneVm.Asset.Hierarchy.RootParts.Count;

                sceneVm.AssetHierarchyPropertyGraph.AddPartToAsset(
                    collection,
                    collection.Single().Value,
                    parentEntity,
                    insertIndex);

                undoRedoService.SetName(transaction, $"Create entity '{name}'");
            }

            return new
            {
                error = (string?)null,
                entity = (object)new
                {
                    id = newEntity.Id.ToString(),
                    name = newEntity.Name,
                    parentId = parentEntity?.Id.ToString(),
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
