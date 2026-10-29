// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Core;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Engine;

namespace Stride.Assets.Presentation.ViewModels;

public abstract class EntityHierarchyItemViewModel : AssetCompositeItemViewModel<EntityHierarchyViewModel, EntityHierarchyItemViewModel>, IAssetPartViewModel
{
    protected EntityHierarchyItemViewModel(EntityHierarchyViewModel asset, IEnumerable<EntityDesign> childEntities)
        : base(asset)
    {
        AddItems(childEntities.Select(asset.CreatePartViewModel));
    }

    /// <inheritdoc />
    public abstract AbsoluteId Id { get; }

    /// <summary>
    /// An enumeration of the items represented by this item.
    /// </summary>
    /// <remarks>
    /// In case of an <see cref="EntityViewModel"/> it is equivalent to <c>this</c>.
    /// </remarks>
    // FIXME: find a better name
    public abstract IEnumerable<EntityViewModel> InnerSubEntities { get; }

    protected EntityHierarchyAssetBase EntityHierarchy => (EntityHierarchyAssetBase)Asset.Asset;

    /// <inheritdoc />
    public override void Destroy()
    {
        // The children go with their parent
        foreach (var child in Children.ToList())
        {
            child.Destroy();
        }
        base.Destroy();
    }

    /// <summary>
    /// Inserts the view model of an entity the asset just got, and notifies the root of the hierarchy.
    /// </summary>
    /// <param name="index">The index of the entity in the children of this item.</param>
    /// <param name="entityDesign">The design of the added entity.</param>
    protected void InsertEntity(int index, EntityDesign entityDesign)
    {
        var entity = ((EntityHierarchyViewModel)Asset).CreatePartViewModel(entityDesign);
        InsertItem(index, entity);
        GetRoot()?.NotifyEntityAdded(this, entity, index);
    }

    /// <summary>
    /// Removes the view model of an entity the asset just lost, notifies the root of the hierarchy, then destroys the view
    /// model with its children.
    /// </summary>
    /// <param name="assetSideEntity">The removed entity.</param>
    protected void RemoveEntity(Entity assetSideEntity)
    {
        var index = Children.IndexOf(x => x is EntityViewModel entity && entity.AssetSideEntity == assetSideEntity);
        if (index < 0)
            throw new InvalidOperationException($"The entity {assetSideEntity.Id} has no view model in this item.");

        var root = GetRoot();
        var entity = (EntityViewModel)Children[index];
        RemoveItemAt(index);
        root?.NotifyEntityRemoved(this, entity, index);
        entity.Destroy();
    }

    private EntityHierarchyRootViewModel? GetRoot()
    {
        var item = this;
        while (item.Parent is { } parent)
        {
            item = parent;
        }
        return item as EntityHierarchyRootViewModel;
    }
}
