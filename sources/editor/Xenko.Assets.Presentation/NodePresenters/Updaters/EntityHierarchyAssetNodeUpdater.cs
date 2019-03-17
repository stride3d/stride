// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Core.Reflection;
using Xenko.Assets.Entities;
using Xenko.Assets.Presentation.NodePresenters.Keys;
using Xenko.Assets.Presentation.ViewModel;
using Xenko.Engine;
using Xenko.Core.Presentation.Core;

namespace Xenko.Assets.Presentation.NodePresenters.Updaters
{
    internal sealed class EntityHierarchyAssetNodeUpdater : AssetNodePresenterUpdaterBase
    {
        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if (!(node.Asset?.Asset is EntityHierarchyAssetBase))
                return;

            if (node.Value is EntityComponent && node.Parent?.Type == typeof(EntityComponentCollection))
            {
                // Apply the display name of the component
                var displayAttribute = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(node.Value.GetType());
                if (!string.IsNullOrEmpty(displayAttribute?.Name))
                    node.DisplayName = displayAttribute.Name;

                node.CombineKey = node.Value.GetType().Name;
                if (node.Value is TransformComponent)
                {
                    // Always put the transformation component in first.
                    node.Order = -1;
                    var removeCommand = node.Commands.FirstOrDefault(x => x.Name == RemoveItemCommand.CommandName);
                    node.Commands.Remove(removeCommand);

                    // Remove the Children property of the transformation component (it should be accessible via the scene graph)
                    node[nameof(TransformComponent.Children)].IsVisible = false;
                }
            }
            if (node.Type == typeof(EntityComponentCollection))
            {
                var types = typeof(EntityComponent).GetInheritedInstantiableTypes()
                    .Where(x => Attribute.GetCustomAttribute(x, typeof(NonInstantiableAttribute)) == null &&
                                (EntityComponentAttributes.Get(x).AllowMultipleComponents
                                 || ((EntityComponentCollection)node.Value).All(y => y.GetType() != x)))
                    .OrderBy(DisplayAttribute.GetDisplayName)
                    .Select(x => new AbstractNodeType(x)).ToArray();
                node.AttachedProperties.Add(EntityHierarchyData.EntityComponentAvailableTypesKey, types);

                //TODO: Choose a better grouping method.
                var typeGroups =                     
                    types.GroupBy(t => ComponentCategoryAttribute.GetCategory(t.Type))
                    .OrderBy(g => g.Key)
                    .Select(g => new AbstractNodeTypeGroup(g.Key, g.ToArray())).ToArray();

                node.AttachedProperties.Add(EntityHierarchyData.EntityComponentAvailableTypeGroupsKey, typeGroups);

                // Cannot replace entity component collection.
                var replaceCommandIndex = node.Commands.IndexOf(x => x.Name == ReplacePropertyCommand.CommandName);
                if (replaceCommandIndex >= 0)
                    node.Commands.RemoveAt(replaceCommandIndex);

                // Combine components by type, but also append a index in case multiple components of the same types exist in the same collection.
                var componentCount = new Dictionary<Type, int>();
                foreach (var componentNode in node.Children)
                {
                    var type = componentNode.Value.GetType();
                    int count;
                    componentCount.TryGetValue(type, out count);
                    componentNode.CombineKey = $"{type.Name}@{count}";
                    componentCount[type] = ++count;
                }
            }
            if (typeof(EntityComponent).IsAssignableFrom(node.Type))
            {
                node.AttachedProperties.Add(ReferenceData.Key, new ComponentReferenceViewModel());
            }
            if (typeof(Entity) == node.Type)
            {
                node.AttachedProperties.Add(ReferenceData.Key, new EntityReferenceViewModel());
            }
        }
    }
}
