// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CSharp.RuntimeBinder;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Yaml;
using Stride.Core.Extensions;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor.Components.CopyPasteProcessors;

/// <summary>
/// Paste processor for collection of <see cref="AssetItem"/>.
/// </summary>
internal sealed class AssetItemPasteProcessor : PasteProcessorBase
{
    private readonly ISessionViewModel session;

    public AssetItemPasteProcessor(ISessionViewModel session)
    {
        this.session = session;
    }

    /// <inheritdoc />
    public override bool Accept(Type targetRootType, Type targetMemberType, Type pastedDataType)
    {
        return typeof(ICollection<AssetItem>).IsAssignableFrom(targetRootType)
            && typeof(AssetItem).IsAssignableFrom(TypeDescriptorFactory.Default.Find(pastedDataType).GetInnerCollectionType());
    }

    /// <inheritdoc />
    public override bool ProcessDeserializedData(AssetPropertyGraphContainer graphContainer, object targetRootObject, Type targetMemberType, ref object data, bool isRootDataObjectReference, AssetId? sourceId, YamlAssetMetadata<OverrideType> overrides, YamlAssetPath basePath)
    {
        var collectionDescriptor = (CollectionDescriptor)TypeDescriptorFactory.Default.Find(targetRootObject.GetType());

        if (data is not IList<AssetItem> collection)
        {
            collection = (IList<AssetItem>)Activator.CreateInstance(collectionDescriptor.Type, true);
            collectionDescriptor.Add(collection, data);
        }

        for (var i = 0; i < collection.Count; i++)
        {
            var assetItem = collection[i];
            // If the asset already exists, clone it with new identifiers
            if (session.GetAssetById(assetItem.Id) != null)
            {
                // Create a derived asset and restore archetype to handle asset-specific cloning process.
                var clone = AssetCloner.Clone(assetItem.Asset, AssetClonerFlags.GenerateNewIdsForIdentifiableObjects, out var idRemapping);

                var assetType = assetItem.Asset.GetType();
                if (assetType.HasInterface(typeof(AssetCompositeHierarchy<,>)))
                {
                    try
                    {
                        // TODO: Find a way to fallback to the asset or generalize for all asset composite
                        dynamic assetComposite = clone;
                        // Remap indices of parts in Hierarchy.Part
                        var path = basePath.Clone();
                        path.PushItemId(CollectionItemIdHelper.GetCollectionItemIds(collection)[i]);
                        AssetCloningHelper.RemapIdentifiablePaths(overrides, idRemapping, path);
                        AssetPartsAnalysis.GenerateNewBaseInstanceIds(assetComposite.Hierarchy);
                    }
                    catch (RuntimeBinderException e)
                    {
                        e.Ignore();
                    }
                }
                // FIXME: rework this
                var postProcessor = session.ServiceProvider.Get<ICopyPasteService>().PostProcessors.FirstOrDefault(p => p.Accept(assetType));
                postProcessor?.PostPasteDeserialization(clone);
                collection[i] = new AssetItem(assetItem.Location, clone);
            }
        }

        // Get the fixed-up value
        data = collection;
        return true;
    }
}
