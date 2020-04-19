// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using System.Linq;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters
{
    public sealed class CategoryNodeUpdater : NodePresenterUpdaterBase
    {
        public override void UpdateNode(INodePresenter node)
        {
            var member = node as MemberNodePresenter;
            if (member != null)
            {
                // First update: if a member has a CategoryAttribute, we forward this information as an attached property so we can switch its template to represent a header.
                var memberDescriptor = member.MemberDescriptor;
                if (member.MemberAttributes.OfType<CategoryAttribute>().Any())
                {
                    node.AttachedProperties.Add(CategoryData.Key, true);
                    node.AttachedProperties.Set(DisplayData.AutoExpandRuleKey, ExpandRule.Always);
                }

                // Second updater: if a member has a DisplayAttribute with a non-null Category property, we create this category if it doesn't exist and move it into it.
                var displayAttribute = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(memberDescriptor.MemberInfo);
                if (displayAttribute == null)
                    return;

                var categoryPropertyName = CategoryData.ComputeCategoryNodeName(displayAttribute.Category);
                if (node.Parent != null && !string.IsNullOrEmpty(displayAttribute.Category) && node.Parent.Name != categoryPropertyName)
                {
                    var categoryNode = node.Parent.Children.FirstOrDefault(x => x.Name == categoryPropertyName);
                    if (categoryNode == null)
                    {
                        var parent = node.Parent;
                        var orders = TypeDescriptorFactory.Default.AttributeRegistry.GetAttributes<CategoryOrderAttribute>(memberDescriptor.DeclaringType);
                        var matchingOrder = orders.FirstOrDefault(x => x.Name == displayAttribute.Category);
                        categoryNode = parent.CreateCategory(displayAttribute.Category, matchingOrder?.Order, matchingOrder?.Expand);
                    }

                    if (!(categoryNode is VirtualNodePresenter))
                        throw new InvalidOperationException("The category of the node {0} cannot be created because another node with this name already exists.");

                    node.ChangeParent(categoryNode);
                }
            }
        }
    }
}
