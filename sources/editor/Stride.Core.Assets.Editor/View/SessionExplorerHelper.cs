// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Presentation.ViewModel;
using TreeView = Stride.Core.Presentation.Controls.TreeView;
using TreeViewItem = Stride.Core.Presentation.Controls.TreeViewItem;

namespace Stride.Core.Assets.Editor.View
{
    public static class SessionExplorerHelper
    {
        static SessionExplorerHelper()
        {
            CommandManager.RegisterClassCommandBinding(typeof(TreeView), new CommandBinding(ExpandAssetFolders, OnExpandAssetsFolders));
            CommandManager.RegisterClassCommandBinding(typeof(TreeView), new CommandBinding(ExpandAllFolders, OnExpandAllFolders));
            CommandManager.RegisterClassCommandBinding(typeof(TreeView), new CommandBinding(CollapseAllFolders, OnCollapseAllFolders));
        }

        public static RoutedCommand ExpandAssetFolders { get; } = new RoutedCommand(nameof(ExpandAssetFolders), typeof(TreeView));

        public static RoutedCommand ExpandAllFolders { get; } = new RoutedCommand(nameof(ExpandAllFolders), typeof(TreeView));

        public static RoutedCommand CollapseAllFolders { get; } = new RoutedCommand(nameof(CollapseAllFolders), typeof(TreeView));

        private static void OnExpandAssetsFolders(object sender, ExecutedRoutedEventArgs e)
        {
            var treeView = (TreeView)sender;
            foreach (PackageCategoryViewModel category in treeView.Items)
            {
                ExpandPackageCategory(treeView, category, true);
            }
        }

        private static void OnExpandAllFolders(object sender, ExecutedRoutedEventArgs e)
        {
            var treeView = (TreeView)sender;
            foreach (PackageCategoryViewModel category in treeView.Items)
            {
                ExpandPackageCategory(treeView, category, false);
            }
        }

        private static void OnCollapseAllFolders(object sender, ExecutedRoutedEventArgs e)
        {
            var treeView = (TreeView)sender;
            for (var i = 0; i < treeView.Items.Count; i++)
            {
                var categoryItem = treeView.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                if (categoryItem == null)
                    continue;

                for (var j = 0; j < categoryItem.Items.Count; j++)
                {
                    var packageItem = categoryItem.ItemContainerGenerator.ContainerFromIndex(j) as TreeViewItem;
                    if (packageItem == null)
                        continue;
                    CollapseRecursively(packageItem);
                }
            }
            // Reset scrolling to the top element
            (treeView.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement)?.BringIntoView();
        }

        private static void CollapseRecursively([NotNull] TreeViewItem item)
        {
            for (var i = 0; i < item.Items.Count; i++)
            {
                var child = item.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                if (child == null)
                    continue;

                CollapseRecursively(child);
            }
            item.IsExpanded = false;
        }

        private static void ExpandPackageCategory([NotNull] TreeView treeView, PackageCategoryViewModel category, bool assetsOnly)
        {
            treeView.Dispatcher.BeginInvoke(new Action(() =>
            {
                var item = treeView.GetTreeViewItemFor(category);
                if (item == null)
                    return;

                item.IsExpanded = true;
                foreach (var package in category.Content)
                {
                    ExpandPackage(treeView, package, assetsOnly);
                }
            }), DispatcherPriority.ApplicationIdle);

        }

        private static void ExpandPackage([NotNull] TreeView treeView, PackageViewModel package, bool assetsOnly)
        {
            treeView.Dispatcher.BeginInvoke(new Action(() =>
            {
                var item = treeView.GetTreeViewItemFor(package);
                if (item == null)
                    return;

                item.IsExpanded = true;
                if (assetsOnly)
                {
                    ExpandDirectories(treeView, package.AssetMountPoint);
                }
                else
                {
                    foreach (var content in package.Content)
                    {
                        ExpandViewModel(treeView, content);
                    }
                }
            }), DispatcherPriority.ApplicationIdle);

        }

        private static void ExpandDirectories([NotNull] TreeView treeView, DirectoryBaseViewModel directory)
        {
            treeView.Dispatcher.BeginInvoke(new Action(() =>
            {
                var item = treeView.GetTreeViewItemFor(directory);
                if (item == null)
                    return;

                item.IsExpanded = true;
                foreach (var subDirectory in directory.SubDirectories)
                {
                    ExpandDirectories(treeView, subDirectory);
                }
            }), DispatcherPriority.ApplicationIdle);
        }

        private static void ExpandViewModel([NotNull] TreeView treeView, ViewModelBase viewModel)
        {
            var directory = viewModel as DirectoryBaseViewModel;
            if (directory != null)
            {
                ExpandDirectories(treeView, directory);
            }
            else
            {
                treeView.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var item = treeView.GetTreeViewItemFor(viewModel);
                    if (item == null)
                        return;

                    item.IsExpanded = true;
                }), DispatcherPriority.ApplicationIdle);
            }
        }
    }
}
