// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Reflection;

namespace Stride.Core.Quantum;

public interface IGraphNode1
{
    Guid Guid { get; }
    Type Type { get; }
    ITypeDescriptor Descriptor { get; }
    bool IsReference { get; }

    object? Retrieve();
    object? Retrieve(NodeIndex index);
}

/// <summary>
/// The <see cref="IGraphNode"/> interface represents a node in a Quantum object graph. This node can represent an object or a member of an object.
/// </summary>
public interface IGraphNode
{
    /// <summary>
    /// Gets or sets the <see cref="System.Guid"/>.
    /// </summary>
    Guid Guid { get; }

    /// <summary>
    /// Gets the expected type of for the content of this node.
    /// </summary>
    /// <remarks>The actual type of the content can be different, for example it could be a type inheriting from this type.</remarks>
    Type Type { get; }

    /// <summary>
    /// Gets or sets the type descriptor of this content
    /// </summary>
    ITypeDescriptor Descriptor { get; }

    /// <summary>
    /// Gets whether this node holds a reference or is a direct value.
    /// </summary>
    bool IsReference { get; }

    /// <summary>
    /// Retrieves the value of this node.
    /// </summary>
    object? Retrieve();

    /// <summary>
    /// Retrieves the value of one of the item of this node, if it holds a collection.
    /// </summary>
    /// <param name="index">The index to use to retrieve the value.</param>
    object? Retrieve(NodeIndex index);
}
