// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;

namespace Stride.Engine.Mcp.Tools
{
    [McpServerToolType]
    public sealed class GetSceneEntitiesTool
    {
        [McpServerTool(Name = "get_scene_entities"), Description("Returns the entity hierarchy of the active scene as a tree. Each node includes id, name, enabled state, position, child count, component summary, and children.")]
        public static async Task<string> GetSceneEntities(
            GameBridge bridge,
            [Description("Maximum depth of the hierarchy tree (default: 3)")] int maxDepth = 3,
            CancellationToken cancellationToken = default)
        {
            var result = await bridge.RunOnGameThread(game =>
            {
                var rootScene = game.SceneSystem?.SceneInstance?.RootScene;
                if (rootScene == null)
                    return new { error = "No scene loaded" };

                var entities = new List<object>();
                foreach (var entity in rootScene.Entities)
                {
                    entities.Add(SerializeEntityNode(entity, 0, maxDepth));
                }

                return (object)new
                {
                    sceneName = rootScene.Name ?? "(root)",
                    entities,
                };
            }, cancellationToken);

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }

        private static object SerializeEntityNode(Entity entity, int currentDepth, int maxDepth)
        {
            var node = new Dictionary<string, object>
            {
                ["id"] = entity.Id.ToString(),
                ["name"] = entity.Name ?? "(unnamed)",
                ["enabled"] = true,
            };

            if (entity.Transform != null)
            {
                node["position"] = new
                {
                    x = entity.Transform.Position.X,
                    y = entity.Transform.Position.Y,
                    z = entity.Transform.Position.Z,
                };
            }

            node["componentSummary"] = entity.Components.Select(c => c.GetType().Name).ToList();
            node["childCount"] = entity.Transform?.Children.Count ?? 0;

            if (currentDepth < maxDepth && entity.Transform != null)
            {
                var children = new List<object>();
                foreach (var childTransform in entity.Transform.Children)
                {
                    if (childTransform.Entity != null)
                    {
                        children.Add(SerializeEntityNode(childTransform.Entity, currentDepth + 1, maxDepth));
                    }
                }
                node["children"] = children;
            }

            return node;
        }
    }
}
