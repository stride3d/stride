// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// An element of the hierarchy of a scene or a prefab that has entities as children of its transform: the root of the
/// hierarchy or an entity.
/// </summary>
public abstract class EntityHierarchyElementViewModel : EntityHierarchyItemViewModel
{
    private bool isLocked;

    protected EntityHierarchyElementViewModel(EntityHierarchyViewModel asset, IEnumerable<EntityDesign> childEntities)
        : base(asset, childEntities)
    {
        LockCommand = new AnonymousCommand<bool>(ServiceProvider, recursive => RequestLocking(!IsLocked, recursive));
    }

    /// <summary>
    /// Gets whether this element is locked in the view of the editor: a locked entity cannot be selected there, hence it
    /// cannot be moved with the gizmos. It can still be selected in the hierarchy.
    /// </summary>
    public bool IsLocked { get => isLocked; private set => SetValue(ref isLocked, value, () => GetRoot()?.NotifyLockChanged(this)); }

    /// <summary>
    /// Gets the command that locks or unlocks this element. Its parameter says whether the children are locked or unlocked
    /// too, recursively (Ctrl in the WPF scene editor).
    /// </summary>
    public ICommandBase LockCommand { get; }

    /// <inheritdoc/>
    public override EntityHierarchyElementViewModel Owner => this;

    /// <summary>
    /// Gets the number of entities whose transform is a child of the transform of this element, folders ignored.
    /// </summary>
    public abstract int EntityCount { get; }

    /// <inheritdoc/>
    public override int FindIndexInParent(int indexInEntity, IEnumerable<EntityDesign> entityCollection)
    {
        // The entities without a folder, before the given one
        var indexInParent = 0;
        var i = 0;
        foreach (var entity in entityCollection)
        {
            if (i++ == indexInEntity)
                break;

            if (string.IsNullOrEmpty(entity.Folder))
                ++indexInParent;
        }
        return indexInParent;
    }

    /// <summary>
    /// Gets the index of the given entity in the children of the transform of this element, folders ignored.
    /// </summary>
    /// <returns>The index, or -1 when the entity is not a child of this element.</returns>
    public abstract int IndexOfEntity(EntityViewModel entity);

    /// <summary>
    /// Locks or unlocks this element.
    /// </summary>
    /// <param name="lock"><c>true</c> to lock, <c>false</c> to unlock.</param>
    /// <param name="recursive">Whether the entities under this element are locked or unlocked too, recursively. They
    /// always are for the root.</param>
    public void RequestLocking(bool @lock, bool recursive = false)
    {
        IsLocked = @lock;
        // Locking the root always applies to all its entities, as in the WPF scene editor
        if (!recursive && this is not EntityHierarchyRootViewModel)
            return;

        foreach (var entity in TransformChildren.BreadthFirst(x => x.TransformChildren))
        {
            entity.IsLocked = @lock;
        }
    }

    private EntityHierarchyRootViewModel? GetRoot()
    {
        EntityHierarchyItemViewModel item = this;
        while (item.Parent is { } parent)
        {
            item = parent;
        }
        return item as EntityHierarchyRootViewModel;
    }
}
