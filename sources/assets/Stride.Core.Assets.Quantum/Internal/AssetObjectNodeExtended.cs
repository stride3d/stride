// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Reflection;
using Stride.Core.Quantum;
using System.Diagnostics.CodeAnalysis;

namespace Stride.Core.Assets.Quantum.Internal;

internal struct AssetObjectNodeExtended
{
    private readonly IAssetObjectNodeInternal node;
    private readonly Dictionary<string, IGraphNode> contents;
    private readonly Dictionary<ItemId, OverrideType> itemOverrides;
    private readonly Dictionary<ItemId, OverrideType> keyOverrides;
    private readonly HashSet<ItemId> disconnectedDeletedIds;
    private CollectionItemIdentifiers? collectionItemIdentifiers;
    private ItemId restoringId;

    public AssetObjectNodeExtended(IAssetObjectNodeInternal node)
    {
        this.node = node;
        contents = [];
        itemOverrides = [];
        keyOverrides = [];
        disconnectedDeletedIds = [];
        collectionItemIdentifiers = null;
        restoringId = ItemId.Empty;
        PropertyGraph = null;
        BaseNode = null;
        ResettingOverride = false;
    }

    public AssetPropertyGraph? PropertyGraph { get; private set; }

    public IGraphNode? BaseNode { get; private set; }

    internal bool ResettingOverride { get; set; }

    public readonly void SetContent(string key, IGraphNode node)
    {
        contents[key] = node;
    }

    public readonly IGraphNode? GetContent(string key)
    {
        contents.TryGetValue(key, out var node);
        return node;
    }

    /// <inheritdoc/>
    public void ResetOverrideRecursively(NodeIndex indexToReset)
    {
        OverrideItem(false, indexToReset);
        PropertyGraph?.ResetAllOverridesRecursively(node, indexToReset);
    }

    public void OverrideItem(bool isOverridden, NodeIndex index)
    {
        node.NotifyOverrideChanging();
        var id = IndexToId(index);
        SetOverride(isOverridden ? OverrideType.New : OverrideType.Base, id, itemOverrides);
        node.NotifyOverrideChanged();
    }

    public void OverrideKey(bool isOverridden, NodeIndex index)
    {
        node.NotifyOverrideChanging();
        var id = IndexToId(index);
        SetOverride(isOverridden ? OverrideType.New : OverrideType.Base, id, keyOverrides);
        node.NotifyOverrideChanged();
    }

    public void OverrideDeletedItem(bool isOverridden, ItemId deletedId)
    {
        if (TryGetCollectionItemIds(node.Retrieve(), out var ids))
        {
            node.NotifyOverrideChanging();
            SetOverride(isOverridden ? OverrideType.New : OverrideType.Base, deletedId, itemOverrides);
            if (isOverridden)
            {
                ids.MarkAsDeleted(deletedId);
                disconnectedDeletedIds.Remove(deletedId);
            }
            else
            {
                ids.UnmarkAsDeleted(deletedId);
            }
            node.NotifyOverrideChanged();
        }
    }

    public void DisconnectOverriddenDeletedItem(ItemId deletedId)
    {
        disconnectedDeletedIds.Add(deletedId);
        OverrideDeletedItem(false, deletedId);
    }

    public bool IsItemDeleted(ItemId itemId)
    {
        if (disconnectedDeletedIds.Contains(itemId))
            return true;

        var collection = node.Retrieve();
        if (!TryGetCollectionItemIds(collection, out var ids))
            throw new InvalidOperationException("No Collection item identifier associated to the given collection.");
        return ids.IsDeleted(itemId);
    }

    private bool TryGetCollectionItemIds(object? instance, [MaybeNullWhen(false)] out CollectionItemIdentifiers itemIds)
    {
        if (collectionItemIdentifiers is not null)
        {
            itemIds = collectionItemIdentifiers;
            return true;
        }

        var result = CollectionItemIdHelper.TryGetCollectionItemIds(instance, out collectionItemIdentifiers);
        itemIds = collectionItemIdentifiers;
        return result;
    }

    public void Restore(object restoredItem, ItemId id)
    {
        if (TryGetCollectionItemIds(node.Retrieve(), out var ids))
        {
            // Remove the item from deleted ids if it was here.
            ids.UnmarkAsDeleted(id);
        }
        // Actually restore the item.
        node.Add(restoredItem);
    }

