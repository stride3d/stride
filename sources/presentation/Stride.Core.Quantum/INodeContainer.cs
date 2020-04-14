// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;

namespace Stride.Core.Quantum
{
    /// <summary>
    /// A delegate representing a factory used to create a graph node from a content and its related information.
    /// </summary>
    /// <param name="name">The name of the node to create.</param>
    /// <param name="nodeent">The content for which to create a node.</param>
    /// <param name="guid">The unique identifier of the node to create.</param>
    /// <returns>A new instance of <see cref="IGraphNode"/> containing the given content.</returns>
    public delegate IGraphNode NodeFactoryDelegate(string name, IGraphNode node, Guid guid);

    /// <summary>
    /// An interface representing a container for graph nodes.
    /// </summary>
    public interface INodeContainer
    {
        /// <summary>
        /// Gets or set the visitor to use to create nodes. Default value is a <see cref="DefaultNodeBuilder"/> constructed with default parameters.
        /// </summary>
        INodeBuilder NodeBuilder { get; set; }

        /// <summary>
        /// Gets the node associated to a data object, if it exists, otherwise creates a new node for the object and its member recursively.
        /// </summary>
        /// <param name="rootObject">The data object.</param>
        /// <returns>The <see cref="IGraphNode"/> associated to the given object.</returns>
        [CanBeNull]
        IObjectNode GetOrCreateNode(object rootObject);

        /// <summary>
        /// Gets the <see cref="IGraphNode"/> associated to a data object, if it exists.
        /// </summary>
        /// <param name="rootObject">The data object.</param>
        /// <returns>The <see cref="IGraphNode"/> associated to the given object if available, or <c>null</c> otherwise.</returns>
        /// <remarks>Calling this method will update references of the returned node and its children, recursively.</remarks>
        IObjectNode GetNode(object rootObject);
    }
}
