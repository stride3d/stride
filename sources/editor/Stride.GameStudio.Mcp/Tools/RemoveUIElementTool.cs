// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.UI;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Services;
using Stride.UI;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class RemoveUIElementTool
{
    [McpServerTool(Name = "remove_ui_element"), Description("Removes a UI element and its descendants from a UIPageAsset. This operation supports undo/redo.")]
    public static async Task<string> RemoveUIElement(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the UIPageAsset")] string assetId,
        [Description("The UI element ID to remove (GUID from get_ui_tree)")] string elementId,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(assetId, out var id))
            {
                return new { error = "Invalid asset ID format. Expected a GUID.", removed = (object?)null };
            }

            if (!Guid.TryParse(elementId, out var elementGuid))
            {
                return new { error = "Invalid element ID format. Expected a GUID.", removed = (object?)null };
            }

            var assetVm = session.GetAssetById(id);
            if (assetVm is not UIBaseViewModel uiPageVm)
            {
                var errorMsg = assetVm == null
                    ? $"Asset not found: {assetId}"
                    : $"Asset is not a UI page: {assetVm.Name} ({assetVm.AssetType.Name})";
                return new { error = errorMsg, removed = (object?)null };
            }

            var uiAsset = (UIAssetBase)uiPageVm.Asset;

            if (!uiAsset.Hierarchy.Parts.TryGetValue(elementGuid, out var design))
            {
                return new { error = $"UI element not found: {elementId}", removed = (object?)null };
            }

            var element = design.UIElement;
            var elementName = element.Name ?? element.GetType().Name;

            // Count descendants for reporting
            int descendantCount = CountDescendants(element);

            // Remove via the property graph with undo/redo support
            var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
            using (var transaction = undoRedoService.CreateTransaction())
            {
                uiPageVm.AssetHierarchyPropertyGraph.RemovePartFromAsset(design);
                undoRedoService.SetName(transaction, $"Remove UI element '{elementName}'");
            }

            return new
            {
                error = (string?)null,
                removed = (object)new
                {
                    id = elementId,
                    name = elementName,
                    descendantsRemoved = descendantCount,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    private static int CountDescendants(UIElement element)
    {
        int count = 0;
        foreach (var child in element.VisualChildren)
        {
            count += 1 + CountDescendants(child);
        }
        return count;
    }
}
