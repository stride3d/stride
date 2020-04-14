// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Controls;
using TreeView = Xenko.Core.Presentation.Controls.TreeView;
using TreeViewItem = Xenko.Core.Presentation.Controls.TreeViewItem;

namespace Xenko.Core.Assets.Editor.View.Behaviors
{
    public class TreeViewAutoExpandBehavior : Behavior<TreeView>
    {
        private readonly HashSet<string> expandedPropertyPaths = new HashSet<string>();
        private readonly HashSet<string> collapsedPropertyPaths = new HashSet<string>();

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            AssociatedObject.PrepareItem += PrepareItem;
            AssociatedObject.ClearItem += ClearItem;
            base.OnAttached();
        }

        /// <inheritdoc/>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PrepareItem -= PrepareItem;
            AssociatedObject.ClearItem -= ClearItem;
        }

        private void PrepareItem(object sender, TreeViewItemEventArgs e)
        {
            RegisterItem(e.Container);
            foreach (var propertyItem in expandedPropertyPaths.ToList().Select(GetTreeViewItem).NotNull())
            {
                propertyItem.IsExpanded = true;
            }
            foreach (var propertyItem in collapsedPropertyPaths.ToList().Select(GetTreeViewItem).NotNull())
            {
                propertyItem.IsExpanded = false;
            }
        }

        private void ClearItem(object sender, TreeViewItemEventArgs e)
        {
            UnregisterItem(e.Container);
        }

        private TreeViewItem GetTreeViewItem(string propertyPath)
        {
            IEnumerable<TreeViewItem> currentPropertyCollection = GetItems(AssociatedObject);
            string[] members = propertyPath.Split('.');
            TreeViewItem item = null;

            foreach (var member in members.Skip(0))
            {
                item = currentPropertyCollection.FirstOrDefault(x => { var vm = x.DataContext as IChildViewModel; return vm != null && GetName(vm) == member; });
                if (item == null)
                    return null;

                currentPropertyCollection = GetItems(item);
            }
            return item;
        }

        private void RegisterItem(TreeViewItem item)
        {
            item.Expanded += ExpandedChanged;
            item.Collapsed += ExpandedChanged;

            foreach (var container in GetItems(item))
            {
                RegisterItem(container);
            }
        }

        private void UnregisterItem(TreeViewItem item)
        {
            item.Expanded -= ExpandedChanged;
            item.Collapsed -= ExpandedChanged;
            foreach (var container in GetItems(item))
            {
                UnregisterItem(container);
            }
        }

        private void ExpandedChanged(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)sender;
            var viewModel = item.DataContext as IChildViewModel;
            if (viewModel == null)
                return;
            var propertyPath = GetPath(viewModel);
            if (propertyPath == null)
                return;

            if (item.IsExpanded)
            {
                expandedPropertyPaths.Add(propertyPath);
                collapsedPropertyPaths.Remove(propertyPath);
            }
            else
            {
                expandedPropertyPaths.Remove(propertyPath);
                collapsedPropertyPaths.Add(propertyPath);
            }
        }

        private static string GetPath(IChildViewModel viewModel)
        {
            return viewModel.GetParent() != null ? GetPath(viewModel.GetParent()) + "." + GetName(viewModel) : GetName(viewModel); 
        }

        private static string GetName(IChildViewModel viewModel)
        {
            return viewModel.GetName().Replace('.', '$'); 
        }

        private static IEnumerable<TreeViewItem> GetItems(TreeViewItem parent)
        {
            var item = TreeViewElementFinder.GetFirstVirtualizedItem(parent);
            if (item == null)
                yield break;

            while (item != null)
            {
                yield return item;
                item = (TreeViewItem)TreeViewElementFinder.FindNextSibling(item);
            }
        }

        private static IEnumerable<TreeViewItem> GetItems(TreeView parent)
        {
            var item = TreeViewElementFinder.FindFirst(parent, false);
            if (item == null)
                yield break;

            while (item != null)
            {
                yield return item;
                item = (TreeViewItem)TreeViewElementFinder.FindNextSibling(item);
            }
        }
    }
}
