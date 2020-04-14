// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets.Quantum.Visitors;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Quantum
{
    public abstract class AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart> : AssetCompositePropertyGraph
        where TAssetPart : class, IIdentifiable
        where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
    {
        /// <summary>
        /// A dictionary mapping each base asset to a collection of instance ids existing in this asset.
        /// </summary>
        private readonly Dictionary<AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>, HashSet<Guid>> basePartAssets = new Dictionary<AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>, HashSet<Guid>>();
        /// <summary>
        /// A dictionary mapping a tuple of (base part id, instance id) to the corresponding asset part in this asset.
        /// </summary>
        /// <remarks>Part stored here are preserved after being removed, in case they have to come back later, for example if a part in the base is being moved (removed + added again).</remarks>
        private readonly Dictionary<Tuple<Guid, Guid>, TAssetPartDesign> baseInstanceMapping = new Dictionary<Tuple<Guid, Guid>, TAssetPartDesign>();

        /// <summary>
        /// A mapping of (base part id, instance id) corresponding to deleted parts in specific instances of this asset which base part exists in the base asset.
        /// </summary>
        private readonly HashSet<Tuple<Guid, Guid>> deletedPartsInstanceMapping = new HashSet<Tuple<Guid, Guid>>();
        /// <summary>
        /// A dictionary mapping instance ids to the common ancestor of the parts corresponding to that instance id in this asset.
        /// </summary>
        /// <remarks>This dictionary is used to remember where the part instance was located, if during some time all its parts are removed, for example during some specific operaiton in the base asset.</remarks>
        private readonly Dictionary<Guid, Guid> instancesCommonAncestors = new Dictionary<Guid, Guid>();
        /// <summary>
        /// A hashset of nodes representing the collections of children from a parent part.
        /// </summary>
        private readonly HashSet<IGraphNode> registeredChildParts = new HashSet<IGraphNode>();

        protected AssetCompositeHierarchyPropertyGraph([NotNull] AssetPropertyGraphContainer container, [NotNull] AssetItem assetItem, ILogger logger)
            : base(container, assetItem, logger)
        {
            HierarchyNode = RootNode[nameof(AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>.Hierarchy)].Target;
            var rootPartsNode = HierarchyNode[nameof(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>.RootParts)].Target;
            rootPartsNode.ItemChanged += RootPartsChanged;
            foreach (var childPartNode in Asset.Hierarchy.Parts.Values.SelectMany(x => RetrieveChildPartNodes(x.Part)))
            {
                RegisterChildPartNode(childPartNode);
            }
            var partsNode = HierarchyNode[nameof(AssetCompositeHierarchyData<IAssetPartDesign<IIdentifiable>, IIdentifiable>.Parts)].Target;
            partsNode.ItemChanged += PartsChanged;

            foreach (var part in Asset.Hierarchy.Parts.Values)
            {
                LinkToOwnerPart(Container.NodeContainer.GetNode(part.Part), part);
            }
        }

        /// <inheritdoc cref="AssetPropertyGraph.Asset"/>
        public new AssetCompositeHierarchy<TAssetPartDesign, TAssetPart> Asset => (AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>)base.Asset;

        protected IObjectNode HierarchyNode { get; }

        /// <summary>
        /// Gets the name of the property targeting the part in the <see cref="TAssetPartDesign"/> type.
        /// </summary>
        [NotNull]
        protected virtual string PartName => nameof(IAssetPartDesign<TAssetPart>.Part);

        /// <summary>
        /// Raised when a part is added to this asset.
        /// </summary>
        public event EventHandler<AssetPartChangeEventArgs> PartAdded;

        /// <summary>
        /// Raised when a part is removed from this asset.
        /// </summary>
        public event EventHandler<AssetPartChangeEventArgs> PartRemoved;

        public abstract bool IsChildPartReference(IGraphNode node, NodeIndex index);

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();
            var rootPartsNode = HierarchyNode[nameof(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>.RootParts)].Target;
            rootPartsNode.ItemChanged -= RootPartsChanged;
            registeredChildParts.ToList().ForEach(UnregisterChildPartNode);
            var partsNode = HierarchyNode[nameof(AssetCompositeHierarchyData<IAssetPartDesign<IIdentifiable>, IIdentifiable>.Parts)].Target;
            partsNode.ItemChanged -= PartsChanged;

            foreach (var basePartAsset in basePartAssets.Keys)
            {
                basePartAsset.PartAdded -= PartAddedInBaseAsset;
                basePartAsset.PartRemoved -= PartRemovedInBaseAsset;
            }
            basePartAssets.Clear();
        }

        /// <inheritdoc/>
        public override void ClearReferencesToObjects(IEnumerable<Guid> objectIds)
        {
            if (objectIds == null) throw new ArgumentNullException(nameof(objectIds));
            var visitor = new ClearObjectReferenceVisitor(Definition, objectIds, (node, index) => !IsChildPartReference(node, index));
            visitor.Visit(RootNode);
        }

        /// <summary>
        /// Gets all the instance ids corresponding to an instance of the part of the given base asset.
        /// </summary>
        /// <param name="baseAssetPropertyGraph">The property graph of the base asset for which to return instance ids.</param>
        /// <returns>A collection of instances ids corresponding to instances of parts of the given base asset.</returns>
        [NotNull]
        public IReadOnlyCollection<Guid> GetInstanceIds([NotNull] AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart> baseAssetPropertyGraph)
        {
            if (baseAssetPropertyGraph == null) throw new ArgumentNullException(nameof(baseAssetPropertyGraph));
            HashSet<Guid> instanceIds;
            basePartAssets.TryGetValue(baseAssetPropertyGraph, out instanceIds);
            return (IReadOnlyCollection<Guid>)instanceIds ?? new Guid[0];
        }

        /// <summary>
        /// Retrieves all the assets that contains bases of parts of this asset.
        /// </summary>
        /// <returns></returns>
        [NotNull]
        public IReadOnlyCollection<AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>> GetBasePartAssets()
        {
            return basePartAssets.Keys;
        }

        public void BreakBasePartLinks([NotNull] IEnumerable<TAssetPartDesign> assetPartDesigns)
        {
            foreach (var part in assetPartDesigns.Where(x => x.Base != null))
            {
                var node = Container.NodeContainer.GetNode(part);
                node[nameof(IAssetPartDesign<IIdentifiable>.Base)].Update(null);
                // We must refresh the base to stop further update from the base asset to the instance parts
                RefreshBase((IAssetNode)node, null);
            }
        }

        /// <summary>
        /// Adds a part to this asset. This method updates the <see cref="AssetCompositeHierarchyData{TAssetPartDesign, TAssetPart}.Parts"/> collection.
        /// If <paramref name="parent"/> is null, it also updates the <see cref="AssetCompositeHierarchyData{TAssetPartDesign,TAssetPart}.RootParts"/> collection.
        /// Otherwise, it updates the collection containing the list of children from the parent part.
        /// </summary>
        /// <param name="newPartCollection">A collection containing the part to add and all its child parts recursively, with their associated <typeparamref name="TAssetPartDesign"/>.</param>
        /// <param name="child">The part to add to this asset.</param>
        /// <param name="parent">The parent part in which to add the child part.</param>
        /// <param name="index">The index in which to insert this part, either in the collection of root part or in the collection of child part of the parent part..</param>
        public void AddPartToAsset(AssetPartCollection<TAssetPartDesign, TAssetPart> newPartCollection, [NotNull] TAssetPartDesign child, TAssetPart parent, int index)
        {
            // This insert method does not support negative indices.
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            // For consistency, we need to always add first to the Parts collection before adding to RootParts or as a child of an existing part
            InsertPartInPartsCollection(newPartCollection, child);
            if (parent == null)
            {
                var rootPartsNode = HierarchyNode[nameof(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>.RootParts)].Target;
                rootPartsNode.Add(child.Part, new NodeIndex(index));
            }
            else
            {
                AddChildPartToParentPart(parent, child.Part, index);
            }
        }

        /// <summary>
        /// Removes a part from this asset. This method updates the <see cref="AssetCompositeHierarchyData{TAssetPartDesign, TAssetPart}.Parts"/> collection.
        /// If the part to remove is a root part, it also updates the <see cref="AssetCompositeHierarchyData{TAssetPartDesign,TAssetPart}.RootParts"/> collection.
        /// Otherwise, it updates the collection containing the list of children from the parent of this part.
        /// </summary>
        /// <param name="partDesign">The part to remove from this asset.</param>
        public void RemovePartFromAsset([NotNull] TAssetPartDesign partDesign)
        {
            if (!Asset.Hierarchy.RootParts.Contains(partDesign.Part))
            {
                var parent = Asset.GetParent(partDesign.Part);
                if (parent == null) throw new InvalidOperationException("The part has no parent but is not in the RootParts collection.");
                RemoveChildPartFromParentPart(parent, partDesign.Part);
            }
            else
            {
                var index = new NodeIndex(Asset.Hierarchy.RootParts.IndexOf(partDesign.Part));
                var rootPartsNode = HierarchyNode[nameof(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>.RootParts)].Target;
                rootPartsNode.Remove(partDesign.Part, index);
            }
            RemovePartFromPartsCollection(partDesign);
        }

        /// <summary>
        /// Deletes the given parts and all its children, recursively, and clear all object references to it.
        /// </summary>
        /// <param name="partDesigns">The parts to delete.</param>
        /// <param name="deletedPartsMapping">A mapping of the base information (base part id, instance id) of the deleted parts that have a base.</param>
        public void DeleteParts([NotNull] IEnumerable<TAssetPartDesign> partDesigns, [NotNull] out HashSet<Tuple<Guid, Guid>> deletedPartsMapping)
        {
            if (partDesigns == null) throw new ArgumentNullException(nameof(partDesigns));
            var partsToDelete = new Stack<TAssetPartDesign>(partDesigns);
            var referencesToClear = new HashSet<Guid>();
            deletedPartsMapping = new HashSet<Tuple<Guid, Guid>>();
            while (partsToDelete.Count > 0)
            {
                // We need to remove children first to keep consistency in our data
                var partToDelete = partsToDelete.Peek();
                var children = Asset.EnumerateChildPartDesigns(partToDelete, Asset.Hierarchy, false).ToList();
                if (children.Count > 0)
                {
                    // Enqueue children if there is any, and re-process the stack
                    children.ForEach(x => partsToDelete.Push(x));
                    continue;
                }
                // No children to process, we can safely remove the current part from the stack
                partToDelete = partsToDelete.Pop();
                // First remove all references to the part we are deleting
                // Note: we must do this first so instances of this base will be able to properly make the connection with the base part being cleared
                var containedIdentifiables = IdentifiableObjectCollector.Collect(Definition, Container.NodeContainer.GetNode(partToDelete.Part));
                containedIdentifiables.Keys.ForEach(x => referencesToClear.Add(x));
                referencesToClear.Add(partToDelete.Part.Id);
                // Then actually remove the part from the hierarchy
                RemovePartFromAsset(partToDelete);
                // Keep track of deleted part instances
                if (partToDelete.Base != null)
                {
                    deletedPartsMapping.Add(Tuple.Create(partToDelete.Base.BasePartId, partToDelete.Base.InstanceId));
                }
            }
            TrackDeletedInstanceParts(deletedPartsMapping);
            ClearReferencesToObjects(referencesToClear);
        }

        /// <inheritdoc/>
        public override IGraphNode FindTarget([NotNull] IGraphNode sourceNode, IGraphNode target)
        {
            // TODO: try to generalize what the overrides of this implementation are doing.
            // Connect the parts to their base if any.
            if (sourceNode.Retrieve() is TAssetPart part && sourceNode is IObjectNode)
            {
                // The part might be being moved and could possibly be currently not into the Parts collection.
                if (Asset.Hierarchy.Parts.TryGetValue(part.Id, out TAssetPartDesign partDesign) && partDesign.Base != null)
                {
                    var baseAssetGraph = Container.TryGetGraph(partDesign.Base.BasePartAsset.Id);
                    // Base asset might have been deleted
                    if (baseAssetGraph == null)
                        return base.FindTarget(sourceNode, target);

                    // Part might have been deleted in base asset
                    ((AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>)baseAssetGraph.RootNode.Retrieve()).Hierarchy.Parts.TryGetValue(partDesign.Base.BasePartId, out TAssetPartDesign basePart);
                    return basePart != null ? Container.NodeContainer.GetOrCreateNode(basePart.Part) : base.FindTarget(sourceNode, target);
                }
            }

            return base.FindTarget(sourceNode, target);
        }

        /// <summary>
        /// Clones a sub-hierarchy of a composite hierarchical asset.
        /// </summary>
        /// <param name="nodeContainer">The container in which are the nodes of the hierarchy to clone and in which to create nodes for the cloned hierarchy, used to propagate metadata (overrides, etc.) if needed.</param>
        /// <param name="asset">The asset from which to clone sub-hierarchies.</param>
        /// <param name="sourceRootIds">The ids that are the roots of the sub-hierarchies to clone.</param>
        /// <param name="flags">The flags customizing the cloning operation.</param>
        /// <param name="idRemapping">A dictionary containing the remapping of <see cref="IIdentifiable.Id"/> if <see cref="AssetClonerFlags.GenerateNewIdsForIdentifiableObjects"/> has been passed to the cloner.</param>
        /// <returns>A <see cref="AssetCompositeHierarchyData{TAssetPartDesign, TAssetPart}"/> corresponding to the cloned parts.</returns>
        /// <remarks>The parts passed to this methods must be independent in the hierarchy.</remarks>
        public static AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart> CloneSubHierarchies([NotNull] AssetNodeContainer nodeContainer, [NotNull] AssetCompositeHierarchy<TAssetPartDesign, TAssetPart> asset,
            [NotNull] IEnumerable<Guid> sourceRootIds, SubHierarchyCloneFlags flags, [NotNull] out Dictionary<Guid, Guid> idRemapping)
        {
            return CloneSubHierarchies(nodeContainer, nodeContainer, asset, sourceRootIds, flags, out idRemapping);
        }

        /// <summary>
        /// Clones a sub-hierarchy of a composite hierarchical asset.
        /// </summary>
        /// <param name="sourceNodeContainer">The container in which are the nodes of the hierarchy to clone, used to extract metadata (overrides, etc.) if needed.</param>
        /// <param name="targetNodeContainer">The container in which the nodes of the cloned hierarchy should be created, used to re-apply metadata (overrides, etc.) if needed.</param>
        /// <param name="asset">The asset from which to clone sub-hierarchies.</param>
        /// <param name="sourceRootIds">The ids that are the roots of the sub-hierarchies to clone.</param>
        /// <param name="flags">The flags customizing the cloning operation.</param>
        /// <param name="idRemapping">A dictionary containing the remapping of <see cref="IIdentifiable.Id"/> if <see cref="AssetClonerFlags.GenerateNewIdsForIdentifiableObjects"/> has been passed to the cloner.</param>
        /// <returns>A <see cref="AssetCompositeHierarchyData{TAssetPartDesign, TAssetPart}"/> corresponding to the cloned parts.</returns>
        /// <remarks>The parts passed to this methods must be independent in the hierarchy.</remarks>
        public static AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart> CloneSubHierarchies([NotNull] AssetNodeContainer sourceNodeContainer, [NotNull] AssetNodeContainer targetNodeContainer,
            [NotNull] AssetCompositeHierarchy<TAssetPartDesign, TAssetPart> asset, [NotNull] IEnumerable<Guid> sourceRootIds, SubHierarchyCloneFlags flags, [NotNull] out Dictionary<Guid, Guid> idRemapping)
        {
            if (sourceNodeContainer == null) throw new ArgumentNullException(nameof(sourceNodeContainer));
            if (targetNodeContainer == null) throw new ArgumentNullException(nameof(targetNodeContainer));
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            if (sourceRootIds == null) throw new ArgumentNullException(nameof(sourceRootIds));

            // Extract the actual sub hierarchies to clone from the asset into a new instance of AssetCompositeHierarchyData
            var subTreeHierarchy = new AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>();
            foreach (var rootId in sourceRootIds)
            {
                if (!asset.Hierarchy.Parts.ContainsKey(rootId))
                    throw new ArgumentException(@"The source root parts must be parts of this asset.", nameof(sourceRootIds));

                subTreeHierarchy.RootParts.Add(asset.Hierarchy.Parts[rootId].Part);

                subTreeHierarchy.Parts.Add(asset.Hierarchy.Parts[rootId]);
                foreach (var subTreePart in asset.EnumerateChildParts(asset.Hierarchy.Parts[rootId].Part, true))
                    subTreeHierarchy.Parts.Add(asset.Hierarchy.Parts[subTreePart.Id]);
            }

            var assetType = asset.GetType();

            // Create a new empty asset of the same type, and assign the sub hierachies to clone to it
            var cloneAsset = (AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>)Activator.CreateInstance(assetType);
            cloneAsset.Hierarchy = subTreeHierarchy;
            var assetDefinition = AssetQuantumRegistry.GetDefinition(assetType);

            // We get the node corresponding to the new asset in the source NodeContainer, to be able to generate metadata (overrides, object references) needed for cloning.
            var rootNode = sourceNodeContainer.GetOrCreateNode(cloneAsset);
            var externalReferences = ExternalReferenceCollector.GetExternalReferences(assetDefinition, rootNode);
            var overrides = (flags & SubHierarchyCloneFlags.RemoveOverrides) == 0 ? GenerateOverridesForSerialization(rootNode) : null;

            // Now we ready to clone, let's just translate the flags and pass everything to the asset cloner.
            var clonerFlags = AssetClonerFlags.None;
            if ((flags & SubHierarchyCloneFlags.GenerateNewIdsForIdentifiableObjects) != 0)
                clonerFlags |= AssetClonerFlags.GenerateNewIdsForIdentifiableObjects;
            if ((flags & SubHierarchyCloneFlags.CleanExternalReferences) != 0)
                clonerFlags |= AssetClonerFlags.ClearExternalReferences;
            // We don't need to clone the asset itself, just the hierarchy. The asset itself is just useful so the property graph is in a normal context to do what we need.
            var clonedHierarchy = AssetCloner.Clone(subTreeHierarchy, clonerFlags, externalReferences, out idRemapping);

            if ((flags & SubHierarchyCloneFlags.RemoveOverrides) == 0)
            {
                // We need to propagate the override information to the nodes of the cloned objects into the target node container.
                // Let's reuse our temporary asset, and get its node in the target node container.
                rootNode = targetNodeContainer.GetOrCreateNode(cloneAsset);
                // Replace the initial hierarchy by the cloned one (through the Update method, in case the target container is the same as the source one).
                rootNode[nameof(AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>.Hierarchy)].Update(clonedHierarchy);
                // Remap the paths to overriden properties in case we generated new ids for identifiable objects.
                AssetCloningHelper.RemapIdentifiablePaths(overrides, idRemapping);
                // Finally apply the overrides that come from the source parts.
                ApplyOverrides((IAssetNode)rootNode, overrides);
            }

            return clonedHierarchy;
        }

        /// <inheritdoc/>
        public override void RefreshBase()
        {
            base.RefreshBase();
            UpdateAssetPartBases();
        }

        /// <inheritdoc/>
        public override void RefreshBase(IAssetNode node, IAssetNode baseNode)
        {
            base.RefreshBase(node, baseNode);
            UpdateAssetPartBases();
        }

        /// <summary>
        /// Tracks the given deleted instance parts.
        /// </summary>
        /// <param name="deletedPartsMapping">A mapping of deleted parts (base part id, instance id).</param>
        public void TrackDeletedInstanceParts([NotNull] IEnumerable<Tuple<Guid, Guid>> deletedPartsMapping)
        {
            if (deletedPartsMapping == null) throw new ArgumentNullException(nameof(deletedPartsMapping));
            deletedPartsInstanceMapping.UnionWith(deletedPartsMapping);
        }

        /// <summary>
        /// Untracks the given deleted instance parts.
        /// </summary>
        /// <param name="deletedPartsMapping">A mapping of deleted parts (base part id, instance id).</param>
        public void UntrackDeletedInstanceParts([NotNull] IEnumerable<Tuple<Guid, Guid>> deletedPartsMapping)
        {
            if (deletedPartsMapping == null) throw new ArgumentNullException(nameof(deletedPartsMapping));
            deletedPartsInstanceMapping.ExceptWith(deletedPartsMapping);
        }

        /// <inheritdoc />
        protected override void FinalizeInitialization()
        {
            // Track parts that were removed in instances by comparing to the base
            foreach (var kv in basePartAssets)
            {
                var baseAsset = kv.Key.Asset;
                var instanceIds = kv.Value;
                var baseParts = baseAsset.Hierarchy.Parts.Keys.SelectMany(basePartId => instanceIds, Tuple.Create);
                var existingParts = baseInstanceMapping.Keys;
                var deletedParts = baseParts.Except(existingParts);
                TrackDeletedInstanceParts(deletedParts);
            }
        }

        /// <summary>
        /// Retrieves the Quantum <see cref="IGraphNode"/> instances containing the child parts. These contents can be collections or single values.
        /// </summary>
        /// <param name="part">The part instance for which to retrieve the Quantum content/</param>
        /// <returns>A sequence containing all contents containing child parts.</returns>
        // TODO: this method probably doesn't need to return an enumerable, our current use case are single content only.
        protected abstract IEnumerable<IGraphNode> RetrieveChildPartNodes(TAssetPart part);

        /// <summary>
        /// Retrieves the <see cref="Guid"/> corresponding to the given part.
        /// </summary>
        /// <param name="part">The part for which to retrieve the id.</param>
        protected abstract Guid GetIdFromChildPart(object part);

        /// <summary>
        /// Adds the given child part to the list of children of the given parent part.
        /// </summary>
        /// <param name="parentPart"></param>
        /// <param name="childPart">The child part.</param>
        /// <param name="index">The index of the child part in the list of children of the parent part.</param>
        /// <remarks>This method does not modify the <see cref="AssetCompositeHierarchyData{TAssetPartDesign, TAssetPart}"/> contained in this asset.</remarks>
        protected abstract void AddChildPartToParentPart([NotNull] TAssetPart parentPart, [NotNull] TAssetPart childPart, int index);

        /// <summary>
        /// Removes the given child part from the list of children of the given parent part.
        /// </summary>
        /// <param name="parentPart"></param>
        /// <param name="childPart">The child part.</param>
        /// <remarks>This method does not modify the <see cref="AssetCompositeHierarchyData{TAssetPartDesign, TAssetPart}"/> contained in this asset.</remarks>
        protected abstract void RemoveChildPartFromParentPart([NotNull] TAssetPart parentPart, [NotNull] TAssetPart childPart);

        /// <summary>
        /// When a part is added to the base asset, it could be the result of a move (remove + add).
        /// In that case, remove the new <paramref name="clonedPart"/> and replace it with the <paramref name="existingPart"/>.
        /// </summary>
        /// <param name="baseHierarchy">The cloned base hierarchy.</param>
        /// <param name="clonedPart">The cloned part to replace.</param>
        /// <param name="existingPart">The existing part to restore.</param>
        /// <seealso cref="PartAddedInBaseAsset"/>
        /// <remarks>
        /// Inheriting instance can override this method to perform additional operations.
        /// </remarks>
        protected virtual void ReuseExistingPart([NotNull] AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart> baseHierarchy, [NotNull] TAssetPartDesign clonedPart, [NotNull] TAssetPartDesign existingPart)
        {
            // Replace the cloned part by the one to restore in the list of root if needed
            if (baseHierarchy.RootParts.Remove(clonedPart.Part))
            {
                baseHierarchy.RootParts.Add(existingPart.Part);
            }

            // Replace the cloned part by the one to restore in the list of parts
            if (!baseHierarchy.Parts.Remove(clonedPart.Part.Id)) throw new InvalidOperationException("The new part should be in the baseHierarchy.");
            baseHierarchy.Parts.Add(existingPart);
        }

        /// <summary>
        /// Indicates whether a new part added in a base asset should be also cloned and added to this asset.
        /// </summary>
        /// <param name="baseAssetGraph">The property graph of the base asset.</param>
        /// <param name="newPart">The new part that has been added in the base asset.</param>
        /// <param name="newPartParent">The parent of the new part that has been added in the base asset.</param>
        /// <param name="instanceId">The instance id for which the part might be cloned.</param>
        /// <returns><c>true</c> if the part should be cloned and added to this asset; otherwise, <c>false</c>.</returns>
        protected virtual bool ShouldAddNewPartFromBase(AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart> baseAssetGraph, [NotNull] TAssetPartDesign newPart, TAssetPart newPartParent, Guid instanceId)
        {
            return !deletedPartsInstanceMapping.Contains(Tuple.Create(newPart.Part.Id, instanceId));
        }

        protected virtual void RewriteIds([NotNull] TAssetPart targetPart, [NotNull] TAssetPart sourcePart)
        {
            // TODO: this method is temporary!
            targetPart.Id = sourcePart.Id;
        }

        /// <summary>
        /// Finds the best index (and parent) at which to insert a new part that is propagated after being added to one of the bases of this asset.
        /// </summary>
        /// <param name="baseAsset">The base asset for the part that has been added.</param>
        /// <param name="newBasePart">The new part that has been added to the base.</param>
        /// <param name="newBasePartParent">The parent part of the part that has been added to the base.</param>
        /// <param name="instanceId">The id of the instance for which we are looking for an index and parent.</param>
        /// <param name="instanceParent">The parent in which to insert the new instance part. If null, the new part will be inserted as root of the hierarchy.</param>
        /// <returns>The index at which to insert the new part in the instance, or a negative value if the part should be discarded.</returns>
        protected virtual int FindBestInsertIndex(AssetCompositeHierarchy<TAssetPartDesign, TAssetPart> baseAsset, TAssetPartDesign newBasePart, TAssetPart newBasePartParent, Guid instanceId, out TAssetPartDesign instanceParent)
        {
            instanceParent = null;
            var insertIndex = -1;

            // First, let's find out where it is the best to insert this new part
            if (newBasePartParent == null)
            {
                // The part is a root, so we must place it according to its sibling (since no parent exists).
                var partIndex = baseAsset.Hierarchy.RootParts.IndexOf(x => x.Id == newBasePart.Part.Id);
                // Let's try to find a sibling in the parts preceding it, in order
                for (var i = partIndex - 1; i >= 0 && insertIndex < 0; --i)
                {
                    var sibling = baseAsset.Hierarchy.Parts[baseAsset.Hierarchy.RootParts[i].Id];
                    var instanceSibling = Asset.Hierarchy.Parts.Values.FirstOrDefault(x => x.Base?.InstanceId == instanceId && x.Base?.BasePartId == sibling.Part.Id);
                    // This sibling still exists instance-side, let's get its parent.
                    if (instanceSibling != null)
                    {
                        // If the sibling itself has a parent instance-side, let's use the same parent and insert after it
                        // Otherwise the sibling is root, let's insert after it in the root parts
                        var parent = Asset.GetParent(instanceSibling.Part);
                        instanceParent = parent != null ? Asset.Hierarchy.Parts[parent.Id] : null;
                        insertIndex = Asset.IndexOf(instanceSibling.Part) + 1;
                        break;
                    }
                }

                // Let's try to find a sibling in the parts following it, in order
                for (var i = partIndex + 1; i < baseAsset.Hierarchy.RootParts.Count && insertIndex < 0; ++i)
                {
                    var sibling = baseAsset.Hierarchy.Parts[baseAsset.Hierarchy.RootParts[i].Id];
                    var instanceSibling = Asset.Hierarchy.Parts.Values.FirstOrDefault(x => x.Base?.InstanceId == instanceId && x.Base?.BasePartId == sibling.Part.Id);
                    // This sibling still exists instance-side, let's get its parent.
                    if (instanceSibling != null)
                    {
                        // If the sibling itself has a parent instance-side, let's use the same parent and insert after it
                        // Otherwise the sibling is root, let's insert after it in the root parts
                        var parent = Asset.GetParent(instanceSibling.Part);
                        instanceParent = parent != null ? Asset.Hierarchy.Parts[parent.Id] : null;
                        insertIndex = Asset.IndexOf(instanceSibling.Part);
                        break;
                    }
                }
            }
            else
            {
                // The new part is not root, it has a parent.
                instanceParent = Asset.Hierarchy.Parts.Values.FirstOrDefault(x => x.Base?.InstanceId == instanceId && x.Base?.BasePartId == newBasePartParent.Id);

                // If the parent has been removed instance side, the hierarchy to the new part does not exist anymore. We can discard it
                if (instanceParent != null)
                {
                    var partIndex = baseAsset.IndexOf(newBasePart.Part);

                    // Let's try to find a sibling in the parts preceding it, in order
                    for (var i = partIndex - 1; i >= 0 && insertIndex < 0; --i)
                    {
                        var sibling = baseAsset.GetChild(newBasePartParent, i);
                        var instanceSibling = Asset.Hierarchy.Parts.Values.FirstOrDefault(x => x.Base?.InstanceId == instanceId && x.Base?.BasePartId == sibling.Id);
                        // This sibling still exists instance-side, let's insert after it
                        if (instanceSibling != null)
                            insertIndex = i + 1;
                    }

                    // Let's try to find a sibling in the parts following it, in order
                    for (var i = partIndex + 1; i < baseAsset.GetChildCount(newBasePartParent) && insertIndex < 0; ++i)
                    {
                        var sibling = baseAsset.GetChild(newBasePartParent, i);
                        var instanceSibling = Asset.Hierarchy.Parts.Values.FirstOrDefault(x => x.Base?.InstanceId == instanceId && x.Base?.BasePartId == sibling.Id);
                        // This sibling still exists instance-side, let's insert before it
                        if (instanceSibling != null)
                            insertIndex = i - 1;
                    }

                    // Default position is first index
                    if (insertIndex < 0)
                        insertIndex = 0;
                }
            }

            if (insertIndex < 0)
            {
                // We couldn't find any parent/sibling in the instance. Either the parent has been removed, in which case we'll discard the part,
                // or the base is a single part that has been moved around, and we'll rely on the last known common ancestor of this instance to re-insert it.
                var isAlone = Asset.Hierarchy.Parts.Values.All(x => x.Base?.InstanceId != instanceId);
                if (isAlone)
                {
                    Guid parentId;
                    if (instancesCommonAncestors.TryGetValue(instanceId, out parentId) && parentId != Guid.Empty)
                    {
                        // FIXME: instancesCommonAncestors should be synchronized with existing instances, i.e. if the instance has been compltely removed then the common ancestor should have been cleared.
                        if (Asset.Hierarchy.Parts.TryGetValue(parentId, out instanceParent))
                        {
                            insertIndex = 0;
                        }
                    }
                }
            }
            return insertIndex;
        }

        /// <inheritdoc/>
        protected override void OnContentChanged([NotNull] MemberNodeChangeEventArgs args)
        {
            RelinkToOwnerPart((IAssetNode)args.Member, args.NewValue);
            base.OnContentChanged(args);
        }

        /// <inheritdoc/>
        protected override void OnItemChanged([NotNull] ItemChangeEventArgs args)
        {
            RelinkToOwnerPart((IAssetNode)args.Collection, args.NewValue);
            base.OnItemChanged(args);
        }

        private void RelinkToOwnerPart([NotNull] IAssetNode node, object newValue)
        {
            var partDesign = (TAssetPartDesign)node.GetContent(NodesToOwnerPartVisitor.OwnerPartContentName)?.Retrieve();
            if (partDesign != null)
            {
                // A property of a part has changed
                LinkToOwnerPart(node, partDesign);
            }
            else if (node.Type == typeof(AssetPartCollection<TAssetPartDesign, TAssetPart>) && newValue is TAssetPartDesign)
            {
                // A new part has been added
                partDesign = (TAssetPartDesign)newValue;
                LinkToOwnerPart(Container.NodeContainer.GetNode(partDesign.Part), partDesign);
            }
        }

        private void UpdateAssetPartBases()
        {
            // Unregister deleted base assets
            var deletedBaseParts = basePartAssets.Keys.Where(g => g.AssetItem.IsDeleted).ToList();
            foreach (var basePartAsset in deletedBaseParts)
            {
                basePartAsset.PartAdded -= PartAddedInBaseAsset;
                basePartAsset.PartRemoved -= PartRemovedInBaseAsset;
                basePartAssets.Remove(basePartAsset);
            }

            // We need to subscribe to event of new base assets, but we don't want to unregister from previous one, in case the user is moving (remove + add)
            // the single part of a base. In this case we wouldn't have any part linking to the base once it has been removed.
            var newBasePartAsset = new HashSet<AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>>();

            // We want to enumerate parts that are actually "reachable", so we don't use Hierarchy.Parts for iteration - we iterate from the root parts instead.
            // We use Hierarchy.Parts at the end just to retrieve the part design from the actual part.
            var currentParts = Asset.Hierarchy.RootParts.DepthFirst(x => Asset.EnumerateChildParts(x, false)).Select(x => Asset.Hierarchy.Parts[x.Id]);
            foreach (var part in currentParts)
            {
                if (part.Base == null)
                    continue;

                if (Container.TryGetGraph(part.Base.BasePartAsset.Id) is AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart> baseAssetGraph)
                {
                    if (!basePartAssets.TryGetValue(baseAssetGraph, out HashSet<Guid> instanceIds))
                    {
                        instanceIds = new HashSet<Guid>();
                        basePartAssets.Add(baseAssetGraph, instanceIds);
                        newBasePartAsset.Add(baseAssetGraph);
                    }
                    instanceIds.Add(part.Base.InstanceId);
                }

                // Update mapping
                baseInstanceMapping[Tuple.Create(part.Base.BasePartId, part.Base.InstanceId)] = part;

                // Update common ancestors
                if (!instancesCommonAncestors.TryGetValue(part.Base.InstanceId, out Guid ancestorId))
                {
                    instancesCommonAncestors[part.Base.InstanceId] = Asset.GetParent(part.Part)?.Id ?? Guid.Empty;
                }
                else
                {
                    var parent = ancestorId;
                    var parents = new HashSet<Guid>();
                    while (parent != Guid.Empty)
                    {
                        parents.Add(parent);
                        // Note: parent could have been deleted
                        parent = Asset.Hierarchy.Parts.TryGetValue(parent, out TAssetPartDesign assetPartDesign) ? Asset.GetParent(assetPartDesign.Part)?.Id ?? Guid.Empty : Guid.Empty;
                    }
                    ancestorId = Asset.GetParent(part.Part)?.Id ?? Guid.Empty;
                    while (ancestorId != Guid.Empty && !parents.Contains(ancestorId))
                    {
                        // Note: ancestor could have been deleted
                        ancestorId = Asset.Hierarchy.Parts.TryGetValue(ancestorId, out TAssetPartDesign assetPartDesign) ? (Asset.GetParent(assetPartDesign.Part)?.Id ?? Guid.Empty) : Guid.Empty;
                    }
                    instancesCommonAncestors[part.Base.InstanceId] = ancestorId;
                }
            }

            // Register to new base part events
            foreach (var basePartAsset in newBasePartAsset)
            {
                basePartAsset.PartAdded += PartAddedInBaseAsset;
                basePartAsset.PartRemoved += PartRemovedInBaseAsset;
            }
        }

        private void PartAddedInBaseAsset(object sender, [NotNull] AssetPartChangeEventArgs e)
        {
            UpdatingPropertyFromBase = true;

            var baseAsset = (AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>)e.Asset;
            var newPart = baseAsset.Hierarchy.Parts[e.PartId];
            var newPartParent = baseAsset.GetParent(newPart.Part);
            var baseAssetGraph = Container.TryGetGraph(baseAsset.Id) as AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>;
            if (baseAssetGraph == null) throw new InvalidOperationException("Unable to find the graph corresponding to the base part");


            foreach (var instanceId in basePartAssets[baseAssetGraph])
            {
                // Discard the part if this asset don't want it
                if (!ShouldAddNewPartFromBase(baseAssetGraph, newPart, newPartParent, instanceId))
                    continue;

                var insertIndex = FindBestInsertIndex(baseAsset, newPart, newPartParent, instanceId, out TAssetPartDesign instanceParent);
                if (insertIndex < 0)
                    continue;

                // Now we know where to insert, let's clone the new part.
                var flags = SubHierarchyCloneFlags.GenerateNewIdsForIdentifiableObjects | SubHierarchyCloneFlags.RemoveOverrides;
                var baseHierarchy = CloneSubHierarchies(baseAssetGraph.Container.NodeContainer, baseAssetGraph.Asset, newPart.Part.Id.Yield(), flags, out Dictionary<Guid, Guid> mapping);
                foreach (var ids in mapping)
                {
                    // Process only ids that correspond to parts
                    if (!baseHierarchy.Parts.TryGetValue(ids.Value, out TAssetPartDesign clone))
                        continue;

                    clone.Base = new BasePart(new AssetReference(e.AssetItem.Id, e.AssetItem.Location), ids.Key, instanceId);

                    // This add could actually be a move (remove + add). So we compare to the existing baseInstanceMapping and reuse the existing part if necessary
                    var mappingKey = Tuple.Create(ids.Key, instanceId);
                    if (!deletedPartsInstanceMapping.Contains(mappingKey) && baseInstanceMapping.TryGetValue(mappingKey, out TAssetPartDesign existingPart))
                    {
                        ReuseExistingPart(baseHierarchy, clone, existingPart);
                    }
                }

                // We might have changed some ids, let's refresh the collection
                baseHierarchy.Parts.RefreshKeys();

                // Then actually add the new part
                var rootClone = baseHierarchy.Parts[baseHierarchy.RootParts.Single().Id];
                AddPartToAsset(baseHierarchy.Parts, rootClone, instanceParent?.Part, insertIndex);
            }

            // Reconcile with base
            RefreshBase();
            ReconcileWithBase();

            UpdatingPropertyFromBase = false;
        }

        private void PartRemovedInBaseAsset(object sender, AssetPartChangeEventArgs e)
        {
            UpdatingPropertyFromBase = true;
            foreach (var part in Asset.Hierarchy.Parts.Values.Where(x => x.Base?.BasePartId == e.PartId).ToList())
            {
                RemovePartFromAsset(part);
            }
            UpdatingPropertyFromBase = false;
        }

        private void InsertPartInPartsCollection(AssetPartCollection<TAssetPartDesign, TAssetPart> newPartCollection, [NotNull] TAssetPartDesign rootPart)
        {
            var node = HierarchyNode[nameof(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>.Parts)].Target;
            node.Add(rootPart, new NodeIndex(rootPart.Part.Id));
            foreach (var childPart in Asset.EnumerateChildParts(rootPart.Part, false))
            {
                var partDesign = newPartCollection[childPart.Id];
                InsertPartInPartsCollection(newPartCollection, partDesign);
            }
        }

        private void RemovePartFromPartsCollection([NotNull] TAssetPartDesign rootPart)
        {
            foreach (var childPart in Asset.EnumerateChildParts(rootPart.Part, false))
            {
                var partDesign = Asset.Hierarchy.Parts[childPart.Id];
                RemovePartFromPartsCollection(partDesign);
            }
            var node = HierarchyNode[nameof(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>.Parts)].Target;
            var index = new NodeIndex(rootPart.Part.Id);
            node.Remove(rootPart, index);
        }

        private void NotifyPartAdded(Guid partId)
        {
            UpdateAssetPartBases();
            PartAdded?.Invoke(this, new AssetPartChangeEventArgs(AssetItem, partId));
        }

        private void NotifyPartRemoved(Guid partId)
        {
            UpdateAssetPartBases();
            PartRemoved?.Invoke(this, new AssetPartChangeEventArgs(AssetItem, partId));
        }

        private void RootPartsChanged(object sender, [NotNull] INodeChangeEventArgs e)
        {
            switch (e.ChangeType)
            {
                case ContentChangeType.CollectionAdd:
                    NotifyPartAdded(((TAssetPart)e.NewValue).Id);
                    break;
                case ContentChangeType.CollectionRemove:
                    NotifyPartRemoved(((TAssetPart)e.OldValue).Id);
                    break;
            }
        }

        private void ChildPartChanged(object sender, [NotNull] INodeChangeEventArgs e)
        {
            switch (e.ChangeType)
            {
                case ContentChangeType.CollectionUpdate:
                case ContentChangeType.ValueChange:
                    if (e.OldValue != null)
                    {
                        NotifyPartRemoved(GetIdFromChildPart(e.OldValue));
                    }
                    if (e.NewValue != null)
                    {
                        NotifyPartAdded(GetIdFromChildPart(e.NewValue));
                    }
                    break;
                case ContentChangeType.CollectionAdd:
                    NotifyPartAdded(GetIdFromChildPart(e.NewValue));
                    break;
                case ContentChangeType.CollectionRemove:
                    NotifyPartRemoved(GetIdFromChildPart(e.OldValue));
                    break;
            }
        }

        private void PartsChanged(object sender, [NotNull] ItemChangeEventArgs e)
        {
            TAssetPart part;
            switch (e.ChangeType)
            {
                case ContentChangeType.CollectionAdd:
                    // Ensure that we track children added later to any new part
                    part = ((TAssetPartDesign)e.NewValue).Part;
                    foreach (var childPartNode in RetrieveChildPartNodes(part))
                    {
                        RegisterChildPartNode(childPartNode);
                    }
                    break;
                case ContentChangeType.CollectionRemove:
                    // And untrack removed parts
                    part = ((TAssetPartDesign)e.OldValue).Part;
                    foreach (var childPartNode in RetrieveChildPartNodes(part))
                    {
                        UnregisterChildPartNode(childPartNode);
                    }
                    break;
            }
        }

        private void RegisterChildPartNode(IGraphNode node)
        {
            if (registeredChildParts.Add(node))
            {
                var memberNode = node as IMemberNode;
                if (memberNode != null)
                {
                    memberNode.ValueChanged += ChildPartChanged;
                }
                var objectNode = node as IObjectNode;
                if (objectNode != null)
                {
                    objectNode.ItemChanged += ChildPartChanged;
                }
            }
        }

        private void UnregisterChildPartNode(IGraphNode node)
        {
            if (registeredChildParts.Remove(node))
            {
                var memberNode = node as IMemberNode;
                if (memberNode != null)
                {
                    memberNode.ValueChanged -= ChildPartChanged;
                }
                var objectNode = node as IObjectNode;
                if (objectNode != null)
                {
                    objectNode.ItemChanged -= ChildPartChanged;
                }
            }
        }
    }
}
