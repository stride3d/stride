// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Assets.Entities;
using Stride.Core;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// An item of the hierarchy of a scene or a prefab: the root of the hierarchy, an entity or a folder. Its children are its
/// folders, sorted by name, then its entities, in the order of the asset.
/// </summary>
public abstract class EntityHierarchyItemViewModel : AssetCompositeItemViewModel<EntityHierarchyViewModel, EntityHierarchyItemViewModel>, IAssetPartViewModel, IIsEditableViewModel
{
    private readonly ObservableList<EntityViewModel> subEntities = [];
    private bool isEditing;
    private bool isExpanded;
    private bool isVisible = true;

    protected EntityHierarchyItemViewModel(EntityHierarchyViewModel asset, IEnumerable<EntityDesign> childEntities)
        : base(asset)
    {
        // Folder names ignore the case and the diacritics when sorted, as in the WPF scene editor
        Folders = new AutoUpdatingSortedObservableCollection<EntityFolderViewModel>((x, y) => string.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase));

        // Empty folders and null folders are grouped together
        foreach (var folderGroup in childEntities.GroupBy(x => !string.IsNullOrWhiteSpace(x.Folder) ? x.Folder : null).OrderBy(x => x.Key))
        {
            if (!EntityFolderViewModel.GenerateFolder(this, folderGroup.Key, folderGroup))
            {
                foreach (var child in folderGroup)
                {
                    subEntities.Add(asset.CreatePartViewModel(child));
                }
            }
        }

        AddItems(Folders);
        AddItems(subEntities);

