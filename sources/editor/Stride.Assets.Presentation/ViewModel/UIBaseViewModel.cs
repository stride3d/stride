// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Quantum;
using Stride.Assets.UI;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.Assets.Presentation.ViewModel
{
    /// <summary>
    /// Base view model for <see cref="UIAssetBase"/>.
    /// </summary>
    public abstract class UIBaseViewModel : AssetCompositeHierarchyViewModel<UIElementDesign, UIElement>
    {
        protected UIBaseViewModel([NotNull] AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
        }

        /// <summary>
        /// Inserts a new <see cref="UIElementDesign"/> into this asset.
        /// </summary>
        /// <param name="newElementCollection">A collection containing th element design to insert and all its child element, recursively.</param>
        /// <param name="child">The child element to insert.</param>
        /// <param name="parent">The parent element in which to insert, or null to insert as root element.</param>
        /// <param name="index">The index where to insert the child element. If negative, the element will be inserted at the last position.</param>
        public void InsertUIElement([NotNull] AssetPartCollection<UIElementDesign, UIElement> newElementCollection, [NotNull] UIElementDesign child, UIElement parent, int index = -1)
        {
            index = ResolveInsertionIndex(parent, index);
            AssetHierarchyPropertyGraph.AddPartToAsset(newElementCollection, child, parent, index);
        }

        /// <inheritdoc />
        protected override IObjectNode GetPropertiesRootNode()
        {
            return NodeContainer.GetNode(((UIAssetBase)Asset).Design);
        }

        /// <inheritdoc />
        protected override GraphNodePath GetPathToPropertiesRootNode()
        {
            var path = base.GetPathToPropertiesRootNode();
            path.PushMember(nameof(UIAssetBase.Design));
            path.PushTarget();
            return path;
        }

        /// <summary>
        /// Returns the index to use to insert at the end of the children collection of the given parent if <paramref name="index"/> is negative,
        /// or <paramref name="index"/> itself if it is zero or positive.
        /// </summary>
        /// <param name="parent">The parent to use to compute the insertion index. If null, the index will be computed from the list of root elements.</param>
        /// <param name="index">The index to resolve.</param>
        /// <returns>The index in which to insert a child.</returns>
        private int ResolveInsertionIndex(UIElement parent, int index)
        {
            if (parent == null)
            {
                return index >= 0 ? index : Asset.Hierarchy.RootParts.Count;
            }

            var control = parent as ContentControl;
            if (control != null)
            {
                return index >= 0 ? index : 0;
            }
            var panel = parent as Panel;
            if (panel != null)
            {
                return index >= 0 ? index : panel.Children.Count;
            }
            throw new NotSupportedException();
        }
    }
}