    public void Restore(object restoredItem, NodeIndex index, ItemId id)
    {
        restoringId = id;
        node.Add(restoredItem, index);
        restoringId = ItemId.Empty;
        if (TryGetCollectionItemIds(node.Retrieve(), out var ids))
        {
            // Remove the item from deleted ids if it was here.
            ids.UnmarkAsDeleted(id);
        }
    }

    public void RemoveAndDiscard(object item, NodeIndex itemIndex, ItemId id)
    {
        node.Remove(item, itemIndex);
        if (TryGetCollectionItemIds(node.Retrieve(), out var ids))
        {
            // Remove the item from deleted ids if it was here.
            ids.UnmarkAsDeleted(id);
        }
    }

    public OverrideType GetItemOverride(NodeIndex index)
    {
        var result = OverrideType.Base;
        if (!TryIndexToId(index, out var id))
            return result;
        return itemOverrides.TryGetValue(id, out result) ? result : OverrideType.Base;
    }

    public OverrideType GetKeyOverride(NodeIndex index)
    {
        var result = OverrideType.Base;
        if (!TryIndexToId(index, out var id))
            return result;
        return keyOverrides.TryGetValue(id, out result) ? result : OverrideType.Base;
    }

    public bool IsItemInherited(NodeIndex index)
    {
        return BaseNode is not null && !IsItemOverridden(index);
    }

    public bool IsKeyInherited(NodeIndex index)
    {
        return BaseNode is not null && !IsKeyOverridden(index);
    }

    public bool IsItemOverridden(NodeIndex index)
    {
        if (!TryIndexToId(index, out var id))
            return false;
        return itemOverrides.TryGetValue(id, out var result) && (result & OverrideType.New) == OverrideType.New;
    }

    public bool IsItemOverriddenDeleted(ItemId id)
    {
        return IsItemDeleted(id) && itemOverrides.TryGetValue(id, out var result) && (result & OverrideType.New) == OverrideType.New;
    }

    public bool IsKeyOverridden(NodeIndex index)
    {
        if (!TryIndexToId(index, out var id))
            return false;
        return keyOverrides.TryGetValue(id, out var result) && (result & OverrideType.New) == OverrideType.New;
    }

    public IEnumerable<NodeIndex> GetOverriddenItemIndices()
    {
        if (BaseNode is null)
            yield break;

        var collection = node.Retrieve();
        if (!TryGetCollectionItemIds(collection, out var ids))
            yield break;

        foreach (var flags in itemOverrides)
        {
            if ((flags.Value & OverrideType.New) == OverrideType.New)
            {
                // If the override is a deleted item, there's no matching index to return.
                if (ids.IsDeleted(flags.Key))
                    continue;

                yield return IdToIndex(flags.Key);
            }
        }
    }

    public IEnumerable<NodeIndex> GetOverriddenKeyIndices()
    {
        if (BaseNode is null)
            yield break;

        var collection = node.Retrieve();
        if (!TryGetCollectionItemIds(collection, out var ids))
            yield break;

        foreach (var flags in keyOverrides)
        {
            if ((flags.Value & OverrideType.New) == OverrideType.New)
            {
                // If the override is a deleted item, there's no matching index to return.
                if (ids.IsDeleted(flags.Key))
                    continue;

                yield return IdToIndex(flags.Key);
            }
        }
    }

    public bool HasId(ItemId id)
    {
        return TryIdToIndex(id, out var _);
    }

    public NodeIndex IdToIndex(ItemId id)
    {
        if (!TryIdToIndex(id, out var index)) throw new InvalidOperationException("No Collection item identifier associated to the given collection.");
        return index;
    }

    public bool TryIdToIndex(ItemId id, out NodeIndex index)
    {
        if (id == ItemId.Empty)
        {
            index = NodeIndex.Empty;
            return true;
        }

        var collection = node.Retrieve();
        if (TryGetCollectionItemIds(collection, out var ids))
        {
            index = new NodeIndex(ids.GetKey(id));
            return !index.IsEmpty;
        }
        index = NodeIndex.Empty;
        return false;
    }

    public ItemId IndexToId(NodeIndex index)
    {
        if (!TryIndexToId(index, out var id)) throw new InvalidOperationException("No Collection item identifier associated to the given collection.");
        return id;
    }

