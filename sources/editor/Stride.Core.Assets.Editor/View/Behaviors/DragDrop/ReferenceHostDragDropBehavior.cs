// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.View.Behaviors
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
