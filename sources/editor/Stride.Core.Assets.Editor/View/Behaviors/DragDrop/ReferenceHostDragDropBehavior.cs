// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Presentation.Quantum.ViewModels;

namespace Xenko.Core.Assets.Editor.View.Behaviors
{
    public class ReferenceHostDragDropBehavior : DragDropBehavior<FrameworkElement, FrameworkElement>
    {
        protected override IEnumerable<object> GetItemsToDrag(FrameworkElement container)
        {
            return Enumerable.Empty<object>();
        }

        protected override IAddChildViewModel GetDropTargetItem(FrameworkElement container)
        {
            var node = AssociatedObject.DataContext as NodeViewModel;
            if (node == null)
                return null;

            object data;
            if (!node.AssociatedData.TryGetValue(ReferenceData.AddReferenceViewModel, out data))
                return null;

            var referenceViewModel = data as IAddReferenceViewModel;
            if (referenceViewModel == null)
                return null;

            referenceViewModel.SetTargetNode(node);
            return referenceViewModel;
        }
    }
}
