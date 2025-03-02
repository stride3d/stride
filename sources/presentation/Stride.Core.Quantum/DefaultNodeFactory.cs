// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Reflection;
using Stride.Core.Quantum.References;

namespace Stride.Core.Quantum;

/// <summary>
/// This class is an implementation of the <see cref="INodeFactory"/> interface that can construct <see cref="ObjectNode"/>, <see cref="BoxedNode"/>
/// and <see cref="MemberNode"/> instances.
/// </summary>
public class DefaultNodeFactory : INodeFactory
{
    /// <inheritdoc/>
    public virtual IObjectNode CreateObjectNode(INodeBuilder nodeBuilder, Guid guid, object obj, ITypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(nodeBuilder);
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(descriptor);
        var reference = nodeBuilder.CreateReferenceForNode(descriptor.Type, obj, false) as ReferenceEnumerable;
        return new ObjectNode(nodeBuilder, obj, guid, descriptor, reference);
    }

    /// <inheritdoc/>
    public virtual IObjectNode CreateBoxedNode(INodeBuilder nodeBuilder, Guid guid, object structure, ITypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(nodeBuilder);
        ArgumentNullException.ThrowIfNull(structure);
        ArgumentNullException.ThrowIfNull(descriptor);
        return new BoxedNode(nodeBuilder, structure, guid, descriptor);
    }

    /// <inheritdoc/>
    public virtual IMemberNode CreateMemberNode(INodeBuilder nodeBuilder, Guid guid, IObjectNode parent, IMemberDescriptor member, object? value)
    {
        ArgumentNullException.ThrowIfNull(nodeBuilder);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(member);
        var reference = nodeBuilder.CreateReferenceForNode(member.Type, value, true);
        return new MemberNode(nodeBuilder, guid, parent, member, reference);
    }
}
