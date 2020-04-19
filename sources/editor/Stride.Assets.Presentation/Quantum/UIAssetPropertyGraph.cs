// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Core.Quantum;
using Stride.Assets.UI;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.Assets.Presentation.Quantum
{
    [AssetPropertyGraph(typeof(UIAssetBase))]
    public class UIAssetPropertyGraph : AssetCompositeHierarchyPropertyGraph<UIElementDesign, UIElement>
    {
        public UIAssetPropertyGraph(AssetPropertyGraphContainer container, AssetItem assetItem, ILogger logger)
            : base(container, assetItem, logger)
        {
        }

        /// <inheritdoc/>
        protected override string PartName => nameof(UIElementDesign.UIElement);

        /// <inheritdoc/>
        public override IGraphNode FindTarget(IGraphNode sourceNode, IGraphNode target)
        {
            // Always prevent setting a base for children of Panel or Content of ContentControl: there is dedicated work to do when they change which is done elsewhere.
            var member = sourceNode as IMemberNode;
            if (member?.Name == nameof(Panel.Children) && member.Parent.Retrieve() is Panel)
                return null;
            if (member?.Name == nameof(ContentControl.Content) && member.Parent.Retrieve() is ContentControl)
                return null;

            return base.FindTarget(sourceNode, target);
        }

        /// <inheritdoc/>
        public override bool IsChildPartReference(IGraphNode node, NodeIndex index)
        {
            if (node is IMemberNode memberContent)
            {
                if (memberContent.Name == nameof(ContentControl.Content) && typeof(ContentControl).IsAssignableFrom(memberContent.Parent.Type))
                    return true;
            }
            if ((node as IObjectNode)?.Type == typeof(UIElementCollection))
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        protected override void AddChildPartToParentPart(UIElement parentPart, UIElement childPart, int index)
        {
            var control = parentPart as ContentControl;
            if (control != null)
            {
                var node = Container.NodeContainer.GetOrCreateNode(control)[nameof(ContentControl.Content)];
                if (node.Retrieve() != null)
                    throw new InvalidOperationException($"{nameof(ContentControl)} (Name={control.Name}) already has a content.");

                node.Update(childPart);
                return;
            }
            var panel = parentPart as Panel;
            if (panel != null)
            {
                var node = Container.NodeContainer.GetOrCreateNode(panel)[nameof(Panel.Children)].Target;
                if (index < 0)
                    node.Add(childPart);
                else
                    node.Add(childPart, new NodeIndex(index));
                return;
            }
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        protected override void RemoveChildPartFromParentPart(UIElement parentPart, UIElement childPart)
        {
            var control = parentPart as ContentControl;
            if (control != null)
            {
                var node = Container.NodeContainer.GetOrCreateNode(control)[nameof(ContentControl.Content)];
                if (childPart != node.Retrieve())
                    throw new InvalidOperationException($"The given {childPart.GetType().Name} (Name={childPart.Name}) is not the current content of {nameof(ContentControl)} (Name={control.Name}).");

                node.Update(null);
                return;
            }
            var panel = parentPart as Panel;
            if (panel != null)
            {
                var index = panel.Children.IndexOf(childPart);
                if (index == -1)
                    throw new InvalidOperationException($"The given {childPart.GetType().Name} (Name={childPart.Name}) is not a child of {nameof(Panel)} (Name={panel.Name}).");

                var node = Container.NodeContainer.GetOrCreateNode(panel)[nameof(Panel.Children)].Target;
                node.Remove(childPart, new NodeIndex(index));
                return;
            }
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        protected override Guid GetIdFromChildPart(object part)
        {
            return ((UIElement)part).Id;
        }

        /// <inheritdoc/>
        protected override bool ShouldAddNewPartFromBase(AssetCompositeHierarchyPropertyGraph<UIElementDesign, UIElement> baseAssetGraph, UIElementDesign newPart, UIElement newPartParent, Guid instanceId)
        {
            if (!base.ShouldAddNewPartFromBase(baseAssetGraph, newPart, newPartParent, instanceId))
                return false;

            // Don't add other root element.
            // Remark: child elements of root elements that have been already discarded will also be automatically discarded
            return newPartParent != null;
        }

        /// <inheritdoc/>
        protected override IEnumerable<IGraphNode> RetrieveChildPartNodes(UIElement part)
        {
            var panel = part as Panel;
            if (panel != null)
            {
                yield return Container.NodeContainer.GetNode(panel)[nameof(Panel.Children)].Target;
            }
            var contentControl = part as ContentControl;
            if (contentControl != null)
            {
                yield return Container.NodeContainer.GetNode(contentControl)[nameof(ContentControl.Content)];
            }
        }
    }
}
