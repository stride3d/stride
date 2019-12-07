// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;
using Xenko.Core.Extensions;
using Xenko.Core.Reflection;
using Xenko.Core.Quantum.References;

namespace Xenko.Core.Quantum
{
    /// <summary>
    /// An implementation of <see cref="IGraphNode"/> that gives access to an object or a boxed struct.
    /// </summary>
    /// <remarks>This content is not serialized by default.</remarks>
    public class ObjectNode : GraphNodeBase, IInitializingObjectNode, IGraphNodeInternal
    {
        private readonly HybridDictionary<string, IMemberNode> childrenMap = new HybridDictionary<string, IMemberNode>();
        private object value;

        public ObjectNode([NotNull] INodeBuilder nodeBuilder, object value, Guid guid, [NotNull] ITypeDescriptor descriptor, IReference reference)
            : base(nodeBuilder.SafeArgument(nameof(nodeBuilder)).NodeContainer, guid, descriptor)
        {
            if (reference is ObjectReference)
                throw new ArgumentException($"An {nameof(ObjectNode)} cannot contain an {nameof(ObjectReference)}");
            this.value = value;
            ItemReferences = reference as ReferenceEnumerable;
        }

        /// <inheritdoc/>
        [NotNull]
        public IMemberNode this[[NotNull] string name] => childrenMap[name];

        /// <inheritdoc/>
        public IReadOnlyCollection<IMemberNode> Members => (IReadOnlyCollection<IMemberNode>)childrenMap.Values;

        /// <inheritdoc/>
        public IEnumerable<NodeIndex> Indices => GetIndices();

        /// <inheritdoc/>
        public bool IsEnumerable => Descriptor is CollectionDescriptor || Descriptor is DictionaryDescriptor;

        /// <inheritdoc/>
        public override bool IsReference => ItemReferences != null;

        /// <inheritdoc/>
        public ReferenceEnumerable ItemReferences { get; }

        /// <inheritdoc/>
        protected sealed override object Value => value;

        /// <inheritdoc/>
        public event EventHandler<INodeChangeEventArgs> PrepareChange;

        /// <inheritdoc/>
        public event EventHandler<INodeChangeEventArgs> FinalizeChange;

        /// <inheritdoc/>
        public event EventHandler<ItemChangeEventArgs> ItemChanging;

        /// <inheritdoc/>
        public event EventHandler<ItemChangeEventArgs> ItemChanged;

        /// <inheritdoc/>
        [CanBeNull]
        public IMemberNode TryGetChild([NotNull] string name)
        {
            IMemberNode child;
            childrenMap.TryGetValue(name, out child);
            return child;
        }

        /// <inheritdoc/>
        public IObjectNode IndexedTarget(NodeIndex index)
        {
            if (index == NodeIndex.Empty) throw new ArgumentException(@"index cannot be Index.Empty when invoking this method.", nameof(index));
            if (ItemReferences == null) throw new InvalidOperationException(@"The node does not contain enumerable references.");
            return ItemReferences[index].TargetNode;
        }

        /// <inheritdoc/>
        public void Update(object newValue, NodeIndex index)
        {
            Update(newValue, index, true);
        }

        /// <inheritdoc/>
        public void Add(object newItem)
        {
            var collectionDescriptor = Descriptor as CollectionDescriptor;
            if (collectionDescriptor != null)
            {
                // Some collection (such as sets) won't add item at the end but at an arbitrary location.
                // Better send a null index in this case than sending a wrong value.
                var index = collectionDescriptor.IsList ? new NodeIndex(collectionDescriptor.GetCollectionCount(value)) : NodeIndex.Empty;
                var args = new ItemChangeEventArgs(this, index, ContentChangeType.CollectionAdd, null, newItem);
                NotifyItemChanging(args);
                collectionDescriptor.Add(value, newItem);
                UpdateReferences();
                NotifyItemChanged(args);
            }
            else
                throw new NotSupportedException("Unable to set the node value, the collection is unsupported");
        }

        /// <inheritdoc/>
        public void Add(object newItem, NodeIndex itemIndex)
        {
            var collectionDescriptor = Descriptor as CollectionDescriptor;
            var dictionaryDescriptor = Descriptor as DictionaryDescriptor;
            if (collectionDescriptor != null)
            {
                var index = collectionDescriptor.IsList ? itemIndex : NodeIndex.Empty;
                var args = new ItemChangeEventArgs(this, index, ContentChangeType.CollectionAdd, null, newItem);
                NotifyItemChanging(args);
                if (collectionDescriptor.GetCollectionCount(value) == itemIndex.Int || !collectionDescriptor.HasInsert)
                {
                    collectionDescriptor.Add(value, newItem);
                }
                else
                {
                    collectionDescriptor.Insert(value, itemIndex.Int, newItem);
                }
                UpdateReferences();
                NotifyItemChanged(args);
            }
            else if (dictionaryDescriptor != null)
            {
                var args = new ItemChangeEventArgs(this, itemIndex, ContentChangeType.CollectionAdd, null, newItem);
                NotifyItemChanging(args);
                dictionaryDescriptor.AddToDictionary(value, itemIndex.Value, newItem);
                UpdateReferences();
                NotifyItemChanged(args);
            }
            else
                throw new NotSupportedException("Unable to set the node value, the collection is unsupported");

        }

