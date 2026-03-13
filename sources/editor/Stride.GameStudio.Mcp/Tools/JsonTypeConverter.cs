// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine;

namespace Stride.GameStudio.Mcp.Tools;

/// <summary>
/// Shared utility for serializing/deserializing values between MCP JSON and Stride types.
/// </summary>
internal static class JsonTypeConverter
{
    /// <summary>
    /// Serializes a value to a JSON-compatible representation.
    /// Handles Stride math types, collections, entity references, asset references, and DataMember objects.
    /// </summary>
    public static object? SerializeValue(object? value, int depth = 0)
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
        if (value is Matrix)
            return value.ToString();

        // File/directory paths
        if (value is UPath path)
            return path.ToString();

        // Asset references
        if (value is IReference assetRef)
            return new { assetRef = assetRef.Id.ToString(), url = assetRef.Location?.ToString() };

        // Entity references
        if (value is Entity entity)
            return new { entityRef = entity.Id.ToString(), name = entity.Name };
        if (value is EntityComponent comp)
            return new { componentRef = comp.GetType().Name, entityId = comp.Entity?.Id.ToString() };

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
                items.Add(SerializeValue(item, depth + 1));
            }
            return items;
        }

        // Objects with [DataMember] attributes — serialize their members
        if (depth < 3)
        {
            var members = SerializeDataMembers(value, depth + 1);
            if (members.Count > 0)
                return new { type = type.Name, properties = members };
        }

        // Fallback: just return string representation
        return value.ToString();
    }

    /// <summary>
    /// Serializes all [DataMember] fields and properties of an object to a dictionary.
    /// </summary>
    public static Dictionary<string, object?> SerializeDataMembers(object obj, int depth = 0)
    {
        var type = obj.GetType();
        var properties = new Dictionary<string, object?>();

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.GetCustomAttribute<DataMemberAttribute>() == null)
                continue;

            try
            {
                var value = field.GetValue(obj);
                properties[field.Name] = SerializeValue(value, depth);
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
                var value = prop.GetValue(obj);
                properties[prop.Name] = SerializeValue(value, depth);
            }
            catch
            {
                properties[prop.Name] = "<error reading value>";
            }
        }

        return properties;
    }

    /// <summary>
    /// Converts a JSON element to the specified target type.
    /// Supports primitives, enums, Stride math types, and asset references (when session is provided).
    /// </summary>
    public static object? ConvertJsonToType(JsonElement json, Type targetType, SessionViewModel? session)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Asset reference types — requires session to resolve asset IDs
        if (session != null && json.ValueKind != JsonValueKind.Null &&
            (AssetRegistry.CanBeAssignedToContentTypes(underlyingType, checkIsUrlType: true)
             || typeof(AssetReference).IsAssignableFrom(underlyingType)))
        {
            return ConvertAssetReference(json, underlyingType, session);
        }

        // Null clears asset reference properties even without session
        if (json.ValueKind == JsonValueKind.Null &&
            (AssetRegistry.CanBeAssignedToContentTypes(underlyingType, checkIsUrlType: true)
             || typeof(AssetReference).IsAssignableFrom(underlyingType)))
        {
            return null;
        }

        // Polymorphic types (interfaces/abstract classes) — resolve concrete type and instantiate
        if (IsPolymorphicType(underlyingType))
            return ConvertPolymorphicValue(json, underlyingType, session);

        return ConvertJsonToType(json, targetType);
    }

    /// <summary>
    /// Converts a JSON element to the specified target type.
    /// Supports primitives, enums, and Stride math types.
    /// </summary>
    public static object? ConvertJsonToType(JsonElement json, Type targetType)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (json.ValueKind == JsonValueKind.Null)
            return null;

        // Primitive types
        if (underlyingType == typeof(bool))
            return json.GetBoolean();
        if (underlyingType == typeof(int))
            return json.GetInt32();
        if (underlyingType == typeof(float))
            return json.GetSingle();
        if (underlyingType == typeof(double))
            return json.GetDouble();
        if (underlyingType == typeof(string))
            return json.GetString();
        if (underlyingType == typeof(long))
            return json.GetInt64();

        // Enums
        if (underlyingType.IsEnum)
        {
            var enumString = json.GetString();
            if (enumString != null && Enum.TryParse(underlyingType, enumString, ignoreCase: true, out var enumValue))
                return enumValue;
            if (json.ValueKind == JsonValueKind.Number)
                return Enum.ToObject(underlyingType, json.GetInt32());
            throw new InvalidOperationException($"Cannot convert '{json}' to enum {underlyingType.Name}");
        }

        // File/directory paths (UFile, UDirectory)
        if (underlyingType == typeof(UFile))
            return new UFile(json.GetString());
        if (underlyingType == typeof(UDirectory))
            return new UDirectory(json.GetString());

        // Stride Vector3
        if (underlyingType == typeof(Vector3) && json.ValueKind == JsonValueKind.Object)
        {
            return new Vector3(
                json.TryGetProperty("x", out var x) || json.TryGetProperty("X", out x) ? x.GetSingle() : 0f,
                json.TryGetProperty("y", out var y) || json.TryGetProperty("Y", out y) ? y.GetSingle() : 0f,
                json.TryGetProperty("z", out var z) || json.TryGetProperty("Z", out z) ? z.GetSingle() : 0f);
        }

        // Stride Vector2
        if (underlyingType == typeof(Vector2) && json.ValueKind == JsonValueKind.Object)
        {
            return new Vector2(
                json.TryGetProperty("x", out var x) || json.TryGetProperty("X", out x) ? x.GetSingle() : 0f,
                json.TryGetProperty("y", out var y) || json.TryGetProperty("Y", out y) ? y.GetSingle() : 0f);
        }

        // Stride Quaternion
        if (underlyingType == typeof(Quaternion) && json.ValueKind == JsonValueKind.Object)
        {
            return new Quaternion(
                json.TryGetProperty("x", out var x) || json.TryGetProperty("X", out x) ? x.GetSingle() : 0f,
                json.TryGetProperty("y", out var y) || json.TryGetProperty("Y", out y) ? y.GetSingle() : 0f,
                json.TryGetProperty("z", out var z) || json.TryGetProperty("Z", out z) ? z.GetSingle() : 0f,
                json.TryGetProperty("w", out var w) || json.TryGetProperty("W", out w) ? w.GetSingle() : 1f);
        }

        // Stride Color4
        if (underlyingType == typeof(Color4) && json.ValueKind == JsonValueKind.Object)
        {
            return new Color4(
                json.TryGetProperty("r", out var r) || json.TryGetProperty("R", out r) ? r.GetSingle() : 0f,
                json.TryGetProperty("g", out var g) || json.TryGetProperty("G", out g) ? g.GetSingle() : 0f,
                json.TryGetProperty("b", out var b) || json.TryGetProperty("B", out b) ? b.GetSingle() : 0f,
                json.TryGetProperty("a", out var a) || json.TryGetProperty("A", out a) ? a.GetSingle() : 1f);
        }

        // Stride Color3
        if (underlyingType == typeof(Color3) && json.ValueKind == JsonValueKind.Object)
        {
            return new Color3(
                json.TryGetProperty("r", out var r) || json.TryGetProperty("R", out r) ? r.GetSingle() : 0f,
                json.TryGetProperty("g", out var g) || json.TryGetProperty("G", out g) ? g.GetSingle() : 0f,
                json.TryGetProperty("b", out var b) || json.TryGetProperty("B", out b) ? b.GetSingle() : 0f);
        }

        // Polymorphic types (interfaces/abstract classes) — resolve concrete type and instantiate
        if (IsPolymorphicType(underlyingType))
            return ConvertPolymorphicValue(json, underlyingType, session: null);

        throw new InvalidOperationException($"Cannot convert JSON value to type {targetType.Name}. Supported types: bool, int, float, double, string, long, enum, UFile, UDirectory, Vector2, Vector3, Quaternion, Color3, Color4, and asset references (use session overload).");
    }

    /// <summary>
    /// Parses an asset ID from JSON and creates a proper asset reference via ContentReferenceHelper.
    /// Accepted formats: {"assetId":"GUID"}, {"assetRef":"GUID"}, "GUID", or null.
    /// </summary>
    private static object? ConvertAssetReference(JsonElement json, Type targetType, SessionViewModel session)
    {
        // Parse asset ID from various JSON formats
        string? assetIdStr = null;

        if (json.ValueKind == JsonValueKind.Object)
        {
            if (json.TryGetProperty("assetId", out var idProp))
                assetIdStr = idProp.GetString();
            else if (json.TryGetProperty("assetRef", out var refProp))
                assetIdStr = refProp.GetString();
        }
        else if (json.ValueKind == JsonValueKind.String)
        {
            assetIdStr = json.GetString();
        }

        if (string.IsNullOrEmpty(assetIdStr))
        {
            throw new InvalidOperationException("Asset reference JSON must contain an asset ID. Use {\"assetId\":\"GUID\"}, {\"assetRef\":\"GUID\"}, or \"GUID\".");
        }

        if (!AssetId.TryParse(assetIdStr, out var assetId))
        {
            throw new InvalidOperationException($"Invalid asset ID format: '{assetIdStr}'. Expected a GUID.");
        }

        var assetVm = session.GetAssetById(assetId);
        if (assetVm == null)
        {
            throw new InvalidOperationException($"Asset not found: '{assetIdStr}'. Use query_assets to find valid asset IDs.");
        }

        var reference = ContentReferenceHelper.CreateReference(assetVm, targetType);
        if (reference == null)
        {
            throw new InvalidOperationException($"Cannot create a reference of type {targetType.Name} to asset '{assetVm.Name}' ({assetVm.AssetItem.Asset.GetType().Name}). The asset type may not be compatible with this property.");
        }

        return reference;
    }

    /// <summary>
    /// Returns true if the type is an interface or abstract class that requires polymorphic resolution.
    /// </summary>
    private static bool IsPolymorphicType(Type type)
        => (type.IsInterface || type.IsAbstract) && type != typeof(string);

    /// <summary>
    /// Resolves a user-provided type name to a concrete type that implements the given interface/abstract type.
    /// Resolution order: DataContract alias, short class name, fully qualified name.
    /// </summary>
    private static Type? ResolveConcreteType(string typeName, Type targetType)
    {
        var concreteTypes = targetType.GetInheritedInstantiableTypes()
            .Where(t => Attribute.GetCustomAttribute(t, typeof(NonInstantiableAttribute)) == null)
            .ToList();

        // 1. Match by DataContract alias
        foreach (var type in concreteTypes)
        {
            var dc = type.GetCustomAttribute<DataContractAttribute>(false);
            if (dc?.Alias != null && string.Equals(dc.Alias, typeName, StringComparison.OrdinalIgnoreCase))
                return type;
        }

        // 2. Match by short class name
        foreach (var type in concreteTypes)
        {
            if (string.Equals(type.Name, typeName, StringComparison.OrdinalIgnoreCase))
                return type;
        }

        // 3. Match by fully qualified name
        foreach (var type in concreteTypes)
        {
            if (string.Equals(type.FullName, typeName, StringComparison.OrdinalIgnoreCase))
                return type;
        }

        return null;
    }

    /// <summary>
    /// Returns a comma-separated list of available concrete type names for error messages.
    /// Uses DataContract alias when available, falls back to class name.
    /// </summary>
    private static string GetAvailableTypeNames(Type targetType)
    {
        var concreteTypes = targetType.GetInheritedInstantiableTypes()
            .Where(t => Attribute.GetCustomAttribute(t, typeof(NonInstantiableAttribute)) == null)
            .OrderBy(t => t.Name)
            .ToList();

        var names = concreteTypes.Select(t =>
        {
            var dc = t.GetCustomAttribute<DataContractAttribute>(false);
            return dc?.Alias ?? t.Name;
        });

        return string.Join(", ", names);
    }

    /// <summary>
    /// Converts a JSON value to a concrete instance of a polymorphic (interface/abstract) type.
    /// Accepts two formats: a string type name, or an object with "$type" plus optional inline properties.
    /// </summary>
    private static object? ConvertPolymorphicValue(JsonElement json, Type targetType, SessionViewModel? session)
    {
        if (json.ValueKind == JsonValueKind.Null)
            return null;

        string? typeName;
        JsonElement? propertiesJson = null;

        if (json.ValueKind == JsonValueKind.String)
        {
            // Format 1: "LightPoint"
            typeName = json.GetString();
        }
        else if (json.ValueKind == JsonValueKind.Object)
        {
            // Format 2: {"$type": "LightPoint", "Radius": 5.0, ...}
            if (!json.TryGetProperty("$type", out var typeElement))
            {
                throw new InvalidOperationException(
                    $"Polymorphic value for '{targetType.Name}' must be a type name string or an object with a '$type' property. " +
                    $"Available types: {GetAvailableTypeNames(targetType)}");
            }
            typeName = typeElement.GetString();
            propertiesJson = json;
        }
        else
        {
            throw new InvalidOperationException(
                $"Polymorphic value for '{targetType.Name}' must be a type name string or an object with a '$type' property. " +
                $"Available types: {GetAvailableTypeNames(targetType)}");
        }

        if (string.IsNullOrEmpty(typeName))
        {
            throw new InvalidOperationException(
                $"Type name cannot be empty for '{targetType.Name}'. " +
                $"Available types: {GetAvailableTypeNames(targetType)}");
        }

        var concreteType = ResolveConcreteType(typeName, targetType);
        if (concreteType == null)
        {
            throw new InvalidOperationException(
                $"Type '{typeName}' not found for '{targetType.Name}'. " +
                $"Available types: {GetAvailableTypeNames(targetType)}");
        }

        // Instantiate via ObjectFactoryRegistry (same as editor property grid)
        object instance;
        try
        {
            instance = ObjectFactoryRegistry.NewInstance(concreteType);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create instance of '{concreteType.Name}' for '{targetType.Name}': {ex.Message}", ex);
        }

        // Apply inline properties if object format was used
        if (propertiesJson.HasValue)
        {
            ApplyInlineProperties(instance, propertiesJson.Value, session);
        }

        return instance;
    }

    /// <summary>
    /// Applies inline properties from a JSON object to a newly created polymorphic instance.
    /// Matches properties by name (case-insensitive) against [DataMember] fields and properties.
    /// </summary>
    private static void ApplyInlineProperties(object instance, JsonElement json, SessionViewModel? session)
    {
        var type = instance.GetType();

        foreach (var jsonProp in json.EnumerateObject())
        {
            // Skip the $type discriminator
            if (jsonProp.Name == "$type")
                continue;

            // Search for matching [DataMember] property
            var prop = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p =>
                    p.GetCustomAttribute<DataMemberAttribute>() != null &&
                    string.Equals(p.Name, jsonProp.Name, StringComparison.OrdinalIgnoreCase) &&
                    p.CanWrite);

            if (prop != null)
            {
                var value = session != null
                    ? ConvertJsonToType(jsonProp.Value, prop.PropertyType, session)
                    : ConvertJsonToType(jsonProp.Value, prop.PropertyType);
                prop.SetValue(instance, value);
                continue;
            }

            // Search for matching [DataMember] field
            var field = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(f =>
                    f.GetCustomAttribute<DataMemberAttribute>() != null &&
                    string.Equals(f.Name, jsonProp.Name, StringComparison.OrdinalIgnoreCase) &&
                    !f.IsInitOnly);

            if (field != null)
            {
                var value = session != null
                    ? ConvertJsonToType(jsonProp.Value, field.FieldType, session)
                    : ConvertJsonToType(jsonProp.Value, field.FieldType);
                field.SetValue(instance, value);
                continue;
            }

            // Silently skip unknown properties — the agent may pass extra metadata
        }
    }
}
