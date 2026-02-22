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
using Stride.Assets.Presentation.ViewModel;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Engine;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class GetEntityTool
{
    [McpServerTool(Name = "get_entity"), Description("Returns detailed information about a specific entity, including all its components and their properties. The entityId is the entity GUID (from get_scene_tree), and sceneId is needed to locate the entity within the scene hierarchy.")]
    public static async Task<string> GetEntity(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the scene containing the entity")] string sceneId,
        [Description("The entity ID (GUID from get_scene_tree)")] string entityId,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(sceneId, out var sceneAssetId))
            {
                return new { error = "Invalid scene ID format. Expected a GUID.", entity = (object?)null };
            }

            if (!Guid.TryParse(entityId, out var entityGuid))
            {
                return new { error = "Invalid entity ID format. Expected a GUID.", entity = (object?)null };
            }

            var assetVm = session.GetAssetById(sceneAssetId);
            if (assetVm is not SceneViewModel sceneVm)
            {
                return new { error = $"Scene not found: {sceneId}", entity = (object?)null };
            }

            var sceneAsset = sceneVm.Asset;
            if (!sceneAsset.Hierarchy.Parts.TryGetValue(entityGuid, out var entityDesign))
            {
                return new { error = $"Entity not found in scene: {entityId}", entity = (object?)null };
            }

            var entity = entityDesign.Entity;
            var parentId = (string?)null;
            if (entity.Transform?.Parent != null)
            {
                parentId = entity.Transform.Parent.Entity?.Id.ToString();
            }

            var childIds = entity.Transform?.Children
                .Where(c => c.Entity != null)
                .Select(c => c.Entity.Id.ToString())
                .ToList() ?? [];

            var components = entity.Components
                .Select(c => SerializeComponent(c))
                .ToList();

            return new
            {
                error = (string?)null,
                entity = (object)new
                {
                    id = entity.Id.ToString(),
                    name = entity.Name,
                    parentId,
                    childIds,
                    components,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    private static object SerializeComponent(EntityComponent component)
    {
        var properties = JsonTypeConverter.SerializeDataMembers(component);
        return new
        {
            type = component.GetType().Name,
            properties,
        };
    }
}
