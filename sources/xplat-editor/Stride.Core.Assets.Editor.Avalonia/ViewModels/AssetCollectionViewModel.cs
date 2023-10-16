// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.ViewModels;

public sealed class AssetCollectionViewModel : DispatcherViewModel
{
    private readonly ObservableSet<AssetViewModel> assets = [];
    private readonly HashSet<DirectoryBaseViewModel> monitoredDirectories = [];
    private readonly ObservableSet<AssetViewModel> selectedAssets = [];
    private readonly ObservableSet<object> selectedContent = [];
    private object? singleSelectedContent;

    public AssetCollectionViewModel(SessionViewModel session)
        : base(session.ServiceProvider)
    {
        Session = session;

        selectedContent.CollectionChanged += SelectedContentCollectionChanged;
        SelectedLocations.CollectionChanged += SelectedLocationCollectionChanged;
    }

    public IReadOnlyObservableCollection<AssetViewModel> Assets => assets;

    /// <remarks>
    /// <see cref="SelectedAssets"/> is a sub-collection of <see cref="SelectedContent"/>.
    /// It should always be read from and never directly updated, except in <see cref="SelectedContentCollectionChanged"/>.
    /// </remarks>
    public IReadOnlyObservableCollection<AssetViewModel> SelectedAssets => selectedAssets;

    /// <summary>
    /// List of all selected items (e.g. in the asset view).
    /// </summary>
    public IReadOnlyObservableCollection<object> SelectedContent => selectedContent;

    public ObservableCollection<object> SelectedLocations { get; } = [];

    public SessionViewModel Session { get; }

    public object? SingleSelectedContent
    {
        get => singleSelectedContent;
        private set => SetValue(ref singleSelectedContent, value);
    }
 
    internal IReadOnlyCollection<DirectoryBaseViewModel> GetSelectedDirectories(bool includeSubDirectoriesOfSelected)
    {
        var selectedDirectories = new List<DirectoryBaseViewModel>();
        foreach (var location in SelectedLocations)
        {
            //var packageCategory = location as PackageCategoryViewModel;
            //if (packageCategory != null && includeSubDirectoriesOfSelected)
            //{
            //    selectedDirectories.AddRange(packageCategory.Content.Select(x => x.AssetMountPoint).NotNull());
            //}
            switch (location)
            {
                case DirectoryBaseViewModel directory:
                    selectedDirectories.Add(directory);
                    break;
                case PackageViewModel package:
                    selectedDirectories.Add(package.AssetMountPoint);
                    break;
            }
        }

        if (!includeSubDirectoriesOfSelected)
        {
            return selectedDirectories;
        }

        var result = new HashSet<DirectoryBaseViewModel>();
        foreach (var selectedDirectory in selectedDirectories)
        {
            foreach (var directory in selectedDirectory.GetDirectoryHierarchy())
            {
                result.Add(directory);
            }
        }
        return result.ToList();
    }

    private void AssetsCollectionInDirectoryChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // If the changes are too important, rebuild completely the collection.
        if (e.Action == NotifyCollectionChangedAction.Reset || e.NewItems is { Count: > 3 } || e.OldItems is { Count: > 3 })
        {
            var selectedDirectoryHierarchy = GetSelectedDirectories(false);
            UpdateAssetsCollection(selectedDirectoryHierarchy.SelectMany(x => x.Assets).ToList(), false);
        }
        // Otherwise, simply perform Adds and Removes on the current collection
        else
        {
            if (e.OldItems != null)
            {
                foreach (AssetViewModel oldItem in e.OldItems)
                {
                    assets.Remove(oldItem);
                }
            }
            if (e.NewItems != null)
            {
                var refreshFilter = false;
                foreach (AssetViewModel newItem in e.NewItems)
                {
                    assets.Add(newItem);
                    refreshFilter = true;
                }

                //if (refreshFilter)
                //{
                //    // Some assets have been added, we need to refresh sorting
                //    RefreshFilters();
                //}
            }
        }
    }

    private void SelectedContentCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateSingleSelectedContent();

        // Synchronize SelectedAssets collection
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            selectedAssets.Clear();
            selectedAssets.AddRange(SelectedContent.OfType<AssetViewModel>());
        }
        else
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<AssetViewModel>())
                {
                    selectedAssets.Remove(item);
                }
            }

            if (e.NewItems != null)
            {
                selectedAssets.AddRange(e.NewItems.OfType<AssetViewModel>());
            }
        }
    }

    private void SelectedLocationCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateLocations();
    }

    private void SubDirectoriesCollectionInDirectoryChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateLocations();
    }

    private void UpdateAssetsCollection(ICollection<AssetViewModel> newAssets, bool clearMonitoredDirectory)
    {
        if (clearMonitoredDirectory)
        {
            foreach (var directory in monitoredDirectories)
            {
                directory.Assets.CollectionChanged -= AssetsCollectionInDirectoryChanged;
                ((INotifyCollectionChanged)directory.SubDirectories).CollectionChanged -= SubDirectoriesCollectionInDirectoryChanged;
            }
        }

        var previousSelection = SelectedAssets.Where(newAssets.Contains).ToList();
        // If the selection can be restored as it is currently, prevent the CollectionChanged handler to rebuild the view model for nothing.
        if (previousSelection.Count == SelectedAssets.Count)
        {
            //discardSelectionChanges = true;
        }

        assets.Clear();
        foreach (var newAsset in newAssets)
        {
            assets.Add(newAsset);
        }

        selectedContent.Clear();
        selectedContent.AddRange(previousSelection);

        //RefreshFilters();

        //discardSelectionChanges = false;
    }

    private void UpdateLocations()
    {
        // Clear up currently monitored directories
        foreach (var directory in monitoredDirectories)
        {
            directory.Assets.CollectionChanged -= AssetsCollectionInDirectoryChanged;
            ((INotifyCollectionChanged)directory.SubDirectories).CollectionChanged -= SubDirectoriesCollectionInDirectoryChanged;
        }
        monitoredDirectories.Clear();

        var selectedDirectoryHierarchy = GetSelectedDirectories(false);
        var newAssets = new List<AssetViewModel>();
        foreach (var directory in selectedDirectoryHierarchy)
        {
            ((INotifyCollectionChanged)directory.SubDirectories).CollectionChanged += SubDirectoriesCollectionInDirectoryChanged;
            directory.Assets.CollectionChanged += AssetsCollectionInDirectoryChanged;
            monitoredDirectories.Add(directory);
            newAssets.AddRange(directory.Assets);
        }
        UpdateAssetsCollection(newAssets, false);

    }

    private void UpdateSingleSelectedContent()
    {
        SingleSelectedContent = SelectedContent.Count == 1 ? SelectedContent.First() : null;
    }
}
