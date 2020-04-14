// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel.CopyPasteProcessors;
using Stride.Core.Assets.Quantum;
using Stride.Core;
using Stride.Core.Quantum;
using Stride.Assets.Presentation.Quantum;
using Stride.Assets.UI;
using Stride.UI;
using Stride.UI.Panels;

namespace Stride.Assets.Presentation.ViewModel.CopyPasteProcessors
{
    public class UIHierarchyPasteProcessor : AssetCompositeHierarchyPasteProcessor<UIElementDesign, UIElement>
    {
        public override Task Paste(IPasteItem pasteResultItem, AssetPropertyGraph assetPropertyGraph, ref NodeAccessor nodeAccessor, ref PropertyContainer container)
        {
            if (pasteResultItem == null) throw new ArgumentNullException(nameof(pasteResultItem));

            var propertyGraph = (UIAssetPropertyGraph)assetPropertyGraph;
            var parentElement = nodeAccessor.RetrieveValue() as UIElement;

            // 1. try to paste as hierarchy
            var hierarchy = pasteResultItem.Data as AssetCompositeHierarchyData<UIElementDesign, UIElement>;
            if (hierarchy != null)
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
}
