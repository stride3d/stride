// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters
{
    internal sealed class DisplayAttributeNodeUpdater : NodePresenterUpdaterBase
    {
        public override void UpdateNode(INodePresenter node)
        {
            var expandRule = ExpandRule.Auto;

            var displayAttribute = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(node.Value?.GetType() ?? node.Type);
            if (displayAttribute != null)
            {
                if (!string.IsNullOrEmpty(displayAttribute.Name))
                {
                    // Put the display name of the type on a specific attached property, it can be used in some templates.
                    node.AttachedProperties.Add(DisplayData.AttributeDisplayNameKey, displayAttribute.Name);
                }
                // First override of the expand rule is from the type.
                expandRule = displayAttribute.Expand;
            }

            // Propagate DisplayName from the DisplayAttribute on the member to the node presenter.
            var member = node as MemberNodePresenter;
            displayAttribute = member?.MemberAttributes.OfType<DisplayAttribute>().FirstOrDefault();
            if (displayAttribute != null)
            {
                if (displayAttribute.Name != null)
                {
                    node.DisplayName = displayAttribute.Name;
                }
                // Second override of the expand rule is from the member.
                expandRule = displayAttribute.Expand;
            }

            // Override the expand rule if needed.
            if (expandRule != ExpandRule.Auto)
            {
                node.AttachedProperties.Set(DisplayData.AutoExpandRuleKey, expandRule);
            }

        }
    }
}
