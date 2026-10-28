// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.ComponentModel;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Reflection;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModels;

public enum FilterCategory
{
    AssetName,
    AssetTag,
    AssetType
}

public enum SortRule
{
    Name,
    TypeOrderThenName,
    DirtyThenName,
    ModificationDateThenName,
}

public enum DisplayAssetMode
{
    AssetInSelectedFolderOnly,
    AssetAndFolderInSelectedFolder,
    AssetInSelectedFolderAndSubFolder,
}

/// <summary>
/// The assets shown in an asset view, with their filters and selection. The editors of the assets are managed by <see cref="EditorCollectionViewModel"/>.
/// </summary>
public sealed class AssetCollectionViewModel : DispatcherViewModel, IAssetCollectionViewModel
{
    public static readonly IEnumerable<FilterCategory> AllFilterCategories = Enum.GetValues<FilterCategory>();

    private readonly ObservableSet<AssetViewModel> assets = [];
    private readonly ObservableList<AssetViewModel> filteredAssets = [];
    private readonly ObservableList<object> filteredContent = [];
    private readonly HashSet<DirectoryBaseViewModel> monitoredDirectories = [];
    /// <remarks>
    /// <see cref="selectedAssets"/> is a sub-collection of <see cref="selectedContent"/>. It should always be read from and never directly updated, except in <see cref="SelectedContentCollectionChanged"/>.
    /// </remarks>
    private readonly ObservableSet<AssetViewModel> selectedAssets = [];
    /// <summary>
    /// List of all selected items (e.g. in the asset view).
    /// </summary>
    private readonly ObservableSet<object> selectedContent = [];
    private readonly List<AssetFilterViewModel> typeFilters = [];
    private int typeFiltersVersion = -1;
    private readonly Dictionary<FilterCategory, bool> availableFilterCategories;
    private readonly ObservableList<AssetFilterViewModel> availableAssetFilters = [];
    private readonly ObservableSet<AssetFilterViewModel> currentAssetFilters = [];
    private object? singleSelectedContent;
    private Func<AssetViewModel, bool>? customFilter;

    private bool discardSelectionChanges;
    private bool refreshing;
    private DisplayAssetMode displayAssetMode;
    private SortRule sortRule;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetCollectionViewModel"/> class.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="isMainView">Whether this is the main asset view of the session, which persists its display settings and filters and shows the package properties.</param>
    /// <param name="filterCategories">The categories of filters the user can add; all of them by default.</param>
    public AssetCollectionViewModel(SessionViewModel session, bool isMainView = false, IEnumerable<FilterCategory>? filterCategories = null)
        : base(session.SafeArgument().ServiceProvider)
    {
        refreshing = true;
        Session = session;
        IsMainView = isMainView;

        // Initialize the view model that will manage the properties of the assets selected on this asset view
        AssetViewProperties = new SessionObjectPropertiesViewModel(session);

        availableFilterCategories = AllFilterCategories.ToDictionary(f => f, f => filterCategories is null);
        foreach (var category in filterCategories ?? [])
        {
            availableFilterCategories[category] = true;
        }

        if (isMainView)
        {
            displayAssetMode = InternalSettings.AssetViewDisplayMode.GetValue();
            sortRule = InternalSettings.AssetViewSortRule.GetValue();
        }

        SelectAssetCommand = new AnonymousCommand<AssetViewModel>(ServiceProvider, x => SelectAssets(x.Yield()!));
        DeleteContentCommand = new AnonymousTaskCommand(ServiceProvider, () => DeleteContent(SelectedContent));
        CutLocationsCommand = new AnonymousTaskCommand(ServiceProvider, CutSelectedLocationsAsync, CanCopy);
        CopyLocationsCommand = new AnonymousTaskCommand(ServiceProvider, CopySelectedLocationsAsync, CanCopy);
        CutContentCommand = new AnonymousTaskCommand(ServiceProvider, CutSelectedContentAsync, CanCopy);
        CopyContentCommand = new AnonymousTaskCommand(ServiceProvider, CopySelectedContentAsync, CanCopy);
        CopyAssetsRecursivelyCommand = new AnonymousTaskCommand(ServiceProvider, CopySelectedAssetsRecursivelyAsync, CanCopy);
        CopyAssetUrlCommand = new AnonymousTaskCommand(ServiceProvider, CopyAssetUrlAsync);
        PasteCommand = new AnonymousTaskCommand(ServiceProvider, PasteAsync, CanCopy);
        AddAssetFilterCommand = new AnonymousCommand<AssetFilterViewModel>(ServiceProvider, AddAssetFilter);
        ClearAssetFiltersCommand = new AnonymousCommand(ServiceProvider, ClearAssetFilters);
        RefreshAssetFilterCommand = new AnonymousCommand<AssetFilterViewModel>(ServiceProvider, RefreshAssetFilter);
        ChangeDisplayAssetModeCommand = new AnonymousCommand<DisplayAssetMode>(ServiceProvider, mode => DisplayAssetMode = mode);
        SortAssetsCommand = new AnonymousCommand<SortRule>(ServiceProvider, rule => { SortRule = rule; UpdateCommands(); });

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
        filteredAssets.CollectionChanged += FilteredAssetsCollectionChanged;
        if (isMainView)
        {
            LoadAssetFilters();
        }
        refreshing = false;

        DependentProperties.Add(nameof(DisplayAssetMode), [nameof(DisplayLocationContentRecursively)]);
        DependentProperties.Add(nameof(SingleSelectedContent), [nameof(SingleSelectedAsset)]);
    }

