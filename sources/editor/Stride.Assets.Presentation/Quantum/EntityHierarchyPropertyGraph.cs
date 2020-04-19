// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Core.Quantum;
using Stride.Assets.Entities;
using Stride.Engine;

namespace Stride.Assets.Presentation.Quantum
{
    [AssetPropertyGraph(typeof(EntityHierarchyAssetBase))]
    public class EntityHierarchyPropertyGraph : AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>
    {
        public EntityHierarchyPropertyGraph(AssetPropertyGraphContainer container, AssetItem assetItem, ILogger logger)
            : base(container, assetItem, logger)
        {
        }

        /// <inheritdoc/>
        protected override string PartName => nameof(EntityDesign.Entity);

        /// <inheritdoc/>
        public override IGraphNode FindTarget(IGraphNode sourceNode, IGraphNode target)
        {
            // Always prevent setting a base for children of TransformComponent: there is dedicated work to do when they change which is done elsewhere.
            var member = sourceNode as IMemberNode;
            if (member?.Name == nameof(TransformComponent.Children) && member.Parent.Retrieve() is TransformComponent)
                return null;

            return base.FindTarget(sourceNode, target);
        }

        /// <inheritdoc/>
        public override bool IsChildPartReference(IGraphNode node, NodeIndex index)
        {
            return (node as IObjectNode)?.Type == typeof(TransformComponent.TransformChildrenCollection);
        }

        /// <inheritdoc/>
        protected override bool CanUpdate(IAssetNode node, ContentChangeType changeType, NodeIndex index, object value)
        {
            // Check if we are in the component collection of an entity (where we actually add new components)
            if (IsComponentForComponentCollection(node, value) && changeType == ContentChangeType.CollectionAdd)
            {
                var componentType = value.GetType();
                var attributes = EntityComponentAttributes.Get(componentType);
                var onlySingleComponent = !attributes.AllowMultipleComponents;
                var collection = (EntityComponentCollection)node.Retrieve();

                foreach (var existingItem in collection)
                {

                    if (ReferenceEquals(existingItem, value))
                    {
                        return false;
                    }

                    if (onlySingleComponent && componentType == existingItem.GetType())
                    {
                        return false;
                    }
                }
            }

            return base.CanUpdate(node, changeType, index, value);
        }

        /// <inheritdoc/>
        protected override object CloneValueFromBase(object value, IAssetNode node)
        {
            // TODO: check CloneObjectForGameSide if modifying this method, the logic should be factorized at some point.
            if (IsComponentForComponentCollection(node, value) && value is TransformComponent)
            {
                // We never clone TransformComponent, we cannot replace them. Instead, return the existing one.
                var transformComponent = (TransformComponent)node.Retrieve(new NodeIndex(0));
                // We still reset the Entity to null to make sure it works nicely with reconcilation, etc.
                transformComponent.Entity = null;
                return transformComponent;
            }

            var clone = base.CloneValueFromBase(value, node);
            var component = clone as EntityComponent;
            if (component != null)
            {
                // Components need their Entity to be cleared first because subsequent actions will try to restore it and safeguards might throw if it's not null beforehand
                component.Entity = null;
            }
            return clone;
        }

        /// <inheritdoc />
        protected override void AddChildPartToParentPart(Entity parentPart, Entity childPart, int index)
        {
            var node = Container.NodeContainer.GetNode(parentPart.Transform);
            node[nameof(TransformComponent.Children)].Target.Add(childPart.Transform, new NodeIndex(index));
        }

        /// <inheritdoc />
        protected override void RemoveChildPartFromParentPart(Entity parentPart, Entity childPart)
        {
            var transformNode = Container.NodeContainer.GetNode(parentPart.Transform);
            var reference = parentPart.Transform.Children.Single(x => x.Entity.Id == childPart.Id);
            var childrenNode = transformNode[nameof(TransformComponent.Children)].Target;
            var index = new NodeIndex(parentPart.Transform.Children.IndexOf(reference));
            var item = childrenNode.Retrieve(index);
            childrenNode.Remove(item, index);
        }

        /// <inheritdoc />
        protected override void ReuseExistingPart(AssetCompositeHierarchyData<EntityDesign, Entity> baseHierarchy, EntityDesign clonedPart, EntityDesign existingPart)
        {
            // Update the folder information
            existingPart.Folder = clonedPart.Folder;
            base.ReuseExistingPart(baseHierarchy, clonedPart, existingPart);
        }

        /// <inheritdoc />
        protected override void RewriteIds(Entity targetPart, Entity sourcePart)
        {
            for (var i = 0; i < targetPart.Components.Count; ++i)
            {
                targetPart.Components[i].Id = sourcePart.Components[i].Id;
            }

            // If the source entity (that technically comes from baseInstanceMapping) has a parent that is still in the hierarchy, we need to clear it
            // because the fixup pass that we do right after will set the source entity as child of its parent again and this
            if (sourcePart.Transform.Parent != null && Asset.Hierarchy.Parts.ContainsKey(sourcePart.Transform.Parent.Entity.Id))
            {
                sourcePart.Transform.Parent = null;
            }
            base.RewriteIds(targetPart, sourcePart);
        }

        /// <inheritdoc />
        protected override IEnumerable<IGraphNode> RetrieveChildPartNodes(Entity part)
        {
            yield return Container.NodeContainer.GetNode(part.Transform)[nameof(TransformComponent.Children)].Target;
        }

        /// <inheritdoc />
        protected override Guid GetIdFromChildPart(object part)
        {
            return ((TransformComponent)part).Entity.Id;
        }

        private static bool IsComponentForComponentCollection(IGraphNode node, object value)
        {
            if (value is EntityComponent)
            {
                // Check if we are in the component collection of an entity (where we actually add new components)
                // or anywhere else (where we just reference components)
                var objectNode = node as IObjectNode;
                return objectNode?.Type == typeof(EntityComponentCollection);
            }
            return false;
        }
    }
}
