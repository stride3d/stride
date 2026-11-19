// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.GameStudio.Avalonia.Views;

public partial class SolutionExplorerView : UserControl
{
    private SessionViewModel? session;

    public SolutionExplorerView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (session is not null)
            session.AssetCollection.SelectedLocations.CollectionChanged -= SelectedLocationsChanged;
        session = DataContext as SessionViewModel;
        if (session is not null)
        {
            session.AssetCollection.SelectedLocations.CollectionChanged += SelectedLocationsChanged;
            Dispatcher.UIThread.Post(ExpandToSelection, DispatcherPriority.Background);
        }
    }

    /// <summary>
    /// Expands the categories, the packages and every folder of their assets.
    /// </summary>
    public void ExpandAssetFolders()
    {
        foreach (var package in ExpandPackages())
        {
            if (package.Container.ContainerFromItem(package.ViewModel.AssetMountPoint) is TreeViewItem assets)
                ExpandRecursively(assets);
        }
    }

    /// <summary>
    /// Expands the categories, the packages and everything they contain.
    /// </summary>
    public void ExpandAllFolders()
    {
        foreach (var package in ExpandPackages())
            ExpandRecursively(package.Container);
    }

    /// <summary>
    /// Collapses the packages and everything they contain, then shows the top of the tree. The categories stay expanded, with
    /// their packages in view.
    /// </summary>
    public void CollapseAllFolders()
    {
        foreach (var category in SolutionTree.GetRealizedContainers().OfType<TreeViewItem>())
        {
            foreach (var package in category.GetRealizedContainers().OfType<TreeViewItem>())
                CollapseRecursively(package);
        }
        SolutionTree.ContainerFromIndex(0)?.BringIntoView();
    }

    // Expands the categories and their packages, and returns the packages with their container
    private List<(PackageViewModel ViewModel, TreeViewItem Container)> ExpandPackages()
    {
        var packages = new List<(PackageViewModel, TreeViewItem)>();
        if (session is null)
            return packages;

        foreach (var category in session.PackageCategories.Values)
        {
            if (Expand(SolutionTree, category) is not { } categoryItem)
                continue;
            foreach (var package in category.Content)
            {
                if (Expand(categoryItem, package) is { } packageItem)
                    packages.Add((package, packageItem));
            }
        }
        return packages;
    }

    // Expands the container of the item, which then has the containers of its children
    private static TreeViewItem? Expand(ItemsControl owner, object item)
    {
        if (owner.ContainerFromItem(item) is not TreeViewItem container)
            return null;

        if (!container.IsExpanded)
        {
            container.IsExpanded = true;
            container.UpdateLayout();
        }
        return container;
    }

    private static void ExpandRecursively(TreeViewItem item)
    {
        if (item.ItemCount == 0)
            return;

        if (!item.IsExpanded)
        {
            item.IsExpanded = true;
            item.UpdateLayout();
        }
        foreach (var child in item.GetRealizedContainers().OfType<TreeViewItem>().ToList())
            ExpandRecursively(child);
    }

    private static void CollapseRecursively(TreeViewItem item)
    {
        foreach (var child in item.GetRealizedContainers().OfType<TreeViewItem>())
            CollapseRecursively(child);
        item.IsExpanded = false;
    }

    private void OnExpandAssetFoldersClick(object? sender, RoutedEventArgs e) => ExpandAssetFolders();

    private void OnExpandAllFoldersClick(object? sender, RoutedEventArgs e) => ExpandAllFolders();

    private void OnCollapseAllFoldersClick(object? sender, RoutedEventArgs e) => CollapseAllFolders();

    private void SelectedLocationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Once the tree has the new selection
        Dispatcher.UIThread.Post(ExpandToSelection, DispatcherPriority.Background);
    }

    /// <summary>
    /// Expands the folders, packages and categories that contain the selected locations, when
    /// the asset view selects an asset (e.g. the editors that open with the session).
    /// </summary>
    private void ExpandToSelection()
    {
        if (session is null)
            return;

        foreach (var location in session.AssetCollection.SelectedLocations.ToList())
        {
            var ancestors = new List<IChildViewModel>();
            for (var parent = (location as IChildViewModel)?.GetParent(); parent is not null; parent = parent.GetParent())
                ancestors.Insert(0, parent);

            ItemsControl owner = SolutionTree;
            foreach (var ancestor in ancestors)
            {
                if (owner.ContainerFromItem(ancestor) is not TreeViewItem container)
                    continue;

                if (!container.IsExpanded)
                {
                    container.IsExpanded = true;
                    // The children get their containers
                    container.UpdateLayout();
                }
                owner = container;
            }
            (owner.ContainerFromItem(location) as Control)?.BringIntoView();
        }
    }
}
