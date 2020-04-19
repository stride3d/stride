// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics.Contracts;
using Stride.Core.Annotations;
using Stride.Core.Extensions;

namespace Stride.Core.Quantum
{
    /// <summary>
    /// An object representing a single accessor of the value of a node, or one if its item.
    /// </summary>
    public struct NodeAccessor
    {
        /// <summary>
        /// The node of the accessor.
        /// </summary>
        public readonly IGraphNode Node;
        /// <summary>
        /// The index of the accessor.
        /// </summary>
        public readonly NodeIndex Index;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeAccessor"/> structure.
        /// </summary>
        /// <param name="node">The target node of this accessor.</param>
        /// <param name="index">The index of the target item if this accessor target an item. <see cref="Quantum.NodeIndex.Empty"/> otherwise.</param>
        public NodeAccessor([NotNull] IGraphNode node, NodeIndex index)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (node is IMemberNode && !index.IsEmpty) throw new ArgumentException($"Cannot create an accessor for an {nameof(IMemberNode)} that use a non-empty index.");
            Node = node;
            Index = index;
        }

        /// <summary>
        /// Gets whether this accessor is targeting a member of an object.
        /// </summary>
        public bool IsMember => Node is IMemberNode;

        /// <summary>
        /// Gets whether this accessor is targeting an item of a collection.
        /// </summary>
        public bool IsItem => Node is IObjectNode && !Index.IsEmpty;

        /// <summary>
        /// Retrieves the value backed by this accessor.
        /// </summary>
        /// <returns>The value backed by this accessor.</returns>
        [Pure]
        public object RetrieveValue() => Node.Retrieve(Index);

        /// <summary>
        /// Updates the value backed by this accessor.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        public void UpdateValue(object value)
        {
            if (Index != NodeIndex.Empty)
            {
                ((IObjectNode)Node).Update(value, Index);
            }
            else
            {
                ((IMemberNode)Node).Update(value);
            }
        }

        /// <summary>
        /// Indicates whether this accessor can accept a value of the given type to update the targeted node.
        /// </summary>
        /// <param name="type">The type to evaluate.</param>
        /// <returns>True if this type is accepted, false otherwise.</returns>
        [Pure]
        public bool AcceptType(Type type)
        {
            return Node.Descriptor.GetInnerCollectionType().IsAssignableFrom(type);
        }

        /// <summary>
        /// Indicates whether this accessor can accept the given value to update the targeted node.
        /// </summary>
        /// <param name="value">The value to evaluate.</param>
        /// <returns>True if the value is accepted, false otherwise.</returns>
        [Pure]
        public bool AcceptValue(object value)
        {
            return value == null ? !Node.Descriptor.GetInnerCollectionType().IsValueType : Node.Descriptor.GetInnerCollectionType().IsInstanceOfType(value);
        }
    }
}
