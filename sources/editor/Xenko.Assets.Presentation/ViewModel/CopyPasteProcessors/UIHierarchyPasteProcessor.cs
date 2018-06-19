// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel.CopyPasteProcessors;
using Xenko.Core.Assets.Quantum;
using Xenko.Core;
using Xenko.Core.Quantum;
using Xenko.Assets.Presentation.Quantum;
using Xenko.Assets.UI;
using Xenko.UI;
using Xenko.UI.Panels;

namespace Xenko.Assets.Presentation.ViewModel.CopyPasteProcessors
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
