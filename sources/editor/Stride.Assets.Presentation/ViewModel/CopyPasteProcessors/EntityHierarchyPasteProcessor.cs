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
using Stride.Assets.Entities;
using Stride.Assets.Presentation.Quantum;
using Stride.Engine;

namespace Stride.Assets.Presentation.ViewModel.CopyPasteProcessors
{
    public class EntityHierarchyPasteProcessor : AssetCompositeHierarchyPasteProcessor<EntityDesign, Entity>
    {
        public static readonly PropertyKey<string> TargetFolderKey = new PropertyKey<string>("TargetFolder", typeof(EntityHierarchyPasteProcessor));

        public override Task Paste(IPasteItem pasteResultItem, AssetPropertyGraph assetPropertyGraph, ref NodeAccessor nodeAccessor, ref PropertyContainer propertyContainer)
        {
            if (pasteResultItem == null) throw new ArgumentNullException(nameof(pasteResultItem));

            var propertyGraph = (EntityHierarchyPropertyGraph)assetPropertyGraph;
            var parentEntity = nodeAccessor.RetrieveValue() as Entity;
            string targetFolder;
            propertyContainer.TryGetValue(TargetFolderKey, out targetFolder);

            var hierarchy = pasteResultItem.Data as AssetCompositeHierarchyData<EntityDesign, Entity>;
            if (hierarchy != null)
            {
                foreach (var rootEntity in hierarchy.RootParts)
                {
                    var insertIndex = parentEntity?.Transform.Children.Count ?? propertyGraph.Asset.Hierarchy.RootParts.Count;
                    var entityDesign = hierarchy.Parts[rootEntity.Id];
                    var folder = targetFolder;
                    if (!string.IsNullOrEmpty(entityDesign.Folder))
                    {
                        if (!string.IsNullOrEmpty(targetFolder))
                            folder = folder + "/" + entityDesign.Folder;
                        else
                            folder = entityDesign.Folder;
                    }
                    entityDesign.Folder = folder ?? string.Empty;
                    propertyGraph.AddPartToAsset(hierarchy.Parts, entityDesign, parentEntity, insertIndex);
                }
            }

            return Task.CompletedTask;
        }
    }
}
