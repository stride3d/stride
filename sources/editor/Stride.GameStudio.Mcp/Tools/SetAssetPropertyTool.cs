// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class SetAssetPropertyTool
{
    [McpServerTool(Name = "set_asset_property"), Description("Sets a property on an asset using a dot-notation path through the property graph. Use get_asset_details to discover available property names. Supports nested paths (e.g. 'Attributes.CullMode'). When a path segment is invalid, the error lists available property names at that level. Supports undo/redo.")]
    public static async Task<string> SetAssetProperty(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID (GUID from query_assets)")] string assetId,
        [Description("Dot-notation property path (e.g. 'Width', 'Attributes.CullMode', 'Layers[0].DiffuseModel')")] string propertyPath,
        [Description("JSON value to set (e.g. '2048', 'true', '\"Back\"', '{\"r\":1,\"g\":0,\"b\":0,\"a\":1}')")] string value,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(assetId, out var id))
            {
                return new { error = "Invalid asset ID format. Expected a GUID.", result = (object?)null };
            }

            var assetVm = session.GetAssetById(id);
            if (assetVm == null)
            {
                return new { error = $"Asset not found: {assetId}", result = (object?)null };
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

            // Navigate the property path
            var segments = propertyPath.Split('.');
            IObjectNode currentObject = rootNode;
            IMemberNode? leafMember = null;

            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];

                // Handle indexed access: "Layers[0]"
                string memberName = segment;
                int? index = null;
                var bracketStart = segment.IndexOf('[');
                if (bracketStart >= 0)
                {
                    memberName = segment[..bracketStart];
                    var bracketEnd = segment.IndexOf(']');
                    if (bracketEnd > bracketStart + 1 &&
                        int.TryParse(segment[(bracketStart + 1)..bracketEnd], out var parsedIndex))
                    {
                        index = parsedIndex;
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

                if (i == segments.Length - 1 && index == null)
                {
                    // This is the leaf — what we want to update
                    leafMember = member;
                }
                else
                {
                    // Navigate deeper
                    var target = member.Target;
                    if (index.HasValue && target != null)
                    {
                        try
                        {
                            target = target.IndexedTarget(new NodeIndex(index.Value));
                        }
                        catch
                        {
                            return new
                            {
                                error = $"Index [{index.Value}] is out of range for property '{memberName}'.",
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

                    // If this is the last segment and we used an index, we need the member on the indexed target
                    if (i == segments.Length - 1 && index.HasValue)
                    {
                        // The indexed target is the leaf object — but we can't set the whole object.
                        // This case means the path ended with an indexed access like "Layers[0]"
                        // which refers to the collection item, not a property on it.
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
                convertedValue = JsonTypeConverter.ConvertJsonToType(jsonValue, leafMember.Type);
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
                undoRedoService.SetName(transaction, $"Set {propertyPath} on '{assetVm.Name}'");
            }

            return new
            {
                error = (string?)null,
                result = (object)new
                {
                    assetId = assetVm.Id.ToString(),
                    assetName = assetVm.Name,
                    propertyPath,
                    newValueType = leafMember.Type.Name,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
