// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Reflection;
using Stride.Core.Quantum.References;

namespace Stride.Core.Quantum
{
    /// <summary>
    /// This interface provides objects and methods to build a nodal view model from a given object.
    /// </summary>
    public interface INodeBuilder
    {
        /// <summary>
        /// Gets the instance of <see cref="NodeContainer"/> associated to this node builder.
        /// </summary>
        NodeContainer NodeContainer { get; }

        /// <summary>
        /// Gets the type descriptor factory.
        /// </summary>
        /// <value>The type descriptor factory.</value>
        ITypeDescriptorFactory TypeDescriptorFactory { get; }

        /// <summary>
        /// Gets or sets the factory that will create instances of <see cref="IGraphNode"/> for nodes.
        /// </summary>
        INodeFactory NodeFactory { get; set; }
        /// <summary>
        /// Registers a type as a primitive type.
        /// </summary>
        /// <param name="type">The type to register.</param>
        /// <remarks>
        /// Any type can be registered as a primitive type. The node builder won't construct nodes for members of primitive types, and won't
        /// use reference for them even if they are not value type.
        /// </remarks>
        /// <seealso cref="UnregisterPrimitiveType"/>
        /// <seealso cref="IsPrimitiveType"/>
        void RegisterPrimitiveType(Type type);

        /// <summary>
        /// Unregisters a type as a primitive type.
        /// </summary>
        /// <param name="type">The type to register.</param>
        /// <remarks>
        /// Any type can be registered as a primitive type. The node builder won't construct nodes for members of primitive types, and won't
        /// use reference for them even if they are not value type.
        /// </remarks>
        /// <seealso cref="RegisterPrimitiveType"/>
        /// <seealso cref="IsPrimitiveType"/>
        void UnregisterPrimitiveType(Type type);

        /// <summary>
        /// Indicates whether a type is a primitive type for this node builder.
        /// </summary>
        /// <param name="type">The type to register.</param>
        /// <remarks>
        /// Any type can be registered as a primitive type. The node builder won't construct nodes for members of primitive types, and won't
        /// use reference for them even if they are not value type.
        /// </remarks>
        /// <seealso cref="RegisterPrimitiveType"/>
        /// <seealso cref="UnregisterPrimitiveType"/>
        bool IsPrimitiveType(Type type);

        /// <summary>
        /// Build the node hierarchy corresponding to the given object.
        /// </summary>
        /// <param name="obj">The object. Can be <c>null</c>.</param>
        /// <param name="rootGuid">The <see cref="Guid"/> To assign to the root node.</param>
        /// <returns>The root node of the node hierarchy corresponding to the given object.</returns>
        IObjectNode Build(object obj, Guid rootGuid);

        /// <summary>
        /// Creates a reference for the specified type/value node.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        IReference CreateReferenceForNode(Type type, object value, bool isMember);
    }
}
