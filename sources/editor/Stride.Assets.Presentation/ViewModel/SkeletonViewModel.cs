// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Reflection;
using Xenko.Core.Quantum;
using Xenko.Assets.Models;

namespace Xenko.Assets.Presentation.ViewModel
{
    [AssetViewModel(typeof(SkeletonAsset))]
    public class SkeletonViewModel : ImportedAssetViewModel<SkeletonAsset>
    {
        public SkeletonViewModel(AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
        }

        protected override IAssetImporter GetImporter()
        {
            return AssetRegistry.FindImporterForFile(Asset.Source).OfType<ModelAssetImporter>().FirstOrDefault();
        }

        protected override void UpdateAssetFromSource(SkeletonAsset assetToMerge)
        {
            // Create a dictionary containing all new and old nodes, favoring old ones to maintain existing references
            var dictionary = assetToMerge.Nodes.ToDictionary(x => x.Name, x => x);
            Asset.Nodes.ForEach(x => dictionary[x.Name] = x);

            // Create a dictionary mapping existing materials to their item id, to attempt to maintain existing ids and avoid unnecessary changes.
            var ids = CollectionItemIdHelper.GetCollectionItemIds(Asset.Nodes).ToDictionary(x => Asset.Nodes[(int)x.Key].Name, x => x.Value);

            // Remove currently existing nodes, one by one because Quantum does not provide a Clear method.
            var skeletonNodes = AssetRootNode[nameof(SkeletonAsset.Nodes)].Target;
            while (Asset.Nodes.Count > 0)
            {
                skeletonNodes.Remove(Asset.Nodes[0], new NodeIndex(0));
            }

            // Repopulate the list of nodes
            for (var i = 0; i < assetToMerge.Nodes.Count; ++i)
            {
                // Information such as Depth has to come from the new asset
                var newNode = assetToMerge.Nodes[i];
                // Information such as Preserve should come from the merged dictionary.
                newNode.Preserve = dictionary[assetToMerge.Nodes[i].Name].Preserve;

                // Retrieve or create an id for the node
                ItemId id;
                if (!ids.TryGetValue(assetToMerge.Nodes[i].Name, out id))
                    id = ItemId.New();

                skeletonNodes.Restore(newNode, new NodeIndex(i), id);
            }
        }
    }
}
