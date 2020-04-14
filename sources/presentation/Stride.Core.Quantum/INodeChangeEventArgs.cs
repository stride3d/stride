// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Annotations;

namespace Stride.Core.Quantum
{
    /// <summary>
    /// A global interface representing any kind of change in a node.
    /// </summary>
    public interface INodeChangeEventArgs
    {
        /// <summary>
        /// The node that has changed.
        /// </summary>
        [NotNull]
        IGraphNode Node { get; }

        /// <summary>
        /// The type of change.
        /// </summary>
        ContentChangeType ChangeType { get; }

        /// <summary>
        /// The old value of the node or the item of the node that has changed.
        /// </summary>
        object OldValue { get; }

        /// <summary>
        /// The new value of the node or the item of the node that has changed.
        /// </summary>
        object NewValue { get; }
    }
}
