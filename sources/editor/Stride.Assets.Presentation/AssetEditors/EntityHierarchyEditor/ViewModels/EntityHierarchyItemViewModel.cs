// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Quantum;
using Xenko.Core.Assets.Editor.View.Behaviors;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Assets.Quantum.Visitors;
using Xenko.Core.Assets.Serializers;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.Mathematics;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Quantum;
using Xenko.Assets.Entities;
using Xenko.Assets.Models;
using Xenko.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories;
using Xenko.Assets.Presentation.AssetEditors.SceneEditor.ViewModels;
using Xenko.Assets.Presentation.Quantum;
using Xenko.Assets.Presentation.ViewModel;
using Xenko.Engine;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    public abstract class EntityHierarchyItemViewModel : AssetCompositeItemViewModel<EntityHierarchyViewModel, EntityHierarchyItemViewModel>, IAddChildViewModel, IInsertChildViewModel
    {
        private readonly List<IAddAssetPolicy> addAssetPolicies;
        private readonly ObservableList<EntityViewModel> subEntities = new ObservableList<EntityViewModel>();
        private bool isEditing;
        private bool isExpanded;

        protected EntityHierarchyItemViewModel([NotNull] EntityHierarchyEditorViewModel editor, [NotNull] EntityHierarchyViewModel asset, [NotNull] IEnumerable<EntityDesign> childEntities)
            : base(editor, asset)
        {
            if (childEntities == null) throw new ArgumentNullException(nameof(childEntities));

            // note: we want to ignore case and diacritics (e.g. accentuation) when sorting the folders
            Folders = new AutoUpdatingSortedObservableCollection<EntityFolderViewModel>((x, y) => string.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase));

            // Note: we make sure that empty folders and null folders are grouped together
            // TODO: implement a more robust folder grouping method that trims, etc.
            // TODO: also ensure that empty folder are serialized as null (or vice-versa) to avoid this kind of issue
            foreach (var folderGroup in childEntities.GroupBy(x => !string.IsNullOrWhiteSpace(x.Folder) ? x.Folder : null).OrderBy(x => x.Key))
            {
                if (!EntityFolderViewModel.GenerateFolder(this, folderGroup.Key, folderGroup))
                {
                    foreach (var child in folderGroup)
                    {
                        var viewModel = (EntityViewModel)Editor.CreatePartViewModel(Asset, child);
                        subEntities.Add(viewModel);
                    }
                }
            }

            AddItems(Folders);
            AddItems(subEntities);

            // We register the collection changed handler after having added already-existing children
            Folders.CollectionChanged += FolderCollectionChanged;
            subEntities.CollectionChanged += SubEntityCollectionChanged;

            // Add policies for adding/inserting assets
            // TODO: make it work with plugins (discovery, registration, override...)
            addAssetPolicies = new List<IAddAssetPolicy>
            {
                new AddModelAssetPolicy<ModelAsset>(),
                new AddModelAssetPolicy<PrefabModelAsset>(),
                new AddModelAssetPolicy<ProceduralModelAsset>(),
                new AddPrefabAssetPolicy(),
                new AddSceneAssetPolicy(),
                new AddScriptSourceFileAssetPolicy(),
                new AddSpriteSheetAssetPolicy(),
                new AddSpriteStudioModelAssetPolicy(),
                new AddTextureAssetPolicy(),
                new AddVideoAssetPolicy(),
                new AddUIPageAssetPolicy(),
            };
        }

        [NotNull]
        public new EntityHierarchyEditorViewModel Editor => (EntityHierarchyEditorViewModel)base.Editor;

        [NotNull]
        public EntityHierarchyAssetBase EntityHierarchy => (EntityHierarchyAssetBase)Asset.Asset;

        public AutoUpdatingSortedObservableCollection<EntityFolderViewModel> Folders { get; }

        /// <summary>
        /// An enumeration of the items represented by this item.
        /// </summary>
        /// <remarks>
        /// In case of a <see cref="EntityFolderViewModel"/> it is equivalent to <see cref="TransformChildren"/>.
        /// In case of an <see cref="EntityViewModel"/> it is equivalent to <c>this</c>.
        /// </remarks>
        // FIXME: find a better name
        [ItemNotNull, NotNull]
        public abstract IEnumerable<EntityViewModel> InnerSubEntities { get; }

        public virtual bool IsEditable => true;

        public virtual bool IsEditing { get => isEditing; set => SetValue(ref isEditing, value); }

        public bool IsExpanded { get => isExpanded; set => SetValue(ref isExpanded, value); }

        /// <summary>
        /// The owner of this item.
        /// </summary>
        /// <remarks>
        /// In case of a <see cref="EntityFolderViewModel"/> it looks up for an ancestor <see cref="EntityViewModel"/> or <see cref="EntityHierarchyRootViewModel"/>.
        /// In case of an <see cref="EntityViewModel"/> it is equivalent to <c>this</c>.
        /// </remarks>
        // FIXME: find a better name
        [NotNull]
        public abstract EntityHierarchyElementViewModel Owner { get; }

        [ItemNotNull, NotNull]
        public IEnumerable<EntityViewModel> TransformChildren => Children.SelectMany(x => x.InnerSubEntities);

        [CanBeNull]
        public EntityHierarchyElementViewModel TransformParent => Parent?.Owner;

        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(EntityHierarchyItemViewModel));
            Folders.CollectionChanged -= FolderCollectionChanged;
            subEntities.CollectionChanged -= SubEntityCollectionChanged;
            Editor.SelectedContent.Remove(this);
            base.Destroy();
        }

        public abstract int FindIndexInParent(int indexInEntity, IEnumerable<EntityDesign> entityCollection);

        [NotNull]
        internal IReadOnlyCollection<EntityViewModel> AddEntitiesFromAssets([NotNull] IReadOnlyCollection<object> assets, int index, AddChildModifiers modifiers, Vector3 rootPosition = new Vector3())
        {
            // Scene policy
            var scenePolicy = addAssetPolicies.OfType<ICustomPolicy>().FirstOrDefault(p => p.Accept(typeof(SceneAsset)));
            scenePolicy?.ApplyPolicy(this, assets.OfType<SceneViewModel>(), index, rootPosition);

            var result = new List<EntityViewModel>();
            var newEntities = new AssetCompositeHierarchyData<EntityDesign, Entity>();
            var folderName = (this as EntityFolderViewModel)?.Path ?? "";
            foreach (var asset in assets.OfType<AssetViewModel>())
            {
                var component = CreateComponentFromAsset(asset);
                if (component != null)
                {
                    var name = EntityFactory.ComputeNewName(this, asset.Name);
                    var newAssetEntity = new Entity(name)
                    {
                        Components = { component }
                    };
                    newEntities.Parts.Add(new EntityDesign(newAssetEntity, folderName));
                    newEntities.RootParts.Add(newAssetEntity);
                }
                else
                {
                    foreach (var policy in addAssetPolicies.OfType<ICreateEntitiesPolicy>().Where(p => p.Accept(asset.AssetType)))
                    {
                        var instance = policy.CreateEntitiesFromAsset(this, asset);
                        if (instance == null)
                            continue;
                        var name = EntityFactory.ComputeNewName(this, asset.Name);
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
                var positionNode = (IAssetMemberNode)Editor.NodeContainer.GetNode(entity.Transform)[nameof(TransformComponent.Position)];
                if (positionNode.BaseNode != null)
                {
                    positionNode.OverrideContent(true);
                }
                // Add newly created entity to the result
                if (Editor.FindPartViewModel(new AbsoluteId(Asset.Id, entity.Id)) is EntityViewModel viewmodel)
                    result.Add(viewmodel);
            }

            return result;
        }

        internal void InsertEntityViewModel([NotNull] EntityViewModel entity, int index, bool expand = true)
        {
            if (entity.IsDestroyed)
                throw new InvalidOperationException("The entity to insert has already been destroyed");

            // Add the view model first, so actual entities can be fetched
            if (index < 0)
            {
                subEntities.Add(entity);
            }
            else
            {
                subEntities.Insert(index, entity);
            }

            if (expand)
            {
                IsExpanded = true;
            }
        }

        internal static void RemoveEntityViewModel([NotNull] EntityViewModel entity)
        {
            if (entity.Parent == null) throw new InvalidOperationException($"{nameof(entity)}.{nameof(entity.Parent)} cannot be null");
            // Remove the view model - from its parent in case there is a hierarchy of folders
            entity.Parent?.subEntities.Remove(entity);
            entity.Destroy();
        }

        [NotNull]
        public EntityHierarchyItemViewModel FindOrCreateFolder(string folderPath, bool expand = true)
        {
            // TODO: this could be factorized with EntityFolderViewModel.GenerateFolder
            if (string.IsNullOrEmpty(folderPath))
                return this;

            var folders = folderPath.Split(new[] { EntityFolderViewModel.FolderSeparator }, StringSplitOptions.RemoveEmptyEntries);
            var folderCollection = Folders;

            if (expand)
                IsExpanded = true;

            EntityFolderViewModel subFolder = null;
            var parent = this;
            foreach (var folderName in folders.Take(folders.Length))
            {
                subFolder = folderCollection.FirstOrDefault(x => string.Equals(x.Name, folderName, EntityFolderViewModel.FolderCase));
                if (subFolder == null)
                {
                    subFolder = new EntityFolderViewModel(parent.Editor, parent.Asset, folderName, Enumerable.Empty<EntityDesign>());
                    parent.Folders.Add(subFolder);
                    // Note: we don't push an operation when the new folder comes from a change in the base prefab, since the order of operation would be incorrect when undoing.
                    if (!Editor.UndoRedoService.UndoRedoInProgress && !Asset.PropertyGraph.UpdatingPropertyFromBase)
                    {
                        var operation = new EntityFolderOperation(Asset, EntityFolderOperation.Action.FolderCreated, subFolder.Path, subFolder.Owner.Id);
                        Editor.UndoRedoService.PushOperation(operation);
                    }
                }
                if (expand)
                    subFolder.IsExpanded = true;

                folderCollection = subFolder.Folders;
                parent = subFolder;
            }

            return subFolder ?? this;
        }

        [CanBeNull]
        protected EntityComponent CreateComponentFromAsset([NotNull] AssetViewModel asset)
        {
            foreach (var policy in addAssetPolicies.OfType<ICreateComponentPolicy>())
            {
                if (policy.Accept(asset.AssetType))
                {
                    return policy.CreateComponentFromAsset(this, asset);
                }
            }
            return null;
        }

        private void FolderCollectionChanged(object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var addIndex = e.NewStartingIndex;
                    foreach (EntityFolderViewModel newItem in e.NewItems)
                    {
                        InsertItem(e.NewStartingIndex < 0 ? Folders.Count - e.NewItems.Count : addIndex++, newItem);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (EntityFolderViewModel oldItem in e.OldItems)
                    {
                        if (e.OldStartingIndex < 0)
                            RemoveItem(oldItem);
                        else
                            RemoveItemAt(e.OldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    // This is a bit experimental, let's strongly assert on the arguments
                    if (e.OldItems.Count != 1) throw new InvalidOperationException("OldItems.Count must be equal to one when using NotifyCollectionChangedAction.Move");
                    if (e.NewItems.Count != 1) throw new InvalidOperationException("NewItems.Count must be equal to one when using NotifyCollectionChangedAction.Move");
                    if (e.NewItems[0] != e.OldItems[0]) throw new InvalidOperationException("The old item must be equal to the new item when using NotifyCollectionChangedAction.Move");
                    if (e.OldStartingIndex < 0) throw new InvalidOperationException("OldStartingIndex must be greater or equal to zero when using NotifyCollectionChangedAction.Move");
                    if (e.NewStartingIndex < 0) throw new InvalidOperationException("NewStartingIndex must be greater or equal to zero when using NotifyCollectionChangedAction.Move");
                    var item = (EntityFolderViewModel)e.NewItems[0];
                    RemoveItemAt(e.OldStartingIndex);
                    InsertItem(e.NewStartingIndex, item);
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SubEntityCollectionChanged(object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            var offset = Folders.Count;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var addIndex = offset + e.NewStartingIndex;
                    foreach (EntityHierarchyItemViewModel newItem in e.NewItems)
                    {
                        if (e.NewStartingIndex < 0)
                            AddItem(newItem);
                        else
                            InsertItem(addIndex++, newItem);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    var delIndex = offset + e.OldStartingIndex;
                    foreach (EntityHierarchyItemViewModel oldItem in e.OldItems)
                    {
                        if (e.OldStartingIndex < 0)
                            RemoveItem(oldItem);
                        else
                            RemoveItemAt(delIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException("Replace, Move and Reset are not supported on this collection.");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual bool CanAddOrInsertChildren([NotNull] IReadOnlyCollection<object> children, bool checkSameParent, AddChildModifiers modifiers, int index, [NotNull] out string message)
        {
            var root = Owner as EntityHierarchyRootViewModel ?? EntityHierarchyEditorViewModel.GetRoot((EntityViewModel)Owner);
            if (root.IsLoading)
            {
                message = "Loading. Please wait";
                return false;
            }

            message = "Empty selection";
            var parentName = this is EntityHierarchyRootViewModel ? "root level" : (!string.IsNullOrWhiteSpace(Name) ? Name : "this location");

            foreach (var child in children)
            {
                if (child is AssetViewModel asset)
                {
                    var policy = addAssetPolicies.FirstOrDefault(p => p.Accept(asset.AssetType));
                    if (policy != null)
                    {
                        if (!policy.CanAddOrInsert(this, asset, modifiers, index, out message, parentName))
                            return false;
                        continue;
                    }
                    message = $"Can't add {asset.AssetType.Name}s here";
                    return false;
                }
                if (index > Owner.EntityCount)
                {
                    message = "Index out of range";
                    return false;
                }
                if (child is EntityViewModel entity)
                {
                    if (entity == this)
                    {
                        message = "Can't drop an entity on itself or its children";
                        return false;
                    }
                    if (checkSameParent && Children.Contains(entity))
                    {
                        message = "Entity already has this parent";
                        return false;
                    }
                    var currentParent = Parent;
                    while (currentParent != null)
                    {
                        if (currentParent == entity)
                        {
                            message = "Can't drop an entity on itself or its children";
                            return false;
                        }
                        currentParent = currentParent.Parent;
                    }
                    // Check base
                    if (Editor.GatherAllBasePartAssets(entity, true).Contains(Asset.Id))
                    {
                        message = "Entity depends on this asset";
                        return false;
                    }

                    // Accepting entities
                    message = (modifiers & AddChildModifiers.Alt) != AddChildModifiers.Alt
                        ? $"Add as a child to {parentName}\r\n(Hold Alt to maintain world position)"
                        : $"Add as a child to {parentName}\r\n(Release Alt to maintain local position)";
                    continue;
                }
                if (child is EntityFolderViewModel folder)
                {
                    if (Folders.Any(x => string.Equals(x.Name, folder.Name, EntityFolderViewModel.FolderCase)))
                    {
                        message = "A folder with the same name already exists";
                        return false;
                    }
                    if (folder == this)
                    {
                        message = "Can't drop a folder on itself or its children";
                        return false;
                    }
                    if (Children.Contains(folder))
                    {
                        message = "Folder already has this parent";
                        return false;
                    }
                    var currentParent = Parent;
                    while (currentParent != null)
                    {
                        if (currentParent == folder)
                        {
                            message = "Can't drop a folder on itself or its children";
                            return false;
                        }
                        currentParent = currentParent.Parent;
                    }
                    // Check base
                    if (folder.TransformChildren.SelectMany(e => Editor.GatherAllBasePartAssets(e, true)).Contains(Asset.Id))
                    {
                        message = "Folder depends on this asset";
                        return false;
                    }

                    // Accepting folders
                    message = $"Move the selection to {parentName}";
                    continue;
                }
                message = DragDropBehavior.InvalidDropAreaMessage;
                return false;
            }
            return true;
        }

        private static Vector3 CalculateSceneOffset([NotNull] EntityHierarchyElementViewModel element)
        {
            // Find parent scene
            SceneRootViewModel scene;
            do
            {
                scene = element as SceneRootViewModel;
                element = element.TransformParent;
            } while (scene == null && element != null);

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

        private void ComputeRelativePosition([NotNull] EntityViewModel entity, [NotNull] EntityHierarchyElementViewModel relativeTo)
        {
            // Get entity world space transformation
            Vector3 translation; Quaternion rotation; Vector3 scale;
            var transform = entity.AssetSideEntity.Transform;
            transform.UpdateWorldMatrix();
            transform.GetWorldTransformation(out translation, out rotation, out scale);

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
            var transformNode = Editor.NodeContainer.GetOrCreateNode(transform);
            transformNode[nameof(TransformComponent.Position)].Update(translation);
            transformNode[nameof(TransformComponent.Rotation)].Update(rotation);
            transformNode[nameof(TransformComponent.Scale)].Update(scale);
        }

        // FIXME: consider using the cut/paste mechanism for moving entities
        protected virtual void MoveChildren([NotNull] IReadOnlyCollection<object> children, AddChildModifiers modifiers, int index)
        {
            if (children.Count == 0)
                return;

            var moved = false;

            // Save the selection to restore it after the operation.
            var selection = Editor.SelectedContent.ToList();
            // Clear the selection
            Editor.ClearSelection();

            using (var transaction = Editor.UndoRedoService.CreateTransaction())
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
                    Editor.UndoRedoService.PushOperation(operation);
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
                foreach (var kv in entities)
                {
                    var folderName = kv.Key;
                    var entityId = kv.Value.Id;
                    var hierarchy = hierarchies[entityId.ObjectId];
                    // New identifiers might have been generated
                    if (!idRemapping.TryGetValue(entityId.ObjectId, out Guid partId))
                        partId = entityId.ObjectId;
                    var movedEntity = hierarchy.Parts[partId];

                    var node = Editor.NodeContainer.GetOrCreateNode(movedEntity)[nameof(EntityDesign.Folder)];
                    var oldValue = node.Retrieve();
                    node.Update(folderName);
                    // This is a bit hackish, but the entity should be currently disconnected from any asset (since we're going to add it), so we need to manually create an action item for the folder change.
                    // TODO: update the folder after insert, and subscribe to changes in EntityDesignData.Folder from EntityViewModel to propagate folder change at that level
                    var actionItem = new ContentValueChangeOperation(node, ContentChangeType.ValueChange, NodeIndex.Empty, oldValue, folderName, Asset.Dirtiables);
                    Asset.ServiceProvider.Get<IUndoRedoService>().PushOperation(actionItem);
                    Asset.AssetHierarchyPropertyGraph.AddPartToAsset(hierarchy.Parts, movedEntity, (Owner as EntityViewModel)?.AssetSideEntity, index++);
                    moved = true;
                }

                // Fixup external references in previous and current assets
                assetsToFixup.Add(Asset);
                foreach (var asset in assetsToFixup)
                {
                    // FIXME: the following lines are identical in AssetCompositeHierarchyPasteProcessor.ProcessDeserializedData(). Consider factorization.
                    // Collect all referenceable objects from the target asset (where we're pasting)
                    var targetPropertyGraph = Editor.Session.GraphContainer.TryGetGraph(asset.Id);
                    var referenceableObjects = IdentifiableObjectCollector.Collect(targetPropertyGraph.Definition, targetPropertyGraph.RootNode);
                    // Replace references in the hierarchy being pasted by the real objects from the target asset.
                    var externalReferences = new HashSet<Guid>(ExternalReferenceCollector.GetExternalReferences(asset.PropertyGraph.Definition, asset.PropertyGraph.RootNode).Select(x => x.Id));
                    var visitor = new ObjectReferencePathGenerator(asset.PropertyGraph.Definition)
                    {
                        ShouldOutputReference = x => externalReferences.Contains(x)
                    };
                    visitor.Visit(asset.PropertyGraph.RootNode);
                    FixupObjectReferences.FixupReferences(asset.Asset, visitor.Result, referenceableObjects, true, (memberPath, _, value) =>
                    {
                        var graphPath = GraphNodePath.From(asset.PropertyGraph.RootNode, memberPath, out NodeIndex i);
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
                    Editor.UndoRedoService.PushOperation(operation);
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
                    if (!idRemapping.TryGetValue(folder.Key.ObjectId, out Guid objectId))
                        objectId = folder.Key.ObjectId;
                    var ownerId = new AbsoluteId(Asset.Id, objectId);
                    var owner = (EntityHierarchyElementViewModel)Editor.FindPartViewModel(ownerId);
                    if (owner == null) throw new InvalidOperationException($"{nameof(owner)} cannot be null");
                    owner.FindOrCreateFolder(folder.Value.Item1);
                }

                Editor.UndoRedoService.SetName(transaction, moved ? "Move entities to the scene" : "Add entities to the scene");
            }

            // Fixup selection since adding/inserting may create new viewmodels
            Editor.FixupAndRestoreSelection(selection, children);
        }

        /// <summary>
        /// Gets the index at which children should be added.
        /// </summary>
        /// <param name="children"></param>
        /// <remarks>This method ignores the concept of folder.</remarks>
        /// <returns>The add index if the provided <paramref name="children"/> can be added; otherwise, <c>-1</c>.</returns>
        /// <seealso cref="CanAddOrInsertChildren"/>
        /// <seealso cref="MoveChildren"/>
        protected virtual int GetAddIndex([NotNull] IReadOnlyCollection<object> children)
        {
            return Owner.EntityCount;
        }

        /// <summary>
        /// Gets the index at which children should be inserted.
        /// </summary>
        /// <param name="position"></param>
        /// <remarks>This method ignores the concept of folder.</remarks>
        /// <returns></returns>
        /// <seealso cref="CanAddOrInsertChildren"/>
        /// <seealso cref="MoveChildren"/>
        protected virtual int GetInsertionIndex(InsertPosition position)
        {
            // If we are trying to insert around a folder (this), the child will be put at proper index automatically (auto-sorted if it's a folder, at index 0 if it's an entity.
            return 0;
        }

        /// <inheritdoc/>
        bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            var addIndex = GetAddIndex(children);
            if (addIndex < -1)
            {
                message = DragDropBehavior.InvalidDropAreaMessage;
                return false;
            }
            return CanAddOrInsertChildren(children, true, modifiers, addIndex, out message);
        }

        /// <inheritdoc/>
        void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            MoveChildren(children, modifiers, GetAddIndex(children));
        }

        /// <inheritdoc/>
        bool IInsertChildViewModel.CanInsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers, out string message)
        {
            var parentName = Parent is SceneRootViewModel ? "root level" : (!string.IsNullOrWhiteSpace(Parent?.Name) ? Parent.Name : "this location");

            message = "This entity has no parent.";
            var canAdd = Parent != null && Parent.CanAddOrInsertChildren(children, false, modifiers, GetInsertionIndex(position), out message);
            // TODO: try to move all evaluation into CheckAddOrInsertChildren so everything is at the same place
            if (canAdd)
            {
                if (children.Any(x => x == this))
                {
                    message = "Can't insert before or after selected entity";
                    return false;
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
            }
            return canAdd;
        }

        /// <inheritdoc/>
        void IInsertChildViewModel.InsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers)
        {
            if (Parent == null) throw new InvalidOperationException($"{nameof(Parent)} can't be null");
            Parent.MoveChildren(children, modifiers, GetInsertionIndex(position));
        }
    }
}
