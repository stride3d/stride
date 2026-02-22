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
using Stride.Engine;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class ModifyComponentTool
{
    [McpServerTool(Name = "modify_component"), Description("Adds, removes, or updates a component on an entity. The scene must be open in the editor (use open_scene first). Actions: 'add' creates a new component, 'remove' deletes a component by index, 'update' sets properties on a component by index. The TransformComponent (index 0) cannot be removed. This operation supports undo/redo in the editor.")]
    public static async Task<string> ModifyComponent(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the scene containing the entity")] string sceneId,
        [Description("The entity ID to modify")] string entityId,
        [Description("The action to perform: 'add', 'remove', or 'update'")] string action,
        [Description("For 'add': the component type name (e.g. 'ModelComponent', 'Stride.Engine.LightComponent'). For 'remove'/'update': not required.")] string? componentType = null,
        [Description("For 'remove'/'update': the zero-based index of the component in the entity's component list. Use get_entity to see component indices.")] int? componentIndex = null,
        [Description("For 'update': JSON object of property names and values to set (e.g. '{\"Intensity\":2.0,\"Enabled\":false}')")] string? properties = null,
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
            return new { error = $"Component type not found: '{componentTypeName}'. Use a fully qualified name like 'Stride.Engine.ModelComponent'.", component = (object?)null };
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
                var memberNode = componentNode.TryGetChild(propName);
                if (memberNode == null)
                {
                    errors.Add($"Property '{propName}' not found on {targetComponent.GetType().Name}.");
                    continue;
                }

                try
                {
                    var targetType = memberNode.Type;
                    var convertedValue = JsonTypeConverter.ConvertJsonToType(jsonValue, targetType);
                    memberNode.Update(convertedValue);
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

    internal static Type? ResolveComponentType(string typeName)
    {
        // Try exact match with assembly
        var type = Type.GetType(typeName, throwOnError: false);
        if (type != null && typeof(EntityComponent).IsAssignableFrom(type))
            return type;

        // Search in loaded assemblies by full name or short name
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Try full name match
            type = assembly.GetType(typeName, throwOnError: false);
            if (type != null && typeof(EntityComponent).IsAssignableFrom(type))
                return type;
        }

        // Try common Stride namespaces for short names
        var candidateNamespaces = new[]
        {
            "Stride.Engine",
            "Stride.Rendering",
            "Stride.Rendering.Lights",
            "Stride.Audio",
            "Stride.Navigation",
            "Stride.Particles.Components",
            "Stride.Physics",
            "Stride.SpriteStudio.Runtime",
            "Stride.Video",
        };

        foreach (var ns in candidateNamespaces)
        {
            var qualifiedName = $"{ns}.{typeName}";
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(qualifiedName, throwOnError: false);
                if (type != null && typeof(EntityComponent).IsAssignableFrom(type))
                    return type;
            }
        }

        return null;
    }

}
