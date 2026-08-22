// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Stride.Core.Annotations;
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
        /// <remarks>
        /// An expanded item has no band to insert after it. The lower part of its row adds the items as its last
        /// children instead. A line there would show above the first child of the item, but it would mean "sibling of
        /// the item". To go after an expanded item, use the gap below its last descendant and move the pointer to the
        /// left. See <see cref="ResolveInsertAfterTarget"/>.
        /// The empty area below the last item has no row to measure. Therefore it keeps bands of a fixed size.
        /// </remarks>
        protected override IInsertChildViewModel GetInsertTargetItem(FrameworkElement container, Point mousePosition, out InsertPosition insertPosition)
        {
            insertPosition = InsertPosition.Before;

            if (!(container is TreeViewItem item))
            {
                if (mousePosition.Y >= 0 && mousePosition.Y <= InsertThreshold)
                {
                    return container.DataContext as IInsertChildViewModel;
                }
                if (mousePosition.Y >= container.ActualHeight - InsertThreshold && mousePosition.Y <= container.ActualHeight)
                {
                    insertPosition = InsertPosition.After;
                    return container.DataContext as IInsertChildViewModel;
                }
                return null;
            }

            var allowAfter = !(item.IsExpanded && item.HasItems);
            var position = DropZoneCalculator.GetInsertPosition(mousePosition.Y, item.HeaderHeight, allowAfter);
            if (position == null)
                return null;

            insertPosition = position.Value;
            var target = insertPosition == InsertPosition.After ? ResolveInsertAfterTarget(item, mousePosition.X) : item;
            return target.DataContext as IInsertChildViewModel;
        }

        /// <summary>
        /// Finds the item after which the dragged items go, from the horizontal position of the pointer.
        /// </summary>
        /// <param name="item">The item that the pointer is over.</param>
        /// <param name="mouseX">The horizontal position of the pointer, relative to the item.</param>
        /// <returns>The item after which the dragged items go.</returns>
        /// <remarks>
        /// The gap below the last item of a subtree is also the gap below each ancestor that ends there. The pointer
        /// then selects one of these items by its indentation. Therefore an item can go after an expanded ancestor,
        /// which has no gap of its own.
        /// </remarks>
        [NotNull]
        private static TreeViewItem ResolveInsertAfterTarget([NotNull] TreeViewItem item, double mouseX)
        {
            var target = item;
            var child = item;
            var parent = child.ParentTreeViewItem;
            while (parent != null && IsLastChild(child, parent))
            {
                // The deepest item whose indentation the pointer has passed wins. Moving left selects an ancestor.
                if (mouseX < target.Offset)
                    target = parent;
                child = parent;
                parent = child.ParentTreeViewItem;
            }
            return target;
        }

        private static bool IsLastChild([NotNull] TreeViewItem child, [NotNull] TreeViewItem parent)
        {
            var count = parent.Items.Count;
            return count > 0 && Equals(parent.Items[count - 1], child.DataContext);
        }

        /// <inheritdoc />
        protected override void UpdateInsertVisual(FrameworkElement container, Point position, InsertPosition insertPosition)
        {
            var indent = double.NaN;
            if (insertPosition == InsertPosition.After && container is TreeViewItem item)
                indent = ResolveInsertAfterTarget(item, position.X).Offset;

            DragDropAdornerManager.UpdateInsertAdorner(container, insertPosition, indent);
        }

        /// <inheritdoc />
        /// <remarks>
        /// The header of the item is highlighted, not the item, because the height of an expanded item includes its
        /// child items. The empty area below the last item has no header. Therefore it gets no highlight.
        /// </remarks>
        protected override void UpdateDropTargetVisual(FrameworkElement container, DropAcceptance acceptance)
        {
            DragDropAdornerManager.UpdateDropTargetAdorner((container as TreeViewItem)?.HeaderElement, acceptance);
        }

        /// <inheritdoc />
        protected override void ClearDropTargetVisual()
        {
            DragDropAdornerManager.ClearDropTargetAdorner();
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