        // The handlers come after the existing children, which are already added
        Folders.CollectionChanged += FolderCollectionChanged;
        subEntities.CollectionChanged += SubEntityCollectionChanged;
    }

    /// <inheritdoc />
    public abstract AbsoluteId Id { get; }

    /// <summary>
    /// Gets the folders of this item, sorted by name.
    /// </summary>
    public AutoUpdatingSortedObservableCollection<EntityFolderViewModel> Folders { get; }

    /// <summary>
    /// An enumeration of the items represented by this item.
    /// </summary>
    /// <remarks>
    /// In case of a <see cref="EntityFolderViewModel"/> it is equivalent to <see cref="TransformChildren"/>.
    /// In case of an <see cref="EntityViewModel"/> it is equivalent to <c>this</c>.
    /// </remarks>
    // FIXME: find a better name
    public abstract IEnumerable<EntityViewModel> InnerSubEntities { get; }

    /// <summary>
    /// Gets the element that owns this item: the item itself for an entity or the root of the hierarchy, and the entity
    /// or the root that contains a folder.
    /// </summary>
    public abstract EntityHierarchyElementViewModel Owner { get; }

    /// <summary>
    /// Gets the entities whose transform is a child of the transform of <see cref="Owner"/> and that are under this item.
    /// </summary>
    public IEnumerable<EntityViewModel> TransformChildren => Children.SelectMany(x => x.InnerSubEntities);

    /// <summary>
    /// Gets the element whose transform is the parent of the transform of this item, or <c>null</c> for the root.
    /// </summary>
    public EntityHierarchyElementViewModel? TransformParent => Parent?.Owner;

    /// <inheritdoc />
    public virtual bool IsEditable => true;

    /// <summary>
    /// Gets or sets whether the name of this item is being edited in place (F2).
    /// </summary>
    public virtual bool IsEditing { get => isEditing; set => SetValue(ref isEditing, value); }

    /// <summary>
    /// Gets or sets whether this item shows its children in the tree.
    /// </summary>
    public bool IsExpanded { get => isExpanded; set => SetValue(ref isExpanded, value); }

    /// <summary>
    /// Gets or sets whether this item passes the filter of the hierarchy (it or one of its children matches).
    /// </summary>
    public bool IsVisible { get => isVisible; set => SetValue(ref isVisible, value); }

    protected EntityHierarchyAssetBase EntityHierarchy => (EntityHierarchyAssetBase)Asset.Asset;

    /// <summary>
    /// Gets the scene or the prefab this item belongs to.
    /// </summary>
    protected EntityHierarchyViewModel HierarchyAsset => (EntityHierarchyViewModel)Asset;

    protected IUndoRedoService? UndoRedoService => ServiceProvider.TryGet<IUndoRedoService>();

    /// <inheritdoc />
    public override void Destroy()
    {
        Folders.CollectionChanged -= FolderCollectionChanged;
        subEntities.CollectionChanged -= SubEntityCollectionChanged;
        // The children go with their parent
        foreach (var child in Children.ToList())
        {
            child.Destroy();
        }
        base.Destroy();
    }

    /// <summary>
    /// Gets the index, among the entities of this item, of the entity at the given index of the children of the owner.
    /// </summary>
    /// <param name="indexInEntity">The index of the entity in the children of the owner.</param>
    /// <param name="entityCollection">The children of the owner.</param>
    public abstract int FindIndexInParent(int indexInEntity, IEnumerable<EntityDesign> entityCollection);

    /// <summary>
    /// Finds the folder of the given path under this item, or creates it with the missing folders above it.
    /// </summary>
    /// <param name="folderPath">The path of the folder, relative to this item.</param>
    /// <param name="expand">Whether to expand this item and the folders of the path.</param>
    /// <returns>The folder, or this item when the path is empty.</returns>
    public EntityHierarchyItemViewModel FindOrCreateFolder(string? folderPath, bool expand = true)
    {
        if (string.IsNullOrEmpty(folderPath))
            return this;

        var folders = folderPath.Split(EntityFolderViewModel.FolderSeparator, StringSplitOptions.RemoveEmptyEntries);
        var folderCollection = Folders;

        if (expand)
            IsExpanded = true;

        EntityFolderViewModel? subFolder = null;
        var parent = this;
        foreach (var folderName in folders)
        {
            subFolder = folderCollection.FirstOrDefault(x => string.Equals(x.Name, folderName, EntityFolderViewModel.FolderCase));
            if (subFolder is null)
            {
                subFolder = new EntityFolderViewModel(parent.HierarchyAsset, folderName, []);
                parent.Folders.Add(subFolder);
                // No operation when the new folder comes from a change in the base prefab: the order of the operations
                // would be wrong when undoing
                if (UndoRedoService is { UndoRedoInProgress: false } undoRedo && !Asset.PropertyGraph!.UpdatingPropertyFromBase)
                {
                    undoRedo.PushOperation(new EntityFolderOperation(HierarchyAsset, EntityFolderOperation.Action.FolderCreated, subFolder.Path, subFolder.Owner.Id));
                }
            }
            if (expand)
                subFolder.IsExpanded = true;

            folderCollection = subFolder.Folders;
            parent = subFolder;
        }

        return subFolder ?? this;
    }

    /// <summary>
    /// Inserts the view model of an entity the asset just got under its folder, and notifies the root of the hierarchy.
    /// </summary>
    /// <param name="index">The index of the entity in the children of this element.</param>
    /// <param name="entityDesign">The design of the added entity.</param>
    /// <param name="siblings">The children of this element, after the change.</param>
    protected void InsertEntity(int index, EntityDesign entityDesign, IEnumerable<EntityDesign> siblings)
    {
        var entity = HierarchyAsset.CreatePartViewModel(entityDesign);
        var parent = FindOrCreateFolder(entityDesign.Folder);
        parent.InsertEntityViewModel(entity, parent.FindIndexInParent(index, siblings));
        GetRoot()?.NotifyEntityAdded(Owner, entity, index);
    }

    /// <summary>
    /// Removes the view model of an entity the asset just lost, wherever its folder is, notifies the root of the hierarchy,
    /// then destroys the view model with its children.
    /// </summary>
    /// <param name="assetSideEntity">The removed entity.</param>
    /// <param name="index">The index the entity had in the children of this element.</param>
    protected void RemoveEntity(Entity assetSideEntity, int index)
    {
        var entity = TransformChildren.FirstOrDefault(x => x.AssetSideEntity == assetSideEntity)
                     ?? throw new InvalidOperationException($"The entity {assetSideEntity.Id} has no view model in this item.");

        var root = GetRoot();
        entity.Parent!.subEntities.Remove(entity);
        root?.NotifyEntityRemoved(Owner, entity, index);
        entity.Destroy();
    }

    private void InsertEntityViewModel(EntityViewModel entity, int index)
    {
        if (entity.IsDestroyed)
            throw new InvalidOperationException("The entity to insert has already been destroyed");

        if (index < 0)
            subEntities.Add(entity);
        else
            subEntities.Insert(index, entity);
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

    private void FolderCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                var addIndex = e.NewStartingIndex;
                foreach (EntityFolderViewModel newItem in e.NewItems!)
                {
                    InsertItem(e.NewStartingIndex < 0 ? Folders.Count - e.NewItems.Count : addIndex++, newItem);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (EntityFolderViewModel oldItem in e.OldItems!)
                {
                    if (e.OldStartingIndex < 0)
                        RemoveItem(oldItem);
                    else
                        RemoveItemAt(e.OldStartingIndex);
                }
                break;
            case NotifyCollectionChangedAction.Move:
                // A folder renamed: it moves to its new place in the sorted folders
                if (e.OldItems?.Count != 1 || e.NewItems?.Count != 1 || e.NewItems[0] != e.OldItems[0] || e.OldStartingIndex < 0 || e.NewStartingIndex < 0)
                    throw new InvalidOperationException("A move of the folders must move one folder to a known index.");
                var item = (EntityFolderViewModel)e.NewItems[0]!;
                RemoveItemAt(e.OldStartingIndex);
                InsertItem(e.NewStartingIndex, item);
                break;
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Reset:
                throw new NotSupportedException();
            default:
                throw new ArgumentOutOfRangeException(nameof(e));
        }
    }

    private void SubEntityCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // The entities come after the folders
        var offset = Folders.Count;
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                var addIndex = offset + e.NewStartingIndex;
                foreach (EntityHierarchyItemViewModel newItem in e.NewItems!)
                {
                    if (e.NewStartingIndex < 0)
                        AddItem(newItem);
                    else
                        InsertItem(addIndex++, newItem);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                var removeIndex = offset + e.OldStartingIndex;
                foreach (EntityHierarchyItemViewModel oldItem in e.OldItems!)
                {
                    if (e.OldStartingIndex < 0)
                        RemoveItem(oldItem);
                    else
                        RemoveItemAt(removeIndex);
                }
                break;
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Reset:
                throw new NotSupportedException("Replace, Move and Reset are not supported on this collection.");
            default:
                throw new ArgumentOutOfRangeException(nameof(e));
        }
    }
}
