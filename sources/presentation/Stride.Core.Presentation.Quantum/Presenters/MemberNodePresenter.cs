// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Presenters
{
    public class MemberNodePresenter : NodePresenterBase
    {
        protected readonly IMemberNode Member;
        private readonly List<Attribute> memberAttributes = new List<Attribute>();

        public MemberNodePresenter([NotNull] INodePresenterFactoryInternal factory, IPropertyProviderViewModel propertyProvider, [NotNull] INodePresenter parent, [NotNull] IMemberNode member)
            : base(factory, propertyProvider, parent)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            Member = member ?? throw new ArgumentNullException(nameof(member));
            Name = member.Name;
            CombineKey = Name;
            DisplayName = Name;
            IsReadOnly = !Member.MemberDescriptor.HasSet;
            memberAttributes.AddRange(TypeDescriptorFactory.Default.AttributeRegistry.GetAttributes(member.MemberDescriptor.MemberInfo));

            member.ValueChanging += OnMemberChanging;
            member.ValueChanged += OnMemberChanged;

            if (member.Target != null)
            {
                member.Target.ItemChanging += OnItemChanging;
                member.Target.ItemChanged += OnItemChanged;
            }
            var displayAttribute = memberAttributes.OfType<DisplayAttribute>().FirstOrDefault();
            Order = displayAttribute?.Order ?? member.MemberDescriptor.Order;

            AttachCommands();
        }

        public override void Dispose()
        {
            base.Dispose();
            Member.ValueChanging -= OnMemberChanging;
            Member.ValueChanged -= OnMemberChanged;
            if (Member.Target != null)
            {
                Member.Target.ItemChanging -= OnItemChanging;
                Member.Target.ItemChanged -= OnItemChanged;
            }
        }

        public override Type Type => Member.Type;

        public override bool IsEnumerable => Member.Target?.IsEnumerable ?? false;

        public override NodeIndex Index => NodeIndex.Empty;

        [NotNull]
        public override ITypeDescriptor Descriptor => Member.Descriptor;

        public override object Value => Member.Retrieve();

        [NotNull]
        public IMemberDescriptor MemberDescriptor => Member.MemberDescriptor;

        public IReadOnlyList<Attribute> MemberAttributes => memberAttributes;

        protected override IObjectNode ParentingNode => Member.Target;

        public override void UpdateValue(object newValue)
        {
            try
            {
                Member.Update(newValue);
            }
            catch (Exception e)
            {
                throw new NodePresenterException("An error occurred while updating the value of the node, see the inner exception for more information.", e);
            }
        }

        public override void AddItem(object value)
        {
            if (Member.Target == null || !Member.Target.IsEnumerable)
                throw new NodePresenterException($"{nameof(MemberNodePresenter)}.{nameof(AddItem)} cannot be invoked on members that are not collection.");

            try
            {
                Member.Target.Add(value);
            }
            catch (Exception e)
            {
                throw new NodePresenterException("An error occurred while adding an item to the node, see the inner exception for more information.", e);
            }
        }

        public override void AddItem(object value, NodeIndex index)
        {
            if (Member.Target == null || !Member.Target.IsEnumerable)
                throw new NodePresenterException($"{nameof(MemberNodePresenter)}.{nameof(AddItem)} cannot be invoked on members that are not collection.");

            try
            {
                Member.Target.Add(value, index);
            }
            catch (Exception e)
            {
                throw new NodePresenterException("An error occurred while adding an item to the node, see the inner exception for more information.", e);
            }
        }

        public override void RemoveItem(object value, NodeIndex index)
        {
            if (Member.Target == null || !Member.Target.IsEnumerable)
                throw new NodePresenterException($"{nameof(MemberNodePresenter)}.{nameof(RemoveItem)} cannot be invoked on members that are not collection.");

            try
            {
                Member.Target.Remove(value, index);
            }
            catch (Exception e)
            {
                throw new NodePresenterException("An error occurred while removing an item to the node, see the inner exception for more information.", e);
            }
        }

        public override NodeAccessor GetNodeAccessor()
        {
            return new NodeAccessor(Member, NodeIndex.Empty);
        }

        private void OnMemberChanging(object sender, MemberNodeChangeEventArgs e)
        {
            RaiseValueChanging(Value);
            if (Member.Target != null)
            {
                Member.Target.ItemChanging -= OnItemChanging;
                Member.Target.ItemChanged -= OnItemChanged;
            }
        }

        private void OnMemberChanged(object sender, MemberNodeChangeEventArgs e)
        {
            Refresh();
            if (Member.Target != null)
            {
                Member.Target.ItemChanging += OnItemChanging;
                Member.Target.ItemChanged += OnItemChanged;
            }
            RaiseValueChanged(Value);
        }

        private void OnItemChanging(object sender, ItemChangeEventArgs e)
        {
            RaiseValueChanging(Value);
        }

        private void OnItemChanged(object sender, ItemChangeEventArgs e)
        {
            Refresh();
            RaiseValueChanged(Value);
        }
    }
}
