// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Core.Assets;
using Stride.Core.Extensions;
using Stride.Core.Quantum;
using Stride.Engine;

namespace Stride.Assets.Presentation.ViewModels;

public abstract class EntityHierarchyRootViewModel : EntityHierarchyElementViewModel
{
    private readonly IObjectNode rootPartsNode;

    protected EntityHierarchyRootViewModel(EntityHierarchyViewModel asset)
        : base(asset, asset.Asset.Hierarchy.EnumerateRootPartDesigns())
    {
        rootPartsNode = asset.Session.AssetNodeContainer.GetNode(EntityHierarchy.Hierarchy)[nameof(AssetCompositeHierarchyData<EntityDesign, Entity>.RootParts)].Target!;
        rootPartsNode.ItemChanging += RootPartsChanging;
        rootPartsNode.ItemChanged += RootPartsChanged;
        // The folder operations find the folders of the asset through its open hierarchy
        asset.HierarchyRoot = this;
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

    /// <summary>
    /// Raised when an element of the hierarchy (the root or an entity) was locked or unlocked.
    /// </summary>
    public event EventHandler<EntityHierarchyElementEventArgs>? LockChanged;

    /// <inheritdoc/>
    public override IEnumerable<EntityViewModel> InnerSubEntities => Children.SelectMany(x => x.InnerSubEntities);

    /// <inheritdoc/>
    public override bool IsEditable => false;

    /// <inheritdoc/>
    public override int EntityCount => EntityHierarchy.Hierarchy.RootParts.Count;

    /// <inheritdoc/>
    public override void Destroy()
    {
        rootPartsNode.ItemChanging -= RootPartsChanging;
        rootPartsNode.ItemChanged -= RootPartsChanged;
        if (((EntityHierarchyViewModel)Asset).HierarchyRoot == this)
            ((EntityHierarchyViewModel)Asset).HierarchyRoot = null;
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

    /// <inheritdoc/>
    public override int IndexOfEntity(EntityViewModel entity)
    {
        return EntityHierarchy.Hierarchy.RootParts.IndexOf(x => x.Id == entity.Id.ObjectId);
    }

    /// <summary>
    /// Finds an item of this hierarchy by its id: the root, an entity or a folder.
    /// </summary>
    public EntityHierarchyItemViewModel? FindItem(Core.AbsoluteId id)
    {
        return id == Id ? this : Children.BreadthFirst(x => x.Children).FirstOrDefault(x => x.Id == id);
    }

    internal void NotifyEntityAdded(EntityHierarchyElementViewModel parent, EntityViewModel entity, int index)
    {
        EntityAdded?.Invoke(this, new EntityHierarchyChangedEventArgs(parent, entity, index));
    }

    internal void NotifyEntityRemoved(EntityHierarchyElementViewModel parent, EntityViewModel entity, int index)
    {
        EntityRemoved?.Invoke(this, new EntityHierarchyChangedEventArgs(parent, entity, index));
    }

    internal void NotifyLockChanged(EntityHierarchyElementViewModel element)
    {
        LockChanged?.Invoke(this, new EntityHierarchyElementEventArgs(element));
    }

    private void RootPartsChanging(object? sender, ItemChangeEventArgs e)
    {
        // The folder exists before the entity goes in
        if (e.ChangeType == ContentChangeType.CollectionAdd)
        {
            FindOrCreateFolder(EntityHierarchy.Hierarchy.Parts[((Entity)e.NewValue!).Id].Folder);
        }
    }

    private void RootPartsChanged(object? sender, ItemChangeEventArgs e)
    {
        switch (e.ChangeType)
        {
            case ContentChangeType.CollectionAdd:
                InsertEntity(e.Index.Int, EntityHierarchy.Hierarchy.Parts[((Entity)e.NewValue!).Id], EntityHierarchy.Hierarchy.EnumerateRootPartDesigns());
                break;
            case ContentChangeType.CollectionRemove:
                RemoveEntity((Entity)e.OldValue!, e.Index.Int);
                break;
        }
    }
}
