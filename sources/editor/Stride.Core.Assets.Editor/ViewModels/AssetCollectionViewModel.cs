// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Reflection;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// Assets are filtered via categories defined in this enum.
/// </summary>
public enum FilterCategory
{
    /// <summary>
    /// Filters the asset collection by the name of the asset.
    /// </summary>
    AssetName,
    /// <summary>
    /// Filters the asset collection by the asset's tag
    /// </summary>
    AssetTag,
    /// <summary>
    /// Filters the asset collection by the asset's type.
    /// </summary>
    AssetType
}

/// <summary>
/// Assets are sorted based on rules defined in this enum.
/// </summary>
public enum SortRule
{
    /// <summary>
    /// Sorts based on name.
    /// </summary>
    Name,
    /// <summary>
    /// Sorts based on type, asset then name
    /// </summary>
    TypeOrderThenName,
    /// <summary>
    /// Sorts based on if the asset has unsaved changes, then name.
    /// </summary>
    DirtyThenName,
    /// <summary>
    /// Sorts based on date modified, then name.
    /// </summary>
    ModificationDateThenName,
}

/// <summary>
/// Filters which assets are meant to be shown based on folder hierarchy.
/// </summary>
public enum DisplayAssetMode
{
    /// <summary>
    /// Filters assets that are only in the selected folder.
    /// </summary>
    AssetInSelectedFolderOnly,
    /// <summary>
    /// Filters assets and folders that are in the selected folder.
    /// </summary>
    AssetAndFolderInSelectedFolder,
    /// <summary>
    /// Filters assets that are in selected folders and sub-folders.
    /// </summary>
    AssetInSelectedFolderAndSubFolder,
}

public sealed class AssetCollectionViewModel : DispatcherViewModel
{
    private readonly ObservableSet<AssetViewModel> assets = [];
    private readonly HashSet<DirectoryBaseViewModel> monitoredDirectories = [];
    private readonly ObservableSet<AssetViewModel> selectedAssets = [];
    private readonly ObservableSet<object> selectedContent = [];
    private readonly List<AssetFilterViewModel> typeFilters = [];
    private readonly ObservableList<AssetFilterViewModel> availableAssetFilters = [];
    private readonly ObservableSet<AssetFilterViewModel> currentAssetFilters = [];
    private object? singleSelectedContent;

    private bool discardSelectionChanges;
    private DisplayAssetMode displayAssetMode;
    private SortRule sortRule;

