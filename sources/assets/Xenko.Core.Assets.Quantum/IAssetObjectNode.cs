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

        new IAssetObjectNode IndexedTarget(Index index);

        void OverrideItem(bool isOverridden, Index index);

        void OverrideKey(bool isOverridden, Index index);

        void OverrideDeletedItem(bool isOverridden, ItemId deletedId);

        bool IsItemDeleted(ItemId itemId);

        void Restore(object restoredItem, ItemId id);

        void Restore(object restoredItem, Index index, ItemId id);

        void RemoveAndDiscard(object item, Index itemIndex, ItemId id);

        bool IsItemInherited(Index index);

        bool IsKeyInherited(Index index);

        bool IsItemOverridden(Index index);

        bool IsItemOverriddenDeleted(ItemId id);

        bool IsKeyOverridden(Index index);

        ItemId IndexToId(Index index);

        bool TryIndexToId(Index index, out ItemId id);

        bool HasId(ItemId id);

        Index IdToIndex(ItemId id);

        bool TryIdToIndex(ItemId id, out Index index);

        /// <summary>
        /// Resets the overrides attached to this node at a specific index and to its descendants, recursively.
        /// </summary>
        /// <param name="indexToReset">The index of the override to reset in this node.</param>
        void ResetOverrideRecursively(Index indexToReset);

        IEnumerable<Index> GetOverriddenItemIndices();

        IEnumerable<Index> GetOverriddenKeyIndices();
    }
}
