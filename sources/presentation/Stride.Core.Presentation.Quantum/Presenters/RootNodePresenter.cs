// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Annotations;
using Xenko.Core.Reflection;
using Xenko.Core.Quantum;

namespace Xenko.Core.Presentation.Quantum.Presenters
{
    public class RootNodePresenter : NodePresenterBase
    {
        protected readonly IObjectNode RootNode;

        public RootNodePresenter([NotNull] INodePresenterFactoryInternal factory, IPropertyProviderViewModel propertyProvider, [NotNull] IObjectNode rootNode)
            : base(factory, propertyProvider, null)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            RootNode = rootNode ?? throw new ArgumentNullException(nameof(rootNode));
            Name = "Root";
            DisplayName = string.Empty;

            foreach (var command in factory.AvailableCommands)
            {
                if (command.CanAttach(this))
                    Commands.Add(command);
            }

            rootNode.ItemChanging += OnItemChanging;
            rootNode.ItemChanged += OnItemChanged;
            AttachCommands();
        }

        public override Type Type => RootNode.Type;

        public override NodeIndex Index => NodeIndex.Empty;

        public override bool IsEnumerable => RootNode.IsEnumerable;

        [NotNull]
        public override ITypeDescriptor Descriptor => RootNode.Descriptor;

        public override object Value => RootNode.Retrieve();

        protected override IObjectNode ParentingNode => RootNode;

        public override void Dispose()
        {
            base.Dispose();
            RootNode.ItemChanging -= OnItemChanging;
            RootNode.ItemChanged -= OnItemChanged;
        }

        public override void UpdateValue(object newValue)
        {
            throw new NodePresenterException($"A {nameof(RootNodePresenter)} cannot have its own value updated.");
        }

        public override void AddItem(object value)
        {
            if (!RootNode.IsEnumerable)
                throw new NodePresenterException($"{nameof(RootNodePresenter)}.{nameof(AddItem)} cannot be invoked on objects that are not collection.");

            try
            {
                RootNode.Add(value);
            }
            catch (Exception e)
            {
                throw new NodePresenterException("An error occurred while adding an item to the node, see the inner exception for more information.", e);
            }
        }

        public override void AddItem(object value, NodeIndex index)
        {
            if (!RootNode.IsEnumerable)
                throw new NodePresenterException($"{nameof(RootNodePresenter)}.{nameof(AddItem)} cannot be invoked on objects that are not collection.");

            try
            {
                RootNode.Add(value, index);
            }
            catch (Exception e)
            {
                throw new NodePresenterException("An error occurred while adding an item to the node, see the inner exception for more information.", e);
            }
        }

        public override void RemoveItem(object value, NodeIndex index)
        {
            if (!RootNode.IsEnumerable)
                throw new NodePresenterException($"{nameof(RootNodePresenter)}.{nameof(RemoveItem)} cannot be invoked on objects that are not collection.");

            try
            {
                RootNode.Remove(value, index);
            }
            catch (Exception e)
            {
                throw new NodePresenterException("An error occurred while removing an item to the node, see the inner exception for more information.", e);
            }
        }

        public override NodeAccessor GetNodeAccessor()
        {
            return new NodeAccessor(RootNode, NodeIndex.Empty);
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
