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
using Stride.Assets.Presentation.ViewModel;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Engine;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class GetSceneTreeTool
{
    [McpServerTool(Name = "get_scene_tree"), Description("Returns the full entity hierarchy tree for a given scene. Each entity includes its ID, name, and children. Use this to understand the structure of a scene before inspecting individual entities with get_entity.")]
    public static async Task<string> GetSceneTree(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the scene (from get_editor_status or query_assets)")] string sceneId,
        [Description("Maximum depth to traverse (default 50)")] int maxDepth = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(sceneId, out var assetId))
            {
                return new { error = "Invalid scene ID format. Expected a GUID.", scene = (object?)null };
            }

            var assetVm = session.GetAssetById(assetId);
            if (assetVm is not SceneViewModel sceneVm)
            {
                return new { error = $"Scene not found or asset is not a scene: {sceneId}", scene = (object?)null };
            }

            var sceneAsset = sceneVm.Asset;
            var rootEntities = sceneAsset.Hierarchy.RootParts;

            var entityTree = rootEntities
                .Select(e => BuildEntityNode(e, 0, maxDepth))
                .ToList();

            var totalCount = sceneAsset.Hierarchy.Parts.Count;

            return new
            {
                error = (string?)null,
                scene = (object)new
                {
                    id = sceneId,
                    name = sceneVm.Name,
                    entityCount = totalCount,
                    entities = entityTree,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    private static object BuildEntityNode(Entity entity, int depth, int maxDepth)
    {
        var children = new List<object>();

        if (depth < maxDepth && entity.Transform != null)
        {
            foreach (var childTransform in entity.Transform.Children)
            {
                if (childTransform.Entity != null)
                {
                    children.Add(BuildEntityNode(childTransform.Entity, depth + 1, maxDepth));
                }
            }
        }

        var components = entity.Components
            .Select(c => c.GetType().Name)
            .ToList();

        return new
        {
            id = entity.Id.ToString(),
            name = entity.Name,
            components,
            children,
        };
    }
}
