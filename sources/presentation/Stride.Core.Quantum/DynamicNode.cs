// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Reflection;

namespace Stride.Core.Quantum
{
    public abstract class DynamicNode : DynamicObject, IEnumerable
    {
        protected readonly IGraphNode Node;

        internal DynamicNode(IGraphNode node)
        {
            Node = node;
        }

        /// <summary>
        /// Creates a dynamic node from the given <see cref="IGraphNode"/>.
        /// </summary>
        /// <param name="node">The node to use to create the dynamic node.</param>
        /// <returns>A <see cref="DynamicNode"/> representing the given node.</returns>
        [NotNull]
        public static dynamic FromNode(IGraphNode node)
        {
            if (node is MemberNode)
                throw new ArgumentException("Cannot create a dynamic node from a member node.");

            return new DynamicDirectNode(node);
        }

        /// <summary>
        /// Returns the <see cref="IGraphNode"/> associated to the given dynamic node.
        /// </summary>
        /// <param name="node">The node from which to retrieve the graph node.</param>
        /// <returns>A <see cref="IGraphNode"/> associated to the given node.</returns>
        public static IGraphNode GetNode([NotNull] DynamicNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            return node.Node;
        }

        /// <summary>
        /// Returns the <see cref="IGraphNode"/> associated to the given dynamic node.
        /// </summary>
        /// <returns>A <see cref="IGraphNode"/> associated to the given node.</returns>
        public IGraphNode GetNode() => Node;

        /// <summary>
        /// Adds an item to the content of this node, assuming it's a collection.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(object item)
        {
            var targetNode = GetTargetNode();
            if (targetNode == null)
                throw new InvalidOperationException($"Cannot invoke {nameof(Add)} on this property.");
            targetNode.Add(item);
        }

        /// <summary>
        /// Inserts an item to the content of this node, assuming it's a collection.
        /// </summary>
        /// <param name="item">The item to insert.</param>
        /// <param name="index">The index of the item to insert.</param>
        public void Insert(object item, NodeIndex index)
        {
            var targetNode = GetTargetNode();
            if (targetNode == null)
                throw new InvalidOperationException($"Cannot invoke {nameof(Insert)} on this property.");
            targetNode.Add(item, index);
        }

        /// <summary>
        /// Removes an item from the content if this node, assuming it's a collection.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <param name="index">The index of the item to remove.</param>
        public void Remove(object item, NodeIndex index)
        {
            var targetNode = GetTargetNode();
            if (targetNode == null)
                throw new InvalidOperationException($"Cannot invoke {nameof(Remove)} on this property.");
            targetNode.Remove(item, index);
        }

        /// <summary>
        /// Retrieves the actual value of this node.
        /// </summary>
        /// <returns>The actual value of this node.</returns>
        public object Retrieve() => RetrieveValue();

        /// <summary>
        /// Retrieves the actual value of this node.
        /// </summary>
        /// <typeparam name="T">The type of expected value</typeparam>
        /// <returns>The actual value of this node.</returns>
        /// <exception cref="InvalidCastException">The actual type of the node value does not match the given <typeparamref name="T"/> type.</exception>
        public T Retrieve<T>() => (T)Retrieve();

        /// <inheritdoc/>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var memberNode = GetTargetMemberNode(binder.Name);
            result = memberNode != null ? new DynamicDirectNode(memberNode) : null;
            return result != null;
        }

        /// <inheritdoc/>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var memberNode = GetTargetMemberNode(binder.Name);
            if (memberNode == null)
                return false;

