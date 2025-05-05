// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.Quantum;
using Stride.Assets.UI;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets;
using Stride.Core.Quantum;
using Stride.Core;
using Stride.Core.Assets.Editor.Components.CopyPasteProcessors;
using Stride.Core.Assets.Editor.Services;
using Stride.UI.Panels;
using Stride.UI;

namespace Stride.Assets.Editor.Components.CopyPasteProcessors;

internal sealed class UIHierarchyPasteProcessor : AssetCompositeHierarchyPasteProcessor<UIElementDesign, UIElement>
{
    public override Task Paste(IPasteItem pasteResultItem, AssetPropertyGraph assetPropertyGraph, ref NodeAccessor nodeAccessor, ref PropertyContainer container)
    {
        if (pasteResultItem == null) throw new ArgumentNullException(nameof(pasteResultItem));

        var propertyGraph = (UIAssetPropertyGraph)assetPropertyGraph;
        var parentElement = nodeAccessor.RetrieveValue() as UIElement;

        // 1. try to paste as hierarchy
        if (pasteResultItem.Data is AssetCompositeHierarchyData<UIElementDesign, UIElement> hierarchy)
        {
            // Note: check that adding or inserting is supported is done in CanPaste()
            foreach (var rootUIElement in hierarchy.RootParts)
            {
                var asset = (UIAssetBase)propertyGraph.Asset;
                var insertIndex = parentElement == null ? asset.Hierarchy.RootParts.Count : ((parentElement as Panel)?.Children.Count ?? 0);
                propertyGraph.AddPartToAsset(hierarchy.Parts, hierarchy.Parts[rootUIElement.Id], parentElement, insertIndex);
            }
        }
        return Task.CompletedTask;
    }
}
