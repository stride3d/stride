// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Quantum.Internal;
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Core.Assets.Yaml;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Yaml;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Quantum
{
    [AssetPropertyGraph(typeof(Asset))]
    // ReSharper disable once RequiredBaseTypesIsNotInherited - due to a limitation on how ReSharper checks this requirement (see https://youtrack.jetbrains.com/issue/RSRP-462598)
    public class AssetPropertyGraph : IDisposable
    {
        protected readonly AssetItem AssetItem;

        public struct NodeOverride
        {
            public NodeOverride(IAssetNode overriddenNode, NodeIndex overriddenIndex, OverrideTarget target)
            {
                Node = overriddenNode;
                Index = overriddenIndex;
                Target = target;
            }
            public readonly IAssetNode Node;
            public readonly NodeIndex Index;
            public readonly OverrideTarget Target;
        }

        private struct NodeChangeHandlers
        {
            public readonly EventHandler<MemberNodeChangeEventArgs> ValueChange;
            public readonly EventHandler<ItemChangeEventArgs> ItemChange;
            public NodeChangeHandlers(EventHandler<MemberNodeChangeEventArgs> valueChange, EventHandler<ItemChangeEventArgs> itemChange)
            {
                ValueChange = valueChange;
                ItemChange = itemChange;
            }
        }

        private readonly Dictionary<IGraphNode, OverrideType> previousOverrides = new Dictionary<IGraphNode, OverrideType>();
        private readonly Dictionary<IGraphNode, ItemId> removedItemIds = new Dictionary<IGraphNode, ItemId>();

        /// <summary>
        /// The asset this property graph represents.
        /// </summary>
        protected readonly Asset Asset;
        // TODO: turn back private
        internal readonly AssetToBaseNodeLinker baseLinker;
        private readonly AssetToBaseNodeLinker baseUnlinker;
        private readonly GraphNodeChangeListener nodeListener;
        private readonly Dictionary<IAssetNode, NodeChangeHandlers> baseLinkedNodes = new Dictionary<IAssetNode, NodeChangeHandlers>();
        private IBaseToDerivedRegistry baseToDerivedRegistry;
        private bool isDisposed;

        public AssetPropertyGraph([NotNull] AssetPropertyGraphContainer container, [NotNull] AssetItem assetItem, ILogger logger)
        {
            AssetItem = assetItem ?? throw new ArgumentNullException(nameof(assetItem));
            Container = container ?? throw new ArgumentNullException(nameof(container));
            Definition = AssetQuantumRegistry.GetDefinition(AssetItem.Asset.GetType());
            AssetCollectionItemIdHelper.GenerateMissingItemIds(assetItem.Asset);
            CollectionItemIdsAnalysis.FixupItemIds(assetItem, logger);
            Asset = assetItem.Asset;
            RootNode = (AssetObjectNode)Container.NodeContainer.GetOrCreateNode(assetItem.Asset);
            var overrides = assetItem.YamlMetadata?.RetrieveMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey);
            ApplyOverrides(RootNode, overrides);
            nodeListener = new AssetGraphNodeChangeListener(RootNode, Definition);
            nodeListener.Initialize();
            nodeListener.ValueChanging += AssetContentChanging;
            nodeListener.ValueChanged += AssetContentChanged;
            nodeListener.ItemChanging += AssetItemChanging;
            nodeListener.ItemChanged += AssetItemChanged;

            baseLinker = new AssetToBaseNodeLinker(this) { LinkAction = LinkBaseNode };
            baseUnlinker = new AssetToBaseNodeLinker(this) { LinkAction = UnlinkBaseNode };
        }

        public virtual void Dispose()
        {
            nodeListener.ValueChanging -= AssetContentChanging;
            nodeListener.ValueChanged -= AssetContentChanged;
            nodeListener.ItemChanging -= AssetItemChanging;
            nodeListener.ItemChanged -= AssetItemChanged;
            nodeListener.Dispose();
            ClearAllBaseLinks();
            isDisposed = true;
        }

        /// <summary>
        /// The identifier of this asset.
        /// </summary>
        public AssetId Id => AssetItem.Id;

        /// <summary>
        /// The root node of this asset.
        /// </summary>
        public IAssetObjectNode RootNode { get; }

        /// <summary>
        /// The container containing all asset property graphs.
        /// </summary>
        public AssetPropertyGraphContainer Container { get; }

        /// <summary>
        /// The <see cref="AssetPropertyGraphDefinition"/> associated to the type of the asset.
        /// </summary>
        public AssetPropertyGraphDefinition Definition { get; }

        /// <summary>
        /// The property graph of the archetype asset, if it has one.
        /// </summary>
        public AssetPropertyGraph Archetype { get; set; }

        /// <summary>
        /// Indicates whether a property is currently being updated from a change in the base of this asset.
        /// </summary>
        public bool UpdatingPropertyFromBase { get; protected set; }

        /// <summary>
        /// Raised after one of the node referenced by the related root node has changed.
        /// </summary>
        public event EventHandler<MemberNodeChangeEventArgs> Changing { add => nodeListener.ValueChanging += value; remove => nodeListener.ValueChanging -= value; }

        /// <summary>
        /// Raised after one of the node referenced by the related root node has changed.
        /// </summary>
        /// <remarks>In addition to the usual <see cref="MemberNodeChangeEventArgs"/> generated by <see cref="IGraphNode"/> this event also gives information about override changes.</remarks>
        /// <seealso cref="AssetMemberNodeChangeEventArgs"/>.
        public event EventHandler<AssetMemberNodeChangeEventArgs> Changed;

        public event EventHandler<ItemChangeEventArgs> ItemChanging { add => nodeListener.ItemChanging += value; remove => nodeListener.ItemChanging -= value; }

        public event EventHandler<AssetItemNodeChangeEventArgs> ItemChanged;

        /// <summary>
        /// Raised when a base content has changed, after updating the related content of this graph.
        /// </summary>
        public Action<INodeChangeEventArgs, IGraphNode> BaseContentChanged;

        /// <summary>
        /// Indicates whether this property graph has been initialized.
        /// </summary>
        protected bool IsInitialized { get; private set; }

        /// <summary>
        /// Indicates whether this property graph is currently being initialized.
        /// </summary>
        protected bool IsInitializing { get; private set; }

        [NotNull]
        private IBaseToDerivedRegistry BaseToDerivedRegistry => baseToDerivedRegistry ?? (baseToDerivedRegistry = CreateBaseToDerivedRegistry());

        /// <summary>
        /// Initializes this property graph.
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
                throw new InvalidOperationException("This property graph has already been initialized.");

            try
            {
                IsInitializing = true;
                RefreshBase();
                ReconcileWithBase();
                FinalizeInitialization();
            }
            finally
            {
                IsInitializing = false;
            }
            IsInitialized = true;
        }

        public virtual void RefreshBase()
        {
            if (AssetItem.Asset.Archetype != null)
            {
                Archetype = Container.TryGetGraph(AssetItem.Asset.Archetype.Id);
                if (Archetype == null)
                    throw new InvalidOperationException($"Unable to find the base [{AssetItem.Asset.Archetype.Location}] of asset [{AssetItem.Location}].");
            }

            // Unlink previously linked nodes
            ClearAllBaseLinks();

            // Link nodes to the new base.
            // Note: in case of composition (prefabs, etc.), even if baseAssetGraph is null, each part (entities, etc.) will discover
            // its own base by itself via the FindTarget method.
            LinkToBase(RootNode, Archetype?.RootNode);
        }

        public virtual void RefreshBase([NotNull] IAssetNode node, IAssetNode baseNode)
        {
            UnlinkFromBase(node);
            LinkToBase(node, baseNode);
        }

        public void ReconcileWithBase()
        {
            ReconcileWithBase(RootNode);
        }

        private void ReconcileWithBase([NotNull] IAssetNode rootNode, Dictionary<IGraphNode, NodeIndex> nodesToReset = null)
        {
            // Two passes: first pass will reconcile almost everything, but skip object reference.
            // The reason is that the target of the reference might not exist yet (might need to be reconcilied)
            var visitor = CreateReconcilierVisitor();
            visitor.Visiting += (node, path) => ReconcileWithBaseNode((IAssetNode)node, false, nodesToReset);
            visitor.Visit(rootNode);
            // Second pass: this one should only reconcile remaining object reference.
            // TODO: these two passes could be improved!
            visitor = CreateReconcilierVisitor();
            visitor.Visiting += (node, path) => ReconcileWithBaseNode((IAssetNode)node, true, nodesToReset);
            visitor.Visit(rootNode);
        }

        /// <summary>
        /// Resets the overrides attached to the given node and its descendants, recursively.
        /// </summary>
        /// <param name="rootNode">The node for which to reset overrides.</param>
        /// <param name="indexToReset">The index of the override to reset in this node, if relevant.</param>
        internal void ResetAllOverridesRecursively([NotNull] IAssetNode rootNode, NodeIndex indexToReset)
        {
            if (rootNode is IAssetMemberNode && indexToReset != NodeIndex.Empty) throw new ArgumentException(@"The index must be empty when invoking this method on a member node.", nameof(indexToReset));

            // We first use a visitor to reset recursively all overrides
            var nodesToReset = new Dictionary<IGraphNode, NodeIndex>();

            IGraphNode visitRoot = null;
            var memberNode = rootNode as AssetMemberNode;
            if (memberNode != null)
            {
                if (indexToReset != NodeIndex.Empty) throw new InvalidOperationException("Expecting empty index when resetting a member node.");
                visitRoot = memberNode.Target;
                nodesToReset.Add(rootNode, indexToReset);
            }

            var objectNode = rootNode as AssetObjectNode;
            if (objectNode != null)
            {
                if (indexToReset != NodeIndex.Empty)
                {
                    nodesToReset.Add(rootNode, indexToReset);
                    visitRoot = objectNode.IsReference ? objectNode.IndexedTarget(indexToReset) : null;
                    objectNode.OverrideItem(false, indexToReset);
                }
                else
                {
                    // Otherwise reset everything
                    visitRoot = objectNode;
                }
            }
            if (visitRoot != null)
            {
                var visitor = new AssetGraphVisitorBase(Definition);
                // If we're in scenario where rootNode is an object node and index is not empty, we might already have the node in the dictionary so let's check this in Visiting
                visitor.Visiting += (node, path) => { if (!nodesToReset.ContainsKey(node)) nodesToReset.Add(node, NodeIndex.Empty); };
                visitor.Visit(rootNode);
            }
            // Then we reconcile (recursively) with the base.
            ReconcileWithBase(rootNode, nodesToReset);
        }

        /// <summary>
        /// Clears all object references targeting the <see cref="IIdentifiable"/> objects corresponding to the given identifiers.
        /// </summary>
        /// <param name="objectIds">The identifiers of the objects for which to clear targeting references.</param>
        public virtual void ClearReferencesToObjects([NotNull] IEnumerable<Guid> objectIds)
        {
            if (objectIds == null) throw new ArgumentNullException(nameof(objectIds));
            var visitor = new ClearObjectReferenceVisitor(Definition, objectIds);
            visitor.Visit(RootNode);
        }

        /// <summary>
        /// Creates an instance of <see cref="GraphVisitorBase"/> that is suited to reconcile properties with the base.
        /// </summary>
        /// <returns>A new instance of <see cref="GraphVisitorBase"/> for reconciliation.</returns>
        [NotNull]
        public virtual GraphVisitorBase CreateReconcilierVisitor()
        {
            return new AssetGraphVisitorBase(Definition);
        }

        public virtual IGraphNode FindTarget(IGraphNode sourceNode, IGraphNode target)
        {
            return target;
        }

        public void PrepareForSave(ILogger logger, [NotNull] AssetItem assetItem)
        {
            if (assetItem.Asset != Asset) throw new ArgumentException($@"The given {nameof(Assets.AssetItem)} does not match the asset associated with this instance", nameof(assetItem));
            AssetCollectionItemIdHelper.GenerateMissingItemIds(assetItem.Asset);
            CollectionItemIdsAnalysis.FixupItemIds(assetItem, logger);
            assetItem.YamlMetadata.AttachMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey, GenerateOverridesForSerialization(RootNode));
            assetItem.YamlMetadata.AttachMetadata(AssetObjectSerializerBackend.ObjectReferencesKey, GenerateObjectReferencesForSerialization(RootNode));
        }

        /// <summary>
        /// When overridden in a derived class, initializes the <see cref="AssetPropertyGraph"/>-derived class.
        /// </summary>
        /// <remarks>
        /// Override <see cref="FinalizeInitialization"/> to implement custom initialization behavior for your property graph.
        /// </remarks>
        protected virtual void FinalizeInitialization()
        {
            // Default implementation does nothing
        }

        protected virtual void OnContentChanged(MemberNodeChangeEventArgs args)
        {
            // Do nothing by default
        }

        protected virtual void OnItemChanged(ItemChangeEventArgs args)
        {
            // Do nothing by default
        }

        [CanBeNull]
        private static IAssetNode ResolveObjectPath([NotNull] IAssetNode rootNode, [NotNull] YamlAssetPath path, out NodeIndex index, out bool resolveOnIndex)
        {
            var currentNode = rootNode;
            index = NodeIndex.Empty;
            resolveOnIndex = false;
            for (var i = 0; i < path.Elements.Count; i++)
            {
                var item = path.Elements[i];
                switch (item.Type)
                {
                    case YamlAssetPath.ElementType.Member:
                    {
                        index = NodeIndex.Empty;
                        resolveOnIndex = false;
                        if (currentNode.IsReference)
                        {
                            var memberNode = currentNode as IMemberNode;
                            if (memberNode == null) throw new InvalidOperationException($"An IMemberNode was expected when processing the path [{path}]");
                            currentNode = (IAssetNode)memberNode.Target;
                        }
                        var objectNode = currentNode as IObjectNode;
                        if (objectNode == null) throw new InvalidOperationException($"An IObjectNode was expected when processing the path [{path}]");
                        var name = item.AsMember();
                        currentNode = (IAssetNode)objectNode.TryGetChild(name);
                        break;
                    }
                    case YamlAssetPath.ElementType.Index:
                    {
                        index = new NodeIndex(item.Value);
                        resolveOnIndex = true;
                        var memberNode = currentNode as IMemberNode;
                        if (memberNode == null) throw new InvalidOperationException($"An IMemberNode was expected when processing the path [{path}]");
                        currentNode = (IAssetNode)memberNode.Target;
                        if (currentNode.IsReference && i < path.Elements.Count - 1)
                        {
                            var objNode = currentNode as IObjectNode;
                            if (objNode == null) throw new InvalidOperationException($"An IObjectNode was expected when processing the path [{path}]");
                            currentNode = (IAssetNode)objNode.IndexedTarget(new NodeIndex(item.Value));
                        }
                        break;
                    }
                    case YamlAssetPath.ElementType.ItemId:
                    {
                        var ids = CollectionItemIdHelper.GetCollectionItemIds(currentNode.Retrieve());
                        var key = ids.GetKey(item.AsItemId());
                        index = new NodeIndex(key);
                        resolveOnIndex = false;
                        var memberNode = currentNode as IMemberNode;
                        if (memberNode == null) throw new InvalidOperationException($"An IMemberNode was expected when processing the path [{path}]");
                        currentNode = (IAssetNode)memberNode.Target;
                        if (currentNode.IsReference && i < path.Elements.Count - 1)
                        {
                            var objNode = currentNode as IObjectNode;
                            if (objNode == null) throw new InvalidOperationException($"An IObjectNode was expected when processing the path [{path}]");
                            currentNode = (IAssetNode)objNode.IndexedTarget(new NodeIndex(key));
                        }
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // Something wrong happen, the node is unreachable.
                if (currentNode == null)
                    return null;
            }

            return currentNode;
        }

        [NotNull]
        public static YamlAssetMetadata<OverrideType> GenerateOverridesForSerialization([NotNull] IGraphNode rootNode)
        {
            if (rootNode == null) throw new ArgumentNullException(nameof(rootNode));

            var visitor = new OverrideTypePathGenerator();
            visitor.Visit(rootNode);
            return visitor.Result;
        }

        public YamlAssetMetadata<Guid> GenerateObjectReferencesForSerialization([NotNull] IGraphNode rootNode)
        {
            if (rootNode == null) throw new ArgumentNullException(nameof(rootNode));

            var visitor = new ObjectReferencePathGenerator(Definition);
            visitor.Visit(rootNode);
            return visitor.Result;
        }

        public static void ApplyOverrides([NotNull] IAssetNode rootNode, YamlAssetMetadata<OverrideType> overrides)
        {
            if (rootNode == null) throw new ArgumentNullException(nameof(rootNode));

            if (overrides == null)
                return;

            foreach (var overrideInfo in overrides)
            {
                var node = ResolveObjectPath(rootNode, overrideInfo.Key, out NodeIndex index, out bool overrideOnKey);
                // The node is unreachable, skip this override.
                if (node == null)
                    continue;

                var memberNode = node as AssetMemberNode;
                memberNode?.SetContentOverride(overrideInfo.Value);

                var objectNode = node as IAssetObjectNode;
                if (objectNode != null)
                {
                    if (!overrideOnKey)
                    {
                        objectNode.OverrideItem(true, index);
                    }
                    else
                    {
                        objectNode.OverrideKey(true, index);
                    }
                }
            }
        }

        [NotNull]
        public List<NodeOverride> ClearAllOverrides()
        {
            // Unregister handlers - must be done first!
            ClearAllBaseLinks();

            var clearedOverrides = new List<NodeOverride>();
            // Clear override and base from node
            if (RootNode != null)
            {
                var visitor = new GraphVisitorBase { SkipRootNode = true };
                visitor.Visiting += (node, path) =>
                {
                    var memberNode = node as AssetMemberNode;
                    var objectNode = node as AssetObjectNode;

                    if (memberNode != null && memberNode.IsContentOverridden())
                    {
                        memberNode.OverrideContent(false);
                        clearedOverrides.Add(new NodeOverride(memberNode, NodeIndex.Empty, OverrideTarget.Content));
                    }
                    if (objectNode != null)
                    {
                        foreach (var index in objectNode.GetOverriddenItemIndices().ToList())
                        {
                            objectNode.OverrideItem(false, index);
                            clearedOverrides.Add(new NodeOverride(objectNode, index, OverrideTarget.Item));
                        }
                        foreach (var index in objectNode.GetOverriddenKeyIndices().ToList())
                        {
                            objectNode.OverrideKey(false, index);
                            clearedOverrides.Add(new NodeOverride(objectNode, index, OverrideTarget.Key));
                        }
                    }
                };
                visitor.Visit(RootNode);
            }

            RefreshBase();

            return clearedOverrides;
        }

        public void RestoreOverrides([NotNull] List<NodeOverride> overridesToRestore, AssetPropertyGraph archetypeBase)
        {
            foreach (var clearedOverride in overridesToRestore)
            {
                // TODO: this will need improvement when adding support for Seal
                switch (clearedOverride.Target)
                {
                    case OverrideTarget.Content:
                        ((AssetMemberNode)clearedOverride.Node).OverrideContent(true);
                        break;
                    case OverrideTarget.Item:
                        ((AssetObjectNode)clearedOverride.Node).OverrideItem(true, clearedOverride.Index);
                        break;
                    case OverrideTarget.Key:
                        ((AssetObjectNode)clearedOverride.Node).OverrideKey(true, clearedOverride.Index);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void LinkToBase([NotNull] IAssetNode sourceRootNode, IAssetNode targetRootNode)
        {
            baseLinker.LinkGraph(sourceRootNode, targetRootNode);
        }

        private void UnlinkFromBase([NotNull] IAssetNode sourceRootNode)
        {
            baseUnlinker.LinkGraph(sourceRootNode, sourceRootNode.BaseNode);
        }

        [NotNull]
        protected virtual IBaseToDerivedRegistry CreateBaseToDerivedRegistry()
        {
            return new AssetBaseToDerivedRegistry(this);
        }

        // TODO: this method is should be called in every scenario of ReconcileWithBase, it is not the case yet.
        protected virtual bool CanUpdate(IAssetNode node, ContentChangeType changeType, NodeIndex index, object value)
        {
            return true;
        }

        protected virtual object CloneValueFromBase(object value, IAssetNode node)
        {
            return CloneFromBase(value);
        }

        /// <summary>
        /// Clones the given object, remove any override information on it, and propagate its id (from <see cref="IdentifiableHelper"/>) to the cloned object.
        /// </summary>
        /// <param name="value">The object to clone.</param>
        /// <returns>A clone of the given object.</returns>
        /// <remarks>If the given object is null, this method returns null.</remarks>
        /// <remarks>If the given object is a content reference, the given object won't be cloned but directly returned.</remarks>
        private static object CloneFromBase(object value)
        {
            if (value == null)
                return null;

            // TODO: check if the cloner is aware of the content type (attached reference) and does not already avoid cloning them.

            // TODO FIXME
            //if (SessionViewModel.Instance.ContentReferenceService.IsContentType(value.GetType()))
            //    return value;

            var result = AssetCloner.Clone<object>(value, AssetClonerFlags.GenerateNewIdsForIdentifiableObjects, out Dictionary<Guid, Guid> remapping);
            return result;
        }

        private void LinkBaseNode([NotNull] IGraphNode currentNode, IGraphNode baseNode)
        {
            var assetNode = (IAssetNodeInternal)currentNode;
            assetNode.SetPropertyGraph(this);
            assetNode.SetBaseNode(baseNode);

            BaseToDerivedRegistry.RegisterBaseToDerived((IAssetNode)baseNode, assetNode);

            if (!baseLinkedNodes.ContainsKey(assetNode))
            {
                EventHandler<MemberNodeChangeEventArgs> valueChange = null;
                EventHandler<ItemChangeEventArgs> itemChange = null;
                if (baseNode != null)
                {
                    if (assetNode.BaseNode is IMemberNode member)
                    {
                        valueChange = (s, e) => OnBaseContentChanged(e, currentNode);
                        member.ValueChanged += valueChange;
                    }

                    if (assetNode.BaseNode is IObjectNode objectNode)
                    {
                        itemChange = (s, e) => OnBaseContentChanged(e, currentNode);
                        objectNode.ItemChanged += itemChange;
                    }
                }
                baseLinkedNodes.Add(assetNode, new NodeChangeHandlers(valueChange, itemChange));
            }
        }

        private void UnlinkBaseNode(IGraphNode currentNode, IGraphNode baseNode)
        {
            var assetNode = (IAssetNode)currentNode;

            if (baseLinkedNodes.TryGetValue(assetNode, out NodeChangeHandlers linkedNode))
            {
                if (assetNode.BaseNode is IMemberNode member)
                {
                    member.ValueChanged -= linkedNode.ValueChange;
                }

                if (assetNode.BaseNode is IObjectNode objectNode)
                {
                    objectNode.ItemChanged -= linkedNode.ItemChange;
                }
            }
            baseLinkedNodes.Remove(assetNode);
        }

        private void ClearAllBaseLinks()
        {
            foreach (var linkedNode in baseLinkedNodes)
            {
                if (linkedNode.Key.BaseNode is IMemberNode member)
                {
                    member.ValueChanged -= linkedNode.Value.ValueChange;
                }

                if (linkedNode.Key.BaseNode is IObjectNode objectNode)
                {
                    objectNode.ItemChanged -= linkedNode.Value.ItemChange;
                }

            }
            baseLinkedNodes.Clear();
        }

        private void AssetContentChanging(object sender, [NotNull] MemberNodeChangeEventArgs e)
        {
            var node = (AssetMemberNode)e.Member;
            var overrideValue = node.GetContentOverride();
            previousOverrides[e.Member] = overrideValue;
        }

        private void AssetContentChanged(object sender, [NotNull] MemberNodeChangeEventArgs e)
        {
            var previousOverride = previousOverrides[e.Member];
            previousOverrides.Remove(e.Member);
            var node = (AssetMemberNode)e.Member;
            var overrideValue = node.GetContentOverride();
            if (node.ResettingOverride)
                overrideValue &= ~OverrideType.New;

            // Link the node that has changed to its base.
            LinkToBase(node, (IAssetNode)node.BaseNode);
            OnContentChanged(e);
            Changed?.Invoke(sender, new AssetMemberNodeChangeEventArgs(e, previousOverride, overrideValue, ItemId.Empty));
        }

        private void AssetItemChanging(object sender, [NotNull] ItemChangeEventArgs e)
        {
            var overrideValue = OverrideType.Base;
            var node = (AssetObjectNode)e.Collection;
            var collection = node.Retrieve();
            // For value change and remove, we store the current override state.
            if (CollectionItemIdHelper.HasCollectionItemIds(collection))
            {
                overrideValue = node.GetItemOverride(e.Index);
                if (e.ChangeType == ContentChangeType.CollectionRemove)
                {
                    // For remove, we also collect the id of the item that will be removed, so we can pass it to the Changed event.
                    var ids = CollectionItemIdHelper.GetCollectionItemIds(collection);
                    ids.TryGet(e.Index.Value, out ItemId itemId);
                    removedItemIds[e.Collection] = itemId;
                }
            }
            previousOverrides[e.Collection] = overrideValue;
        }

        private void AssetItemChanged(object sender, [NotNull] ItemChangeEventArgs e)
        {
            var previousOverride = previousOverrides[e.Collection];
            previousOverrides.Remove(e.Collection);

            var itemId = ItemId.Empty;
            var overrideValue = OverrideType.Base;
            var node = (IAssetObjectNodeInternal)e.Collection;
            var collection = node.Retrieve();
            if (e.ChangeType == ContentChangeType.CollectionUpdate || e.ChangeType == ContentChangeType.CollectionAdd)
            {
                // We're changing an item of a collection. If the collection has identifiable items, retrieve the override status of the item.
                if (CollectionItemIdHelper.HasCollectionItemIds(collection))
                {
                    overrideValue = node.GetItemOverride(e.Index);

                    // Also retrieve the id of the modified item (this should fail only if the collection doesn't have identifiable items)
                    var ids = CollectionItemIdHelper.GetCollectionItemIds(collection);
                    ids.TryGet(e.Index.Value, out itemId);
                }
            }
            else if (e.ChangeType == ContentChangeType.CollectionRemove)
            {
                // When deleting we are always overriding (unless there is no base or non-identifiable items)
                if (CollectionItemIdHelper.HasCollectionItemIds(collection))
                {
                    overrideValue = node.BaseNode != null && !UpdatingPropertyFromBase ? OverrideType.New : OverrideType.Base;
                    itemId = removedItemIds[e.Collection];
                    removedItemIds.Remove(e.Collection);
                }
            }

            // Link the node that has changed to its base.
            // TODO: can link only the changed item instead of the whole collection
            LinkToBase(node, (IAssetNode)node.BaseNode);
            OnItemChanged(e);

            if (node.ResettingOverride)
                overrideValue &= ~OverrideType.New;

            ItemChanged?.Invoke(sender, new AssetItemNodeChangeEventArgs(e, previousOverride, overrideValue, itemId));
        }

        private void OnBaseContentChanged(INodeChangeEventArgs e, IGraphNode node)
        {
            var assetNode = (IAssetNode)node;

            // Ignore base change if propagation is disabled.
            if (!Container.PropagateChangesFromBase)
                return;

            if (node.IsReference && e.OldValue != null && !e.OldValue.GetType().IsValueType)
            {
                var oldNode = (IAssetNode)Container.NodeContainer.GetNode(e.OldValue);
                UnlinkFromBase(oldNode);
            }

            UpdatingPropertyFromBase = true;
            var baseNode = assetNode.BaseNode;
            if (assetNode.BaseNode == null)
                throw new InvalidOperationException("The base is unset for the current node.");
            RefreshBase(assetNode, (IAssetNode)baseNode);
            ReconcileWithBase((IAssetNode)node);
            UpdatingPropertyFromBase = false;

            BaseContentChanged?.Invoke(e, node);
        }

        private void ReconcileWithBaseNode(IAssetNode assetNode, bool reconcileObjectReference, Dictionary<IGraphNode, NodeIndex> nodesToReset)
        {
            var memberNode = assetNode as AssetMemberNode;
            var objectNode = assetNode as IAssetObjectNodeInternal;
            // Non-overridable members should not be reconcilied.
            if (assetNode?.BaseNode == null || !memberNode?.CanOverride == true)
                return;

            var localValue = assetNode.Retrieve();
            var baseValue = assetNode.BaseNode.Retrieve();

            // Reconcile occurs only when the node is not overridden.
            if (memberNode != null)
            {
                if (ShouldReconcileMember(memberNode, reconcileObjectReference, nodesToReset))
                {
                    // If we have no setter, we cannot reconcile this property. Usually it means that the value is already correct (eg. it's an instance of the correct type,
                    // or it's a value that cannot change), so we'll just keep going and try to reconcile the children of this member.
                    if (memberNode.MemberDescriptor.HasSet)
                    {
                        memberNode.ResettingOverride = true;

                        object clonedValue;
                        // Object references
                        if (baseValue is IIdentifiable && Definition.IsMemberTargetObjectReference((IMemberNode)memberNode.BaseNode, memberNode.BaseNode.Retrieve()))
                            clonedValue = BaseToDerivedRegistry.ResolveFromBase(baseValue, memberNode);
                        else
                            clonedValue = CloneValueFromBase(baseValue, assetNode);

                        // Clear override, in case we are resetting it during this reconciliation.
                        memberNode.Update(clonedValue);
                        memberNode.OverrideContent(false);

                        memberNode.ResettingOverride = false;
                    }
                }
            }
            if (objectNode != null)
            {
                var baseNode = (IAssetObjectNodeInternal)assetNode.BaseNode;
                objectNode.ResettingOverride = true;
                // Handle collection and dictionary cases
                if ((assetNode.Descriptor is CollectionDescriptor || assetNode.Descriptor is DictionaryDescriptor) && CollectionItemIdHelper.HasCollectionItemIds(objectNode.Retrieve()))
                {
                    // Items to add and to remove are stored in local collections and processed later, since they might affect indices
                    var itemsToRemove = new List<ItemId>();
                    var itemsToAdd = new SortedList<object, ItemId>(new DefaultKeyComparer());

                    // Check for item present in the instance and absent from the base.
                    foreach (var index in objectNode.Indices)
                    {
                        // Skip overridden items, if they are not marked to be reset.
                        if (objectNode.IsItemOverridden(index) || (nodesToReset != null && nodesToReset.TryGetValue(objectNode, out NodeIndex indexToReset) && indexToReset != index))
                            continue;

                        var itemId = objectNode.IndexToId(index);
                        if (itemId != ItemId.Empty)
                        {
                            // Look if an item with the same id exists in the base.
                            if (!baseNode.HasId(itemId))
                            {
                                // If not, remove this item from the instance.
                                itemsToRemove.Add(itemId);
                            }
                        }
                        else
                        {
                            // This case should not happen, but if we have an empty id due to corrupted data let's just remove the item.
                            itemsToRemove.Add(itemId);
                        }
                    }

                    var ids = CollectionItemIdHelper.GetCollectionItemIds(localValue);
                    // Clean items marked as "override-deleted" that are absent from the base.
                    foreach (var deletedId in ids.DeletedItems.ToList())
                    {
                        if (baseNode.Indices.All(x => baseNode.IndexToId(x) != deletedId))
                        {
                            // We "disconnect" it instead of purely remove it, so it can still be restored by undo/redo
                            objectNode.DisconnectOverriddenDeletedItem(deletedId);
                        }
                    }

                    // Add item present in the base and missing here, and also update items that have different values between base and instance
                    foreach (var index in baseNode.Indices)
                    {
                        var itemId = baseNode.IndexToId(index);
                        // TODO: What should we do if it's empty? It can happen only from corrupted data

                        // Skip items marked as "override-deleted"
                        if (itemId == ItemId.Empty || objectNode.IsItemDeleted(itemId))
                        {
                            // We force-write the item to be deleted, in case it was just "disconnected"
                            objectNode.OverrideDeletedItem(true, itemId);
                            continue;
                        }

                        if (!objectNode.TryIdToIndex(itemId, out NodeIndex localIndex))
                        {
                            // For dictionary, we might have a key collision, if so, we consider that the new value from the base is deleted in the instance.
                            var keyCollision = assetNode.Descriptor is DictionaryDescriptor && (objectNode.ItemReferences?.HasIndex(index) == true || objectNode.Indices.Any(x => index.Equals(x)));
                            // For specific collections (eg. EntityComponentCollection) it might not be possible to add due to other kinds of collisions or invalid value.
                            var itemRejected = !CanUpdate(assetNode, ContentChangeType.CollectionAdd, localIndex, baseNode.Retrieve(index));

                            // We cannot add the item, let's mark it as deleted.
                            if (keyCollision || itemRejected)
                            {
                                objectNode.OverrideDeletedItem(true, itemId);
                            }
                            else
                            {
                                // Add it if the key is available for add
                                itemsToAdd.Add(index.Value, itemId);
                            }
                        }
                        else
                        {
                            // If the item is present in both the instance and the base, check if we need to reconcile the value
                            if (ShouldReconcileItem(objectNode, localIndex, index, reconcileObjectReference, nodesToReset))
                            {
                                object clonedValue;
                                var baseItemValue = objectNode.BaseNode.Retrieve(index);
                                // Object references
                                if (baseItemValue is IIdentifiable && Definition.IsTargetItemObjectReference((IObjectNode)objectNode.BaseNode, index, objectNode.BaseNode.Retrieve(index)))
                                    clonedValue = BaseToDerivedRegistry.ResolveFromBase(baseItemValue, objectNode);
                                else
                                    clonedValue = CloneValueFromBase(baseItemValue, assetNode);

                                objectNode.Update(clonedValue, localIndex);
                                objectNode.OverrideItem(false, localIndex);
                            }
                            // In dictionaries, the keys might be different between the instance and the base. We need to reconcile them too
                            if (objectNode.Descriptor is DictionaryDescriptor && !objectNode.IsKeyOverridden(localIndex))
                            {
                                if (ShouldReconcileIndex(localIndex, index))
                                {
                                    // Reconcile using a move (Remove + Add) of the key-value pair
                                    var clonedIndex = new NodeIndex(CloneValueFromBase(index.Value, assetNode));
                                    var localItemValue = assetNode.Retrieve(localIndex);
                                    objectNode.Remove(localItemValue, localIndex);
                                    objectNode.Add(localItemValue, clonedIndex);
                                    ids[clonedIndex.Value] = itemId;
                                }
                            }
                        }
                    }

                    // Process items marked to be removed
                    foreach (var item in itemsToRemove)
                    {
                        var index = objectNode.IdToIndex(item);
                        var value = assetNode.Retrieve(index);
                        objectNode.Remove(value, index);
                        // We're reconciling, so let's hack the normal behavior of marking the removed item as deleted.
                        objectNode.OverrideDeletedItem(false, item);
                    }

                    // Process items marked to be added
                    foreach (var item in itemsToAdd)
                    {
                        var baseIndex = baseNode.IdToIndex(item.Value);
                        var baseItemValue = baseNode.Retrieve(baseIndex);
                        var clonedValue = CloneValueFromBase(baseItemValue, assetNode);
                        if (assetNode.Descriptor is CollectionDescriptor)
                        {
                            // In a collection, we need to find an index that matches the index on the base to maintain order.
                            // To do so, we iterate from the index in the base to zero.
                            var currentBaseIndex = baseIndex.Int - 1;

                            // Initialize the target index to zero, in case we don't find any better index.
                            var localIndex = new NodeIndex(0);

                            // Find the first item of the base that also exists (in term of id) in the local node, iterating backward (from baseIndex to 0)
                            while (currentBaseIndex >= 0)
                            {
                                // This should not happen since the currentBaseIndex comes from the base.
                                if (!baseNode.TryIndexToId(new NodeIndex(currentBaseIndex), out ItemId baseId))
                                    throw new InvalidOperationException("Cannot find an identifier matching the index in the base collection");

                                // If we have an matching item, we want to insert right after it
                                if (objectNode.TryIdToIndex(baseId, out NodeIndex sameIndexInInstance))
                                {
                                    localIndex = new NodeIndex(sameIndexInInstance.Int + 1);
                                    break;
                                }
                                currentBaseIndex--;
                            }

                            objectNode.Restore(clonedValue, localIndex, item.Value);
                        }
                        else
                        {
                            // This case is for dictionary. Key collisions have already been handle at that point so we can directly do the add without further checks.
                            objectNode.Restore(clonedValue, baseIndex, item.Value);
                        }
                    }
                }

                objectNode.ResettingOverride = false;
            }
        }

        private bool ShouldReconcileMember([NotNull] IAssetMemberNode memberNode, bool reconcileObjectReference, Dictionary<IGraphNode, NodeIndex> nodesToReset)
        {
            var localValue = memberNode.Retrieve();
            var baseValue = memberNode.BaseNode.Retrieve();

            // First rule: if the node is to be reset, we should reconcile.
            var index = NodeIndex.Empty;
            if (nodesToReset?.TryGetValue(memberNode, out index) ?? false)
            {
                return index == NodeIndex.Empty;
            }

            // Second rule: if the node is overridden, we shouldn't reconcile.
            if (memberNode.IsContentOverridden())
                return false;

            // Object references
            if (baseValue is IIdentifiable && Definition.IsMemberTargetObjectReference((IMemberNode)memberNode.BaseNode, baseValue))
            {
                if (!reconcileObjectReference)
                    return false;

                var derivedTarget = BaseToDerivedRegistry.ResolveFromBase(baseValue, memberNode);
                return !Equals(localValue, derivedTarget);
            }

            // Non value type and non primitive types
            if (memberNode.IsReference || memberNode.BaseNode.IsReference)
            {
                return localValue?.GetType() != baseValue?.GetType();
            }

            // Content reference (note: they are not treated as reference but as primitive type)
            if (AssetRegistry.IsContentType(localValue?.GetType()) || AssetRegistry.IsContentType(baseValue?.GetType()))
            {
                var localRef = AttachedReferenceManager.GetAttachedReference(localValue);
                var baseRef = AttachedReferenceManager.GetAttachedReference(baseValue);
                return localRef?.Id != baseRef?.Id || localRef?.Url != baseRef?.Url;
            }

            // Value type, we check for equality
            return !Equals(localValue, baseValue);
        }

        private bool ShouldReconcileItem([NotNull] IAssetObjectNode node, NodeIndex localIndex, NodeIndex baseIndex, bool reconcileObjectReference, Dictionary<IGraphNode, NodeIndex> nodesToReset)
        {
            var localValue = node.Retrieve(localIndex);
            var baseValue = node.BaseNode.Retrieve(baseIndex);

            // First rule: if the node is to be reset, we should reconcile.
            var index = NodeIndex.Empty;
            if (nodesToReset?.TryGetValue(node, out index) ?? false)
            {
                return index == NodeIndex.Empty || index == localIndex;
            }

            // Second rule: if the node is overridden, we shouldn't reconcile.
            if (node.IsItemOverridden(localIndex))
                return false;

            // Object references
            if (baseValue is IIdentifiable && Definition.IsTargetItemObjectReference((IObjectNode)node.BaseNode, baseIndex, node.BaseNode.Retrieve(baseIndex)))
            {
                if (!reconcileObjectReference)
                    return false;

                var derivedTarget = BaseToDerivedRegistry.ResolveFromBase(baseValue, node);
                return !Equals(localValue, derivedTarget);
            }

            // Non value type and non primitive types
            if (node.IsReference || node.BaseNode.IsReference)
            {
                return localValue?.GetType() != baseValue?.GetType();
            }

            // Content reference (note: they are not treated as reference but as primitive type)
            if (AssetRegistry.IsContentType(localValue?.GetType()) || AssetRegistry.IsContentType(baseValue?.GetType()))
            {
                var localRef = AttachedReferenceManager.GetAttachedReference(localValue);
                var baseRef = AttachedReferenceManager.GetAttachedReference(baseValue);
                return localRef?.Id != baseRef?.Id || localRef?.Url != baseRef?.Url;
            }

            // Value type, we check for equality
            return !Equals(localValue, baseValue);
        }

        private static bool ShouldReconcileIndex(NodeIndex localIndex, NodeIndex baseIndex)
        {
            return !Equals(localIndex, baseIndex);
        }
    }
}
