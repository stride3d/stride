// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Models;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Extensions;
using Stride.Core.Quantum;
using Stride.Core.Reflection;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// View model for <see cref="ModelAsset"/>.
/// </summary>
/// <remarks>
/// The "Create Skeleton" asset command of the WPF view model is not available here: it runs an asset template, and templates are not part of the cross-platform editor yet.
/// </remarks>
[AssetViewModel<ModelAsset>]
public class ModelViewModel : ImportedAssetViewModel<ModelAsset>
{
    public ModelViewModel(AssetViewModelConstructionParameters parameters)
        : base(parameters)
    {
    }

    /// <inheritdoc />
    protected override IAssetImporter? GetImporter()
    {
        return AssetRegistry.FindImporterForFile(Asset.Source).OfType<ModelAssetImporter>().FirstOrDefault();
    }

    /// <inheritdoc />
    protected override void PrepareImporterInputParametersForUpdateFromSource(PropertyCollection importerInputParameters, ModelAsset asset)
    {
        // This setting will be ignored if it's the FBX importer
        importerInputParameters.Set(ModelAssetImporter.DeduplicateMaterialsKey, asset.DeduplicateMaterials);
    }

    /// <inheritdoc />
    protected override void UpdateAssetFromSource(ModelAsset assetToMerge)
    {
        UpdateMorphTargetNames(AssetRootNode, assetToMerge.MorphTargetNames);

        // Create a dictionary containing all new and old materials, favoring old ones to maintain existing references
        var dictionary = assetToMerge.Materials.ToDictionary(x => x.Name, x => x);
        Asset.Materials.ForEach(x => dictionary[x.Name] = x);

        // Create a dictionary mapping existing materials to their item id, to attempt to maintain existing ids and avoid unnecessary changes.
        var ids = CollectionItemIdHelper.GetCollectionItemIds(Asset.Materials).ToDictionary(x => Asset.Materials[(int)x.Key].Name, x => x.Value);

        // Remove currently existing materials, one by one because Quantum does not provide a Clear method.
        var materialsNode = AssetRootNode[nameof(ModelAsset.Materials)].Target!;
        while (Asset.Materials.Count > 0)
        {
            materialsNode.Remove(Asset.Materials[0], new NodeIndex(0));
        }

        // Repopulate the list of materials
        for (var i = 0; i < assetToMerge.Materials.Count; ++i)
        {
            // Retrieve or create an id for the material
            if (!ids.TryGetValue(assetToMerge.Materials[i].Name, out var id))
                id = ItemId.New();

            // Use Restore to allow to set manually the id.
            materialsNode.Restore(dictionary[assetToMerge.Materials[i].Name], new NodeIndex(i), id);
        }
    }

    internal static void UpdateMorphTargetNames(IAssetObjectNode modelNode, IReadOnlyList<string> importedNames)
    {
        // Use Quantum so refreshing this imported metadata is saved and undone with the source update.
        var namesNode = modelNode[nameof(ModelAsset.MorphTargetNames)].Target!;
        var currentNames = (List<string>)namesNode.Retrieve()!;
        var firstChangedIndex = 0;
        while (firstChangedIndex < currentNames.Count && firstChangedIndex < importedNames.Count)
        {
            if (currentNames[firstChangedIndex] != importedNames[firstChangedIndex])
                break;

            ++firstChangedIndex;
        }
        if (firstChangedIndex == currentNames.Count && firstChangedIndex == importedNames.Count)
            return;

        // Only preserve IDs for entries that will be removed and may be restored.
        // Appending names or removing names from the end needs no lookup.
        Dictionary<string, ItemId>? itemIdsByName = null;
        if (firstChangedIndex < currentNames.Count && firstChangedIndex < importedNames.Count)
        {
            itemIdsByName = new Dictionary<string, ItemId>(currentNames.Count - firstChangedIndex, StringComparer.Ordinal);
            var currentItemIds = CollectionItemIdHelper.GetCollectionItemIds(currentNames);
            foreach (var item in currentItemIds)
            {
                var index = (int)item.Key;
                if (index >= firstChangedIndex)
                    itemIdsByName.Add(currentNames[index], item.Value);
            }
        }

        // Remove names from the first change onward, working backwards to keep earlier indexes valid.
        for (var index = currentNames.Count - 1; index >= firstChangedIndex; --index)
        {
            namesNode.Remove(currentNames[index], new NodeIndex(index));
        }

        for (var index = firstChangedIndex; index < importedNames.Count; ++index)
        {
            var name = importedNames[index];
            var itemId = itemIdsByName != null && itemIdsByName.TryGetValue(name, out var existingId)
                ? existingId
                : ItemId.New();
            namesNode.Restore(name, new NodeIndex(index), itemId);
        }
    }
}