    public bool TryIndexToId(NodeIndex index, out ItemId id)
    {
        if (index == NodeIndex.Empty)
        {
            id = ItemId.Empty;
            return true;
        }

        var collection = node.Retrieve();
        if (TryGetCollectionItemIds(collection, out var ids))
        {
            return ids.TryGet(index.Value, out id);
        }
        id = ItemId.Empty;
        return false;
    }

    internal void OnItemChanged(object? sender, ItemChangeEventArgs e)
    {
        var value = node.Retrieve();

        if (!CollectionItemIdHelper.HasCollectionItemIds(value))
            return;

        // Make sure that we have item ids everywhere we're supposed to.
        AssetCollectionItemIdHelper.GenerateMissingItemIds(e.Collection.Retrieve());

        // Clear the cached item identifier collection.
        collectionItemIdentifiers = null;

        // Create new ids for collection items
        var baseNode = (AssetObjectNode?)BaseNode;
        var removedId = ItemId.Empty;
        var isOverriding = baseNode is not null && !PropertyGraph.UpdatingPropertyFromBase;
        var itemIds = CollectionItemIdHelper.GetCollectionItemIds(value);
        var collectionDescriptor = node.Descriptor as CollectionDescriptor;
        switch (e.ChangeType)
        {
            case ContentChangeType.CollectionUpdate:
            {
                if (collectionDescriptor is SetDescriptor setDescriptor)
                {
                    itemIds.Delete(e.OldValue);
                    object? newValue = e.NewValue;
                    if (newValue is null && !setDescriptor.ElementType.IsNullable())
                    {
                        newValue = setDescriptor.ElementType.Default();
                    }
                    if (!itemIds.ContainsKey(newValue))
                    {
                        var itemId = restoringId != ItemId.Empty ? restoringId : ItemId.New();
                        itemIds[newValue] = itemId;
                    }
                }
                break;
            }
            case ContentChangeType.CollectionAdd:
            {
                // Compute the id we will add for this item
                var itemId = restoringId != ItemId.Empty ? restoringId : ItemId.New();
                // Add the id to the proper location (insert or add)
                if (collectionDescriptor is not null && collectionDescriptor.Category != DescriptorCategory.Set)
                {
                    if (e.Index == NodeIndex.Empty)
                        throw new InvalidOperationException("An item has been added to a collection that does not have a predictable Add. Consider using NonIdentifiableCollectionItemsAttribute on this collection.");

                    itemIds.Insert(e.Index.Int, itemId);
                }
                else
                {
                    itemIds[e.Index.Value] = itemId;
                }
                break;
            }
            case ContentChangeType.CollectionRemove:
            {
                var itemId = itemIds[e.Index.Value];
                // update isOverriding, it should be true only if the item being removed exist in the base.
                isOverriding = isOverriding && (baseNode?.HasId(itemId) ?? false);
                if (collectionDescriptor is not null && collectionDescriptor.Category != DescriptorCategory.Set)
                {
                    removedId = itemIds.DeleteAndShift(e.Index.Int, isOverriding);
                }
                else
                {
                    removedId = itemIds.Delete(e.Index.Value, isOverriding);
                }
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        // Don't update override if propagation from base is disabled.
        if (PropertyGraph?.Container?.PropagateChangesFromBase == false)
            return;

        // Mark it as New if it does not come from the base
        if (!ResettingOverride)
        {
            if (e.ChangeType != ContentChangeType.CollectionRemove && isOverriding)
            {
                // If it's an add or an updating, there is no scenario where we can be "un-overriding" without ResettingOverride, so we pass true.
                OverrideItem(true, e.Index);
            }
            else if (e.ChangeType == ContentChangeType.CollectionRemove)
            {
                // If it's a delete, it could be an item that was previously added as an override, and that should not be marked as "deleted-overridden", so we pass isOverriding
                OverrideDeletedItem(isOverriding, removedId);
            }
        }
    }

    private static void SetOverride(OverrideType overrideType, ItemId id, Dictionary<ItemId, OverrideType> dictionary)
    {
        if (overrideType == OverrideType.Base)
        {
            dictionary.Remove(id);
        }
        else
        {
            dictionary[id] = overrideType;
        }
    }

    public void SetPropertyGraph(AssetPropertyGraph assetPropertyGraph)
    {
        PropertyGraph = assetPropertyGraph ?? throw new ArgumentNullException(nameof(assetPropertyGraph));
    }

    public void SetBaseContent(IGraphNode baseNode)
    {
        BaseNode = baseNode;
    }
}
