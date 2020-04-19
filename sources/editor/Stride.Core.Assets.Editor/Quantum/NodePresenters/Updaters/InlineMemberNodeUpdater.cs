// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Reflection;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters
{
    internal sealed class InlineMemberNodeUpdater : NodePresenterUpdaterBase
    {
        public override void FinalizeTree(INodePresenter root)
        {
            // Note: this needs to be done in FinalizeTree, because if the inlined property itself is modified, UpdateNode won't be called again on the parent property,
            TransferAttachedProperties(root);
            foreach (var node in root.Children.SelectDeep(x => x.Children))
            {
                TransferAttachedProperties(node);
            }
            base.FinalizeTree(root);
        }

        private static void TransferAttachedProperties(INodePresenter node)
        {
            if (TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<InlinePropertyAttribute>(node.Type) != null)
                node.AttachedProperties.Set(InlineData.InlineMemberKey, true);

            if (node.Value == null)
                return;

            // Hide the Enabled properties - they must be inlined in their parent node to be usable
            if (node.Name == "Enabled")
                node.IsVisible = false;

            var type = node.Value.GetType();
            var properties = type.GetProperties();
            string mainProperty = null;
            // Inner properties of a inlined node are usually never expanded, unless explicitly stated
            var expand = ExpandRule.Never;

            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttribute<InlinePropertyAttribute>();
                if (attribute != null)
                {
                    if (mainProperty != null)
                        throw new InvalidOperationException("Multiple properties of the same node have the InlinePropertyAttribute.");
                    mainProperty = property.Name;
                    expand = attribute.Expand;
                }
            }

            if (mainProperty != null)
            {
                node.AttachedProperties.Set(DisplayData.AutoExpandRuleKey, expand);
                node.AttachedProperties.Set(InlineData.InlineMemberKey, true);

                // If the updater has already been run, the property is already properly named.
                var mainPropertyNode = node.TryGetChild(InlineData.InlinedProperty) ?? node[mainProperty];
                if (mainPropertyNode != null)
                {
                    TransferAttachedProperty(NumericData.MinimumKey, node, mainPropertyNode);
                    TransferAttachedProperty(NumericData.MaximumKey, node, mainPropertyNode);
                    TransferAttachedProperty(NumericData.SmallStepKey, node, mainPropertyNode);
                    TransferAttachedProperty(NumericData.LargeStepKey, node, mainPropertyNode);
                    TransferAttachedProperty(NumericData.DecimalPlacesKey, node, mainPropertyNode);
                    mainPropertyNode.Rename(InlineData.InlinedProperty);
                    mainPropertyNode.IsVisible = false;
                    node.AddDependency(mainPropertyNode, false);
                }
            }
        }

        private static void TransferAttachedProperty([NotNull] PropertyKey propertyKey, [NotNull] INodePresenter from, [NotNull] INodePresenter to)
        {
            object value;
            if (from.AttachedProperties.TryGetValue(propertyKey, out value))
                to.AttachedProperties.SetObject(propertyKey, value);
        }
    }
}
