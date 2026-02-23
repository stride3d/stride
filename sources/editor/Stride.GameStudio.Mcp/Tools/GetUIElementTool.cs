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

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class GetUIElementTool
{
    [McpServerTool(Name = "get_ui_element"), Description("Returns detailed properties for a specific UI element within a UIPageAsset. Includes parent/child relationships and all serialized properties.")]
    public static async Task<string> GetUIElement(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the UIPageAsset")] string assetId,
        [Description("The UI element ID (GUID from get_ui_tree)")] string elementId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await dispatcher.InvokeOnUIThread(() =>
            {
                if (!AssetId.TryParse(assetId, out var id))
                {
                    return new { error = "Invalid asset ID format. Expected a GUID.", element = (object?)null };
                }

                if (!Guid.TryParse(elementId, out var elementGuid))
                {
                    return new { error = "Invalid element ID format. Expected a GUID.", element = (object?)null };
                }

                var assetVm = session.GetAssetById(id);
                if (assetVm == null)
                {
                    return new { error = $"Asset not found: {assetId}", element = (object?)null };
                }

                if (assetVm.Asset is not UIAssetBase uiAsset)
                {
                    return new { error = $"Asset is not a UI page: {assetVm.Name} ({assetVm.AssetType.Name})", element = (object?)null };
                }

                if (!uiAsset.Hierarchy.Parts.TryGetValue(elementGuid, out var design))
                {
                    return new { error = $"UI element not found: {elementId}", element = (object?)null };
                }

                var element = design.UIElement;
                var parentId = element.VisualParent?.Id.ToString();
                var childIds = element.VisualChildren
                    .Select(c => c.Id.ToString())
                    .ToList();

                var properties = JsonTypeConverter.SerializeDataMembers(element);

                return new
                {
                    error = (string?)null,
                    element = (object)new
                    {
                        id = element.Id.ToString(),
                        name = element.Name ?? "",
                        type = element.GetType().Name,
                        parentId,
                        childIds,
                        properties,
                    },
                };
            }, cancellationToken);

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Internal error: {ex.GetType().Name}: {ex.Message}", element = (object?)null }, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
