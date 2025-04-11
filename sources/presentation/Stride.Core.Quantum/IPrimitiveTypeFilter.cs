// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Quantum;

/// <summary>
/// Used to conditionally test whether a type is primitive in the context of the <see cref="INodeBuilder"/>
/// </summary>
public interface IPrimitiveTypeFilter
{
    /// <summary>
    /// Indicates whether a type is a primitive type for this node builder.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <remarks>
    /// Any type can be registered as a primitive type. The node builder won't construct nodes for members of primitive types, and won't
    /// use reference for them even if they are not value type.
    /// </remarks>
    bool IsPrimitiveType(Type type);
}