    public AssetCollectionViewModel(SessionViewModel session)
        : base(session.SafeArgument().ServiceProvider)
    {
        Session = session;

        // Initialize the view model that will manage the properties of the assets selected on the main asset view
        AssetViewProperties = new SessionObjectPropertiesViewModel(session);

        typeFilters.AddRange(AssetRegistry.GetPublicTypes()
            .Where(type => type != typeof(Package))
            .Select(type => new AssetFilterViewModel(this, FilterCategory.AssetType, type.FullName!, TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(type)?.Name ?? type.Name)));
        typeFilters.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.InvariantCultureIgnoreCase));

        SelectAssetCommand = new AnonymousCommand<AssetViewModel>(ServiceProvider, x => SelectAssets(x.Yield()!));
        AddAssetFilterCommand = new AnonymousCommand<AssetFilterViewModel>(ServiceProvider, AddAssetFilter);
        ClearAssetFiltersCommand = new AnonymousCommand(ServiceProvider, ClearAssetFilters);
        RefreshAssetFilterCommand = new AnonymousCommand<AssetFilterViewModel>(ServiceProvider, RefreshAssetFilter);
        ChangeDisplayAssetModeCommand = new AnonymousCommand<DisplayAssetMode>(ServiceProvider, mode => DisplayAssetMode = mode);
        SortAssetsCommand = new AnonymousCommand<SortRule>(ServiceProvider, rule => SortRule = rule);

        currentAssetFilters.CollectionChanged += (_, e) =>
        {
            if (e.OldItems != null)
                foreach (AssetFilterViewModel f in e.OldItems)
                    f.PropertyChanged -= OnFilterPropertyChanged;
            if (e.NewItems != null)
                foreach (AssetFilterViewModel f in e.NewItems)
                    f.PropertyChanged += OnFilterPropertyChanged;
            RefreshFilters();
        };
        selectedContent.CollectionChanged += SelectedContentCollectionChanged;
        SelectedLocations.CollectionChanged += SelectedLocationCollectionChanged;
    }

    public IReadOnlyObservableCollection<AssetViewModel> Assets => assets;
    /// <summary>
    /// A list of assets after the filter rules are applied to the <see cref="Assets"/>
    /// </summary>
    public IReadOnlyObservableCollection<AssetViewModel> FilteredAssets => filteredAssets;
    private readonly ObservableList<AssetViewModel> filteredAssets = [];

    /// <summary>
    /// A list of asset filters that can currently be applied.
    /// </summary>
    public IReadOnlyObservableCollection<AssetFilterViewModel> AvailableAssetFilters => availableAssetFilters;

    /// <summary>
    /// A list of applied asset filters.
    /// </summary>
    public IReadOnlyObservableCollection<AssetFilterViewModel> CurrentAssetFilters => currentAssetFilters;

    /// <summary>
    /// The filter text actively being searched.
    /// </summary>
    public string? AssetFilterPattern
    {
        get;
        set => SetValue(ref field, value, () => UpdateAvailableAssetFilters(value));
    }

    /// <summary>
    /// Adds the asset filters. See <see cref="AddAssetFilter"/>.
    /// </summary>
    public ICommandBase AddAssetFilterCommand { get; }
    
    /// <summary>
    /// Clears the asset filters. See <see cref="ClearAssetFilters"/>.
    /// </summary>
    public ICommandBase ClearAssetFiltersCommand { get; }

    /// <summary>
    /// Adds an asset filter, but replaces it if it's of the same type. See <see cref="RefreshAssetFilter"/>.
    /// </summary>
    public ICommandBase RefreshAssetFilterCommand { get; }

    /// <summary>
    /// Updates the <see cref="DisplayAssetMode"/>.
    /// </summary>
    public ICommandBase ChangeDisplayAssetModeCommand { get; }

    /// <summary>
    /// Updates the <see cref="SortRule"/>.
    /// </summary>
    public ICommandBase SortAssetsCommand { get; }

    /// <summary>
    /// Current value of <see cref="ViewModels.DisplayAssetMode"/>.
    /// </summary>
    public DisplayAssetMode DisplayAssetMode
    {
        get => displayAssetMode;
        set => SetValue(ref displayAssetMode, value, UpdateLocations);
    }
    /// <summary>
    /// Current value of <see cref="ViewModels.SortRule"/>.
    /// </summary>
    public SortRule SortRule
    {
        get => sortRule;
        set => SetValue(ref sortRule, value, RefreshFilters);
    }
    
    private void AddAssetFilter(AssetFilterViewModel filter)
    {
        filter.IsActive = true;
        currentAssetFilters.Add(filter);
    }

    private void ClearAssetFilters()
    {
        foreach (var filter in currentAssetFilters.ToList())
            RemoveAssetFilter(filter);
    }

    private void RefreshAssetFilter(AssetFilterViewModel filter)
    {
        foreach (var f in currentAssetFilters.Where(f => f.Category == filter.Category).ToList())
            RemoveAssetFilter(f);
        AddAssetFilter(filter);
    }

    /// <summary>
    /// Removes an asset filter.
    /// </summary>
    /// <param name="filter"></param>
    public void RemoveAssetFilter(AssetFilterViewModel filter)
    {
        filter.IsActive = false;
        currentAssetFilters.Remove(filter);
    }

    private void UpdateAvailableAssetFilters(string? filterText)
    {
        availableAssetFilters.Clear();
        if (string.IsNullOrEmpty(filterText))
            return;

        availableAssetFilters.Add(new AssetFilterViewModel(this, FilterCategory.AssetName, filterText, filterText));
        availableAssetFilters.Add(new AssetFilterViewModel(this, FilterCategory.AssetTag, filterText, filterText));

        foreach (var filter in typeFilters)
        {
            if (filter.DisplayName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                availableAssetFilters.Add(filter);
        }
    }
    
    private void RefreshFilters()
    {
        filteredAssets.Clear();
        // Add assets either that matches the filter or are currently selected.
        var filteredList = Assets.Where(x => selectedAssets.Contains(x) || Match(x)).ToList();
        var nameComparer = new NaturalStringComparer();
        var assetNameComparer = new AnonymousComparer<AssetViewModel>((x, y) => nameComparer.Compare(x?.Name, y?.Name));
        int GetTypeOrder(Type t) =>
            TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(t)?.Order ?? 0;

        IComparer<AssetViewModel> comparer = sortRule switch
        {
            SortRule.TypeOrderThenName => new AnonymousComparer<AssetViewModel>((x, y) =>
            {
                ArgumentNullException.ThrowIfNull(x);
                ArgumentNullException.ThrowIfNull(y);
                var r = -GetTypeOrder(x.AssetType).CompareTo(GetTypeOrder(y.AssetType));
                return r == 0 ? assetNameComparer.Compare(x, y) : r;
            }),
            SortRule.DirtyThenName => new AnonymousComparer<AssetViewModel>((x, y) =>
            {
                ArgumentNullException.ThrowIfNull(x);
                ArgumentNullException.ThrowIfNull(y);
                var r = -x.IsDirty.CompareTo(y.IsDirty);
                return r == 0 ? assetNameComparer.Compare(x, y) : r;
            }),
            SortRule.ModificationDateThenName => new AnonymousComparer<AssetViewModel>((x, y) =>
            {
                ArgumentNullException.ThrowIfNull(x);
                ArgumentNullException.ThrowIfNull(y);
                var r = -x.AssetItem.ModifiedTime.CompareTo(y.AssetItem.ModifiedTime);
                return r == 0 ? assetNameComparer.Compare(x, y) : r;
            }),
            _ => assetNameComparer
        };

        filteredList.Sort(comparer);
        filteredAssets.AddRange(filteredList);
    }

    private void OnFilterPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AssetFilterViewModel.IsActive))
            RefreshFilters();
    }

    private bool Match(AssetViewModel asset)
    {
        // Type filters are OR-ed
        var activeTypeFilters = currentAssetFilters.Where(f => f is { Category: FilterCategory.AssetType, IsActive: true }).ToList();
        if (activeTypeFilters.Count > 0 && !activeTypeFilters.Any(f => f.Match(asset)))
            return false;

        // Name and tag filters are AND-ed
        return currentAssetFilters.Where(f => f.Category != FilterCategory.AssetType && f.IsActive).All(f => f.Match(asset));
    }
    
    /// <summary>
    /// Gets the <see cref="SessionObjectPropertiesViewModel"/> associated to the current selection in the collection.
    /// </summary>
    // FIXME: do we need both ActiveProperties and AssetCollection.AssetViewProperties?
    //        could be related to reusing this class for ReferencesViewModel and EditorDialogService
    public SessionObjectPropertiesViewModel AssetViewProperties { get; }

    /// <remarks>
    /// <see cref="SelectedAssets"/> is a sub-collection of <see cref="SelectedContent"/>.
    /// It should always be read from and never directly updated, except in <see cref="SelectedContentCollectionChanged"/>.
    /// </remarks>
    public IReadOnlyObservableList<AssetViewModel> SelectedAssets => selectedAssets;

    /// <summary>
    /// List of all selected items (e.g. in the asset view).
    /// </summary>
    public IReadOnlyObservableCollection<object> SelectedContent => selectedContent;

    public ObservableCollection<object> SelectedLocations { get; } = [];

    public SessionViewModel Session { get; }

    public AssetViewModel? SingleSelectedAsset => SingleSelectedContent as AssetViewModel;

    public object? SingleSelectedContent
    {
        get => singleSelectedContent;
        private set => SetValue(ref singleSelectedContent, value);
    }

    public ICommandBase SelectAssetCommand { get; }

    public void SelectAssets(IEnumerable<AssetViewModel> assetsToSelect)
    {
        Dispatcher.EnsureAccess();

        var assetList = assetsToSelect.ToList();

        // Ensure the location of the assets to select are themselves selected.
        var locations = new HashSet<DirectoryBaseViewModel>(assetList.Select(x => x.Directory));
        if (locations.All(x => !SelectedLocations.Contains(x)))
        {
            SelectedLocations.Clear();
            SelectedLocations.AddRange(locations);
        }

        // Don't reselect if the current selection is the same
        if (assetList.Count != SelectedAssets.Count || !assetList.All(x => SelectedAssets.Contains(x)))
        {
            selectedContent.Clear();
            // FIXME xplat-editor filters
            selectedContent.AddRange(assetList);
        }
    }

    internal IReadOnlyCollection<DirectoryBaseViewModel> GetSelectedDirectories(bool includeSubDirectoriesOfSelected)
    {
        var selectedDirectories = new List<DirectoryBaseViewModel>();
        foreach (var location in SelectedLocations)
        {
            if (location is PackageCategoryViewModel packageCategory && includeSubDirectoriesOfSelected)
            {
                selectedDirectories.AddRange(packageCategory.Content.Select(x => x.AssetMountPoint).NotNull());
            }
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

    internal void UpdateAssetsCollection(ICollection<AssetViewModel> newAssets)
    {
        UpdateAssetsCollection(newAssets, true);
    }

    private void AssetsCollectionInDirectoryChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // If the changes are too important, rebuild completely the collection.
        if (e.Action == NotifyCollectionChangedAction.Reset || e.NewItems is { Count: > 3 } || e.OldItems is { Count: > 3 })
        {
            var includeSubFolders = displayAssetMode == DisplayAssetMode.AssetInSelectedFolderAndSubFolder;
            var selectedDirectoryHierarchy = GetSelectedDirectories(includeSubFolders);
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

                if (refreshFilter)
                {
                    // Some assets have been added, we need to refresh sorting
                    RefreshFilters();
                }
            }
        }
    }

    private async void SelectedContentCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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

        if (discardSelectionChanges)
            return;

        AssetViewProperties.UpdateTypeAndName(SelectedAssets, x => x.TypeDisplayName, x => x.Url, "assets");
        await AssetViewProperties.GenerateSelectionPropertiesAsync(SelectedAssets);
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
            discardSelectionChanges = true;
        }

        assets.Clear();
        foreach (var newAsset in newAssets)
        {
            assets.Add(newAsset);
        }

        selectedContent.Clear();
        selectedContent.AddRange(previousSelection);

        RefreshFilters();

        discardSelectionChanges = false;
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

        var includeSubFolders = displayAssetMode == DisplayAssetMode.AssetInSelectedFolderAndSubFolder;
        var selectedDirectoryHierarchy = GetSelectedDirectories(includeSubFolders);
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
