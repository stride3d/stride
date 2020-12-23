// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Quantum.References;

namespace Stride.Core.Quantum
{
    /// <summary>
    /// A class that is capable of visiting two object hierarchies and "link" corresponding nodes together.
    /// </summary>
    /// <remarks>
    /// One of the two hierarchies is considered to be the "source" for the linker. This hierarchy is visited by the <see cref="GraphNodeLinker"/>,
    /// and for each node, it will try to find a corresponding node in the other "target" hierarchy.
    /// By deriving this class, the way this correspondance between two nodes is established can be customized.
    /// </remarks>
    public class GraphNodeLinker
    {
        private sealed class GraphNodeLinkerVisitor : GraphVisitorBase
        {
            private readonly GraphNodeLinker linker;
            internal readonly Dictionary<IGraphNode, IGraphNode> VisitedLinks = new Dictionary<IGraphNode, IGraphNode>();

            public GraphNodeLinkerVisitor(GraphNodeLinker linker)
            {
                this.linker = linker;
            }

            public void Reset([NotNull] IGraphNode sourceNode, IGraphNode targetNode)
            {
                VisitedLinks.Clear();
                VisitedLinks.Add(sourceNode, targetNode);
            }

            protected override void VisitNode(IGraphNode node)
            {
                var targetNode = linker.FindTarget(node);
                // Override the target node, in case FindTarget returned a different one.
                VisitedLinks[node] = targetNode;
                linker.LinkNodes(node, targetNode);
                base.VisitNode(node);
            }

            protected override void VisitChildren(IObjectNode node)
            {
                if (VisitedLinks.TryGetValue(node, out IGraphNode targetNodeParent))
                {
                    var objNode = targetNodeParent as IObjectNode;
                    var members = node.Members;
                    if (members is List<IMemberNode> asList)
                    {
                        foreach (var child in asList)
                        {
                            VisitedLinks.Add(child, objNode?.TryGetChild(child.Name));
                        }
                    }
                    else if(members is Dictionary<string, IMemberNode>.ValueCollection asVCol)
                    {
                        foreach (var child in asVCol)
                        {
                            VisitedLinks.Add(child, objNode?.TryGetChild(child.Name));
                        }
                    }
                    else
                    {
                        foreach (var child in members)
                        {
                            VisitedLinks.Add(child, objNode?.TryGetChild(child.Name));
                        }
                    }
                }
                base.VisitChildren(node);
            }

            protected override void VisitReference(IGraphNode referencer, ObjectReference reference)
            {
                if (reference.TargetNode != null)
                {
                    // Prevent re-entrancy in the same object
                    if (VisitedLinks.ContainsKey(reference.TargetNode))
                        return;

                    if (VisitedLinks.TryGetValue(referencer, out IGraphNode targetNode))
                    {
                        ObjectReference targetReference = null;
                        if (targetNode != null)
                            targetReference = linker.FindTargetReference(referencer, targetNode, reference);

                        VisitedLinks.Add(reference.TargetNode, targetReference?.TargetNode);
                    }
                }
                base.VisitReference(referencer, reference);
            }

            protected internal override bool ShouldVisitMemberTarget(IMemberNode memberNode)
            {
                return linker.ShouldVisitMemberTarget(memberNode) && base.ShouldVisitMemberTarget(memberNode);
            }

            protected internal override bool ShouldVisitTargetItem(IObjectNode collectionNode, NodeIndex index)
            {
                return linker.ShouldVisitTargetItem(collectionNode, index) && base.ShouldVisitTargetItem(collectionNode, index);
            }
        }

        private readonly GraphNodeLinkerVisitor visitor;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphNodeLinker"/> class.
        /// </summary>
        public GraphNodeLinker()
        {
            visitor = new GraphNodeLinkerVisitor(this);
        }

        /// <summary>
        /// Gets or sets the action to execute when two nodes should be linked.
        /// </summary>
        public Action<IGraphNode, IGraphNode> LinkAction { get; set; }

        /// <summary>
        /// Visits and links the node of two different object hierarchies.
        /// </summary>
        /// <param name="sourceNode">The root node of the "source" object to link.</param>
        /// <param name="targetNode">The root node of the "target" object to link.</param>
        public void LinkGraph([NotNull] IGraphNode sourceNode, IGraphNode targetNode)
        {
            visitor.Reset(sourceNode, targetNode);
            visitor.Visit(sourceNode);
        }

        protected virtual bool ShouldVisitMemberTarget(IMemberNode member)
        {
            return true;
        }

        protected virtual bool ShouldVisitTargetItem(IObjectNode collectionNode, NodeIndex index)
        {
            return true;
        }

        /// <summary>
        /// Links a single node of the "source" hierarchy to a node of the "target" hierarchy.
        /// </summary>
        /// <param name="sourceNode">The node from the source hierarchy. Cannot be null.</param>
        /// <param name="targetNode">The node from the target hierarchy. Can be null.</param>
        /// <exception cref="ArgumentNullException">The source node is null.</exception>
        /// <remarks>The default implementation will simply invoke <see cref="LinkAction"/>.</remarks>
        protected virtual void LinkNodes([NotNull] IGraphNode sourceNode, IGraphNode targetNode)
        {
            if (sourceNode == null) throw new ArgumentNullException(nameof(sourceNode));
            LinkAction?.Invoke(sourceNode, targetNode);
        }

        /// <summary>
        /// Finds the target of a given source node.
        /// </summary>
        /// <param name="sourceNode">The source node for which to find a target node. Cannot be null.</param>
        /// <returns>A node in the target hierarchy that corresponds to the given node.</returns>
        /// <remarks>
        /// The default implementation looks for node with the same names and or the same types of reference.
        /// This method can return null if there is no matching node in the target hierarchy.
        /// </remarks>
        [CanBeNull]
        protected virtual IGraphNode FindTarget([NotNull] IGraphNode sourceNode)
        {
            return visitor.VisitedLinks.TryGetValue(sourceNode, out IGraphNode targetNode) ? targetNode : null;
        }

        /// <summary>
        /// Finds a reference in a target node to correspond with the given reference from the source node.
        /// </summary>
        /// <param name="sourceNode">The source node that contains the reference.</param>
        /// <param name="targetNode">The target node in which to look for a matching reference.</param>
        /// <param name="sourceReference">The reference in the source node for which to look for a correspondance in the target node.</param>
        /// <returns>A reference of the target node corresponding to the given reference in the source node, or null if there is no match.</returns>
        /// <remarks>
        /// The source reference can either be directly the <see cref="IGraphNode.TargetReference"/> of the source node if this reference is
        /// an <see cref="ObjectReference"/>, or one of the reference contained inside <see cref="IGraphNode.ItemReferences"/> if this reference
        /// is a <see cref="ReferenceEnumerable"/>. The <see cref="NodeIndex"/> property indicates the index of the reference in this case.
        /// The default implementation returns a reference in the target node that matches the index of the source reference, if available.
        /// </remarks>
        // TODO: turn back protected!
        [CanBeNull]
        public virtual ObjectReference FindTargetReference(IGraphNode sourceNode, IGraphNode targetNode, [NotNull] ObjectReference sourceReference)
        {
            if (sourceNode is IMemberNode)
                return (targetNode as IMemberNode)?.TargetReference;

            var targetReference = (targetNode as IObjectNode)?.ItemReferences;
            return targetReference != null && targetReference.HasIndex(sourceReference.Index) ? targetReference[sourceReference.Index] : null;
        }
    }
}
