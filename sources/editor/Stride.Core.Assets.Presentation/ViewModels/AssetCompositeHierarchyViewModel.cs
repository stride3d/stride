// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Presentation.ViewModels;

public abstract class AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart> : AssetCompositeViewModel<AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>>
    where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
    where TAssetPart : class, IIdentifiable
{
    protected AssetCompositeHierarchyViewModel(ConstructorParameters parameters)
        : base(parameters)
    {
    }

    public AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart> AssetHierarchyPropertyGraph => (AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>)PropertyGraph;

    public abstract AssetCompositeItemViewModel CreatePartViewModel(TAssetPartDesign partDesign);

    /// <summary>
    /// Gathers all base assets used in the composition of this asset, recursively.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>> GatherAllBasePartAssets()
    {
        var baseAssets = new HashSet<AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>>();
        var assetToProcess = new Stack<AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>>();
        assetToProcess.Push(this);

        while (assetToProcess.Count > 0)
        {
            var asset = assetToProcess.Pop();
            if (asset != null)
            {
                foreach (var basePartAsset in asset.AssetHierarchyPropertyGraph.GetBasePartAssets())
                {
                    if (Session.GetAssetById(basePartAsset.Id) is AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart> viewModel && baseAssets.Add(viewModel))
                    {
                        assetToProcess.Push(viewModel);
                    }
                }
            }
        }

        return baseAssets;
    }

    /// <inheritdoc />
    protected override bool ShouldConstructPropertyItem(IObjectNode collection, NodeIndex index)
    {
        // Don't construct properties for item referencing child parts.
        if (AssetHierarchyPropertyGraph.IsChildPartReference(collection, index))
            return false;

        return base.ShouldConstructPropertyItem(collection, index);
    }

    /// <inheritdoc />
    protected override bool ShouldConstructPropertyMember(IMemberNode member)
    {
        // Don't construct properties of the Hierarchy object.
        if (member.MemberDescriptor.DeclaringType == typeof(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>))
            return false;

        // Don't construct properties for member referencing child parts.
        if (AssetHierarchyPropertyGraph.IsChildPartReference(member, NodeIndex.Empty))
            return false;

        return base.ShouldConstructPropertyMember(member);
    }
}
