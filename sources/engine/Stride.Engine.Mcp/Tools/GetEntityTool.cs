// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;

namespace Stride.Engine.Mcp.Tools
{
    [McpServerToolType]
    public sealed class GetEntityTool
    {
        [McpServerTool(Name = "get_entity"), Description("Returns detailed information about a specific entity including all its components and their serialized properties. Search by entity ID (GUID) or name (first match).")]
        public static async Task<string> GetEntity(
            GameBridge bridge,
            [Description("Entity GUID to look up")] string entityId = null,
            [Description("Entity name to look up (first match)")] string entityName = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(entityId) && string.IsNullOrEmpty(entityName))
            {
                return JsonSerializer.Serialize(new { error = "Either entityId or entityName must be provided" });
            }

            var result = await bridge.RunOnGameThread(game =>
            {
                var rootScene = game.SceneSystem?.SceneInstance?.RootScene;
                if (rootScene == null)
                    return new { error = "No scene loaded" };

                Entity found = null;

                if (!string.IsNullOrEmpty(entityId) && Guid.TryParse(entityId, out var guid))
                {
                    found = FindEntityById(rootScene, guid);
                }
                else if (!string.IsNullOrEmpty(entityName))
                {
                    found = FindEntityByName(rootScene, entityName);
                }

                if (found == null)
                {
                    return (object)new { error = $"Entity not found: {entityId ?? entityName}" };
                }

                return RuntimeEntitySerializer.SerializeEntity(found, includeComponents: true);
            }, cancellationToken);

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }

        internal static Entity FindEntityById(Scene scene, Guid id)
        {
            foreach (var entity in scene.Entities)
            {
                var found = FindEntityByIdRecursive(entity, id);
                if (found != null)
                    return found;
            }
            foreach (var child in scene.Children)
            {
                var found = FindEntityById(child, id);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static Entity FindEntityByIdRecursive(Entity entity, Guid id)
        {
            if (entity.Id == id)
                return entity;
            if (entity.Transform != null)
            {
                foreach (var childTransform in entity.Transform.Children)
                {
                    if (childTransform.Entity != null)
                    {
                        var found = FindEntityByIdRecursive(childTransform.Entity, id);
                        if (found != null)
                            return found;
                    }
                }
            }
            return null;
        }

        internal static Entity FindEntityByName(Scene scene, string name)
        {
            foreach (var entity in scene.Entities)
            {
                var found = FindEntityByNameRecursive(entity, name);
                if (found != null)
                    return found;
            }
            foreach (var child in scene.Children)
            {
                var found = FindEntityByName(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static Entity FindEntityByNameRecursive(Entity entity, string name)
        {
            if (string.Equals(entity.Name, name, StringComparison.OrdinalIgnoreCase))
                return entity;
            if (entity.Transform != null)
            {
                foreach (var childTransform in entity.Transform.Children)
                {
                    if (childTransform.Entity != null)
                    {
                        var found = FindEntityByNameRecursive(childTransform.Entity, name);
                        if (found != null)
                            return found;
                    }
                }
            }
            return null;
        }
    }
}
