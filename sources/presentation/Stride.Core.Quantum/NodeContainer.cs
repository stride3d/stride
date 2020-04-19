// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Stride.Core.Annotations;

namespace Stride.Core.Quantum
{
    /// <summary>
    /// A container used to store nodes and resolve references between them.
    /// </summary>
    public class NodeContainer : INodeContainer
    {
        private readonly object lockObject = new object();
        private readonly ThreadLocal<HashSet<IGraphNode>> processedNodes = new ThreadLocal<HashSet<IGraphNode>>();
        private ConditionalWeakTable<object, IObjectNode> nodesByObject = new ConditionalWeakTable<object, IObjectNode>();

        /// <summary>
        /// Creates a new instance of <see cref="NodeContainer"/> class.
        /// </summary>
        public NodeContainer()
        {
            NodeBuilder = CreateDefaultNodeBuilder();
        }

        /// <inheritdoc/>
        public INodeBuilder NodeBuilder { get; set; }

        /// <inheritdoc/>
        public IObjectNode GetOrCreateNode(object rootObject)
        {
            if (rootObject == null)
                return null;

            lock (lockObject)
            {
                if (!processedNodes.IsValueCreated)
                    processedNodes.Value = new HashSet<IGraphNode>();

                var node = GetOrCreateNodeInternal(rootObject);

                processedNodes.Value.Clear();
                return node;
            }
        }

        /// <inheritdoc/>
        public IObjectNode GetNode(object rootObject)
        {
            lock (lockObject)
            {
                if (!processedNodes.IsValueCreated)
                    processedNodes.Value = new HashSet<IGraphNode>();

                var node = GetNodeInternal(rootObject);

                processedNodes.Value.Clear();
                return node;
            }
        }

        /// <summary>
        /// Refresh all references contained in the given node, creating new nodes for newly referenced objects.
        /// </summary>
        /// <param name="node">The node to update</param>
        internal void UpdateReferences([NotNull] IGraphNode node)
        {
            lock (lockObject)
            {
                if (!processedNodes.IsValueCreated)
                    processedNodes.Value = new HashSet<IGraphNode>();

                UpdateReferencesInternal(node);

                processedNodes.Value.Clear();
            }
        }

        /// <summary>
        /// Removes all nodes that were previously registered.
        /// </summary>
        public void Clear()
        {
            lock (lockObject)
            {
                nodesByObject = new ConditionalWeakTable<object, IObjectNode>();
            }
        }

        /// <summary>
        /// Gets the <see cref="IGraphNode"/> associated to a data object, if it exists. If the NodeContainer has been constructed without <see cref="IGuidContainer"/>, this method will throw an exception.
        /// </summary>
        /// <param name="rootObject">The data object.</param>
        /// <returns>The <see cref="IGraphNode"/> associated to the given object if available, or <c>null</c> otherwise.</returns>
        internal IObjectNode GetNodeInternal(object rootObject)
        {
            lock (lockObject)
            {
                if (rootObject == null)
                    return null;

                IObjectNode node;
                nodesByObject.TryGetValue(rootObject, out node);
                return node;
            }
        }

        /// <summary>
        /// Gets the node associated to a data object, if it exists, otherwise creates a new node for the object and its member recursively.
        /// </summary>
        /// <param name="rootObject">The data object.</param>
        /// <returns>The <see cref="IGraphNode"/> associated to the given object.</returns>
        [CanBeNull]
        internal IObjectNode GetOrCreateNodeInternal(object rootObject)
        {
            if (rootObject == null)
                return null;

            lock (lockObject)
            {
                IObjectNode result;
                if (!rootObject.GetType().IsValueType)
                {
                    result = GetNodeInternal(rootObject);
                    if (result != null)
                        return result;
                }

                result = NodeBuilder.Build(rootObject, Guid.NewGuid());

                if (result != null)
                {
                    // Register reference objects
                    if (!(result is BoxedNode))
                    {
                        nodesByObject.Add(rootObject, result);
                    }
                    // Create or update nodes of referenced objects
                    UpdateReferencesInternal(result);
                }
                return result;
            }
        }

        /// <summary>
        /// Refresh all references contained in the given node, creating new nodes for newly referenced objects.
        /// </summary>
        /// <param name="node">The node to update</param>
        private void UpdateReferencesInternal([NotNull] IGraphNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            lock (lockObject)
            {
                if (processedNodes.Value.Contains(node))
                    return;

                processedNodes.Value.Add(node);

                // If the node was holding a reference, refresh the reference
                if (node.IsReference)
                {
                    (node as IMemberNode)?.TargetReference?.Refresh(node, this);
                    (node as IObjectNode)?.ItemReferences?.Refresh(node, this);
                }
                else
                {
                    // Otherwise refresh potential references in its children.
                    var objectNode = node as IObjectNode;
                    if (objectNode != null)
                    {
                        foreach (var child in objectNode.Members)
                        {
                            UpdateReferencesInternal(child);
                        }
                    }
                }
            }
        }

        [NotNull]
        private INodeBuilder CreateDefaultNodeBuilder()
        {
            return new DefaultNodeBuilder(this);
        }
    }
}
