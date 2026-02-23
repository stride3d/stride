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
using Stride.Assets.UI;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.UI;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class GetUITreeTool
{
    [McpServerTool(Name = "get_ui_tree"), Description("Returns the full UI element hierarchy tree for a UIPageAsset. Each element includes its ID, name, type, and children. Use this to understand the structure of a UI page before inspecting individual elements with get_ui_element.")]
    public static async Task<string> GetUITree(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the UIPageAsset (from query_assets)")] string assetId,
        [Description("Maximum depth to traverse (default 50)")] int maxDepth = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(assetId, out var id))
            {
                return new { error = "Invalid asset ID format. Expected a GUID.", uiPage = (object?)null };
            }

            var assetVm = session.GetAssetById(id);
            if (assetVm == null)
            {
                return new { error = $"Asset not found: {assetId}", uiPage = (object?)null };
            }

            if (assetVm.Asset is not UIAssetBase uiAsset)
            {
                return new { error = $"Asset is not a UI page: {assetVm.Name} ({assetVm.AssetType.Name})", uiPage = (object?)null };
            }

            var rootElements = uiAsset.Hierarchy.RootParts;

            var elementTree = rootElements
                .Select(e => BuildElementNode(e, 0, maxDepth))
                .ToList();

            var totalCount = uiAsset.Hierarchy.Parts.Count;

            return new
            {
                error = (string?)null,
                uiPage = (object)new
                {
                    id = assetId,
                    name = assetVm.Name,
                    elementCount = totalCount,
                    elements = elementTree,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    private static object BuildElementNode(UIElement element, int depth, int maxDepth)
    {
        var children = new List<object>();

        if (depth < maxDepth)
        {
            foreach (var child in element.VisualChildren)
            {
                children.Add(BuildElementNode(child, depth + 1, maxDepth));
            }
        }

        return new
        {
            id = element.Id.ToString(),
            name = element.Name ?? "",
            type = element.GetType().Name,
            children,
        };
    }
}
