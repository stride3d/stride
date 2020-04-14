// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    public class PropertyViewItemDragDropBehavior : DragDropBehavior<FrameworkElement, PropertyViewItem>
    {
        protected override bool CanInitializeDrag(object originalSource)
        {
            var container = GetContainer(originalSource);
            var node = container?.DataContext as NodeViewModel;
            if (node?.Parent == null)
                return false;

            if (!(TypeDescriptorFactory.Default.Find(node.Parent.Type) is CollectionDescriptor))
                return false;

            object data;
            if (!node.AssociatedData.TryGetValue(CollectionData.ReorderCollectionItem, out data))
                return false;

            return data is IReorderItemViewModel && base.CanInitializeDrag(originalSource);
        }

        protected override IEnumerable<object> GetItemsToDrag(PropertyViewItem container)
        {
            var node = container.DataContext as NodeViewModel;
            if (node?.Parent == null)
                return Enumerable.Empty<object>();

            if (!(TypeDescriptorFactory.Default.Find(node.Parent.Type) is CollectionDescriptor))
                return Enumerable.Empty<object>();

            object data;
            if (!node.AssociatedData.TryGetValue(CollectionData.ReorderCollectionItem, out data))
                return Enumerable.Empty<object>();

            return data is IReorderItemViewModel ? node.Yield() : Enumerable.Empty<object>();
        }

        protected override IAddChildViewModel GetDropTargetItem(PropertyViewItem container)
        {
            return null;
        }

        protected override IInsertChildViewModel GetInsertTargetItem(PropertyViewItem container, Point mousePosition, out InsertPosition insertPosition)
        {
            insertPosition = InsertPosition.Before;

            var node = container.DataContext as NodeViewModel;
            if (node == null)
                return null;

            object data;
            if (!node.AssociatedData.TryGetValue(CollectionData.ReorderCollectionItem, out data))
                return null;

            var reorderItemViewModel = data as IReorderItemViewModel;
            if (reorderItemViewModel == null)
                return null;
            
            if (mousePosition.Y >= 0 && mousePosition.Y <= InsertThreshold)
            {
                insertPosition = InsertPosition.Before;
                reorderItemViewModel.SetTargetNode(node);
                return reorderItemViewModel;
            }
            if (mousePosition.Y >= container.ActualHeight - InsertThreshold && mousePosition.Y <= container.ActualHeight)
            {
                insertPosition = InsertPosition.After;
                reorderItemViewModel.SetTargetNode(node);
                return reorderItemViewModel;
            }
            return null;
        }
    }
}
