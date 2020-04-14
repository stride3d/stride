// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Xenko.Core.Assets.Editor.ViewModel.CopyPasteProcessors;
using Xenko.Core.Extensions;
using Xenko.Core.Reflection;
using Xenko.Core.Quantum;
using Xenko.Engine;

namespace Xenko.Assets.Presentation.ViewModel.CopyPasteProcessors
{
    public class EntityComponentPasteProcessor : AssetPropertyPasteProcessor
    {
        /// <inheritdoc/>
        public override bool Accept(Type targetRootType, Type targetMemberType, Type pastedDataType)
        {
            return (targetMemberType == typeof(EntityComponent) || targetMemberType == typeof(EntityComponentCollection)) &&
                   typeof(EntityComponent).IsAssignableFrom(TypeDescriptorFactory.Default.Find(pastedDataType).GetInnerCollectionType());
        }

        /// <inheritdoc/>
        protected override bool CanRemoveItem(IObjectNode collection, NodeIndex index)
        {
            if (collection.Type != typeof(EntityComponentCollection))
                return base.CanRemoveItem(collection, index);

            // Cannot remove the transform component
            if (index.Int == 0)
                return false;

            return base.CanRemoveItem(collection, index);
        }

        /// <inheritdoc/>
        protected override bool CanInsertItem(IObjectNode collection, NodeIndex index, object newItem)
        {
            if (collection.Type != typeof(EntityComponentCollection))
                return base.CanInsertItem(collection, index, newItem);

            if (newItem == null)
                return false;

            var componentType = newItem.GetType();
            if (!EntityComponentAttributes.Get(componentType).AllowMultipleComponents)
            {
                // Cannot insert components that disallow multiple components
                var components = (EntityComponentCollection)collection.Retrieve();
                if (components.Any(x => x.GetType() == componentType))
                    return false;
            }
            return base.CanInsertItem(collection, index, newItem);
        }

        /// <inheritdoc/>
        protected override bool CanReplaceItem(IObjectNode collection, NodeIndex index, object newItem)
        {
            if (collection.Type != typeof(EntityComponentCollection))
                return base.CanReplaceItem(collection, index, newItem);

            if (newItem == null)
                return false;

            var componentType = newItem.GetType();
            // Cannot replace the transform component by another type of component
            if (collection.IndexedTarget(index).Type == typeof(TransformComponent) && componentType != typeof(TransformComponent))
                return false;

            if (!EntityComponentAttributes.Get(componentType).AllowMultipleComponents)
            {
                // Cannot replace components that disallow multiple components, unless it is that specific component we're replacing
                var components = (EntityComponentCollection)collection.Retrieve();
                if (components.Where((x, i) => x.GetType() == componentType && i != index.Int).Any())
                    return false;
            }
            return base.CanReplaceItem(collection, index, newItem);
        }

        /// <inheritdoc/>
        protected override void ReplaceItem(IObjectNode collection, NodeIndex index, object newItem)
        {
            // If we're replacing the transform component, only manually copy allowed properties to the existing one.
            if (collection.Type == typeof(EntityComponentCollection) && newItem is TransformComponent newTransform)
            {
                var node = collection.IndexedTarget(index);
                node[nameof(TransformComponent.Position)].Update(newTransform.Position);
                node[nameof(TransformComponent.Rotation)].Update(newTransform.Rotation);
                node[nameof(TransformComponent.Scale)].Update(newTransform.Scale);
                return;
            }
            base.ReplaceItem(collection, index, newItem);
        }
    }
}
