// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Annotations;

namespace Xenko.Core.Quantum
{
    /// <summary>
    /// An object that tracks the changes in an extended graph of <see cref="IGraphNode"/>, given a root node. The graph extends to all target nodes
    /// referenced by members and collections of the root node, recursively. Nodes can be excluded from the extended graph by providing a custom
    /// visitor in <see cref="CreateVisitor"/>.
    /// </summary>
    public class GraphNodeChangeListener : INotifyNodeValueChange, INotifyNodeItemChange, IDisposable
    {
        private readonly IGraphNode rootNode;
        protected readonly HashSet<IGraphNode> RegisteredNodes = new HashSet<IGraphNode>();

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphNodeChangeListener"/> class.
        /// </summary>
        /// <param name="rootNode">The root node of the extended graph to listen to.</param>
        public GraphNodeChangeListener(IGraphNode rootNode)
        {
            this.rootNode = rootNode;
        }

        /// <summary>
        /// Raised before the value of a member node referenced by the related root node changes.
        /// </summary>
        public event EventHandler<MemberNodeChangeEventArgs> ValueChanging;

        /// <summary>
        /// Raised after the value of a member node referenced by the related root node has changed.
        /// </summary>
        public event EventHandler<MemberNodeChangeEventArgs> ValueChanged;

        /// <summary>
        /// Raised before an item of a collection node referenced by the related root node changes.
        /// </summary>
        public event EventHandler<ItemChangeEventArgs> ItemChanging;

        /// <summary>
        /// Raised after an item of a collection node referenced by the related root node has changed.
        /// </summary>
        public event EventHandler<ItemChangeEventArgs> ItemChanged;

        /// <summary>
        /// Initializes the node listener.
        /// </summary>
        public void Initialize()
        {
            RegisterAllNodes();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            var visitor = CreateVisitor();
            visitor.Visiting += (node, path) => UnregisterNode(node);
            visitor.Visit(rootNode);
        }

        /// <summary>
        /// Creates a proper <see cref="GraphVisitorBase"/> to visit the graph.
        /// </summary>
        /// <returns>A new instance of <see cref="GraphVisitorBase"/>.</returns>
        [NotNull]
        protected virtual GraphVisitorBase CreateVisitor()
        {
            return new GraphVisitorBase();
        }

        protected virtual bool RegisterNode(IGraphNode node)
        {
            // A node can be registered multiple times when it is referenced via multiple paths
            if (RegisteredNodes.Add(node))
            {
                ((IGraphNodeInternal)node).PrepareChange += ContentPrepareChange;
                ((IGraphNodeInternal)node).FinalizeChange += ContentFinalizeChange;
                var memberNode = node as IMemberNode;
                if (memberNode != null)
                {
                    memberNode.ValueChanging += OnValueChanging;
                    memberNode.ValueChanged += OnValueChanged;
                }
                var objectNode = node as IObjectNode;
                if (objectNode != null)
                {
                    objectNode.ItemChanging += OnItemChanging;
                    objectNode.ItemChanged += OnItemChanged;
                }
                return true;
            }

            return false;
        }

        protected virtual bool UnregisterNode(IGraphNode node)
        {
            if (RegisteredNodes.Remove(node))
            {
                ((IGraphNodeInternal)node).PrepareChange -= ContentPrepareChange;
                ((IGraphNodeInternal)node).FinalizeChange -= ContentFinalizeChange;
                var memberNode = node as IMemberNode;
                if (memberNode != null)
                {
                    memberNode.ValueChanging -= OnValueChanging;
                    memberNode.ValueChanged -= OnValueChanged;
                }
                var objectNode = node as IObjectNode;
                if (objectNode != null)
                {
                    objectNode.ItemChanging -= OnItemChanging;
                    objectNode.ItemChanged -= OnItemChanged;
                }
                return true;
            }
            return false;
        }

        private void RegisterAllNodes()
        {
            var visitor = CreateVisitor();
            visitor.Visiting += (node, path) => RegisterNode(node);
            visitor.Visit(rootNode);
        }

        private void ContentPrepareChange(object sender, [NotNull] INodeChangeEventArgs e)
        {
            var node = e.Node;
            var visitor = CreateVisitor();
            visitor.Visiting += (node1, path) => UnregisterNode(node1);
            switch (e.ChangeType)
            {
                case ContentChangeType.ValueChange:
                case ContentChangeType.CollectionUpdate:
                    // The changed node itself is still valid, we don't want to unregister it
                    visitor.SkipRootNode = true;
                    visitor.Visit(node);
                    // TODO: In case of CollectionUpdate we could probably visit only the target node of the corresponding index
                    break;
                case ContentChangeType.CollectionRemove:
                    if (node.IsReference && e.OldValue != null)
                    {
                        var removedNode = (node as IObjectNode)?.ItemReferences[((ItemChangeEventArgs)e).Index].TargetNode;
                        if (removedNode != null)
                        {
                            // TODO: review this
                            visitor.Visit(removedNode, node as MemberNode);
                        }
                    }
                    break;
            }
        }

        private void ContentFinalizeChange(object sender, [NotNull] INodeChangeEventArgs e)
        {
            var visitor = CreateVisitor();
            visitor.Visiting += (node, path) => RegisterNode(node);
            switch (e.ChangeType)
            {
                case ContentChangeType.ValueChange:
                case ContentChangeType.CollectionUpdate:
                    // The changed node itself is still valid, we don't want to re-register it
                    visitor.SkipRootNode = true;
                    visitor.Visit(e.Node);
                    // TODO: In case of CollectionUpdate we could probably visit only the target node of the corresponding index
                    break;
                case ContentChangeType.CollectionAdd:
                    if (e.Node.IsReference && e.NewValue != null)
                    {
                        var objectNode = (IObjectNode)e.Node;
                        IGraphNode addedNode;
                        NodeIndex index;
                        var arg = (ItemChangeEventArgs)e;
                        if (!arg.Index.IsEmpty)
                        {
                            index = arg.Index;
                            addedNode = objectNode.ItemReferences[arg.Index].TargetNode;
                        }
                        else
                        {
                            // TODO: review this
                            var reference = objectNode.ItemReferences.First(x => x.TargetNode.Retrieve() == e.NewValue);
                            index = reference.Index;
                            addedNode = reference.TargetNode;
                        }

                        if (addedNode != null && visitor.ShouldVisitTargetItem(objectNode, index))
                        {
                            var path = new GraphNodePath(e.Node);
                            path.PushIndex(index);
                            visitor.Visit(addedNode, e.Node as MemberNode, path);
                        }
                    }
                    break;
            }
        }

        private void OnValueChanging(object sender, MemberNodeChangeEventArgs e)
        {
            ValueChanging?.Invoke(sender, e);
        }

        private void OnValueChanged(object sender, MemberNodeChangeEventArgs e)
        {
            ValueChanged?.Invoke(sender, e);
        }

        private void OnItemChanging(object sender, ItemChangeEventArgs e)
        {
            ItemChanging?.Invoke(sender, e);
        }

        private void OnItemChanged(object sender, ItemChangeEventArgs e)
        {
            ItemChanged?.Invoke(sender, e);
        }
    }
}
