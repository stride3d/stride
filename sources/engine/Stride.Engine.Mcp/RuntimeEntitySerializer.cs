// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Engine;

namespace Stride.Engine.Mcp
{
    public static class RuntimeEntitySerializer
    {
        public static object SerializeEntity(Entity entity, bool includeComponents = true)
        {
            var result = new Dictionary<string, object>
            {
                ["id"] = entity.Id.ToString(),
                ["name"] = entity.Name ?? "(unnamed)",
                ["enabled"] = true,
            };

            if (entity.Transform != null)
            {
                result["position"] = SerializeVector3(entity.Transform.Position);
                result["rotation"] = SerializeQuaternion(entity.Transform.Rotation);
                result["scale"] = SerializeVector3(entity.Transform.Scale);
            }

            if (includeComponents)
            {
                var components = new List<object>();
                foreach (var component in entity.Components)
                {
                    components.Add(SerializeComponent(component));
                }
                result["components"] = components;
            }
            else
            {
                result["componentSummary"] = entity.Components.Select(c => c.GetType().Name).ToList();
            }

            return result;
        }

        public static object SerializeComponent(EntityComponent component)
        {
            var result = new Dictionary<string, object>
            {
                ["type"] = component.GetType().Name,
            };

            try
            {
                var properties = new Dictionary<string, object>();
                EnumerateDataMembers(component.GetType(), component, properties, 0);
                if (properties.Count > 0)
                    result["properties"] = properties;
            }
            catch
            {
                // If reflection fails, just return the type name
            }

            return result;
        }

        private static void EnumerateDataMembers(Type type, object instance, Dictionary<string, object> properties, int depth)
        {
            if (depth > 3)
                return;

            // Get fields and properties with [DataMember] attribute
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<DataMemberAttribute>() != null
                         && m.GetCustomAttribute<DataMemberIgnoreAttribute>() == null);

            foreach (var member in members)
            {
                try
                {
                    object value = null;
                    if (member is FieldInfo field)
                        value = field.GetValue(instance);
                    else if (member is PropertyInfo prop && prop.CanRead)
                        value = prop.GetValue(instance);
                    else
                        continue;

                    var name = member.GetCustomAttribute<DataMemberAttribute>()?.Name ?? member.Name;
                    properties[name] = SerializeValue(value, depth);
                }
                catch
                {
                    // Skip members that throw on access
                }
            }
        }

        private static object SerializeValue(object value, int depth)
        {
            if (value == null)
                return null;

            if (depth > 3)
                return value.ToString();

            var type = value.GetType();

            // Primitives and strings
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                return value;

            // Enums
            if (type.IsEnum)
                return value.ToString();

            // Guid
            if (type == typeof(Guid))
                return value.ToString();

            // Math types
            if (value is Vector2 v2) return new { x = v2.X, y = v2.Y };
            if (value is Vector3 v3) return SerializeVector3(v3);
            if (value is Vector4 v4) return new { x = v4.X, y = v4.Y, z = v4.Z, w = v4.W };
            if (value is Quaternion q) return SerializeQuaternion(q);
            if (value is Matrix m) return $"Matrix[{m.M11:F2}...]";
            if (value is Color c) return new { r = c.R, g = c.G, b = c.B, a = c.A };
            if (value is Color3 c3) return new { r = c3.R, g = c3.G, b = c3.B };
            if (value is Color4 c4) return new { r = c4.R, g = c4.G, b = c4.B, a = c4.A };

            // Asset references
            if (value is IReference reference)
            {
                return new Dictionary<string, object>
                {
                    ["type"] = reference.GetType().Name,
                    ["url"] = reference.Location?.ToString() ?? "(null)",
                };
            }

            // Entity references
            if (value is Entity entityRef)
            {
                return new Dictionary<string, object>
                {
                    ["type"] = "Entity",
                    ["id"] = entityRef.Id.ToString(),
                    ["name"] = entityRef.Name ?? "(unnamed)",
                };
            }

            // Collections
            if (value is IList list)
            {
                var items = new List<object>();
                var maxItems = Math.Min(list.Count, 10);
                for (int i = 0; i < maxItems; i++)
                {
                    items.Add(SerializeValue(list[i], depth + 1));
                }
                if (list.Count > 10)
                    items.Add($"... and {list.Count - 10} more");
                return items;
            }

            // Dictionaries
            if (value is IDictionary dict)
            {
                var dictResult = new Dictionary<string, object>();
                int count = 0;
                foreach (DictionaryEntry entry in dict)
                {
                    if (count++ >= 10) break;
                    dictResult[entry.Key?.ToString() ?? "null"] = SerializeValue(entry.Value, depth + 1);
                }
                return dictResult;
            }

            // Complex objects — recurse via [DataMember] reflection
            try
            {
                var properties = new Dictionary<string, object>();
                EnumerateDataMembers(type, value, properties, depth + 1);
                if (properties.Count > 0)
                    return properties;
            }
            catch
            {
                // Fall through to ToString
            }

            return value.ToString();
        }

        private static object SerializeVector3(Vector3 v) => new { x = v.X, y = v.Y, z = v.Z };
        private static object SerializeQuaternion(Quaternion q) => new { x = q.X, y = q.Y, z = q.Z, w = q.W };
    }
}