        /// <inheritdoc/>
        public void Remove(object item, NodeIndex itemIndex)
        {
            if (itemIndex.IsEmpty) throw new ArgumentException(@"The given index should not be empty.", nameof(itemIndex));
            var args = new ItemChangeEventArgs(this, itemIndex, ContentChangeType.CollectionRemove, item, null);
            NotifyItemChanging(args);
            var collectionDescriptor = Descriptor as CollectionDescriptor;
            var dictionaryDescriptor = Descriptor as DictionaryDescriptor;
            if (collectionDescriptor != null)
            {
                if (collectionDescriptor.HasRemoveAt)
                {
                    collectionDescriptor.RemoveAt(value, itemIndex.Int);
                }
                else
                {
                    collectionDescriptor.Remove(value, item);
                }
            }
            else if (dictionaryDescriptor != null)
            {
                dictionaryDescriptor.Remove(value, itemIndex.Value);
            }
            else
                throw new NotSupportedException("Unable to set the node value, the collection is unsupported");

            UpdateReferences();
            NotifyItemChanged(args);
        }

        /// <inheritdoc/>
        protected internal override void UpdateFromMember(object newValue, NodeIndex index)
        {
            if (index == NodeIndex.Empty)
            {
                throw new InvalidOperationException("An ObjectNode value cannot be modified after it has been constructed");
            }
            Update(newValue, index, true);
        }

        protected void SetValue(object newValue)
        {
            value = newValue;
        }

        protected void NotifyItemChanging(ItemChangeEventArgs args)
        {
            PrepareChange?.Invoke(this, args);
            ItemChanging?.Invoke(this, args);
        }

        protected void NotifyItemChanged(ItemChangeEventArgs args)
        {
            ItemChanged?.Invoke(this, args);
            FinalizeChange?.Invoke(this, args);
        }

        private void Update(object newValue, NodeIndex index, bool sendNotification)
        {
            if (index == NodeIndex.Empty)
                throw new ArgumentException("index cannot be empty.");
            var oldValue = Retrieve(index);
            ItemChangeEventArgs itemArgs = null;
            if (sendNotification)
            {
                itemArgs = new ItemChangeEventArgs(this, index, ContentChangeType.CollectionUpdate, oldValue, newValue);
                NotifyItemChanging(itemArgs);
            }
            var collectionDescriptor = Descriptor as CollectionDescriptor;
            var dictionaryDescriptor = Descriptor as DictionaryDescriptor;
            if (collectionDescriptor != null)
            {
                collectionDescriptor.SetValue(Value, index.Int, ConvertValue(newValue, collectionDescriptor.ElementType));
            }
            else if (dictionaryDescriptor != null)
            {
                dictionaryDescriptor.SetValue(Value, index.Value, ConvertValue(newValue, dictionaryDescriptor.ValueType));
            }
            else
                throw new NotSupportedException("Unable to set the node value, the collection is unsupported");

            UpdateReferences();
            if (sendNotification)
            {
                NotifyItemChanged(itemArgs);
            }
        }


        private void UpdateReferences()
        {
            NodeContainer?.UpdateReferences(this);
        }

        private IEnumerable<NodeIndex> GetIndices()
        {
            var enumRef = ItemReferences;
            if (enumRef != null)
                return enumRef.Indices;

            return GetIndices(this);
        }

        public override string ToString()
        {
            return $"{{Node: Object {Type.Name} = [{Value}]}}";
        }

        /// <inheritdoc/>
        void IInitializingObjectNode.AddMember(IMemberNode member, bool allowIfReference)
        {
            if (IsSealed)
                throw new InvalidOperationException("Unable to add a child to a GraphNode that has been sealed");

            // ReSharper disable once HeuristicUnreachableCode - this code is reachable only at the specific moment we call this method!
            if (ItemReferences != null && !allowIfReference)
                throw new InvalidOperationException("A GraphNode cannot have children when its content hold a reference.");

            childrenMap.Add(member.Name, (MemberNode)member);
        }
    }
}
