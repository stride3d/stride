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
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.ViewModel;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Engine;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class ModifyComponentTool
{
    [McpServerTool(Name = "modify_component"), Description("Adds, removes, or updates a component on an entity. The scene must be open in the editor (use open_scene first). Actions: 'add' creates a new component, 'remove' deletes a component by index, 'update' sets properties on a component by index. Supports bracket notation for dictionary/list entries (e.g. '{\"Animations[Idle]\":{\"assetId\":\"GUID\"}}') and whole-dictionary JSON objects. The TransformComponent (index 0) cannot be removed. This operation supports undo/redo in the editor. For 'update', asset reference properties (e.g. ModelComponent.Model, BackgroundComponent.Texture) can be set using {\"PropertyName\":{\"assetId\":\"GUID\"}} — use query_assets to find the asset ID. NOTE: User game script types require the project to be built and assemblies reloaded first (use build_project, then get_build_status to wait, then reload_assemblies).")]
    public static async Task<string> ModifyComponent(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the scene containing the entity")] string sceneId,
        [Description("The entity ID to modify")] string entityId,
        [Description("The action to perform: 'add', 'remove', or 'update'")] string action,
        [Description("For 'add': the component type name (e.g. 'ModelComponent', 'Stride.Engine.LightComponent'). For 'remove'/'update': not required.")] string? componentType = null,
        [Description("For 'remove'/'update': the zero-based index of the component in the entity's component list. Use get_entity to see component indices.")] int? componentIndex = null,
        [Description("For 'update': JSON object of property names and values to set. Scalar: '{\"Intensity\":2.0,\"Enabled\":false}'. Asset references: '{\"Model\":{\"assetId\":\"GUID\"}}' or '{\"Model\":\"GUID\"}'. Use null to clear: '{\"Model\":null}'. Bracket notation for dict entries: '{\"Animations[Idle]\":{\"assetId\":\"GUID\"}}'. Whole dict as JSON object: '{\"Animations\":{\"Idle\":{\"assetId\":\"GUID1\"},\"Run\":{\"assetId\":\"GUID2\"}}}'.")] string? properties = null,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(sceneId, out var assetId))
            {
                return new { error = "Invalid scene ID format. Expected a GUID.", component = (object?)null };
            }

            if (!Guid.TryParse(entityId, out var entityGuid))
            {
                return new { error = "Invalid entity ID format. Expected a GUID.", component = (object?)null };
            }

            var assetVm = session.GetAssetById(assetId);
            if (assetVm is not SceneViewModel sceneVm)
            {
                return new { error = $"Scene not found: {sceneId}", component = (object?)null };
            }

            var editorsManager = session.ServiceProvider.Get<IAssetEditorsManager>();
            if (!editorsManager.TryGetAssetEditor<EntityHierarchyEditorViewModel>(sceneVm, out var editor))
            {
                return new { error = $"Scene is not open in the editor. Use open_scene first: {sceneId}", component = (object?)null };
            }

            var absoluteId = new AbsoluteId(assetId, entityGuid);
            var partVm = editor.FindPartViewModel(absoluteId);
            if (partVm is not EntityViewModel entityVm)
            {
                return new { error = $"Entity not found in scene: {entityId}", component = (object?)null };
            }

            var entity = entityVm.AssetSideEntity;

            switch (action.ToLowerInvariant())
            {
                case "add":
                    return AddComponent(session, entity, componentType);
                case "remove":
                    return RemoveComponent(session, entity, componentIndex);
                case "update":
                    return UpdateComponent(session, entity, componentIndex, properties);
                default:
                    return new { error = $"Unknown action: '{action}'. Expected 'add', 'remove', or 'update'.", component = (object?)null };
            }
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    private static object AddComponent(SessionViewModel session, Entity entity, string? componentTypeName)
    {
        if (string.IsNullOrEmpty(componentTypeName))
        {
            return new { error = "componentType is required for 'add' action.", component = (object?)null };
        }

        var resolvedType = ResolveComponentType(componentTypeName);
        if (resolvedType == null)
        {
            var userTypesHint = GetAvailableUserComponentTypes();
            return new
            {
                error = $"Component type not found: '{componentTypeName}'. "
                    + "Built-in examples: ModelComponent, LightComponent, CameraComponent, BackgroundComponent, SpriteComponent, AudioEmitterComponent, RigidbodyComponent, CharacterComponent. "
                    + "User game script types (e.g. PlayerController) require the project to be built and assemblies reloaded — use `build_project`, then `get_build_status` to wait for completion, then `reload_assemblies` to load the new types. "
                    + "Also try the fully qualified type name (e.g. 'MyGame.PlayerController')."
                    + userTypesHint,
                component = (object?)null,
            };
        }

        // Validate singleton constraint
        var attributes = EntityComponentAttributes.Get(resolvedType);
        if (!attributes.AllowMultipleComponents)
        {
            if (entity.Components.Any(c => c.GetType() == resolvedType))
            {
                return new { error = $"Entity already has a {resolvedType.Name} and this component type does not allow multiples.", component = (object?)null };
            }
        }

        var entityNode = session.AssetNodeContainer.GetOrCreateNode(entity);
        if (entityNode == null)
        {
            return new { error = "Failed to access entity node.", component = (object?)null };
        }

        var componentsNode = entityNode[nameof(Entity.Components)].Target;
        if (componentsNode == null)
        {
            return new { error = "Failed to access entity components node.", component = (object?)null };
        }

        var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
        using (var transaction = undoRedoService.CreateTransaction())
        {
            var newComponent = (EntityComponent)Activator.CreateInstance(resolvedType)!;
            componentsNode.Add(newComponent);
            undoRedoService.SetName(transaction, $"Add {resolvedType.Name}");
        }

        return new
        {
            error = (string?)null,
            component = (object)new
            {
                action = "added",
                type = resolvedType.Name,
                index = entity.Components.Count - 1,
            },
        };
    }

    private static object RemoveComponent(SessionViewModel session, Entity entity, int? componentIndex)
    {
        if (componentIndex == null)
        {
            return new { error = "componentIndex is required for 'remove' action.", component = (object?)null };
        }

        if (componentIndex == 0)
        {
            return new { error = "Cannot remove the TransformComponent (index 0). It is required on all entities.", component = (object?)null };
        }

        if (componentIndex < 0 || componentIndex >= entity.Components.Count)
        {
            return new { error = $"Component index {componentIndex} is out of range. Entity has {entity.Components.Count} components (indices 0-{entity.Components.Count - 1}).", component = (object?)null };
        }

        var componentToRemove = entity.Components[componentIndex.Value];
        var componentTypeName = componentToRemove.GetType().Name;

        var entityNode = session.AssetNodeContainer.GetOrCreateNode(entity);
        if (entityNode == null)
        {
            return new { error = "Failed to access entity node.", component = (object?)null };
        }

        var componentsNode = entityNode[nameof(Entity.Components)].Target;
        if (componentsNode == null)
        {
            return new { error = "Failed to access entity components node.", component = (object?)null };
        }

        var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
        using (var transaction = undoRedoService.CreateTransaction())
        {
            componentsNode.Remove(componentToRemove, new NodeIndex(componentIndex.Value));
            undoRedoService.SetName(transaction, $"Remove {componentTypeName}");
        }

        return new
        {
            error = (string?)null,
            component = (object)new
            {
                action = "removed",
                type = componentTypeName,
                index = componentIndex.Value,
            },
        };
    }

    private static object UpdateComponent(SessionViewModel session, Entity entity, int? componentIndex, string? propertiesJson)
    {
        if (componentIndex == null)
        {
            return new { error = "componentIndex is required for 'update' action.", component = (object?)null };
        }

        if (string.IsNullOrEmpty(propertiesJson))
        {
            return new { error = "properties JSON is required for 'update' action.", component = (object?)null };
        }

        if (componentIndex < 0 || componentIndex >= entity.Components.Count)
        {
            return new { error = $"Component index {componentIndex} is out of range. Entity has {entity.Components.Count} components (indices 0-{entity.Components.Count - 1}).", component = (object?)null };
        }

        Dictionary<string, JsonElement>? propertiesToSet;
        try
        {
            propertiesToSet = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(propertiesJson);
        }
        catch (JsonException ex)
        {
            return new { error = $"Invalid properties JSON: {ex.Message}", component = (object?)null };
        }

        if (propertiesToSet == null || propertiesToSet.Count == 0)
        {
            return new { error = "Properties object is empty.", component = (object?)null };
        }

        var targetComponent = entity.Components[componentIndex.Value];
        var componentNode = session.AssetNodeContainer.GetOrCreateNode(targetComponent);
        if (componentNode == null)
        {
            return new { error = "Failed to access component node.", component = (object?)null };
        }

        var updatedProperties = new List<string>();
        var errors = new List<string>();

        var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
        using (var transaction = undoRedoService.CreateTransaction())
        {
            foreach (var (propName, jsonValue) in propertiesToSet)
            {
                try
                {
                    ParsePropertyName(propName, out var memberName, out var bracketKey);

                    var memberNode = componentNode.TryGetChild(memberName);
                    if (memberNode == null)
                    {
                        errors.Add($"Property '{memberName}' not found on {targetComponent.GetType().Name}.");
                        continue;
                    }

                    if (bracketKey != null)
                    {
                        // Bracket notation: "Animations[Idle]" → update single dict/list entry
                        UpdateIndexedProperty(memberNode, bracketKey, jsonValue, session);
                    }
                    else if (IsDictionaryType(memberNode.Type) && jsonValue.ValueKind == JsonValueKind.Object)
                    {
                        // Whole dictionary as JSON object
                        UpdateDictionaryFromJson(memberNode, jsonValue, session);
                    }
                    else
                    {
                        // Simple scalar update
                        var targetType = memberNode.Type;
                        var convertedValue = JsonTypeConverter.ConvertJsonToType(jsonValue, targetType, session);
                        memberNode.Update(convertedValue);
                    }

                    updatedProperties.Add(propName);
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to set '{propName}': {ex.Message}");
                }
            }

            undoRedoService.SetName(transaction, $"Update {targetComponent.GetType().Name}");
        }

        return new
        {
            error = errors.Count > 0 ? string.Join("; ", errors) : (string?)null,
            component = (object)new
            {
                action = "updated",
                type = targetComponent.GetType().Name,
                index = componentIndex.Value,
                updatedProperties = updatedProperties.ToArray(),
            },
        };
    }

    private static void ParsePropertyName(string propName, out string memberName, out string? bracketKey)
    {
        var bracketStart = propName.IndexOf('[');
        if (bracketStart >= 0)
        {
            memberName = propName[..bracketStart];
            var bracketEnd = propName.IndexOf(']');
            bracketKey = bracketEnd > bracketStart + 1 ? propName[(bracketStart + 1)..bracketEnd] : null;
        }
        else
        {
            memberName = propName;
            bracketKey = null;
        }
    }

    private static void UpdateIndexedProperty(IMemberNode memberNode, string bracketKey, JsonElement jsonValue, SessionViewModel session)
    {
        var target = memberNode.Target;
        if (target == null)
            throw new InvalidOperationException("Property has no target object — cannot set indexed value.");

        var descriptor = TypeDescriptorFactory.Default.Find(target.Type);

        Type valueType;
        if (descriptor is DictionaryDescriptor dictDesc)
            valueType = dictDesc.ValueType;
        else if (descriptor is CollectionDescriptor collDesc)
            valueType = collDesc.ElementType;
        else
            throw new InvalidOperationException($"Property type {target.Type.Name} does not support indexed access.");

        var nodeIndex = ResolveNodeIndex(target, bracketKey);
        var convertedValue = JsonTypeConverter.ConvertJsonToType(jsonValue, valueType, session);

        // Check if the index already exists
        bool exists = target.Indices != null && target.Indices.Any(idx => Equals(idx.Value, nodeIndex.Value));

        if (exists)
            target.Update(convertedValue, nodeIndex);
        else
            target.Add(convertedValue, nodeIndex);
    }

    private static void UpdateDictionaryFromJson(IMemberNode memberNode, JsonElement jsonObject, SessionViewModel session)
    {
        var target = memberNode.Target;
        if (target == null)
            throw new InvalidOperationException("Property has no target object — cannot update dictionary.");

        var descriptor = TypeDescriptorFactory.Default.Find(target.Type);
        if (descriptor is not DictionaryDescriptor dictDesc)
            throw new InvalidOperationException($"Property type {target.Type.Name} is not a dictionary.");

        foreach (var entry in jsonObject.EnumerateObject())
        {
            var key = ConvertDictionaryKey(entry.Name, dictDesc.KeyType);
            var nodeIndex = new NodeIndex(key);
            var convertedValue = JsonTypeConverter.ConvertJsonToType(entry.Value, dictDesc.ValueType, session);

            bool exists = target.Indices != null && target.Indices.Any(idx => Equals(idx.Value, nodeIndex.Value));

            if (exists)
                target.Update(convertedValue, nodeIndex);
            else
                target.Add(convertedValue, nodeIndex);
        }
    }

    private static NodeIndex ResolveNodeIndex(IObjectNode target, string bracketKey)
    {
        var descriptor = TypeDescriptorFactory.Default.Find(target.Type);

        if (descriptor is DictionaryDescriptor dictDesc)
        {
            var key = ConvertDictionaryKey(bracketKey, dictDesc.KeyType);
            return new NodeIndex(key);
        }

        // Collection/list: parse as integer index
        if (int.TryParse(bracketKey, out var index))
            return new NodeIndex(index);

        throw new InvalidOperationException($"Cannot resolve index '{bracketKey}' — expected an integer for collection access.");
    }

    private static object ConvertDictionaryKey(string key, Type keyType)
    {
        if (keyType == typeof(string))
            return key;
        if (keyType == typeof(int) && int.TryParse(key, out var intKey))
            return intKey;
        if (keyType.IsEnum && Enum.TryParse(keyType, key, ignoreCase: true, out var enumKey))
            return enumKey!;

        throw new InvalidOperationException($"Cannot convert '{key}' to dictionary key type {keyType.Name}.");
    }

    private static bool IsDictionaryType(Type type)
        => type.IsGenericType && type.GetInterface(typeof(IDictionary<,>).FullName!) != null;

    internal static Type? ResolveComponentType(string typeName)
    {
        // Use the same discovery mechanism as the editor's "Add component" dropdown:
        // typeof(EntityComponent).GetInheritedInstantiableTypes() queries AssemblyRegistry
        // which includes user game script assemblies loaded from the project.
        var allComponentTypes = typeof(EntityComponent).GetInheritedInstantiableTypes();

        // Try exact name match (short name like "ModelComponent")
        var match = allComponentTypes.FirstOrDefault(t => t.Name == typeName);
        if (match != null)
            return match;

        // Try full name match (like "Stride.Engine.ModelComponent" or "MyGame.PlayerController")
        match = allComponentTypes.FirstOrDefault(t => t.FullName == typeName);
        if (match != null)
            return match;

        // Try case-insensitive match
        match = allComponentTypes.FirstOrDefault(t =>
            string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase));
        if (match != null)
            return match;

        // Try case-insensitive full name match
        match = allComponentTypes.FirstOrDefault(t =>
            string.Equals(t.FullName, typeName, StringComparison.OrdinalIgnoreCase));
        if (match != null)
            return match;

        // Fallback: search AppDomain assemblies for types not registered with AssemblyRegistry
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(typeName, throwOnError: false);
            if (type != null && typeof(EntityComponent).IsAssignableFrom(type))
                return type;
        }

        return null;
    }

    internal static string GetAvailableUserComponentTypes()
    {
        var allTypes = typeof(EntityComponent).GetInheritedInstantiableTypes();
        // Filter to non-Stride types (user scripts)
        var userTypes = allTypes
            .Where(t => t.Namespace != null && !t.Namespace.StartsWith("Stride."))
            .Select(t => t.FullName)
            .OrderBy(n => n)
            .ToArray();

        if (userTypes.Length == 0)
            return "";

        return $" Available user script types: {string.Join(", ", userTypes)}.";
    }
}
