// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Presenters
{
    public class VirtualNodePresenter : NodePresenterBase
    {
        protected NodeAccessor AssociatedNode;
        [NotNull] private readonly Func<object> getter;
        private readonly Action<object> setter;
        private readonly List<Attribute> memberAttributes = new List<Attribute>();
        private bool updatingValue;

        public VirtualNodePresenter([NotNull] INodePresenterFactoryInternal factory, IPropertyProviderViewModel propertyProvider, [NotNull] INodePresenter parent, string name, [NotNull] Type type, int? order, [NotNull] Func<object> getter, Action<object> setter)
            : base(factory, propertyProvider, parent)
        {
            this.getter = getter;
            this.setter = setter;
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (getter == null) throw new ArgumentNullException(nameof(getter));
            Name = name;
            CombineKey = Name;
            DisplayName = Name;
            Type = type;
            Order = order;
            Descriptor = TypeDescriptorFactory.Default.Find(type);

            AttachCommands();
        }

        public override Type Type { get; }

        public override bool IsEnumerable => false;

        public override NodeIndex Index => AssociatedNode.Node != null ? AssociatedNode.Index : NodeIndex.Empty;

        public override ITypeDescriptor Descriptor { get; }

        public override object Value => getter();

        public IReadOnlyList<Attribute> MemberAttributes => memberAttributes;

        protected override IObjectNode ParentingNode => null;

        /// <summary>
        /// Registers an <see cref="IGraphNode"/> object to this virtual node so when the node vakye is modified, it will raise the
        /// <see cref="NodePresenterBase.ValueChanging"/> and <see cref="NodePresenterBase.ValueChanged"/> events.
        /// </summary>
        /// <param name="associatedNodeAccessor">An accessor to the node to register.</param>
        /// <remarks>Events subscriptions are cleaned when this virtual node is disposed.</remarks>
        public virtual void RegisterAssociatedNode(NodeAccessor associatedNodeAccessor)
        {
            if (AssociatedNode.Node != null)
                throw new InvalidOperationException("A content has already been registered to this virtual node");

            AssociatedNode = associatedNodeAccessor;
            var memberNode = AssociatedNode.Node as IMemberNode;
            if (memberNode != null)
            {
                memberNode.ValueChanging += AssociatedNodeChanging;
                memberNode.ValueChanged += AssociatedNodeChanged;
            }
            var objectNode = AssociatedNode.Node as IObjectNode;
            if (objectNode != null)
            {
                objectNode.ItemChanging += AssociatedNodeChanging;
                objectNode.ItemChanged += AssociatedNodeChanged;
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            var memberNode = AssociatedNode.Node as IMemberNode;
            if (memberNode != null)
            {
                memberNode.ValueChanging -= AssociatedNodeChanging;
                memberNode.ValueChanged -= AssociatedNodeChanged;
            }
            var objectNode = AssociatedNode.Node as IObjectNode;
            if (objectNode != null)
            {
                objectNode.ItemChanging -= AssociatedNodeChanging;
                objectNode.ItemChanged -= AssociatedNodeChanged;
            }
            base.Dispose();
        }

        /// <inheritdoc/>
        public override void UpdateValue(object newValue)
        {
            try
            {
                var oldValue = getter();
                var changeType = Index == NodeIndex.Empty ? ContentChangeType.ValueChange : ContentChangeType.CollectionUpdate;
                RaiseNodeChanging(newValue, changeType, Index);
                updatingValue = true;
                setter(newValue);
                updatingValue = false;
                RaiseNodeChanged(oldValue, changeType, Index);
            }
            catch (Exception e)
            {
                throw new NodePresenterException("An error occurred while updating the value of the node, see the inner exception for more information.", e);
            }
            finally
            {
                // Note: not sure it is worth doing this in finally block. Currently if we have an exception here we're already screwed.
                updatingValue = false;
            }
        }

        /// <inheritdoc/>
        public override void AddItem(object value)
        {
            throw new NodePresenterException($"{nameof(AddItem)} cannot be used on a {nameof(VirtualNodePresenter)}.");
        }

        /// <inheritdoc/>
        public override void AddItem(object value, NodeIndex index)
        {
            throw new NodePresenterException($"{nameof(AddItem)} cannot be used on a {nameof(VirtualNodePresenter)}.");
        }

        /// <inheritdoc/>
        public override void RemoveItem(object value, NodeIndex index)
        {
            throw new NodePresenterException($"{nameof(RemoveItem)} cannot be used on a {nameof(VirtualNodePresenter)}.");
        }

        /// <inheritdoc/>
        public override NodeAccessor GetNodeAccessor()
        {
            return AssociatedNode;
        }

        private void AssociatedNodeChanging(object sender, [NotNull] INodeChangeEventArgs e)
        {
            RaiseNodeChanging(e.NewValue, e.ChangeType, (e as ItemChangeEventArgs)?.Index ?? NodeIndex.Empty);
        }

        private void AssociatedNodeChanged(object sender, [NotNull] INodeChangeEventArgs e)
        {
            RaiseNodeChanged(e.OldValue, e.ChangeType, (e as ItemChangeEventArgs)?.Index ?? NodeIndex.Empty);
        }

        private void RaiseNodeChanging(object newValue, ContentChangeType changeType, NodeIndex index)
        {
            if (ShouldRaiseEvent(changeType, index))
            {
                RaiseValueChanging(newValue);
            }
        }

        private void RaiseNodeChanged(object oldValue, ContentChangeType changeType, NodeIndex index)
        {
            if (ShouldRaiseEvent(changeType, index))
            {
                RaiseValueChanged(oldValue);
            }
        }

        private bool ShouldRaiseEvent(ContentChangeType changeType, NodeIndex index)
        {
            if (updatingValue)
                return false;

            if (AssociatedNode.Node == null || AssociatedNode.Index == NodeIndex.Empty)
                return true;

            return index != NodeIndex.Empty && ItemNodePresenter.IsValidChange(changeType, index, Index);
        }
    }
}
