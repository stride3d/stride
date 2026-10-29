// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Core.Assets;
using Stride.Core.Quantum;
using Stride.Engine;

namespace Stride.Assets.Presentation.ViewModels;

public abstract class EntityHierarchyRootViewModel : EntityHierarchyItemViewModel
{
    private readonly IObjectNode rootPartsNode;

    protected EntityHierarchyRootViewModel(EntityHierarchyViewModel asset)
        : base(asset, asset.Asset.Hierarchy.EnumerateRootPartDesigns())
    {
        rootPartsNode = asset.Session.AssetNodeContainer.GetNode(EntityHierarchy.Hierarchy)[nameof(AssetCompositeHierarchyData<EntityDesign, Entity>.RootParts)].Target!;
        rootPartsNode.ItemChanged += RootPartsChanged;
    }

    /// <summary>
    /// Raised when the asset got an entity, after its view model (with its children) was inserted in the hierarchy.
    /// </summary>
    public event EventHandler<EntityHierarchyChangedEventArgs>? EntityAdded;

    /// <summary>
    /// Raised when the asset lost an entity, after its view model was removed from the hierarchy and before it is
    /// destroyed with its children.
    /// </summary>
    public event EventHandler<EntityHierarchyChangedEventArgs>? EntityRemoved;

    /// <inheritdoc/>
    public override IEnumerable<EntityViewModel> InnerSubEntities => Children.SelectMany(x => x.InnerSubEntities);

    /// <inheritdoc/>
    public override void Destroy()
    {
        rootPartsNode.ItemChanged -= RootPartsChanged;
        base.Destroy();
    }

    /// <inheritdoc/>
    public override GraphNodePath GetNodePath()
    {
        var path = new GraphNodePath(GetNode());
        path.PushMember(nameof(EntityHierarchy.Hierarchy));
        path.PushTarget();
        return path;
    }

    internal void NotifyEntityAdded(EntityHierarchyItemViewModel parent, EntityViewModel entity, int index)
    {
        EntityAdded?.Invoke(this, new EntityHierarchyChangedEventArgs(parent, entity, index));
    }

    internal void NotifyEntityRemoved(EntityHierarchyItemViewModel parent, EntityViewModel entity, int index)
    {
        EntityRemoved?.Invoke(this, new EntityHierarchyChangedEventArgs(parent, entity, index));
    }

    private void RootPartsChanged(object? sender, ItemChangeEventArgs e)
    {
        switch (e.ChangeType)
        {
            case ContentChangeType.CollectionAdd:
                InsertEntity(e.Index.Int, EntityHierarchy.Hierarchy.Parts[((Entity)e.NewValue!).Id]);
                break;
            case ContentChangeType.CollectionRemove:
                RemoveEntity((Entity)e.OldValue!);
                break;
        }
    }
}
