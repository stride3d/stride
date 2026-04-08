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
using Stride.Core.Reflection;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class SetAssetPropertyTool
{
    [McpServerTool(Name = "set_asset_property"), Description("Sets a property on an asset using a dot-notation path through the property graph. Use get_asset_details to discover available property names. Supports nested paths (e.g. 'Attributes.CullMode'), list indexing (e.g. 'Layers[0].DiffuseModel'), and dictionary key access (e.g. 'Dict[key]'). When a path segment is invalid, the error lists available property names at that level. Supports undo/redo. Asset reference properties can be set using {\"assetId\":\"GUID\"} or just \"GUID\" — use query_assets to find valid asset IDs.")]
    public static async Task<string> SetAssetProperty(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID (GUID from query_assets)")] string assetId,
        [Description("Dot-notation property path (e.g. 'Width', 'Attributes.CullMode', 'Layers[0].DiffuseModel', 'Dict[key]')")] string propertyPath,
        [Description("JSON value to set. Scalar: '2048', 'true', '\"Back\"'. Color: '{\"r\":1,\"g\":0,\"b\":0,\"a\":1}'. Asset reference: '{\"assetId\":\"GUID\"}' or '\"GUID\"'. Clear reference: 'null'.")] string value,
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
            string? leafBracketKey = null;

            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];

                // Parse bracket notation: "Name[key]" or "Name[0]"
                ParseBracket(segment, out var memberName, out var bracketKey);

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

                bool isLastSegment = i == segments.Length - 1;

                if (isLastSegment && bracketKey == null)
                {
                    // Simple leaf property
                    leafMember = member;
                }
                else if (isLastSegment && bracketKey != null)
                {
                    // Last segment with bracket — set indexed value (e.g. "Dict[key]" or "List[0]")
                    leafMember = member;
                    leafBracketKey = bracketKey;
                }
                else
                {
                    // Navigate deeper
                    var target = member.Target;
                    if (bracketKey != null && target != null)
                    {
                        try
                        {
                            var nodeIndex = ResolveNodeIndex(target, bracketKey);
                            target = target.IndexedTarget(nodeIndex);
                        }
                        catch (Exception ex)
                        {
                            return new
                            {
                                error = $"Cannot resolve index [{bracketKey}] for property '{memberName}': {ex.Message}",
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
                }
            }

            if (leafMember == null)
            {
                return new { error = "Could not resolve property path.", result = (object?)null };
            }

            // Apply in undo/redo transaction
            var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
            using (var transaction = undoRedoService.CreateTransaction())
            {
                try
                {
                    if (leafBracketKey != null)
                    {
                        // Set indexed value (dictionary or collection entry)
                        SetIndexedValue(leafMember, leafBracketKey, jsonValue, session);
                    }
                    else
                    {
                        // Simple property update
                        var convertedValue = JsonTypeConverter.ConvertJsonToType(jsonValue, leafMember.Type, session);
                        leafMember.Update(convertedValue);
                    }
                }
                catch (Exception ex)
                {
                    return new { error = $"Cannot set value: {ex.Message}", result = (object?)null };
                }

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

    private static void ParseBracket(string segment, out string memberName, out string? bracketKey)
    {
        var bracketStart = segment.IndexOf('[');
        if (bracketStart >= 0)
        {
            memberName = segment[..bracketStart];
            var bracketEnd = segment.IndexOf(']');
            bracketKey = bracketEnd > bracketStart + 1 ? segment[(bracketStart + 1)..bracketEnd] : null;
        }
        else
        {
            memberName = segment;
            bracketKey = null;
        }
    }

    private static NodeIndex ResolveNodeIndex(IObjectNode target, string bracketKey)
    {
        var descriptor = TypeDescriptorFactory.Default.Find(target.Type);

        if (descriptor is DictionaryDescriptor dictDesc)
        {
            var keyType = dictDesc.KeyType;
            if (keyType == typeof(string))
                return new NodeIndex(bracketKey);
            if (keyType == typeof(int) && int.TryParse(bracketKey, out var intKey))
                return new NodeIndex(intKey);
            if (keyType.IsEnum && Enum.TryParse(keyType, bracketKey, ignoreCase: true, out var enumKey))
                return new NodeIndex(enumKey!);

            throw new InvalidOperationException($"Cannot convert '{bracketKey}' to dictionary key type {keyType.Name}.");
        }

        // Collection/list: parse as integer index
        if (int.TryParse(bracketKey, out var index))
            return new NodeIndex(index);

        throw new InvalidOperationException($"Cannot resolve index '{bracketKey}' — expected an integer for collection access.");
    }

    private static void SetIndexedValue(IMemberNode memberNode, string bracketKey, JsonElement jsonValue, SessionViewModel session)
    {
        var target = memberNode.Target;
        if (target == null)
            throw new InvalidOperationException("Property has no target object — cannot set indexed value.");

        var nodeIndex = ResolveNodeIndex(target, bracketKey);
        var descriptor = TypeDescriptorFactory.Default.Find(target.Type);

        // Determine value type
        Type valueType;
        if (descriptor is DictionaryDescriptor dictDesc)
            valueType = dictDesc.ValueType;
        else if (descriptor is CollectionDescriptor collDesc)
            valueType = collDesc.ElementType;
        else
            throw new InvalidOperationException($"Property type {target.Type.Name} does not support indexed access.");

        var convertedValue = JsonTypeConverter.ConvertJsonToType(jsonValue, valueType, session);

        // Check if the index already exists
        bool exists = false;
        if (target.Indices != null)
        {
            exists = target.Indices.Any(idx => Equals(idx.Value, nodeIndex.Value));
        }

        if (exists)
        {
            target.Update(convertedValue, nodeIndex);
        }
        else
        {
            target.Add(convertedValue, nodeIndex);
        }
    }
}
