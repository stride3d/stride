// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Core.Annotations;
using Xenko.Core.Reflection;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Quantum
{
    public interface IAssetObjectNode : IAssetNode, IObjectNode
    {
        [NotNull]
        new IAssetMemberNode this[string name] { get; }

        new IAssetObjectNode IndexedTarget(NodeIndex index);

        void OverrideItem(bool isOverridden, NodeIndex index);

        void OverrideKey(bool isOverridden, NodeIndex index);

        void OverrideDeletedItem(bool isOverridden, ItemId deletedId);

        bool IsItemDeleted(ItemId itemId);

        void Restore(object restoredItem, ItemId id);

        void Restore(object restoredItem, NodeIndex index, ItemId id);

        void RemoveAndDiscard(object item, NodeIndex itemIndex, ItemId id);

        bool IsItemInherited(NodeIndex index);

        bool IsKeyInherited(NodeIndex index);

        bool IsItemOverridden(NodeIndex index);

        bool IsItemOverriddenDeleted(ItemId id);

        bool IsKeyOverridden(NodeIndex index);

        ItemId IndexToId(NodeIndex index);

        bool TryIndexToId(NodeIndex index, out ItemId id);

        bool HasId(ItemId id);

        NodeIndex IdToIndex(ItemId id);

        bool TryIdToIndex(ItemId id, out NodeIndex index);

        /// <summary>
        /// Resets the overrides attached to this node at a specific index and to its descendants, recursively.
        /// </summary>
        /// <param name="indexToReset">The index of the override to reset in this node.</param>
        void ResetOverrideRecursively(NodeIndex indexToReset);

        IEnumerable<NodeIndex> GetOverriddenItemIndices();

        IEnumerable<NodeIndex> GetOverriddenKeyIndices();
    }
}