    public SessionViewModel Session { get; }

    /// <summary>
    /// Gets whether this is the main asset view of the session.
    /// </summary>
    public bool IsMainView { get; }

    // FIXME: do we need both ActiveProperties and AssetCollection.AssetViewProperties?
    //        could be related to reusing this class for ReferencesViewModel and EditorDialogService
    public SessionObjectPropertiesViewModel AssetViewProperties { get; }

    public IReadOnlyObservableCollection<AssetViewModel> Assets => assets;

    public int AssetCount => assets.Count;

    /// <summary>
    /// Gets the assets that pass the filters, sorted.
    /// </summary>
    public IReadOnlyObservableCollection<AssetViewModel> FilteredAssets => filteredAssets;

    /// <summary>
    /// Gets the folders and assets that pass the filters, sorted.
    /// </summary>
    public IReadOnlyObservableCollection<object> FilteredContent => filteredContent;

    /// <summary>
    /// One filter per asset type, rebuilt when the session's asset assemblies changed (a plugin added, assemblies
    /// reloaded or unloaded).
    /// </summary>
    public IReadOnlyCollection<AssetFilterViewModel> TypeFilters
    {
        get
        {
            if (typeFiltersVersion != AssetRegistry.TypesVersion)
            {
                typeFiltersVersion = AssetRegistry.TypesVersion;
                typeFilters.Clear();
                typeFilters.AddRange(AssetRegistry.GetPublicTypes()
                    .Where(type => type != typeof(Package))
                    .Select(type => new AssetFilterViewModel(this, FilterCategory.AssetType, type.FullName!, DisplayAttribute.GetDisplayName(type) ?? type.Name)));
                typeFilters.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.InvariantCultureIgnoreCase));
            }
            return typeFilters;
        }
    }

    public IReadOnlyObservableCollection<AssetFilterViewModel> AvailableAssetFilters => availableAssetFilters;

    public IReadOnlyObservableCollection<AssetFilterViewModel> CurrentAssetFilters => currentAssetFilters;

    public string? AssetFilterPattern
    {
        get;
        set => SetValue(ref field, value, () => UpdateAvailableAssetFilters(value));
    }

    /// <summary>
    /// Gets or sets an additional filter the assets must pass (e.g. the accepted types of a picker).
    /// </summary>
    public Func<AssetViewModel, bool>? CustomFilter { get => customFilter; set => SetValue(ref customFilter, value, RefreshFilters); }

    public DisplayAssetMode DisplayAssetMode
    {
        get => displayAssetMode;
        set => SetValue(ref displayAssetMode, value, UpdateLocations);
    }

    public bool DisplayLocationContentRecursively => DisplayAssetMode == DisplayAssetMode.AssetInSelectedFolderAndSubFolder;

    public SortRule SortRule
    {
        get => sortRule;
        set => SetValue(ref sortRule, value, RefreshFilters);
    }

    public IReadOnlyObservableList<AssetViewModel> SelectedAssets => selectedAssets;

    public IReadOnlyObservableCollection<object> SelectedContent => selectedContent;

    /// <summary>
    /// Gets the package of the selected assets, when they all belong to the same one.
    /// </summary>
    public PackageViewModel? SelectedAssetsPackage
    {
        get
        {
            var packages = SelectedAssets.Select(x => x.Directory.Package).Distinct().ToArray();
            return packages.Length == 1 ? packages[0] : null;
        }
    }

    /// <summary>
    /// List of selected locations (in the solution explorer).
    /// </summary>
    public ObservableList<object> SelectedLocations { get; } = [];

    public AssetViewModel? SingleSelectedAsset => SingleSelectedContent as AssetViewModel;

    public object? SingleSelectedContent
    {
        get => singleSelectedContent;
        private set => SetValue(ref singleSelectedContent, value);
    }

    public ICommandBase SelectAssetCommand { get; }

    public ICommandBase DeleteContentCommand { get; }

    public ICommandBase CutLocationsCommand { get; }

    public ICommandBase CopyLocationsCommand { get; }

    public ICommandBase CutContentCommand { get; }

    public ICommandBase CopyContentCommand { get; }

    public ICommandBase CopyAssetsRecursivelyCommand { get; }

    public ICommandBase CopyAssetUrlCommand { get; }

    public ICommandBase PasteCommand { get; }

    public ICommandBase AddAssetFilterCommand { get; }

    public ICommandBase ClearAssetFiltersCommand { get; }

    public ICommandBase RefreshAssetFilterCommand { get; }

    public ICommandBase ChangeDisplayAssetModeCommand { get; }

    public ICommandBase SortAssetsCommand { get; }

    IReadOnlyObservableCollection<AssetViewModel> IAssetCollectionViewModel.SelectedAssets => selectedAssets;

    private IDialogService Dialogs => ServiceProvider.Get<IDialogService>();

    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(AssetCollectionViewModel));
        base.Destroy();
    }

    public void ClearSelection()
    {
        selectedContent.Clear();
    }

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
            selectedContent.AddRange(assetList.Where(x => filteredAssets.Contains(x)));
        }

        UpdateCommands();
    }

    public IReadOnlyCollection<DirectoryBaseViewModel> GetSelectedDirectories(bool includeSubDirectoriesOfSelected)
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
            var hierarchy = new List<DirectoryBaseViewModel>();
            selectedDirectory.GetDirectoryHierarchy(hierarchy);
            foreach (var directory in hierarchy)
            {
                result.Add(directory);
            }
        }
        return result.ToList();
    }

    public void UpdateAssetsCollection(ICollection<AssetViewModel> newAssets)
    {
        UpdateAssetsCollection(newAssets, true);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteContent(IReadOnlyCollection<object> locations, bool skipConfirmation = false)
    {
        var result = await Session.DeleteItems(locations, skipConfirmation);
        UpdateCommands();
        return result;
    }

    /// <summary>
    /// Deletes the given assets in a single transaction without asking for confirmation nor fixing broken references.
    /// Assets whose <see cref="AssetViewModel.CanDelete()"/> method returns <c>false</c> won't be deleted, unless <paramref name="forceDelete"/> is <c>true</c>.
    /// </summary>
    /// <param name="assetsToDelete">The list of assets to delete.</param>
    /// <param name="forceDelete">If <c>true</c> the asset whose <see cref="AssetViewModel.CanDelete()"/> method returns <c>false</c> will still be deleted</param>
    /// <returns>The number of assets that have been successfully deleted.</returns>
    internal int DeleteAssets(IEnumerable<AssetViewModel> assetsToDelete, bool forceDelete = false)
    {
        var undoRedoService = Session.UndoRedoService;
        using var transaction = undoRedoService?.CreateTransaction();
        var deletedAssets = new List<AssetViewModel>();
        foreach (var asset in assetsToDelete.Where(x => forceDelete || x.CanDelete()))
        {
            if (asset.Directory == null)
                throw new InvalidOperationException("The asset directory cannot be null before deleting an asset.");

            // This must be done before we clear the Directory property of the asset
            AssetDependenciesViewModel.NotifyAssetChanged(asset.Session, asset);

            var oldDirectory = asset.Directory;

            // It is important to set IsDeleted before clearing the directory, so the parent project can be marked as dirty
            asset.IsDeleted = true;
            asset.Directory.RemoveAsset(asset);
            asset.Directory = null!;

            // Update RootAssets, for both current package and packages referencing this one
            // Note: Package to Asset references should be handled in a more generic way (same as Asset to Asset references)
            // We check only local
            foreach (var localPackage in Session.LocalPackages)
            {
                localPackage.RootAssets.Remove(asset);
            }

            oldDirectory.Package.CheckConsistency();
            deletedAssets.Add(asset);
        }
        if (transaction is not null)
            undoRedoService!.SetName(transaction, deletedAssets.Count == 1 ? $"Delete asset '{deletedAssets[0]}'" : $"Delete {deletedAssets.Count} assets");
        return deletedAssets.Count;
    }

    #region Filters
    public void AddAssetFilter(AssetFilterViewModel? filter)
    {
        if (filter is null)
            return;

        filter.IsActive = true;
        currentAssetFilters.Insert(0, filter);
        SaveAssetFilters();
    }

    public void ClearAssetFilters()
    {
        currentAssetFilters.RemoveWhere(f => !f.IsReadOnly);
        SaveAssetFilters();
    }

    public void RefreshAssetFilter(AssetFilterViewModel? filter)
    {
        if (filter is null)
            return;

        refreshing = true;
        foreach (var f in currentAssetFilters.Where(f => f.Category == filter.Category).ToList())
        {
            RemoveAssetFilter(f);
        }
        refreshing = false;
        AddAssetFilter(filter);
    }

    public void RemoveAssetFilter(AssetFilterViewModel? filter)
    {
        if (filter is null)
            return;

        filter.IsActive = false;
        currentAssetFilters.Remove(filter);
        SaveAssetFilters();
    }

    /// <summary>
    /// Loads the asset filters of the main view from the internal settings.
    /// </summary>
    private void LoadAssetFilters()
    {
        var storedFilters = InternalSettings.ViewFilters.GetValue().ToList();
        // Flip order of filters to stay consistent
        storedFilters.Reverse();
        foreach (var data in storedFilters)
        {
            var filter = new AssetFilterViewModel(this, data.category, data.Filter, data.DisplayName) { IsActive = data.IsActive };
            currentAssetFilters.Insert(0, filter);
        }
        // Save out in case editor is immediately closed
        SaveAssetFilters();
    }

    /// <summary>
    /// Saves the primitive data of the current filters of the main view to the internal settings.
    /// </summary>
    private void SaveAssetFilters()
    {
        if (!IsMainView)
            return;

        // Skip the engine-defined (read-only) filters
        var data = currentAssetFilters.Where(x => !x.IsReadOnly).Select(filter => new AssetFilterViewModelData
        {
            DisplayName = filter.DisplayName,
            Filter = filter.Filter,
            IsActive = filter.IsActive,
            category = filter.Category,
        }).ToList();
        InternalSettings.ViewFilters.SetValue(data);
    }

    private void UpdateAvailableAssetFilters(string? filterText)
    {
        availableAssetFilters.Clear();
        if (string.IsNullOrEmpty(filterText))
            return;

        // Name
        if (availableFilterCategories[FilterCategory.AssetName])
        {
            availableAssetFilters.Add(new AssetFilterViewModel(this, FilterCategory.AssetName, filterText, filterText));
        }
        // Type
        if (availableFilterCategories[FilterCategory.AssetType])
        {
            foreach (var filter in TypeFilters)
            {
                if (filter.DisplayName.Contains(filterText, StringComparison.OrdinalIgnoreCase))
                    availableAssetFilters.Add(filter);
            }
        }
        // Tag
        if (availableFilterCategories[FilterCategory.AssetTag])
        {
            availableAssetFilters.Add(new AssetFilterViewModel(this, FilterCategory.AssetTag, filterText, filterText));
        }
    }

    private void RefreshFilters()
    {
        if (!refreshing)
        {
            refreshing = true;
            filteredAssets.Clear();
            // Add assets either that matches the filter or are currently selected.
            var filteredList = Assets.Where(x => selectedAssets.Contains(x) || Match(x)).ToList();
            var nameComparer = new NaturalStringComparer();
            var assetNameComparer = new AnonymousComparer<AssetViewModel>((x, y) => nameComparer.Compare(x?.Name, y?.Name));

            IComparer<AssetViewModel> comparer = sortRule switch
            {
                SortRule.TypeOrderThenName => new AnonymousComparer<AssetViewModel>((x, y) =>
                {
                    ArgumentNullException.ThrowIfNull(x);
                    ArgumentNullException.ThrowIfNull(y);
                    var r = -(DisplayAttribute.GetOrder(x.AssetType) ?? 0).CompareTo(DisplayAttribute.GetOrder(y.AssetType) ?? 0);
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
                // Sort by name by default.
                _ => assetNameComparer
            };

            filteredList.Sort(comparer);
            filteredAssets.AddRange(filteredList);
            refreshing = false;

            // Force updating the filtered content
            UpdateFilteredContent();
        }

        if (IsMainView)
        {
            InternalSettings.AssetViewSortRule.SetValue(SortRule);
        }
    }

    private void OnFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AssetFilterViewModel.IsActive))
        {
            RefreshFilters();
            SaveAssetFilters();
        }
    }

    private bool Match(AssetViewModel asset)
    {
        if (CustomFilter is not null && !CustomFilter(asset))
            return false;

        // Type filters are OR-ed
        var activeTypeFilters = currentAssetFilters.Where(f => f is { Category: FilterCategory.AssetType, IsActive: true }).ToList();
        if (activeTypeFilters.Count > 0 && !activeTypeFilters.Any(f => f.Match(asset)))
            return false;

        // Name and tag filters are AND-ed
        return currentAssetFilters.Where(f => f.Category != FilterCategory.AssetType && f.IsActive).All(f => f.Match(asset));
    }

    private void FilteredAssetsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Don't rebuild collection when refreshing is in progress
        if (refreshing)
            return;

        UpdateFilteredContent();
    }

    private void UpdateFilteredContent()
    {
        // simple implementation: rebuild the FilteredContent collection each time (could be improve)
        filteredContent.Clear();

        if (displayAssetMode != DisplayAssetMode.AssetAndFolderInSelectedFolder)
        {
            filteredContent.AddRange(filteredAssets);
            return;
        }

        // Filter folders by name
        var nameFilters = currentAssetFilters.Where(f => f is { Category: FilterCategory.AssetName, IsActive: true }).ToList();
        IEnumerable<object> folders = monitoredDirectories.SelectMany(d => d.SubDirectories)
            .Where(d => nameFilters.All(f => AssetFilterViewModel.ComputeTokens(f.Filter).All(x => d.Name.Contains(x, StringComparison.OrdinalIgnoreCase))));
        filteredContent.AddRange(folders.Concat(filteredAssets));
    }
    #endregion // Filters

    #region Copy/paste
    private bool CanCopy()
    {
        return ServiceProvider.TryGet<ICopyPasteService>() is not null && ServiceProvider.TryGet<IClipboardService>() is not null;
    }

    private async Task CopyAssetUrlAsync()
    {
        if (SingleSelectedAsset is null || ServiceProvider.TryGet<IClipboardService>() is not { } clipboard)
            return;

        try
        {
            await clipboard.SetTextAsync(SingleSelectedAsset.Url);
        }
        catch (SystemException e)
        {
            // We don't provide feedback when copying fails.
            e.Ignore();
        }
    }

    private async Task CutSelectedLocationsAsync()
    {
        var directories = GetSelectedDirectories(false);
        await CutSelectionAsync(directories, null);
        UpdateCommands();
    }

    private async Task CopySelectedLocationsAsync()
    {
        var directories = GetSelectedDirectories(false);
        await CopySelectionAsync(directories, null);
        UpdateCommands();
    }

    private async Task CutSelectedContentAsync()
    {
        var directories = SelectedContent.OfType<DirectoryBaseViewModel>().ToList();
        await CutSelectionAsync(directories, SelectedAssets);
        UpdateCommands();
    }

    private async Task CopySelectedContentAsync()
    {
        var directories = SelectedContent.OfType<DirectoryBaseViewModel>().ToList();
        await CopySelectionAsync(directories, SelectedAssets);
        UpdateCommands();
    }

    private async Task CopySelectedAssetsRecursivelyAsync()
    {
        var assetsToCopy = new ObservableSet<AssetViewModel>();
        foreach (var asset in SelectedAssets)
        {
            assetsToCopy.Add(asset);
            assetsToCopy.AddRange(asset.Dependencies.RecursiveReferencedAssets.Where(a => a.IsEditable));
        }

        await CopySelectionAsync(null, assetsToCopy);
        UpdateCommands();
    }

    private async Task CutSelectionAsync(IReadOnlyCollection<DirectoryBaseViewModel>? directories, IEnumerable<AssetViewModel>? assetsToCut)
    {
        // Ensure all directories can be cut
        if (directories?.Any(d => !d.IsEditable) == true)
        {
            await Dialogs.MessageBoxAsync(Tr._p("Message", "Read-only folders can't be cut."), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var assetsToWrite = await GetCopyCollectionAsync(directories, assetsToCut);
        if (assetsToWrite is null || assetsToWrite.Count == 0)
        {
            return;
        }
        // Flatten to a list
        var assetList = assetsToWrite.SelectMany(x => x).ToList();
        foreach (var asset in assetList)
        {
            if (!asset.CanDelete(out var error))
            {
                error = string.Format(Tr._p("Message", "The asset {0} can't be deleted. {1}{2}"), asset.Url, Environment.NewLine, error);
                await Dialogs.MessageBoxAsync(error, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        // Copy
        if (!await WriteToClipboardAsync(assetsToWrite))
        {
            return;
        }

        var undoRedoService = Session.UndoRedoService;
        using var transaction = undoRedoService?.CreateTransaction();
        // Clear the selection at first to reduce view updates in the following actions
        ClearSelection();
        // Add an action item that will fix back the references in the referencers of the assets being cut, in case the
        var assetsToFix = PackageViewModel.GetReferencers(Session.DependencyManager, Session, assetList.Select(x => x.AssetItem));
        var fixReferencesOperation = new FixAssetReferenceOperation(assetsToFix, true, false);
        undoRedoService?.PushOperation(fixReferencesOperation);
        // Delete the assets
        DeleteAssets(assetList);
        if (directories is not null)
        {
            // Delete the directories
            foreach (var directory in directories)
            {
                // Last-chance check (note that we already checked that the directories are not read-only)
                if (!directory.CanDelete(out var error))
                {
                    error = string.Format(Tr._p("Message", "{0} can't be deleted. {1}{2}"), directory.Name, Environment.NewLine, error);
                    await Dialogs.MessageBoxAsync(error, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                directory.Delete();
            }
        }

        if (transaction is not null)
            undoRedoService!.SetName(transaction, "Cut selection");
    }

    private async Task CopySelectionAsync(IReadOnlyCollection<DirectoryBaseViewModel>? directories, IEnumerable<AssetViewModel>? assetsToCopy)
    {
        var assetsToWrite = await GetCopyCollectionAsync(directories, assetsToCopy);
        if (assetsToWrite is null || assetsToWrite.Count == 0)
        {
            return;
        }
        await WriteToClipboardAsync(assetsToWrite);
    }

    /// <summary>
    /// Gets the whole collection of assets to be copied.
    /// </summary>
    /// <param name="directories">The collection of separate directories of assets.</param>
    /// <param name="assetsToCopy">The collection of assets in the current directory.</param>
    /// <remarks>Directories cannot be in the same hierarchy of one another.</remarks>
    /// <returns>The collection of assets to be copied, or null if the selection cannot be copied.</returns>
    private async Task<ICollection<IGrouping<string, AssetViewModel>>?> GetCopyCollectionAsync(IReadOnlyCollection<DirectoryBaseViewModel>? directories, IEnumerable<AssetViewModel>? assetsToCopy)
    {
        var collection = new List<IGrouping<string, AssetViewModel>>();
        // First level assets will be copied as is
        if (assetsToCopy is not null)
        {
            collection.AddRange(assetsToCopy.GroupBy(_ => string.Empty));
        }
        if (directories is not null)
        {
            // Check directory structure
            foreach (var directory in directories)
            {
                var parent = directory.Parent;
                while (parent is not null)
                {
                    if (directories.Contains(parent))
                    {
                        await Dialogs.MessageBoxAsync(Tr._p("Message", "Unable to cut or copy a selection that contains a folder and one of its subfolders."), MessageBoxButton.OK, MessageBoxImage.Information);
                        return null;
                    }
                    parent = parent.Parent;
                }
            }
            // Get all assets from directories
            foreach (var directory in directories)
            {
                var hierarchy = new List<DirectoryBaseViewModel>();
                directory.GetDirectoryHierarchy(hierarchy);
                foreach (var folder in hierarchy)
                {
                    EnsureDirectoryHierarchy(folder.Assets, folder);
                    // Add assets grouped by relative path
                    collection.AddRange(folder.Assets.GroupBy(_ => folder.Path.Remove(0, directory.Parent?.Path.Length ?? 0)));
                }
            }
        }

        return collection;
    }

    /// <summary>
    /// Consistency check. Makes sure <paramref name="assets"/> are indeed inside the given <paramref name="directory"/>.
    /// </summary>
    private static void EnsureDirectoryHierarchy(IEnumerable<AssetViewModel> assets, DirectoryBaseViewModel directory)
    {
        foreach (var asset in assets)
        {
            // Locations in namespaced packages are rooted /Namespace/...; the directory tree is bare
            var location = asset.AssetItem.UnqualifiedUrl;
            if (location.HasDirectory && (directory.Parent is null || !location.FullPath.StartsWith(directory.Parent.Path, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("One of the asset does not match the directory hierarchy.");
            }
        }
    }

    /// <summary>
    /// Actually writes the assets to the clipboard.
    /// </summary>
    private async Task<bool> WriteToClipboardAsync(IEnumerable<IGrouping<string, AssetViewModel>> assetsToWrite)
    {
        var assetCollection = new List<AssetItem>();
        assetCollection.AddRange(assetsToWrite.SelectMany(
                grp => grp.Select(a => new AssetItem(UPath.Combine<UFile>(grp.Key, a.AssetItem.Location.GetFileNameWithoutExtension()!), a.AssetItem.Asset))));
        try
        {
            var text = ServiceProvider.TryGet<ICopyPasteService>()?.CopyMultipleAssets(assetCollection);
            if (string.IsNullOrEmpty(text) || ServiceProvider.TryGet<IClipboardService>() is not { } clipboard)
                return false;

            await clipboard.SetTextAsync(text);
            return true;
        }
        catch (SystemException e)
        {
            // We don't provide feedback when copying fails.
            e.Ignore();
            return false;
        }
    }

    private async Task PasteAsync()
    {
        var directories = GetSelectedDirectories(false);
        if (directories.Count != 1)
        {
            await Dialogs.MessageBoxAsync(Tr._p("Message", "Select a valid asset folder to paste the selection to."), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        // If the selection is already a directory, paste into it
        var directory = SingleSelectedContent as DirectoryBaseViewModel ?? directories.First();
        var package = directory.Package;
        if (!package.IsEditable)
        {
            await Dialogs.MessageBoxAsync(Tr._p("Message", "This package or directory can't be modified."), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var copyPasteService = ServiceProvider.TryGet<ICopyPasteService>();
        var text = ServiceProvider.TryGet<IClipboardService>() is { } clipboard ? await clipboard.GetTextAsync() : null;
        if (copyPasteService is null || string.IsNullOrWhiteSpace(text) || !copyPasteService.CanPaste(text, typeof(List<AssetItem>), typeof(List<AssetItem>), typeof(List<AssetItem>)))
            return;

        var pastedAssets = copyPasteService.DeserializeCopiedData(text, new List<AssetItem>(), typeof(List<AssetItem>)).Items.FirstOrDefault()?.Data as List<AssetItem>;
        if (pastedAssets is null)
            return;

        var updatedAssets = new List<AssetItem>();
        var root = directory.Root;
        var project = (root as ProjectCodeViewModel)?.Project;
        foreach (var assetItem in pastedAssets)
        {
            // Perform allowed asset types validation
            if (!root.AcceptAssetType(assetItem.Asset.GetType()))
            {
                // Skip invalid assets
                continue;
            }

            var location = UPath.Combine(directory.Path, assetItem.Location);

            // Check if we are pasting to package or a project (with a source code)
            if (project is not null)
            {
                // Link source project
                assetItem.SourceFolder = project.Package.RootDirectory;
            }

            // Resolve folders to paste collisions with those existing in a directory
            var assetLocationDir = assetItem.Location.FullPath;
            {
                // Split path into two parts
                var firstSeparator = assetLocationDir.IndexOf(DirectoryBaseViewModel.Separator, StringComparison.Ordinal);
                if (firstSeparator > 0)
                {
                    // Left: (folder)
                    // /
                    // Right: (..folders..) / (file.ext)
                    UDirectory leftPart = assetLocationDir.Remove(firstSeparator);
                    UFile rightPart = assetLocationDir.Substring(firstSeparator + 1);

                    // Find valid left part location (if already in use)
                    leftPart = NamingHelper.ComputeNewName(leftPart, e => directory.GetDirectory(e) != null, "{0} ({1})");

                    // Fix location: (paste directory) / left/ right
                    location = UPath.Combine(Path.Combine(directory.Path, leftPart), rightPart);
                }
            }

            var updatedAsset = assetItem.Clone(true, location, assetItem.Asset);
            updatedAssets.Add(updatedAsset);
        }

        if (updatedAssets.Count == 0)
            return;

        var viewModels = package.PasteAssets(updatedAssets, project);

        var referencerViewModels = AssetViewModel.ComputeRecursiveReferencerAssets(viewModels);
        viewModels.AddRange(referencerViewModels);
        Session.NotifyAssetPropertiesChanged(viewModels);
        UpdateCommands();
    }
    #endregion // Copy/paste

    private void AddAsset(AssetViewModel asset)
    {
        OnPropertyChanging(nameof(AssetCount));
        assets.Add(asset);
        // No need to add to filtered assets here, filters will be refreshed anyway
        OnPropertyChanged(nameof(AssetCount));
    }

    private void RemoveAsset(AssetViewModel asset)
    {
        OnPropertyChanging(nameof(AssetCount));
        assets.Remove(asset);
        filteredAssets.Remove(asset);
        OnPropertyChanged(nameof(AssetCount));
    }

    private void AssetsCollectionInDirectoryChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // If the changes are too important, rebuild completely the collection.
        if (e.Action == NotifyCollectionChangedAction.Reset || e.NewItems is { Count: > 3 } || e.OldItems is { Count: > 3 })
        {
            var selectedDirectoryHierarchy = GetSelectedDirectories(DisplayLocationContentRecursively);
            UpdateAssetsCollection(selectedDirectoryHierarchy.SelectMany(x => x.Assets).ToList(), false);
        }
        // Otherwise, simply perform Adds and Removes on the current collection
        else
        {
            if (e.OldItems != null)
            {
                foreach (AssetViewModel oldItem in e.OldItems)
                {
                    RemoveAsset(oldItem);
                }
            }
            if (e.NewItems != null)
            {
                var refreshFilter = false;
                foreach (AssetViewModel newItem in e.NewItems)
                {
                    AddAsset(newItem);
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
        // The transaction won't work if it's triggered from another thread
        Dispatcher.EnsureAccess();

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

        UpdateCommands();

        AssetViewProperties.UpdateTypeAndName(SelectedAssets, x => x.TypeDisplayName, x => x.Url, "assets");
        await AssetViewProperties.GenerateSelectionPropertiesAsync(SelectedAssets);
    }

    private void SelectedLocationCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // The transaction won't work if it's triggered from another thread
        Dispatcher.EnsureAccess();

        UpdateLocations();

        UpdateCommands();

        if (IsMainView && SelectedLocations.Count == 1)
        {
            if (SelectedLocations.OfType<PackageViewModel>().SingleOrDefault() is { } package)
            {
                AssetViewProperties.TypeDescription = "Package";
                AssetViewProperties.Name = package.Name;
                package.Properties.GenerateSelectionPropertiesAsync(package.Yield()).Forget();
            }
        }
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

        OnPropertyChanging(nameof(AssetCount));
        assets.Clear();
        foreach (var newAsset in newAssets)
        {
            assets.Add(newAsset);
        }
        OnPropertyChanged(nameof(AssetCount));

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

        var selectedDirectoryHierarchy = GetSelectedDirectories(DisplayLocationContentRecursively);
        var newAssets = new List<AssetViewModel>();
        foreach (var directory in selectedDirectoryHierarchy)
        {
            ((INotifyCollectionChanged)directory.SubDirectories).CollectionChanged += SubDirectoriesCollectionInDirectoryChanged;
            directory.Assets.CollectionChanged += AssetsCollectionInDirectoryChanged;
            monitoredDirectories.Add(directory);
            newAssets.AddRange(directory.Assets);
        }
        UpdateAssetsCollection(newAssets, false);

        if (IsMainView)
        {
            InternalSettings.AssetViewDisplayMode.SetValue(DisplayAssetMode);
        }
    }

    private void UpdateSingleSelectedContent()
    {
        SingleSelectedContent = SelectedContent.Count == 1 ? SelectedContent.First() : null;
    }

    private void UpdateCommands()
    {
        var atLeastOneAsset = SelectedAssets.Count > 0;
        var atLeastOneContent = SelectedContent.Count > 0;

        // TODO: Allow to cut asset mount point - but do not remove the mount point
        CutLocationsCommand.IsEnabled = SelectedLocations.All(x => x is DirectoryViewModel or PackageViewModel);
        // Can copy from asset mount point
        CopyLocationsCommand.IsEnabled = SelectedLocations.All(x => x is DirectoryBaseViewModel or PackageViewModel);
        PasteCommand.IsEnabled = SelectedLocations.Count == 1 && SelectedLocations.All(x => x is DirectoryBaseViewModel or PackageViewModel);

        CopyContentCommand.IsEnabled = atLeastOneContent;
        CutContentCommand.IsEnabled = atLeastOneContent;
        DeleteContentCommand.IsEnabled = atLeastOneContent;

        CopyAssetsRecursivelyCommand.IsEnabled = atLeastOneAsset;
        CopyAssetUrlCommand.IsEnabled = SingleSelectedAsset is not null;
    }
}
