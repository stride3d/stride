// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Reflection;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Core.Reflection;
using Xenko.Core.Quantum.References;

namespace Xenko.Core.Quantum
{
    /// <summary>
    /// An implementation of <see cref="IGraphNode"/> that gives access to a member of an object.
    /// </summary>
    public class MemberNode : GraphNodeBase, IMemberNode, IGraphNodeInternal
    {
        public MemberNode([NotNull] INodeBuilder nodeBuilder, Guid guid, [NotNull] IObjectNode parent, [NotNull] IMemberDescriptor memberDescriptor, IReference reference)
            : base(nodeBuilder.SafeArgument(nameof(nodeBuilder)).NodeContainer, guid, nodeBuilder.TypeDescriptorFactory.Find(memberDescriptor.Type))
        {
            if (nodeBuilder == null) throw new ArgumentNullException(nameof(nodeBuilder));
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            MemberDescriptor = memberDescriptor ?? throw new ArgumentNullException(nameof(memberDescriptor));
            Name = memberDescriptor.Name;
            TargetReference = reference as ObjectReference;
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public IObjectNode Parent { get; }

        /// <summary>
        /// The <see cref="IMemberDescriptor"/> used to access the member of the container represented by this content.
        /// </summary>
        public IMemberDescriptor MemberDescriptor { get; protected set; }

        public override bool IsReference => TargetReference != null;

        /// <inheritdoc/>
        public ObjectReference TargetReference { get; }

        /// <inheritdoc/>
        public IObjectNode Target => TargetReference?.TargetNode;

        /// <inheritdoc/>
        public event EventHandler<INodeChangeEventArgs> PrepareChange;

        /// <inheritdoc/>
        public event EventHandler<INodeChangeEventArgs> FinalizeChange;

        /// <inheritdoc/>
        public event EventHandler<MemberNodeChangeEventArgs> ValueChanging;

        /// <inheritdoc/>
        public event EventHandler<MemberNodeChangeEventArgs> ValueChanged;

        /// <inheritdoc/>
        protected sealed override object Value
        {
            get
            {
                var container = Parent.Retrieve();
                if (container == null) throw new InvalidOperationException("Container's value is null");
                return MemberDescriptor.Get(container);
            }
        }

        /// <inheritdoc/>
        public void Update(object newValue)
        {
            Update(newValue, true);
        }

        /// <summary>
        /// Raises the <see cref="ValueChanging"/> event with the given parameters.
        /// </summary>
        /// <param name="args">The arguments of the event.</param>
        protected void NotifyContentChanging(MemberNodeChangeEventArgs args)
        {
            PrepareChange?.Invoke(this, args);
            ValueChanging?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="ValueChanged"/> event with the given arguments.
        /// </summary>
        /// <param name="args">The arguments of the event.</param>
        protected void NotifyContentChanged(MemberNodeChangeEventArgs args)
        {
            ValueChanged?.Invoke(this, args);
            FinalizeChange?.Invoke(this, args);
        }

        protected internal override void UpdateFromMember(object newValue, NodeIndex index)
        {
            if (index != NodeIndex.Empty) throw new ArgumentException(@"index must be Index.Empty.", nameof(NodeIndex));
            Update(newValue, false);
        }

        private void Update(object newValue, bool sendNotification)
        {
            var oldValue = Retrieve();
            MemberNodeChangeEventArgs args = null;
            if (sendNotification)
            {
                args = new MemberNodeChangeEventArgs(this, oldValue, newValue);
                NotifyContentChanging(args);
            }
            var containerValue = Parent.Retrieve();
            if (containerValue == null)
                throw new InvalidOperationException("Container's value is null");
            MemberDescriptor.Set(containerValue, ConvertValue(newValue, MemberDescriptor.Type));

            if (containerValue.GetType().GetTypeInfo().IsValueType)
                ((GraphNodeBase)Parent).UpdateFromMember(containerValue, NodeIndex.Empty);

            UpdateReferences();
            if (sendNotification)
            {
                NotifyContentChanged(args);
            }
        }

        private void UpdateReferences()
        {
            NodeContainer?.UpdateReferences(this);
        }

        public override string ToString()
        {
            return $"{{Node: Member {Name} = [{Value}]}}";
        }
    }
}
