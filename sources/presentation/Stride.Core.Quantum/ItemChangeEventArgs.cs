// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;

namespace Stride.Core.Quantum
{
    /// <summary>
    /// Arguments of the <see cref="IObjectNode.ItemChanging"/> and <see cref="IObjectNode.ItemChanged"/> events.
    /// </summary>
    public class ItemChangeEventArgs : EventArgs, INodeChangeEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemChangeEventArgs"/> class.
        /// </summary>
        /// <param name="node">The node that has changed.</param>
        /// <param name="index">The index in the member where the change occurred.</param>
        /// <param name="changeType">The type of change that occurred.</param>
        /// <param name="oldValue">The old value of the item that has changed.</param>
        /// <param name="newValue">The new value of the item that has changed.</param>
        public ItemChangeEventArgs([NotNull] IObjectNode node, NodeIndex index, ContentChangeType changeType, object oldValue, object newValue)
        {
            Collection = node;
            Index = index;
            ChangeType = changeType;
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Gets the object node of the collection that has changed.
        /// </summary>
        [NotNull]
        public IObjectNode Collection { get; }

        /// <summary>
        /// Gets the index where the change occurred.
        /// </summary>
        public NodeIndex Index { get; }

        /// <summary>
        /// The type of change.
        /// </summary>
        public ContentChangeType ChangeType { get; }

        /// <summary>
        /// Gets the old value of the member or the item of the member that has changed.
        /// </summary>
        public object OldValue { get; }

        /// <summary>
        /// Gets the new value of the member or the item of the member that has changed.
        /// </summary>
        public object NewValue { get; }

        /// <inheritdoc/>
        IGraphNode INodeChangeEventArgs.Node => Collection;
    }
}
