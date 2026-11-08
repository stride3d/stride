// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Models;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Templates;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Quantum;
using Stride.Core.Reflection;
using Stride.Rendering;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.Services;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// View model for <see cref="ModelAsset"/>.
/// </summary>
[AssetViewModel<ModelAsset>]
public class ModelViewModel : ImportedAssetViewModel<ModelAsset>
{
    /// <summary>
    /// The id of the asset template that imports the skeleton of a model file (<c>DefaultSkeleton.sdtpl</c>, run by the
    /// skeleton import generator of the editor).
    /// </summary>
    internal static readonly Guid SkeletonFromFileTemplateId = new("7DA3597C-7AC4-450E-B60B-21DDB6296208");

    public ModelViewModel(AssetViewModelConstructionParameters parameters)
        : base(parameters)
    {
        CreateSkeletonCommand = new AnonymousTaskCommand(ServiceProvider, CreateSkeletonAsync);

        // FIXME: tooltip, icons, etc. should not be created on the view model side (see PDX-2952)
        // Note: the icon is a resource key that the view resolves (see MenuCommandInfo.Icon).
        assetCommands.Add(new MenuCommandInfo(ServiceProvider, CreateSkeletonCommand)
        {
            DisplayName = "Create Skeleton",
            Tooltip = "Create a skeleton asset",
            Icon = "ImageCreateSkeleton",
        });
    }

    /// <summary>
    /// Imports the skeleton of the model's source file as a new asset, and makes it the model's skeleton, in one undoable action.
    /// </summary>
    public ICommandBase CreateSkeletonCommand { get; }

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

    /// <summary>
    /// What <see cref="CreateSkeletonCommand"/> does, for the callers that wait for it.
    /// </summary>
    internal async Task CreateSkeletonAsync()
    {
        var source = Asset.Source;
        if (UPath.IsNullOrEmpty(source))
            return;

        // Templates run in the asset view of the editor's session
        if (Session.ActiveAssetView is not { } assetView)
            return;

        using (var transaction = UndoRedoService.CreateTransaction())
        {
            var template = Session.FindTemplates(TemplateScope.Asset).SingleOrDefault(x => x.Id == SkeletonFromFileTemplateId);
            if (template != null)
            {
                var skeleton = (await assetView.RunAssetTemplateAsync(template, [source])).SingleOrDefault();
                if (skeleton == null)
                    return;

                var skeletonNode = AssetRootNode[nameof(ModelAsset.Skeleton)];
                var reference = ContentReferenceHelper.CreateReference<Skeleton>(skeleton);
                skeletonNode.Update(reference);
            }
            UndoRedoService.SetName(transaction, "Create Skeleton");
        }
    }
}
