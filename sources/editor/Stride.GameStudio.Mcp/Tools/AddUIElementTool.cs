// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.UI;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Services;
using Stride.UI;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class AddUIElementTool
{
    [McpServerTool(Name = "add_ui_element"), Description("Creates a new UI element and adds it to a UIPageAsset hierarchy. Supported types: Button, TextBlock, Grid, StackPanel, Canvas, ImageElement, EditText, ToggleButton, Slider, ScrollViewer, ContentDecorator, Border, UniformGrid. This operation supports undo/redo.")]
    public static async Task<string> AddUIElement(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the UIPageAsset")] string assetId,
        [Description("UI element type name (e.g. 'Button', 'TextBlock', 'StackPanel', 'Grid')")] string elementType,
        [Description("Optional name for the element")] string? name = null,
        [Description("Optional parent element ID (GUID). Must be a Panel or ContentControl. If omitted, adds to root.")] string? parentId = null,
        [Description("Optional insertion index within parent's children. Defaults to append.")] int? index = null,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(assetId, out var id))
            {
                return new { error = "Invalid asset ID format. Expected a GUID.", element = (object?)null };
            }

            var assetVm = session.GetAssetById(id);
            if (assetVm is not UIBaseViewModel uiPageVm)
            {
                var errorMsg = assetVm == null
                    ? $"Asset not found: {assetId}"
                    : $"Asset is not a UI page: {assetVm.Name} ({assetVm.AssetType.Name})";
                return new { error = errorMsg, element = (object?)null };
            }

            var uiAsset = (UIAssetBase)uiPageVm.Asset;

            // Resolve the UIElement type from the type name
            var uiElementAssembly = typeof(UIElement).Assembly;
            var resolvedType = uiElementAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == elementType
                    && typeof(UIElement).IsAssignableFrom(t)
                    && !t.IsAbstract);

            if (resolvedType == null)
            {
                var availableTypes = uiElementAssembly.GetTypes()
                    .Where(t => typeof(UIElement).IsAssignableFrom(t) && !t.IsAbstract && t.IsPublic)
                    .Select(t => t.Name)
                    .OrderBy(n => n)
                    .ToArray();
                return new
                {
                    error = $"Unknown UI element type: '{elementType}'. Available types: {string.Join(", ", availableTypes)}",
                    element = (object?)null,
                };
            }

            // Instantiate the element
            UIElement newElement;
            try
            {
                newElement = (UIElement)Activator.CreateInstance(resolvedType)!;
            }
            catch (Exception ex)
            {
                return new { error = $"Failed to create UI element of type '{elementType}': {ex.Message}", element = (object?)null };
            }

            if (!string.IsNullOrEmpty(name))
            {
                newElement.Name = name;
            }

            // Generate collection IDs
            AssetCollectionItemIdHelper.GenerateMissingItemIds(newElement);

            // Wrap in UIElementDesign collection
            var collection = new AssetPartCollection<UIElementDesign, UIElement>
            {
                new UIElementDesign(newElement)
            };

            // Resolve parent element if specified
            UIElement? parentElement = null;
            if (!string.IsNullOrEmpty(parentId))
            {
                if (!Guid.TryParse(parentId, out var parentGuid))
                {
                    return new { error = $"Invalid parent element ID format: {parentId}", element = (object?)null };
                }

                if (!uiAsset.Hierarchy.Parts.TryGetValue(parentGuid, out var parentDesign))
                {
                    return new { error = $"Parent element not found: {parentId}", element = (object?)null };
                }
                parentElement = parentDesign.UIElement;
            }

            // Determine insertion index (-1 means append, which InsertUIElement handles)
            int insertIndex = index ?? -1;

            // Add to the hierarchy via property graph
            var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
            using (var transaction = undoRedoService.CreateTransaction())
            {
                uiPageVm.InsertUIElement(
                    collection,
                    collection.Single().Value,
                    parentElement,
                    insertIndex);

                undoRedoService.SetName(transaction, $"Add UI element '{name ?? elementType}'");
            }

            return new
            {
                error = (string?)null,
                element = (object)new
                {
                    id = newElement.Id.ToString(),
                    name = newElement.Name ?? "",
                    type = newElement.GetType().Name,
                    parentId = parentElement?.Id.ToString(),
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
