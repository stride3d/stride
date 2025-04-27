// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Assets.Presentation.Quantum;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets;
using Stride.Core.Quantum;
using Stride.Core;
using Stride.Core.Assets.Editor.Components.CopyPasteProcessors;
using Stride.Core.Assets.Editor.Services;
using Stride.Engine;

namespace Stride.Assets.Editor.Components.CopyPasteProcessors;

internal sealed class EntityHierarchyPasteProcessor : AssetCompositeHierarchyPasteProcessor<EntityDesign, Entity>
{
    public static readonly PropertyKey<string> TargetFolderKey = new("TargetFolder", typeof(EntityHierarchyPasteProcessor));

    public override Task Paste(IPasteItem pasteResultItem, AssetPropertyGraph assetPropertyGraph, ref NodeAccessor nodeAccessor, ref PropertyContainer propertyContainer)
    {
        if (pasteResultItem == null) throw new ArgumentNullException(nameof(pasteResultItem));

        var propertyGraph = (EntityHierarchyPropertyGraph)assetPropertyGraph;
        var parentEntity = nodeAccessor.RetrieveValue() as Entity;
        propertyContainer.TryGetValue(TargetFolderKey, out var targetFolder);

        if (pasteResultItem.Data is AssetCompositeHierarchyData<EntityDesign, Entity> hierarchy)
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
