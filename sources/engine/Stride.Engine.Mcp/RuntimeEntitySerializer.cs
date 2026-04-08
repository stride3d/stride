// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
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
            if (value is float f && (float.IsNaN(f) || float.IsInfinity(f)))
                return f.ToString();
            if (value is double d && (double.IsNaN(d) || double.IsInfinity(d)))
                return d.ToString();
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                return value;

            // Enums
            if (type.IsEnum)
                return value.ToString();

            // Guid
            if (type == typeof(Guid))
                return value.ToString();

            // Float vector types
            if (value is Vector2 v2) return new { x = v2.X, y = v2.Y };
            if (value is Vector3 v3) return SerializeVector3(v3);
            if (value is Vector4 v4) return new { x = v4.X, y = v4.Y, z = v4.Z, w = v4.W };
            if (value is Quaternion q) return SerializeQuaternion(q);

            // Integer vector types
            if (value is Int2 i2) return new { x = i2.X, y = i2.Y };
            if (value is Int3 i3) return new { x = i3.X, y = i3.Y, z = i3.Z };
            if (value is Int4 i4) return new { x = i4.X, y = i4.Y, z = i4.Z, w = i4.W };

            // Colors
            if (value is Color c) return new { r = (int)c.R, g = (int)c.G, b = (int)c.B, a = (int)c.A };
            if (value is Color3 c3) return new { r = c3.R, g = c3.G, b = c3.B };
            if (value is Color4 c4) return new { r = c4.R, g = c4.G, b = c4.B, a = c4.A };

            // Rectangles and sizes
            if (value is RectangleF rf) return new { x = rf.X, y = rf.Y, width = rf.Width, height = rf.Height };
            if (value is Rectangle ri) return new { x = ri.X, y = ri.Y, width = ri.Width, height = ri.Height };
            if (value is Size2 s2) return new { width = s2.Width, height = s2.Height };
            if (value is Size2F s2f) return new { width = s2f.Width, height = s2f.Height };
            if (value is Size3 s3) return new { width = s3.Width, height = s3.Height, depth = s3.Depth };

            // Angle
            if (value is AngleSingle angle) return new { degrees = angle.Degrees };

            // Matrix — too large for structured serialization
            if (value is Matrix) return value.ToString();

            // Asset references — explicit IReference types (AssetReference, UrlReferenceBase)
            if (value is IReference reference)
                return new { assetRef = reference.Id.ToString(), url = reference.Location?.ToString() };

            // Asset references — proxy objects with AttachedReference (Model, Material, Texture, etc.)
            var attachedRef = AttachedReferenceManager.GetAttachedReference(value);
            if (attachedRef != null)
                return new { assetRef = attachedRef.Id.ToString(), url = attachedRef.Url };

            // Entity references
            if (value is Entity entityRef)
                return new { entityRef = entityRef.Id.ToString(), name = entityRef.Name };
            if (value is EntityComponent comp)
                return new { componentRef = comp.GetType().Name, entityId = comp.Entity?.Id.ToString() };

            // Collections
            if (value is IList list)
            {
                var items = new List<object>();
                var maxItems = Math.Min(list.Count, 20);
                for (int i = 0; i < maxItems; i++)
                {
                    items.Add(SerializeValue(list[i], depth + 1));
                }
                if (list.Count > 20)
                    items.Add($"... ({list.Count} items total)");
                return items;
            }

            // Dictionaries
            if (value is IDictionary dict)
            {
                var dictResult = new Dictionary<string, object>();
                int count = 0;
                foreach (DictionaryEntry entry in dict)
                {
                    if (count++ >= 20) break;
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
