// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Windows.Controls;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Controls
{
    public static class TreeViewElementFinder
    {
        [CanBeNull]
        public static TreeViewItem FindNext(TreeViewItem treeViewItem, bool visibleOnly)
        {
            while (true)
            {
                // find first child
                if (treeViewItem.IsExpanded || !visibleOnly)
                {
                    var item = GetFirstVirtualizedItem(treeViewItem);
                    if (item != null)
                    {
                        if (item.IsEnabled && !visibleOnly || item.IsVisible)
                        {
                            return item;
                        }
                        treeViewItem = item;
                        continue;
                    }
                }

                // find next sibling
                var sibling = FindNextSiblingRecursive(treeViewItem) as TreeViewItem;
                return sibling != null ? (!visibleOnly || sibling.IsVisible ? sibling : null) : null;
            }
        }

        [CanBeNull]
        public static TreeViewItem GetFirstVirtualizedItem([NotNull] TreeViewItem treeViewItem)
        {
            for (var i = 0; i < treeViewItem.Items.Count; i++)
            {
                var item = treeViewItem.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                if (item != null)
                    return item;
            }

            return null;
        }

        [CanBeNull]
        public static ItemsControl FindNextSibling(ItemsControl itemsControl)
        {
            var parentIc = ItemsControl.ItemsControlFromItemContainer(itemsControl);
            if (parentIc == null)
                return null;

            var index = parentIc.ItemContainerGenerator.IndexFromContainer(itemsControl);
            return parentIc.ItemContainerGenerator.ContainerFromIndex(index + 1) as ItemsControl; // returns null if index to large or nothing found
        }

        /// <summary>
        /// Returns the first item. If tree is virtualized, it is the first realized item.
        /// </summary>
        /// <param name="treeView">The tree.</param>
        /// <param name="visibleOnly">If true, returns the first visible item.</param>
        /// <returns>Returns a TreeViewItem.</returns>
        [CanBeNull]
        public static TreeViewItem FindFirst([NotNull] TreeView treeView, bool visibleOnly)
        {
            for (var i = 0; i < treeView.Items.Count; i++)
            {
                var item = treeView.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                if (item == null) continue;
                if (!visibleOnly || item.IsVisible) return item;
            }
            return null;
        }

        /// <summary>
        /// Returns the last item. If tree is virtualized, it is the last realized item.
        /// </summary>
        /// <param name="treeView">The tree.</param>
        /// <param name="visibleOnly">If true, returns the last visible item.</param>
        /// <returns>Returns a TreeViewItem.</returns>
        [CanBeNull]
        public static TreeViewItem FindLast([NotNull] TreeView treeView, bool visibleOnly)
        {
            for (var i = treeView.Items.Count - 1; i >= 0; i--)
            {
                var item = treeView.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                if (item == null) continue;
                if (!visibleOnly || item.IsVisible) return item;
            }
            return null;
        }

        /// <summary>
        /// Returns all items in tree recursively. If virtualization is enabled, only realized items are returned.
        /// </summary>
        /// <param name="treeView">The tree.</param>
        /// <param name="visibleOnly">True if only visible items should be returned.</param>
        /// <returns>Returns an enumerable of items.</returns>
        [ItemNotNull]
        public static IEnumerable<TreeViewItem> FindAll([NotNull] TreeView treeView, bool visibleOnly)
        {
            var currentItem = FindFirst(treeView, visibleOnly);
            while (currentItem != null)
            {
                if (!visibleOnly || currentItem.IsVisible) yield return currentItem;
                currentItem = FindNext(currentItem, visibleOnly);
            }
        }

        [CanBeNull]
        private static ItemsControl FindNextSiblingRecursive(ItemsControl itemsControl)
        {
            while (true)
            {
                var parentIc = ItemsControl.ItemsControlFromItemContainer(itemsControl);
                if (parentIc == null)
                    return null;
                var index = parentIc.ItemContainerGenerator.IndexFromContainer(itemsControl);
                if (index < parentIc.Items.Count - 1)
                {
                    return parentIc.ItemContainerGenerator.ContainerFromIndex(index + 1) as ItemsControl; // returns null if index to large or nothing found
                }

                itemsControl = parentIc;
            }
        }
    }
}
