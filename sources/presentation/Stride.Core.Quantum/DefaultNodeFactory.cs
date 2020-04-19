// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Reflection;
using Stride.Core.Quantum.References;

namespace Stride.Core.Quantum
{
    /// <summary>
    /// This class is an implementation of the <see cref="INodeFactory"/> interface that can construct <see cref="ObjectNode"/>, <see cref="BoxedNode"/>
    /// and <see cref="MemberNode"/> instances.
    /// </summary>
    public class DefaultNodeFactory : INodeFactory
    {
        /// <inheritdoc/>
        public virtual IObjectNode CreateObjectNode(INodeBuilder nodeBuilder, Guid guid, object obj, ITypeDescriptor descriptor)
        {
            if (nodeBuilder == null) throw new ArgumentNullException(nameof(nodeBuilder));
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            var reference = nodeBuilder.CreateReferenceForNode(descriptor.Type, obj, false) as ReferenceEnumerable;
            return new ObjectNode(nodeBuilder, obj, guid, descriptor, reference);
        }

        /// <inheritdoc/>
        public virtual IObjectNode CreateBoxedNode(INodeBuilder nodeBuilder, Guid guid, object structure, ITypeDescriptor descriptor)
        {
            if (nodeBuilder == null) throw new ArgumentNullException(nameof(nodeBuilder));
            if (structure == null) throw new ArgumentNullException(nameof(structure));
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            return new BoxedNode(nodeBuilder, structure, guid, descriptor);
        }

        /// <inheritdoc/>
        public virtual IMemberNode CreateMemberNode(INodeBuilder nodeBuilder, Guid guid, IObjectNode parent, IMemberDescriptor member, object value)
        {
            if (nodeBuilder == null) throw new ArgumentNullException(nameof(nodeBuilder));
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (member == null) throw new ArgumentNullException(nameof(member));
            var reference = nodeBuilder.CreateReferenceForNode(member.Type, value, true);
            return new MemberNode(nodeBuilder, guid, parent, member, reference);
        }
    }
}
