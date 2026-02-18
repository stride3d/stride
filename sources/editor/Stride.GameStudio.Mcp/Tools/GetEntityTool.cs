// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Assets.Presentation.ViewModel;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Mathematics;
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
        var type = component.GetType();
        var properties = new Dictionary<string, object?>();

        // Collect [DataMember] fields and properties
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.GetCustomAttribute<DataMemberAttribute>() == null)
                continue;

            try
            {
                var value = field.GetValue(component);
                properties[field.Name] = ConvertValue(value);
            }
            catch
            {
                properties[field.Name] = "<error reading value>";
            }
        }

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetCustomAttribute<DataMemberAttribute>() == null)
                continue;
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;

            try
            {
                var value = prop.GetValue(component);
                properties[prop.Name] = ConvertValue(value);
            }
            catch
            {
                properties[prop.Name] = "<error reading value>";
            }
        }

        return new
        {
            type = type.Name,
            properties,
        };
    }

    private static object? ConvertValue(object? value, int depth = 0)
    {
        if (value == null)
            return null;

        if (depth > 3)
            return value.ToString();

        var type = value.GetType();

        // Primitives and strings
        if (type.IsPrimitive || value is string || value is decimal)
            return value;

        // Enums
        if (type.IsEnum)
            return value.ToString();

        // Stride math types
        if (value is Vector2 v2)
            return new { x = v2.X, y = v2.Y };
        if (value is Vector3 v3)
            return new { x = v3.X, y = v3.Y, z = v3.Z };
        if (value is Vector4 v4)
            return new { x = v4.X, y = v4.Y, z = v4.Z, w = v4.W };
        if (value is Quaternion q)
            return new { x = q.X, y = q.Y, z = q.Z, w = q.W };
        if (value is Color c)
            return new { r = c.R, g = c.G, b = c.B, a = c.A };
        if (value is Color3 c3)
            return new { r = c3.R, g = c3.G, b = c3.B };
        if (value is Color4 c4)
            return new { r = c4.R, g = c4.G, b = c4.B, a = c4.A };
        if (value is Matrix m)
            return value.ToString();

        // Collections (limited)
        if (value is IEnumerable enumerable && value is not string)
        {
            var items = new List<object?>();
            var count = 0;
            foreach (var item in enumerable)
            {
                if (count++ >= 20) // Limit collection output
                {
                    items.Add($"... ({count}+ items total)");
                    break;
                }
                items.Add(ConvertValue(item, depth + 1));
            }
            return items;
        }

        // Entity references
        if (value is Entity entity)
            return new { entityRef = entity.Id.ToString(), name = entity.Name };
        if (value is EntityComponent comp)
            return new { componentRef = comp.GetType().Name, entityId = comp.Entity?.Id.ToString() };

        // Fallback: just return string representation
        return value.ToString();
    }
}
