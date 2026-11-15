// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Quantum;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Core.Assets.Serializers;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Engine;
using Stride.Core.Assets.Presentation.Quantum;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// An item of the hierarchy of a scene or a prefab: the root of the hierarchy, an entity or a folder. Its children are its
/// folders, sorted by name, then its entities, in the order of the asset, then (for a scene) its child scenes.
/// </summary>
public abstract class EntityHierarchyItemViewModel : AssetCompositeItemViewModel<EntityHierarchyViewModel, EntityHierarchyItemViewModel>,
    IAssetPartViewModel, IIsEditableViewModel, IAddChildViewModel, IInsertChildViewModel, IEntityHierarchyItemViewModel
{
    private readonly ObservableList<EntityViewModel> subEntities = [];
    private bool isEditing;
    private bool isExpanded;

    protected EntityHierarchyItemViewModel(EntityHierarchyViewModel asset, IEnumerable<EntityDesign> childEntities)
        : base(asset)
    {
        ArgumentNullException.ThrowIfNull(childEntities);

        // Folder names ignore the case and the diacritics when sorted
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
    /// Gets the editor of the hierarchy, or <c>null</c> when the hierarchy has no editor.
    /// </summary>
    /// <remarks>
    /// The item reaches it through its asset, so that an item that left the hierarchy still reaches it.
    /// </remarks>
    public IEntityHierarchyEditorViewModel? Editor => Asset.HierarchyRoot?.Editor;

    /// <summary>
    /// Gets the scene or the prefab this item belongs to.
    /// </summary>
    public EntityHierarchyAssetBase EntityHierarchy => (EntityHierarchyAssetBase)Asset.Asset;

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

    /// <summary>
    /// Gets the root this item belongs to: the scene (main or child) or the prefab, or <c>null</c> when the item is not in
    /// a hierarchy.
    /// </summary>
    public EntityHierarchyRootViewModel? OwnerRoot
    {
        get
        {
            var element = Owner;
            while (element is not EntityHierarchyRootViewModel)
            {
                if (element.TransformParent is not { } parent)
                    return null;
                element = parent;
            }
            return (EntityHierarchyRootViewModel)element;
        }
    }

    /// <summary>
    /// Gets whether items can be inserted before or after this item.
    /// </summary>
    /// <remarks>
    /// An item that has no position in the asset cannot accept an insertion. The drop then applies to the item
    /// itself. Therefore the caller adds the items as children of this item.
    /// </remarks>
    protected virtual bool SupportsInsertion => true;

    protected IUndoRedoService? UndoRedoService => ServiceProvider.TryGet<IUndoRedoService>();

    IReadOnlyList<IAddAssetPolicy> AddAssetPolicies => Editor?.AddAssetPolicies ?? [];

    AssetViewModel IEntityHierarchyItemViewModel.Asset => Asset;

    /// <inheritdoc />
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(EntityHierarchyItemViewModel));
        Folders.CollectionChanged -= FolderCollectionChanged;
        subEntities.CollectionChanged -= SubEntityCollectionChanged;
        Editor?.SelectedContent.Remove(this);
        base.Destroy();
    }

    /// <summary>
    /// Gets the index, among the entities of this item, of the entity at the given index of the children of the owner.
    /// </summary>
    /// <param name="indexInEntity">The index of the entity in the children of the owner.</param>
    /// <param name="entityCollection">The children of the owner.</param>
    public abstract int FindIndexInParent(int indexInEntity, IEnumerable<EntityDesign> entityCollection);

    /// <summary>
    /// Adds to the asset, in this item, the entities made from the given assets: a component of a new entity for a model
    /// or a sprite, the entities of a prefab, or a child scene.
    /// </summary>
    /// <param name="assets">The dropped objects; the assets among them are used.</param>
    /// <param name="index">The index of the first new entity in the children of <see cref="Owner"/>.</param>
    /// <param name="modifiers">With <see cref="AddChildModifiers.Alt"/>, the entities of a prefab go in without an entity
    /// that holds them.</param>
    /// <param name="rootPosition">The position of the new entities, from <see cref="Owner"/>.</param>
    /// <returns>The view models of the new entities.</returns>
    public IReadOnlyCollection<EntityViewModel> AddEntitiesFromAssets(IReadOnlyCollection<object> assets, int index, AddChildModifiers modifiers, Vector3 rootPosition = new())
    {
        // Scene policy
        var scenePolicy = AddAssetPolicies.OfType<ICustomPolicy>().FirstOrDefault(p => p.Accept(typeof(SceneAsset)));
        scenePolicy?.ApplyPolicy(this, assets.OfType<SceneViewModel>(), index, rootPosition);

        var result = new List<EntityViewModel>();
        var newEntities = new AssetCompositeHierarchyData<EntityDesign, Entity>();
        var folderName = (this as EntityFolderViewModel)?.Path ?? "";
        foreach (var asset in assets.OfType<AssetViewModel>())
        {
            var component = CreateComponentFromAsset(asset);
            if (component != null)
            {
                var name = EntityNames.ComputeNewName(this, asset.Name);
                var newAssetEntity = new Entity(name)
                {
                    Components = { component }
                };
                newEntities.Parts.Add(new EntityDesign(newAssetEntity, folderName));
                newEntities.RootParts.Add(newAssetEntity);
            }
            else
            {
                foreach (var policy in AddAssetPolicies.OfType<ICreateEntitiesPolicy>().Where(p => p.Accept(asset.AssetType)))
                {
                    var instance = policy.CreateEntitiesFromAsset(this, asset);
                    if (instance == null)
                        continue;
                    var name = EntityNames.ComputeNewName(this, asset.Name);
                    if ((modifiers & AddChildModifiers.Alt) != AddChildModifiers.Alt)
                    {
                        var prefabRoot = new EntityDesign(new Entity(name), folderName);
                        instance.RootParts.ForEach(x => prefabRoot.Entity.Transform.Children.Add(x.Transform));
                        instance.Parts.ForEach(x => newEntities.Parts.Add(x));
                        newEntities.Parts.Add(prefabRoot);
                        newEntities.RootParts.Add(prefabRoot.Entity);
                    }
                    else
                    {
                        instance.RootParts.ForEach(x =>
                        {
                            var partDesign = instance.Parts[x.Id];
                            partDesign.Folder = EntityFolderViewModel.CombinePath(folderName, partDesign.Folder);
                            newEntities.RootParts.Add(x);
                        });
                        instance.Parts.ForEach(x => newEntities.Parts.Add(x));
                    }
                }
            }
        }

        // Create item ids collections for new objects before actually adding them to the asset.
        AssetCollectionItemIdHelper.GenerateMissingItemIds(newEntities);

        foreach (var entity in newEntities.RootParts)
        {
            entity.Transform.Position += rootPosition;

            Asset.AssetHierarchyPropertyGraph.AddPartToAsset(newEntities.Parts, newEntities.Parts[entity.Id], (Owner as EntityViewModel)?.AssetSideEntity, index++);

            // Make sure to mark the position node as overridden if this entity has a base.
            var positionNode = (IAssetMemberNode)Asset.Session.AssetNodeContainer.GetNode(entity.Transform)![nameof(TransformComponent.Position)];
            if (positionNode.BaseNode != null)
            {
                positionNode.OverrideContent(true);
            }
            // Add newly created entity to the result
            if (Owner.TransformChildren.FirstOrDefault(x => x.AssetSideEntity == entity) is { } viewModel)
                result.Add(viewModel);
        }

        return result;
    }

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
                subFolder = new EntityFolderViewModel(parent.Asset, folderName, []);
                parent.Folders.Add(subFolder);
                // No operation when the new folder comes from a change in the base prefab: the order of the operations
                // would be wrong when undoing
                if (UndoRedoService is { UndoRedoInProgress: false } undoRedo && !Asset.PropertyGraph!.UpdatingPropertyFromBase)
                {
                    undoRedo.PushOperation(new EntityFolderOperation(Asset, EntityFolderOperation.Action.FolderCreated, subFolder.Path, subFolder.Owner.Id));
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
    /// The entity and its children are loaded in the game when their root is.
    /// </summary>
    /// <param name="index">The index of the entity in the children of this element.</param>
    /// <param name="entityDesign">The design of the added entity.</param>
    /// <param name="siblings">The children of this element, after the change.</param>
    protected void InsertEntity(int index, EntityDesign entityDesign, IEnumerable<EntityDesign> siblings)
    {
        var entity = Asset.CreatePartViewModel(entityDesign);
        var parent = FindOrCreateFolder(entityDesign.Folder);
        parent.InsertEntityViewModel(entity, parent.FindIndexInParent(index, siblings));

        // An entity added to a loaded scene or prefab is loaded with its children. The editor adds them to its game when
        // it is notified.
        // FIXME: when we allow loading/unloading single entities, this should be implemented in OnLoadingRequested.
        if (OwnerRoot is { IsLoaded: true })
        {
            entity.SetIsLoaded(true);
            foreach (var child in entity.TransformChildren.BreadthFirst(x => x.TransformChildren))
            {
                child.SetIsLoaded(true);
            }
        }
        GetTopRoot()?.NotifyEntityAdded(Owner, entity, index);
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

        var root = GetTopRoot();
        entity.Parent!.subEntities.Remove(entity);
        // FIXME: when we allow loading/unloading single entities, this should be implemented in OnLoadingRequested.
        entity.SetIsLoaded(false);
        root?.NotifyEntityRemoved(Owner, entity, index);
        entity.Destroy();
    }

    /// <summary>
    /// Gets the top root of the hierarchy: the root of the edited scene or prefab, or <c>null</c> when this item is not in
    /// a hierarchy.
    /// </summary>
    protected EntityHierarchyRootViewModel? GetTopRoot()
    {
        var item = this;
        while (item.Parent is { } parent)
        {
            item = parent;
        }
        return item as EntityHierarchyRootViewModel;
    }

    protected EntityComponent? CreateComponentFromAsset(AssetViewModel asset)
    {
        foreach (var policy in AddAssetPolicies.OfType<ICreateComponentPolicy>())
        {
            if (policy.Accept(asset.AssetType))
            {
                return policy.CreateComponentFromAsset(this, asset);
            }
        }
        return null;
    }

    /// <summary>
    /// Gets how this item accepts the given children.
    /// </summary>
    /// <param name="children">The children to add or to insert.</param>
    /// <param name="checkSameParent">True to give a no-op when a child is already a child of this item.</param>
    /// <param name="modifiers">The modifier keys currently active.</param>
    /// <param name="index">The index at which the children go.</param>
    /// <param name="message">The feedback message that can be used in the user interface.</param>
    /// <remarks>
    /// A rejected child stops the check immediately. If no child is rejected and no child is accepted, but a child
    /// changes nothing, the result is <see cref="DropAcceptance.NoOp"/>. Therefore a selection that mixes items
    /// which are already here with items which are not stays accepted, and it moves only the second group.
    /// </remarks>
    protected virtual DropAcceptance CanAddOrInsertChildren(IReadOnlyCollection<object> children, bool checkSameParent, AddChildModifiers modifiers, int index, out string message)
    {
        if (OwnerRoot is { IsLoading: true })
        {
            message = "Loading. Please wait";
            return DropAcceptance.Rejected;
        }

        message = "Empty selection";
        var parentName = this is EntityHierarchyRootViewModel ? "root level" : (!string.IsNullOrWhiteSpace(Name) ? Name : "this location");
        string? noOpMessage = null;
        var accepted = false;

        foreach (var child in children)
        {
            if (child is AssetViewModel asset)
            {
                var policy = AddAssetPolicies.FirstOrDefault(p => p.Accept(asset.AssetType));
                if (policy != null)
                {
                    if (!policy.CanAddOrInsert(this, asset, modifiers, index, out message, parentName))
                        return DropAcceptance.Rejected;
                    accepted = true;
                    continue;
                }
                message = $"Can't add {asset.AssetType.Name}s here";
                return DropAcceptance.Rejected;
            }
            if (index > Owner.EntityCount)
            {
                message = "Index out of range";
                return DropAcceptance.Rejected;
            }
            if (child is EntityViewModel entity)
            {
                if (entity == this)
                {
                    noOpMessage = "Nothing to move";
                    continue;
                }
                if (checkSameParent && Children.Contains(entity))
                {
                    // The entity keeps its parent and its folder, but the move still changes its order. It changes
                    // nothing only if it is already at the target index. MoveChildren corrects the index for an
                    // entity that it removes from this item, therefore the entity goes to the index before it.
                    if (Owner.IndexOfEntity(entity) == index - 1)
                    {
                        noOpMessage = $"Already the last child of {parentName}";
                        continue;
                    }
                    message = $"Move to the end of {parentName}";
                    accepted = true;
                    continue;
                }
                var currentParent = Parent;
                while (currentParent != null)
                {
                    if (currentParent == entity)
                    {
                        message = "Can't drop an entity on itself or its children";
                        return DropAcceptance.Rejected;
                    }
                    currentParent = currentParent.Parent;
                }
                // Check base
                if (Editor?.GatherAllBasePartAssets(entity, true).Contains(Asset.Id) == true)
                {
                    message = "Entity depends on this asset";
                    return DropAcceptance.Rejected;
                }

                // Accepting entities
                message = (modifiers & AddChildModifiers.Alt) != AddChildModifiers.Alt
                    ? $"Add as a child to {parentName}\r\n(Hold Alt to maintain world position)"
                    : $"Add as a child to {parentName}\r\n(Release Alt to maintain local position)";
                accepted = true;
                continue;
            }
            if (child is EntityFolderViewModel folder)
            {
                // A folder that is already here matches its own name. Therefore the no-op cases come first.
                if (folder == this)
                {
                    noOpMessage = "Nothing to move";
                    continue;
                }
                if (Children.Contains(folder))
                {
                    noOpMessage = $"Already a child of {parentName}";
                    continue;
                }
                if (Folders.Any(x => string.Equals(x.Name, folder.Name, EntityFolderViewModel.FolderCase)))
                {
                    message = "A folder with the same name already exists";
                    return DropAcceptance.Rejected;
                }
                var currentParent = Parent;
                while (currentParent != null)
                {
                    if (currentParent == folder)
                    {
                        message = "Can't drop a folder on itself or its children";
                        return DropAcceptance.Rejected;
                    }
                    currentParent = currentParent.Parent;
                }
                // Check base
                if (Editor is { } editor && folder.TransformChildren.SelectMany(e => editor.GatherAllBasePartAssets(e, true)).Contains(Asset.Id))
                {
                    message = "Folder depends on this asset";
                    return DropAcceptance.Rejected;
                }

                // Accepting folders
                message = $"Move the selection to {parentName}";
                accepted = true;
                continue;
            }
            message = DragDropMessages.InvalidDropArea;
            return DropAcceptance.Rejected;
        }

        if (!accepted && noOpMessage != null)
        {
            message = noOpMessage;
            return DropAcceptance.NoOp;
        }
        return DropAcceptance.Accepted;
    }

    // FIXME: consider using the cut/paste mechanism for moving entities
    protected virtual void MoveChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, int index)
    {
        if (children.Count == 0)
            return;

        var undoRedo = UndoRedoService ?? throw new InvalidOperationException("Moving items needs the undo/redo service.");
        var moved = false;

        // Save the selection to restore it after the operation.
        var editor = Editor;
        var selection = editor?.SelectedContent.ToList() ?? [];
        // Clear the selection
        editor?.ClearSelection();

        using (var transaction = undoRedo.CreateTransaction())
        {
            // Add new entities that are generated out of assets
            AddEntitiesFromAssets(children, index, modifiers);

            // Empty subfolders of entities that are being moved, recursively
            // Note: here we only care about folders which ancestor is an entity, folders and subfolders in the children collection are dealt with later
            // Note: order is reversed to deepest first, so we can create undo/redo operations in proper logical order
            var emptySubfolders = children.OfType<EntityHierarchyItemViewModel>()
                .DepthFirst(x => x.Children).OfType<EntityViewModel>()
                .SelectMany(e => e.Folders).DepthFirst(f => f.Folders)
                .Where(f => !f.InnerSubEntities.Any())
                .Reverse().Select(f => KeyValuePair.Create(f.Owner.Id, Tuple.Create(f.Path, f.Asset))).ToList();

            // Entities that are being directly moved with their folder path
            var thisPath = (this as EntityFolderViewModel)?.Path ?? "";
            // Note: cannot use a dictionary here since sibling entities would have the same path.
            var entities = children.OfType<EntityViewModel>().Select(entity => KeyValuePair.Create(thisPath, entity)).ToList();

            // Move entities that are within folders being directly moved
            foreach (var folder in children.OfType<EntityFolderViewModel>())
            {
                var oldParentPath = (folder.Parent as EntityFolderViewModel)?.Path ?? "";
                var newParentPath = (this as EntityFolderViewModel)?.Path ?? "";
                entities.AddRange(folder.InnerSubEntities.Select(e =>
                {
                    // Remove old parent path
                    var detachedPath = e.EntityDesign.Folder.Substring(oldParentPath.Length);
                    // Prefix the new parent path
                    var newPath = EntityFolderViewModel.CombinePath(newParentPath, detachedPath);
                    return KeyValuePair.Create(newPath, e);
                }));
            }

            // Create operations to remove empty subfolders, so that undo/redo will work.
            // Note: Non-empty subfolders are handled automatically thanks to the entities they contain.
            foreach (var folder in emptySubfolders)
            {
                var operation = new EntityFolderOperation(folder.Value.Item2, EntityFolderOperation.Action.FolderDeleted, folder.Value.Item1, folder.Key);
                undoRedo.PushOperation(operation);
            }

            // Move the entities in two steps: first remove all, then insert all
            var maintainWorldPosition = (modifiers & AddChildModifiers.Alt) == AddChildModifiers.Alt;
            var hierarchies = new Dictionary<Guid, AssetCompositeHierarchyData<EntityDesign, Entity>>();
            var idRemapping = new Dictionary<Guid, Guid>();
            // remove all
            var assetsToFixup = new HashSet<EntityHierarchyViewModel>();
            foreach (var entity in entities.Select(kv => kv.Value))
            {
                var entityToMove = entity.EntityDesign;
                if (maintainWorldPosition)
                {
                    ComputeRelativePosition(entity, Owner);
                }

                // Some of the entities we're moving might already be children of this object, let's count for their removal in the insertion index.
                var entityIndex = Owner.IndexOfEntity(entity);
                if (entityIndex >= 0 && entityIndex < index)
                    --index;

                // Hierarchy must be cloned before removing the entities!
                // Note: if the source asset is different than the current asset, we need to generate new ids.
                var flags = entity.Asset == Asset ? SubHierarchyCloneFlags.None : SubHierarchyCloneFlags.GenerateNewIdsForIdentifiableObjects;
                var hierarchy = EntityHierarchyPropertyGraph.CloneSubHierarchies(entity.Asset.Session.AssetNodeContainer, entity.Asset.Asset, entity.Id.ObjectId.Yield(), flags, out Dictionary<Guid, Guid> remapping);
                idRemapping.AddRange(remapping);
                hierarchies.Add(entity.Id.ObjectId, hierarchy);

                // Remove from previous asset
                entity.Asset.AssetHierarchyPropertyGraph.RemovePartFromAsset(entityToMove);
                assetsToFixup.Add(entity.Asset);
            }
            // insert all
            var nodeContainer = Asset.Session.AssetNodeContainer;
            foreach (var kv in entities)
            {
                var folderName = kv.Key;
                var entityId = kv.Value.Id;
                var hierarchy = hierarchies[entityId.ObjectId];
                // New identifiers might have been generated
                if (!idRemapping.TryGetValue(entityId.ObjectId, out var partId))
                    partId = entityId.ObjectId;
                var movedEntity = hierarchy.Parts[partId];

                var node = nodeContainer.GetOrCreateNode(movedEntity)[nameof(EntityDesign.Folder)];
                var oldValue = node.Retrieve();
                node.Update(folderName);
                // This is a bit hackish, but the entity should be currently disconnected from any asset (since we're going to add it), so we need to manually create an action item for the folder change.
                // TODO: update the folder after insert, and subscribe to changes in EntityDesignData.Folder from EntityViewModel to propagate folder change at that level
                var actionItem = new ContentValueChangeOperation(node, ContentChangeType.ValueChange, NodeIndex.Empty, oldValue, folderName, Asset.Dirtiables);
                undoRedo.PushOperation(actionItem);
                Asset.AssetHierarchyPropertyGraph.AddPartToAsset(hierarchy.Parts, movedEntity, (Owner as EntityViewModel)?.AssetSideEntity, index++);
                moved = true;
            }

            // Fixup external references in previous and current assets
            assetsToFixup.Add(Asset);
            foreach (var asset in assetsToFixup)
            {
                // FIXME: the following lines are identical in AssetCompositeHierarchyPasteProcessor.ProcessDeserializedData(). Consider factorization.
                // Collect all referenceable objects from the target asset (where we're pasting)
                var targetPropertyGraph = Asset.Session.GraphContainer.TryGetGraph(asset.Id)!;
                var referenceableObjects = IdentifiableObjectCollector.Collect(targetPropertyGraph.Definition, targetPropertyGraph.RootNode);
                // Replace references in the hierarchy being pasted by the real objects from the target asset.
                var propertyGraph = asset.PropertyGraph!;
                var externalReferences = new HashSet<Guid>(ExternalReferenceCollector.GetExternalReferences(propertyGraph.Definition, propertyGraph.RootNode).Select(x => x.Id));
                var visitor = new ObjectReferencePathGenerator(propertyGraph.Definition)
                {
                    ShouldOutputReference = x => externalReferences.Contains(x)
                };
                visitor.Visit(propertyGraph.RootNode);
                FixupObjectReferences.FixupReferences(asset.Asset, visitor.Result, referenceableObjects, true, (memberPath, _, value) =>
                {
                    var graphPath = GraphNodePath.From(propertyGraph.RootNode, memberPath, out var i);
                    var node = graphPath.GetNode();
                    if (node is IMemberNode memberNode)
                    {
                        memberNode.Update(value);
                    }
                    else
                    {
                        ((IObjectNode)node).Update(value, i);
                    }
                });
            }

            // Note: for the following operations we need to get the full hierarchies of folders and remember their path
            // Note: order is reversed to deepest first, so we can create undo/redo operations in proper logical order
            var allFolders = children.OfType<EntityFolderViewModel>().DepthFirst(f => f.Folders).Reverse().ToDictionary(f => f.Path);
            // Remove any folder that was moved
            foreach (var folder in allFolders.Values)
            {
                if (folder.Parent == null) throw new InvalidOperationException($"{nameof(folder)}.{nameof(folder.Parent)} cannot be null");
                var operation = new EntityFolderOperation(folder.Asset, EntityFolderOperation.Action.FolderDeleted, folder.Path, folder.Owner.Id);
                undoRedo.PushOperation(operation);
                folder.Parent.Folders.Remove(folder);
            }

            // Recreate empty folder (new folders containing entities have already been created when doing InsertEntity).
            foreach (var folder in allFolders.Where(kv => kv.Value.Children.Count == 0))
            {
                FindOrCreateFolder(folder.Key);
            }

            // Recreate empty subfolders that were removed
            foreach (var folder in emptySubfolders)
            {
                // New identifiers might have been generated
                if (!idRemapping.TryGetValue(folder.Key.ObjectId, out var objectId))
                    objectId = folder.Key.ObjectId;
                var ownerId = new AbsoluteId(Asset.Id, objectId);
                var owner = Asset.HierarchyRoot?.FindItem(ownerId) as EntityHierarchyElementViewModel
                            ?? throw new InvalidOperationException("The owner of an empty subfolder cannot be found.");
                owner.FindOrCreateFolder(folder.Value.Item1);
            }

            undoRedo.SetName(transaction, moved ? "Move entities to the scene" : "Add entities to the scene");
        }

        // Fixup selection since adding/inserting may create new viewmodels
        editor?.FixupAndRestoreSelection(selection, children);
    }

    /// <summary>
    /// Gets the index at which children should be added.
    /// </summary>
    /// <remarks>This method ignores the concept of folder.</remarks>
    /// <returns>The add index if the provided <paramref name="children"/> can be added; otherwise, <c>-1</c>.</returns>
    /// <seealso cref="CanAddOrInsertChildren"/>
    /// <seealso cref="MoveChildren"/>
    protected virtual int GetAddIndex(IReadOnlyCollection<object> children)
    {
        return Owner.EntityCount;
    }

    /// <summary>
    /// Gets the index at which children should be inserted.
    /// </summary>
    /// <remarks>This method ignores the concept of folder.</remarks>
    /// <seealso cref="CanAddOrInsertChildren"/>
    /// <seealso cref="MoveChildren"/>
    protected virtual int GetInsertionIndex(InsertPosition position)
    {
        // If we are trying to insert around a folder (this), the child will be put at proper index automatically (auto-sorted if it's a folder, at index 0 if it's an entity.
        return 0;
    }

    private static Vector3 CalculateSceneOffset(EntityHierarchyElementViewModel element)
    {
        // Find parent scene
        SceneRootViewModel? scene;
        EntityHierarchyElementViewModel? current = element;
        do
        {
            scene = current as SceneRootViewModel;
            current = current.TransformParent;
        } while (scene == null && current != null);

        // Accumulate offsets from all scenes in hierarchy
        var offset = Vector3.Zero;
        var hash = new HashSet<AbsoluteId>();
        while (scene != null)
        {
            if (!hash.Add(scene.Id))
            {
                GlobalLogger.GetLogger("Asset").Error($"Cyclic reference detected for scene {scene.Name} (Id={scene.Id}).");
                break;
            }

            offset += scene.Offset;
            scene = scene.ParentScene;
        }

        return offset;
    }

    private void ComputeRelativePosition(EntityViewModel entity, EntityHierarchyElementViewModel relativeTo)
    {
        // Get entity world space transformation
        var transform = entity.AssetSideEntity.Transform;
        transform.UpdateWorldMatrix();
        transform.GetWorldTransformation(out var translation, out var rotation, out var scale);

        // Apply possible scene changing offset
        translation += CalculateSceneOffset(entity);
        translation -= CalculateSceneOffset(relativeTo);

        // Convert world space to parent local space
        if (relativeTo is EntityViewModel relativeToEntity)
        {
            relativeToEntity.AssetSideEntity.Transform.UpdateWorldMatrix();
            relativeToEntity.AssetSideEntity.Transform.WorldToLocal(ref translation, ref rotation, ref scale);
        }

        // Update transform
        var transformNode = Asset.Session.AssetNodeContainer.GetOrCreateNode(transform);
        transformNode[nameof(TransformComponent.Position)].Update(translation);
        transformNode[nameof(TransformComponent.Rotation)].Update(rotation);
        transformNode[nameof(TransformComponent.Scale)].Update(scale);
    }

    private void InsertEntityViewModel(EntityViewModel entity, int index, bool expand = true)
    {
        if (entity.IsDestroyed)
            throw new InvalidOperationException("The entity to insert has already been destroyed");

        // Add the view model first, so actual entities can be fetched
        if (index < 0)
            subEntities.Add(entity);
        else
            subEntities.Insert(index, entity);

        if (expand)
        {
            IsExpanded = true;
        }
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

    private DropAcceptance CheckAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        var addIndex = GetAddIndex(children);
        if (addIndex < -1)
        {
            message = DragDropMessages.InvalidDropArea;
            return DropAcceptance.Rejected;
        }
        return CanAddOrInsertChildren(children, true, modifiers, addIndex, out message);
    }

    private DropAcceptance CheckInsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers, out string message)
    {
        if (!SupportsInsertion)
        {
            // The caller then adds the items as children of this item instead, and it replaces this message.
            message = DragDropMessages.InvalidDropArea;
            return DropAcceptance.Rejected;
        }

        var parentName = Parent is SceneRootViewModel ? "root level" : (!string.IsNullOrWhiteSpace(Parent?.Name) ? Parent.Name : "this location");

        message = "This entity has no parent.";
        if (Parent == null)
            return DropAcceptance.Rejected;

        // TODO: try to move all evaluation into CheckAddOrInsertChildren so everything is at the same place
        var acceptance = Parent.CanAddOrInsertChildren(children, false, modifiers, GetInsertionIndex(position), out message);
        if (acceptance != DropAcceptance.Accepted)
            return acceptance;

        if (children.Any(x => x == this))
        {
            message = "Nothing to move";
            return DropAcceptance.NoOp;
        }
        message = children.All(x => x is EntityFolderViewModel)
            ? $"Move the selection to {parentName}"
            : $"Insert {(position == InsertPosition.Before ? "before" : "after")} {Name}";

        if (children.OfType<EntityHierarchyItemViewModel>().Any())
        {
            message = (modifiers & AddChildModifiers.Alt) != AddChildModifiers.Alt
                ? $"{message}\r\n(Hold Alt to maintain world position)"
                : $"{message}\r\n(Release Alt to maintain local position)";
        }
        return DropAcceptance.Accepted;
    }

    /// <inheritdoc/>
    bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        // A no-op gives false here, because the caller of this method then does the operation.
        return CheckAddChildren(children, modifiers, out message) == DropAcceptance.Accepted;
    }

    /// <inheritdoc/>
    DropAcceptance IAddChildViewModel.GetAddChildrenAcceptance(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        return CheckAddChildren(children, modifiers, out message);
    }

    /// <inheritdoc/>
    void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
    {
        MoveChildren(children, modifiers, GetAddIndex(children));
    }

    /// <inheritdoc/>
    bool IInsertChildViewModel.CanInsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers, out string message)
    {
        // A no-op gives false here, because the caller of this method then does the operation.
        return CheckInsertChildren(children, position, modifiers, out message) == DropAcceptance.Accepted;
    }

    /// <inheritdoc/>
    DropAcceptance IInsertChildViewModel.GetInsertChildrenAcceptance(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers, out string message)
    {
        return CheckInsertChildren(children, position, modifiers, out message);
    }

    /// <inheritdoc/>
    void IInsertChildViewModel.InsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers)
    {
        if (Parent == null) throw new InvalidOperationException($"{nameof(Parent)} can't be null");
        Parent.MoveChildren(children, modifiers, GetInsertionIndex(position));
    }
}
