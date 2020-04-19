// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Quantum.References;

namespace Stride.Core.Quantum
{
    public interface IMemberNode : IGraphNode, INotifyNodeValueChange
    {
        /// <summary>
        /// Gets the member name.
        /// </summary>
        [NotNull]
        string Name { get; }

        /// <summary>
        /// Gets the <see cref="IObjectNode"/> containing this member node.
        /// </summary>
        [NotNull]
        IObjectNode Parent { get; }

        ObjectReference TargetReference { get; }

        /// <summary>
        /// Gets the target of this node, if this node contains a reference to another node.
        /// </summary>
        /// <exception cref="InvalidOperationException">The node does not contain a reference to another node.</exception>
        [CanBeNull]
        IObjectNode Target { get; }

        /// <summary>
        /// Gets the member descriptor corresponding to this member node.
        /// </summary>
        [NotNull]
        IMemberDescriptor MemberDescriptor { get; }

        /// <summary>
        /// Updates the value of this content with the given value.
        /// </summary>
        /// <param name="newValue">The new value to set.</param>
        void Update(object newValue);
    }
}
