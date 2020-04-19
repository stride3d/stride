// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Controls;
using Stride.Core.Presentation.Extensions;
using TreeView = Stride.Core.Presentation.Controls.TreeView;
using TreeViewItem = Stride.Core.Presentation.Controls.TreeViewItem;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    public class TreeViewDragDropBehavior : DragDropBehavior<TreeView, FrameworkElement>
    {
        /// <summary>
        /// Identifies the <see cref="AllowDropOnEmptyArea"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllowDropOnEmptyAreaProperty =
            DependencyProperty.Register(nameof(AllowDropOnEmptyArea), typeof(bool), typeof(TreeViewDragDropBehavior), new PropertyMetadata(false));

        /// <summary>
        /// Indicates whether drop operation is allowed not only on tree view items, but also on the emoty area below the last items (when the tree viewport is not full).
        /// </summary>
        public bool AllowDropOnEmptyArea { get { return (bool)GetValue(AllowDropOnEmptyAreaProperty); } set { SetValue(AllowDropOnEmptyAreaProperty, value); } }

        /// <inheritdoc />
        protected override FrameworkElement GetContainer(object source)
        {
            var frameworkElement = source as FrameworkElement;
            var contentElement = source as FrameworkContentElement;
            if (contentElement != null)
            {
                frameworkElement = contentElement.Parent as FrameworkElement;
            }
            // Either tree view item...
            FrameworkElement treeViewItem = frameworkElement as TreeViewItem ?? frameworkElement?.FindVisualParentOfType<TreeViewItem>();
            // ...or tree view panel (empty area)
            return treeViewItem ?? (AllowDropOnEmptyArea ? frameworkElement as VirtualizingTreePanel : null);
        }

        /// <inheritdoc />
        protected override IEnumerable<object> GetItemsToDrag(FrameworkElement container)
        {
            // Only tree view item can be dragged
            if (container is TreeViewItem)
            {
                var sourceItem = container.DataContext;
                return AssociatedObject.SelectedItems.Contains(sourceItem) ? AssociatedObject.SelectedItems.ToEnumerable<object>() : sourceItem.ToEnumerable<object>();
            }
            return Enumerable.Empty<object>();
        }

        /// <inheritdoc />
        protected override IInsertChildViewModel GetInsertTargetItem(FrameworkElement container, Point mousePosition, out InsertPosition insertPosition)
        {
            insertPosition = InsertPosition.Before;

            if (mousePosition.Y >= 0 && mousePosition.Y <= InsertThreshold)
            {
                insertPosition = InsertPosition.Before;
                return container.DataContext as IInsertChildViewModel;
            }
            if (mousePosition.Y >= container.ActualHeight - InsertThreshold && mousePosition.Y <= container.ActualHeight)
            {
                insertPosition = InsertPosition.After;
                return container.DataContext as IInsertChildViewModel;
            }
            return null;
        }

        /// <inheritdoc />
        protected override bool CanInitializeDrag(object originalSource)
        {
            var allItems = TreeViewElementFinder.FindAll(AssociatedObject, false);
            var items = allItems.Where(x => AssociatedObject.SelectedItems.Contains(x.DataContext)).ToArray();
            return items.All(x => !IsParentOfItem(x, items) && !x.IsEditing);
        }

        private static bool IsParentOfItem(TreeViewItem item, IEnumerable<TreeViewItem> parentCandidates)
        {
            foreach (var parent in parentCandidates)
            {
                var current = item.ParentTreeViewItem;
                while (current != null)
                {
                    if (ReferenceEquals(current, parent))
                        return true;

                    current = current.ParentTreeViewItem;
                }
            }
            return false;
        }
    }
}
