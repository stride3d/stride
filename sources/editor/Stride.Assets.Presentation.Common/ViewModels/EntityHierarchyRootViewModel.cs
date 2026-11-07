// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Assets.Entities;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Extensions;
using Stride.Core.Quantum;
using Stride.Engine;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// The root of the hierarchy of a scene or a prefab: its children are the root entities of the asset (and, for a scene,
/// its child scenes).
/// </summary>
public abstract class EntityHierarchyRootViewModel : EntityHierarchyElementViewModel
{
    private readonly IObjectNode rootPartsNode;

    /// <param name="asset">The scene or the prefab.</param>
    /// <param name="editor">The editor of the hierarchy, or <c>null</c> when the hierarchy has no editor.</param>
    protected EntityHierarchyRootViewModel(EntityHierarchyViewModel asset, IEntityHierarchyEditorViewModel? editor)
        : base(asset, asset.Asset.Hierarchy.EnumerateRootPartDesigns(), null)
    {
        Editor = editor;
        rootPartsNode = asset.Session.AssetNodeContainer.GetNode(EntityHierarchy.Hierarchy)![nameof(AssetCompositeHierarchyData<EntityDesign, Entity>.RootParts)].Target!;
        rootPartsNode.ItemChanging += RootPartsChanging;
        rootPartsNode.ItemChanged += RootPartsChanged;
        // The items of the asset, and the folder operations, reach the editor and the folders through the open hierarchy
        asset.HierarchyRoot = this;
    }

    /// <summary>
    /// Raised when the asset got an entity, after its view model (with its children) was inserted in the hierarchy. The
    /// entity is loaded (<see cref="EntityHierarchyElementViewModel.IsLoaded"/>) when its root is: the editor then adds it
    /// to its game.
    /// </summary>
    /// <remarks>The top root raises it for the entities of the whole hierarchy, child scenes included.</remarks>
    public event EventHandler<EntityHierarchyChangedEventArgs>? EntityAdded;

    /// <summary>
    /// Raised when the asset lost an entity, after its view model was removed from the hierarchy and before it is
    /// destroyed with its children.
    /// </summary>
    /// <remarks>The top root raises it for the entities of the whole hierarchy, child scenes included.</remarks>
    public event EventHandler<EntityHierarchyChangedEventArgs>? EntityRemoved;

    /// <summary>
    /// Raised when an element of the hierarchy (a root or an entity) was loaded or unloaded, locked or unlocked: whether
    /// it can be selected in the game (<see cref="EntityHierarchyElementViewModel.IsSelectable"/>) may have changed.
    /// </summary>
    /// <remarks>The top root raises it for the elements of the whole hierarchy, child scenes included.</remarks>
    public event EventHandler<EntityHierarchyElementEventArgs>? SelectableChanged;

    /// <summary>
    /// Gets the editor of the hierarchy, or <c>null</c> when the hierarchy has no editor.
    /// </summary>
    public new IEntityHierarchyEditorViewModel? Editor { get; }

    /// <inheritdoc/>
    public override IEnumerable<EntityViewModel> InnerSubEntities => Children.SelectMany(x => x.InnerSubEntities);

    /// <inheritdoc/>
    public override bool IsEditable => false;

    /// <inheritdoc/>
    public override int EntityCount => EntityHierarchy.Hierarchy.RootParts.Count;

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(EntityHierarchyRootViewModel));
        rootPartsNode.ItemChanging -= RootPartsChanging;
        rootPartsNode.ItemChanged -= RootPartsChanged;
        base.Destroy();
        // After the items, which reach the editor through the asset when they are destroyed
        if (Asset.HierarchyRoot == this)
            Asset.HierarchyRoot = null;
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
    /// Finds an item of this hierarchy by its id: a root, an entity or a folder.
    /// </summary>
    public EntityHierarchyItemViewModel? FindItem(AbsoluteId id)
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

    internal void NotifySelectableChanged(EntityHierarchyElementViewModel element)
    {
        SelectableChanged?.Invoke(this, new EntityHierarchyElementEventArgs(element));
    }

    /// <summary>
    /// Sets <see cref="EntityHierarchyElementViewModel.IsLoaded"/> on all the entities of this root, which loads or unloads
    /// with all its content.
    /// </summary>
    protected void SetEntityIsLoadedRecursively(bool isLoaded)
    {
        // This method is called from OnLoadingRequested, therefore we don't need to request the entities to load.
        // We just need to update their status.
        foreach (var entity in InnerSubEntities.BreadthFirst(x => x.TransformChildren))
        {
            entity.SetIsLoaded(isLoaded);
        }
    }

    /// <summary>
    /// Sets <see cref="EntityHierarchyElementViewModel.IsLocked"/> on all the entities of this root, which locks or unlocks
    /// with all its content.
    /// </summary>
    protected void SetEntityIsLockedRecursively(bool isLocked)
    {
        // This method is called from OnLockingRequested, therefore we don't need to request the entities to lock.
        // We just need to update their status.
        foreach (var entity in InnerSubEntities.BreadthFirst(x => x.TransformChildren))
        {
            entity.SetIsLocked(isLocked);
        }
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
            default:
                throw new ArgumentOutOfRangeException(nameof(e));
        }
    }
}
