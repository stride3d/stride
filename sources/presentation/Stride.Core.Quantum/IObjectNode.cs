// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Quantum.References;

namespace Stride.Core.Quantum
{
    public interface IObjectNode : IGraphNode, INotifyNodeItemChange
    {
        /// <summary>
        /// Gets the member corresponding to the given name.
        /// </summary>
        /// <param name="name">The name of the member to retrieve.</param>
        /// <returns>The member corresponding to the given name.</returns>
        /// <exception cref="KeyNotFoundException">This node has no member that matches the given name.</exception>
        IMemberNode this[string name] { get; }

        /// <summary>
        /// Gets the collection of members of this node.
        /// </summary>
        [NotNull]
        IReadOnlyCollection<IMemberNode> Members { get; }

        ReferenceEnumerable ItemReferences { get; }

        /// <summary>
        /// Gets all the indices in the value of this content, if it is a collection. Otherwise, this property returns null.
        /// </summary>
        IEnumerable<NodeIndex> Indices { get; }

        bool IsEnumerable { get; }

        /// <summary>
        /// Gets the target of this node corresponding to the given index, if this node contains a sequence of references to some other nodes.
        /// </summary>
        /// <exception cref="InvalidOperationException">The node does not contain a sequence of references to some other nodes.</exception>
        /// <exception cref="ArgumentException">The index is empty.</exception>
        /// <exception cref="KeyNotFoundException">The index does not exist.</exception>
        IObjectNode IndexedTarget(NodeIndex index);

        /// <summary>
        /// Attempts to retrieve the child node of this <see cref="IGraphNode"/> that matches the given name.
        /// </summary>
        /// <param name="name">The name of the child to retrieve.</param>
        /// <returns>The child node that matches the given name, or <c>null</c> if no child matches.</returns>
        IMemberNode TryGetChild(string name);

        /// <summary>
        /// Updates the value of this content at the given index with the given value.
        /// </summary>
        /// <param name="newValue">The new value to set.</param>
        /// <param name="index">The index where to update the value.</param>
        void Update(object newValue, NodeIndex index);

        /// <summary>
        /// Adds a new item to this content, assuming the content is a collection.
        /// </summary>
        /// <param name="newItem">The new item to add to the collection.</param>
        void Add(object newItem);

        /// <summary>
        /// Adds a new item at the given index to this content, assuming the content is a collection.
        /// </summary>
        /// <param name="newItem">The new item to add to the collection.</param>
        /// <param name="itemIndex">The index at which the new item must be added.</param>
        void Add(object newItem, NodeIndex itemIndex);

        /// <summary>
        /// Removes an item from this content, assuming the content is a collection.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <param name="itemIndex">The index from which the item must be removed.</param>
        void Remove(object item, NodeIndex itemIndex);
    }
}