            try
            {
                // TODO: "changing" notifications will still be sent even if the update fails (but not the "changed") - we should detect preemptively if we can update (implements a bool TryUpdate?)
                memberNode.Update(value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public override bool TryConvert([NotNull] ConvertBinder binder, out object result)
        {
            var value = RetrieveValue();
            if (value == null && !binder.Type.IsValueType)
            {
                result = null;
                return true;
            }
            if (binder.Type.IsInstanceOfType(value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }

        /// <inheritdoc/>
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return GetTargetNode()?.Members.Select(x => x.Name) ?? Enumerable.Empty<string>();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            var node = GetTargetNode();
            // TODO: review this, is there a typo here?
            var indices = (Node as IObjectNode)?.Indices.Select(x => x.Value);
            if (indices == null)
                throw new InvalidOperationException("This node is not enumerable.");

            dynamic thisNode = this;
            return indices.Cast<object>().Select(x => thisNode[x]).GetEnumerator();
        }

        [CanBeNull]
        protected IMemberNode GetTargetMemberNode(string memberName)
        {
            var targetNode = GetTargetNode();
            var memberNode = targetNode?[memberName];
            return memberNode;
        }

        protected abstract object RetrieveValue();

        [CanBeNull]
        protected abstract IObjectNode GetTargetNode();

        protected static bool IsIndexExisting([NotNull] IGraphNode node, NodeIndex index)
        {
            if (node.IsReference)
            {
                var reference = (node as IObjectNode)?.ItemReferences;
                if (reference?.HasIndex(index) ?? false)
                {
                    return true;
                }
            }
            else
            {
                var value = node.Retrieve();
                var collectionDescriptor = node.Descriptor as CollectionDescriptor;
                if (collectionDescriptor != null && index.IsInt && index.Int >= 0 && index.Int < collectionDescriptor.GetCollectionCount(value))
                {
                    return true;
                }
                var dictionaryDescriptor = node.Descriptor as DictionaryDescriptor;
                if (dictionaryDescriptor != null && dictionaryDescriptor.KeyType.IsInstanceOfType(index.Value) && dictionaryDescriptor.ContainsKey(value, index.Value))
                {
                    return true;
                }
            }
            return false;
        }

        protected static bool IsIndexValid([NotNull] IGraphNode node, NodeIndex index)
        {
            if (node.IsReference)
            {
                var reference = (node as IObjectNode)?.ItemReferences;
                return reference != null;
            }
            var collectionDescriptor = node.Descriptor as CollectionDescriptor;
            if (collectionDescriptor != null)
            {
                return index.IsInt && index.Int >= 0;
            }
            var dictionaryDescriptor = node.Descriptor as DictionaryDescriptor;
            if (dictionaryDescriptor != null)
            {
                return dictionaryDescriptor.KeyType.IsInstanceOfType(index.Value);
            }
            return false;
        }

        protected static bool UpdateCollection([NotNull] IObjectNode node, object value, NodeIndex index)
        {
            if (IsIndexExisting(node, index))
            {
                node.Update(value, index);
                return true;
            }
            if (IsIndexValid(node, index))
            {
                node.Add(value, index);
                return true;
            }
            return false;
        }
    }

    internal class DynamicDirectNode : DynamicNode
    {
        internal DynamicDirectNode(IGraphNode node)
            : base(node)
        {
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes.Length == 1)
            {
                var index = new NodeIndex(indexes[0]);
                if (IsIndexExisting(Node, index))
                {
                    result = new DynamicIndexedNode(Node, index);
                    return true;
                }
            }
            result = null;
            return false;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (indexes.Length == 1)
            {
                var index = new NodeIndex(indexes[0]);
                return UpdateCollection((IObjectNode)Node, value, index);
            }
            return false;
        }

        protected override object RetrieveValue()
        {
            return Node.Retrieve();
        }

        protected override IObjectNode GetTargetNode()
        {
            return (Node as IMemberNode)?.Target;
        }
    }

    internal class DynamicIndexedNode : DynamicNode
    {
        private readonly NodeIndex index;

        internal DynamicIndexedNode(IGraphNode node, NodeIndex index)
            : base(node)
        {
            this.index = index;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            var targetNode = GetTargetNode();
            if (indexes.Length == 1 && targetNode != null)
            {
                var nextIndex = new NodeIndex(indexes[0]);
                if (IsIndexExisting(targetNode, nextIndex))
                {
                    result = new DynamicIndexedNode(targetNode, nextIndex);
                    return true;
                }
            }
            result = null;
            return false;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            var targetNode = GetTargetNode();
            if (indexes.Length == 1 && targetNode != null)
            {
                var nextIndex = new NodeIndex(indexes[0]);
                return UpdateCollection((IObjectNode)Node, value, nextIndex);
            }
            return false;
        }

        protected override object RetrieveValue()
        {
            return Node.Retrieve(index);
        }

        protected override IObjectNode GetTargetNode()
        {
            var reference = (Node as IObjectNode)?.ItemReferences;
            if (Node.IsReference && (reference?.HasIndex(index) ?? false))
            {
                return reference[index].TargetNode;
            }
            return null;
        }
    }
}
