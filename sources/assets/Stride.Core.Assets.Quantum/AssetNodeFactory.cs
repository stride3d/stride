// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum.Internal;
using Stride.Core.Reflection;
using Stride.Core.Quantum;
using Stride.Core.Quantum.References;

namespace Stride.Core.Assets.Quantum;

/// <summary>
/// An implementation of <see cref="INodeFactory"/> that creates node capable of storing additional metadata, such as override information, connection
/// to a base node or any other node, etc.
/// </summary>
public class AssetNodeFactory : INodeFactory
{
    /// <inheritdoc/>
    public IObjectNode CreateObjectNode(INodeBuilder nodeBuilder, Guid guid, object obj, ITypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(nodeBuilder);
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(descriptor);
        var reference = nodeBuilder.CreateReferenceForNode(descriptor.Type, obj, false) as ReferenceEnumerable;
        return new AssetObjectNode(nodeBuilder, obj, guid, descriptor, reference);
    }

    /// <inheritdoc/>
    public IObjectNode CreateBoxedNode(INodeBuilder nodeBuilder, Guid guid, object structure, ITypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(nodeBuilder);
        ArgumentNullException.ThrowIfNull(structure);
        ArgumentNullException.ThrowIfNull(descriptor);
        return new AssetBoxedNode(nodeBuilder, structure, guid, descriptor);
    }

    /// <inheritdoc/>
    public IMemberNode CreateMemberNode(INodeBuilder nodeBuilder, Guid guid, IObjectNode parent, IMemberDescriptor member, object? value)
    {
        ArgumentNullException.ThrowIfNull(nodeBuilder);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(member);
        var reference = nodeBuilder.CreateReferenceForNode(member.Type, value, true);
        return new AssetMemberNode(nodeBuilder, guid, parent, member, reference);
    }
}
