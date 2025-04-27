// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Core;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.Engine;

namespace Stride.Assets.Presentation.ViewModels;

public sealed class EntityViewModel : EntityHierarchyItemViewModel, IAssetPropertyProviderViewModel
{
    private readonly MemberGraphNodeBinding<string> nameNodeBinding;
    private readonly ObjectGraphNodeBinding<EntityComponentCollection> componentsNodeBinding;

    public EntityViewModel(EntityHierarchyViewModel asset, EntityDesign entityDesign)
        : base(asset, GetOrCreateChildPartDesigns((EntityHierarchyAssetBase)asset.Asset, entityDesign))
    {
        EntityDesign = entityDesign;

        var assetNode = asset.Session.AssetNodeContainer.GetOrCreateNode(entityDesign.Entity);
        nameNodeBinding = new MemberGraphNodeBinding<string>(assetNode[nameof(Entity.Name)], nameof(Name), OnPropertyChanging, OnPropertyChanged, ServiceProvider.TryGet<IUndoRedoService>());
        componentsNodeBinding = new ObjectGraphNodeBinding<EntityComponentCollection>(assetNode[nameof(Entity.Components)].Target!, nameof(Components), OnPropertyChanging, OnPropertyChanged, ServiceProvider.TryGet<IUndoRedoService>(), false);
    }

    public IEnumerable<EntityComponent> Components => componentsNodeBinding.GetNodeValue();

    public Entity AssetSideEntity => EntityDesign.Entity;

    /// <inheritdoc/>
    public override AbsoluteId Id => new(Asset.Id, AssetSideEntity.Id);

    /// <inheritdoc/>
    public override IEnumerable<EntityViewModel> InnerSubEntities { get { yield return this; } }

    /// <inheritdoc/>
    public override string? Name
    {
        get => nameNodeBinding.Value;
        set => nameNodeBinding.Value = value;
    }

    internal EntityDesign EntityDesign { get; }

    bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

    AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => Asset;

    /// <inheritdoc/>
    public override GraphNodePath GetNodePath()
    {
        var path = new GraphNodePath(GetNode());
        path.PushMember(nameof(EntityHierarchy.Hierarchy));
        path.PushTarget();
        path.PushMember(nameof(EntityHierarchy.Hierarchy.Parts));
        path.PushTarget();
        path.PushIndex(new NodeIndex(Id.ObjectId));
        path.PushMember(nameof(EntityDesign.Entity));
        path.PushTarget();
        return path;
    }

    // TODO: turn this non-static and put it in the base - just keep the entity-specific part here. This need to rework a bit how we initialize folders
    private static IEnumerable<EntityDesign> GetOrCreateChildPartDesigns(EntityHierarchyAssetBase asset, EntityDesign entityDesign)
    {
        foreach (var child in entityDesign.Entity.Transform.Children)
        {
            if (!asset.Hierarchy.Parts.TryGetValue(child.Entity.Id, out var childDesign))
            {
                childDesign = new EntityDesign(child.Entity);
            }
            if (child.Entity != childDesign.Entity) throw new InvalidOperationException();
            yield return childDesign;
        }
    }

    /// <inheritdoc/>
    GraphNodePath IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode()
    {
        return GetNodePath();
    }

    /// <inheritdoc/>
    IObjectNode IPropertyProviderViewModel.GetRootNode()
    {
        return Asset.Session.AssetNodeContainer.GetOrCreateNode(AssetSideEntity);
    }

    /// <inheritdoc/>
    bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => ((IPropertyProviderViewModel)Asset).ShouldConstructItem(collection, index);

    /// <inheritdoc/>
    bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => ((IPropertyProviderViewModel)Asset).ShouldConstructMember(member);
}
