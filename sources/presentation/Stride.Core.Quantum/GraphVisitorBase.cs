// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Quantum.References;

namespace Stride.Core.Quantum
{
    /// <summary>
    /// A class that visits all node referenced by a given root node, including members, targets of members, and targets of collection items.
    /// The nodes to visit can be controlled by implementing <see cref="ShouldVisitMemberTarget"/> and <see cref="ShouldVisitTargetItem"/>.
    /// </summary>
    public class GraphVisitorBase
    {
        private readonly HashSet<IGraphNode> visitedNodes = new HashSet<IGraphNode>();

        /// <summary>
        /// The current path in the visit. This path is mutable and must be cloned if used out of the visitor.
        /// </summary>
        protected GraphNodePath CurrentPath;

        /// <summary>
        /// Gets or sets whether to skip the root node passed to <see cref="Visit"/> when raising the <see cref="Visiting"/> event.
        /// </summary>
        public bool SkipRootNode { get; set; }

        /// <summary>
        /// Gets the root node of the current visit.
        /// </summary>
        protected IGraphNode RootNode { get; private set; }

        /// <summary>
        /// Raised when a node is visited.
        /// </summary>
        public event Action<IGraphNode, GraphNodePath> Visiting;

        /// <summary>
        /// Visits a hierarchy of node, starting by the given root node.
        /// </summary>
        /// <param name="node">The root node of the visit</param>
        /// <param name="memberNode">The member content containing the node to visit, if relevant. This is used to properly check if the root node should be visited.</param>
        /// <param name="initialPath">The initial path of the root node, if this visit occurs in the context of a sub-hierarchy. Can be null.</param>
        public virtual void Visit([NotNull] IGraphNode node, [CanBeNull] MemberNode memberNode = null, [CanBeNull] GraphNodePath initialPath = null)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            CurrentPath = initialPath ?? new GraphNodePath(node);
            RootNode = node;
            VisitNode(node);
            RootNode = null;
        }

        /// <summary>
        /// Visits a single node.
        /// </summary>
        /// <param name="node">The node being visited.</param>
        /// <remarks>This method is in charge of pursuing the visit with the children and references of the given node, as well as raising the <see cref="Visiting"/> event.</remarks>
        protected virtual void VisitNode([NotNull] IGraphNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            visitedNodes.Add(node);
            if (node != RootNode || !SkipRootNode)
            {
                Visiting?.Invoke(node, CurrentPath);
            }

            if (node is IObjectNode objectNode)
            {
                VisitChildren(objectNode);
                VisitItemTargets(objectNode);
            }

            if (node is IMemberNode memberNode)
            {
                VisitMemberTarget(memberNode);
            }
            visitedNodes.Remove(node);
        }

        /// <summary>
        /// Visits the children of the given node.
        /// </summary>
        /// <param name="node">The node being visited.</param>
        protected virtual void VisitChildren([NotNull] IObjectNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            var members = node.Members;
            if (members is List<IMemberNode> asList)
            {
                foreach (var child in asList)
                {
                    CurrentPath.PushMember(child.Name);
                    VisitNode(child);
                    CurrentPath.Pop();
                }
            }
            else if (members is Dictionary<string, IMemberNode>.ValueCollection asVCol)
            {
                foreach (var child in asVCol)
                {
                    CurrentPath.PushMember(child.Name);
                    VisitNode(child);
                    CurrentPath.Pop();
                }
            }
            else
            {
                foreach (var child in members)
                {
                    CurrentPath.PushMember(child.Name);
                    VisitNode(child);
                    CurrentPath.Pop();
                }
            }
        }

        /// <summary>
        /// Visits the <see cref="ObjectReference"/> contained in the given node, if any.
        /// </summary>
        /// <param name="node">The node being visited.</param>
        protected virtual void VisitMemberTarget([NotNull] IMemberNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (node.TargetReference?.TargetNode != null)
            {
                if (ShouldVisitMemberTarget(node))
                {
                    CurrentPath.PushTarget();
                    VisitReference(node, node.TargetReference);
                    CurrentPath.Pop();
                }
            }
        }

        /// <summary>
        /// Visits the <see cref="ReferenceEnumerable"/> contained in the given node, if any.
        /// </summary>
        /// <param name="node">The node being visited.</param>
        protected virtual void VisitItemTargets([NotNull] IObjectNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            var enumerableReference = node.ItemReferences;
            if (enumerableReference != null)
            {
                foreach (var reference in enumerableReference)
                {
                    if (reference.TargetNode == null)
                        continue;

                    if (ShouldVisitTargetItem(node, reference.Index))
                    {
                        CurrentPath.PushIndex(reference.Index);
                        VisitReference(node, reference);
                        CurrentPath.Pop();
                    }
                }
            }
        }

        /// <summary>
        /// Visits an <see cref="ObjectReference"/>.
        /// </summary>
        /// <param name="referencer">The node containing the reference to visit.</param>
        /// <param name="reference">The reference to visit.</param>
        protected virtual void VisitReference([NotNull] IGraphNode referencer, [NotNull] ObjectReference reference)
        {
            if (referencer == null) throw new ArgumentNullException(nameof(referencer));
            if (reference == null) throw new ArgumentNullException(nameof(reference));
            VisitNode(reference.TargetNode);
        }

        /// <summary>
        /// Indicates whether the <see cref="IMemberNode.Target"/> of the given <see cref="IMemberNode"/> should be visited or not.
        /// </summary>
        /// <param name="memberNode">The member node to evaluate.</param>
        /// <returns>True if the target of the member node should be visited, false otherwise.</returns>
        /// <remarks>This method is invoked only when the given <see cref="IMemberNode"/> contains a target node.</remarks>
        protected internal virtual bool ShouldVisitMemberTarget([NotNull] IMemberNode memberNode)
        {
            if (memberNode == null) throw new ArgumentNullException(nameof(memberNode));
            return !visitedNodes.Contains(memberNode.Target);
        }

        /// <summary>
        /// Indicates whether the target node of the item corresponding to the given index in the collection contained in the given node should be visited or not.
        /// </summary>
        /// <param name="collectionNode">The node to evaluate.</param>
        /// <param name="index">The index of the item to evaluate.</param>
        /// <returns>True if the node of the item corresponding to the given index in the collection contained in the given node should be visited, false otherwise.</returns>
        /// <remarks>This method is invoked only when the given <see cref="IObjectNode"/> contains a collection with items being references.</remarks>
        protected internal virtual bool ShouldVisitTargetItem([NotNull] IObjectNode collectionNode, NodeIndex index)
        {
            if (collectionNode == null) throw new ArgumentNullException(nameof(collectionNode));
            var target = collectionNode.IndexedTarget(index);
            return !visitedNodes.Contains(target);
        }
    }
}
