// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Assets.UI;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class SetUIElementPropertyTool
{
    [McpServerTool(Name = "set_ui_element_property"), Description("Sets a property on a UI element within a UIPageAsset via the property graph. Use get_ui_element to discover available property names. Supports dot-notation paths (e.g. 'Width', 'Text', 'Orientation', 'Margin.Left'). Supports undo/redo.")]
    public static async Task<string> SetUIElementProperty(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the UIPageAsset")] string assetId,
        [Description("The UI element ID (GUID from get_ui_tree)")] string elementId,
        [Description("Dot-notation property path relative to the UIElement (e.g. 'Width', 'Text', 'Orientation', 'Margin.Left')")] string propertyPath,
        [Description("JSON value to set. Scalar: '200', 'true', '\"Hello\"'. Enum: '\"Horizontal\"'. Color: '{\"r\":1,\"g\":0,\"b\":0,\"a\":1}'. Asset ref: '{\"assetId\":\"GUID\"}'. Clear: 'null'.")] string value,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(assetId, out var id))
            {
                return new { error = "Invalid asset ID format. Expected a GUID.", result = (object?)null };
            }

            if (!Guid.TryParse(elementId, out var elementGuid))
            {
                return new { error = "Invalid element ID format. Expected a GUID.", result = (object?)null };
            }

            var assetVm = session.GetAssetById(id);
            if (assetVm == null)
            {
                return new { error = $"Asset not found: {assetId}", result = (object?)null };
            }

            if (assetVm.Asset is not UIAssetBase uiAsset)
            {
                return new { error = $"Asset is not a UI page: {assetVm.Name} ({assetVm.AssetType.Name})", result = (object?)null };
            }

            if (!uiAsset.Hierarchy.Parts.TryGetValue(elementGuid, out var design))
            {
                return new { error = $"UI element not found: {elementId}", result = (object?)null };
            }

            var rootNode = assetVm.PropertyGraph?.RootNode;
            if (rootNode == null)
            {
                return new { error = "Cannot access property graph for this asset.", result = (object?)null };
            }

            // Parse the JSON value
            JsonElement jsonValue;
            try
            {
                jsonValue = JsonSerializer.Deserialize<JsonElement>(value);
            }
            catch (JsonException ex)
            {
                return new { error = $"Invalid JSON value: {ex.Message}", result = (object?)null };
            }

            // Navigate to the element's node in the property graph:
            // RootNode -> Hierarchy -> Parts -> [elementGuid] -> UIElement
            var hierarchyMember = rootNode.TryGetChild(nameof(UIAssetBase.Hierarchy));
            if (hierarchyMember?.Target == null)
            {
                return new { error = "Cannot navigate to Hierarchy node.", result = (object?)null };
            }

            var partsMember = hierarchyMember.Target.TryGetChild("Parts");
            if (partsMember?.Target == null)
            {
                return new { error = "Cannot navigate to Parts node.", result = (object?)null };
            }

            // Find the element's design node in the parts dictionary
            IObjectNode? designNode = null;
            foreach (var idx in partsMember.Target.Indices)
            {
                var candidate = partsMember.Target.IndexedTarget(idx);
                if (candidate != null)
                {
                    var uiElementMember = candidate.TryGetChild(nameof(UIElementDesign.UIElement));
                    if (uiElementMember?.Target != null)
                    {
                        var idMember = uiElementMember.Target.TryGetChild("Id");
                        if (idMember != null && idMember.Retrieve() is Guid partId && partId == elementGuid)
                        {
                            designNode = candidate;
                            break;
                        }
                    }
                }
            }

            if (designNode == null)
            {
                return new { error = $"Cannot find UI element node in property graph: {elementId}", result = (object?)null };
            }

            var uiElementNode = designNode.TryGetChild(nameof(UIElementDesign.UIElement));
            if (uiElementNode?.Target == null)
            {
                return new { error = "Cannot navigate to UIElement node in property graph.", result = (object?)null };
            }

            // Navigate the property path from the UIElement node
            var segments = propertyPath.Split('.');
            IObjectNode currentObject = uiElementNode.Target;
            IMemberNode? leafMember = null;

            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];

                // Handle indexed access: "Layers[0]"
                string memberName = segment;
                int? segIndex = null;
                var bracketStart = segment.IndexOf('[');
                if (bracketStart >= 0)
                {
                    memberName = segment[..bracketStart];
                    var bracketEnd = segment.IndexOf(']');
                    if (bracketEnd > bracketStart + 1 &&
                        int.TryParse(segment[(bracketStart + 1)..bracketEnd], out var parsedIndex))
                    {
                        segIndex = parsedIndex;
                    }
                }

                var member = currentObject.TryGetChild(memberName);
                if (member == null)
                {
                    var availableMembers = currentObject.Members.Select(m => m.Name).OrderBy(n => n).ToArray();
                    return new
                    {
                        error = $"Property '{memberName}' not found at path level {i}. Available properties: {string.Join(", ", availableMembers)}",
                        result = (object?)null,
                    };
                }

                if (i == segments.Length - 1 && segIndex == null)
                {
                    leafMember = member;
                }
                else
                {
                    var target = member.Target;
                    if (segIndex.HasValue && target != null)
                    {
                        try
                        {
                            target = target.IndexedTarget(new NodeIndex(segIndex.Value));
                        }
                        catch
                        {
                            return new
                            {
                                error = $"Index [{segIndex.Value}] is out of range for property '{memberName}'.",
                                result = (object?)null,
                            };
                        }
                    }

                    if (target == null)
                    {
                        return new
                        {
                            error = $"Cannot navigate into property '{memberName}' — it has no target object (value may be null).",
                            result = (object?)null,
                        };
                    }

                    currentObject = target;

                    if (i == segments.Length - 1 && segIndex.HasValue)
                    {
                        return new
                        {
                            error = $"Path ends with an indexed access '{segment}'. Add a property name after the index (e.g. '{segment}.PropertyName').",
                            result = (object?)null,
                        };
                    }
                }
            }

            if (leafMember == null)
            {
                return new { error = "Could not resolve property path.", result = (object?)null };
            }

            // Convert the value
            object? convertedValue;
            try
            {
                convertedValue = JsonTypeConverter.ConvertJsonToType(jsonValue, leafMember.Type, session);
            }
            catch (Exception ex)
            {
                return new { error = $"Cannot convert value to type {leafMember.Type.Name}: {ex.Message}", result = (object?)null };
            }

            // Apply in undo/redo transaction
            var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
            using (var transaction = undoRedoService.CreateTransaction())
            {
                leafMember.Update(convertedValue);
                undoRedoService.SetName(transaction, $"Set {propertyPath} on UI element '{design.UIElement.Name ?? design.UIElement.GetType().Name}'");
            }

            return new
            {
                error = (string?)null,
                result = (object)new
                {
                    assetId = assetVm.Id.ToString(),
                    elementId = elementId,
                    propertyPath,
                    newValueType = leafMember.Type.Name,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
