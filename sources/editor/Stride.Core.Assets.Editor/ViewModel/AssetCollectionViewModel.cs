// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.AddAssets;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Assets.Templates;
using Stride.Core.Assets.Tracking;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Interop;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Translation;
using MessageBoxButton = Stride.Core.Presentation.Services.MessageBoxButton;
using MessageBoxImage = Stride.Core.Presentation.Services.MessageBoxImage;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public enum DisplayAssetMode
    {
        AssetInSelectedFolderOnly,
        AssetInSelectedFolderAndSubFolder,
        AssetAndFolderInSelectedFolder,
    }

    public enum FilterCategory
    {
        AssetName,
        AssetTag,
        AssetType,
    }

    public enum SortRule
    {
        Name,
        TypeOrderThenName,
        DirtyThenName,
        ModificationDateThenName,
    }

    public sealed class AssetCollectionViewModel : DispatcherViewModel, IAddChildViewModel
    {
        public sealed class AssetFilterViewModel : DispatcherViewModel, IEquatable<AssetFilterViewModel>
        {
            private readonly AssetCollectionViewModel collection;
            private bool isActive;
            private bool isReadOnly;

            public AssetFilterViewModel([NotNull] AssetCollectionViewModel collection, FilterCategory category, [NotNull] string filter, string displayName)
                : base(collection.SafeArgument(nameof(collection)).ServiceProvider)
            {
                this.collection = collection;
                Category = category;
                DisplayName = displayName;
                Filter = filter ?? throw new ArgumentNullException(nameof(filter));
                isActive = true;

                RemoveFilterCommand = new AnonymousCommand<AssetFilterViewModel>(ServiceProvider, collection.RemoveAssetFilter);
                ToggleIsActiveCommand = new AnonymousCommand(ServiceProvider, () => IsActive = !IsActive);
            }

            public FilterCategory Category { get; }

            public string DisplayName { get; }

            public string Filter { get; }

            public bool IsActive { get => isActive; set => SetValue(ref isActive, value, collection.RefreshFilters); }

            public bool IsReadOnly
            {
                get => isReadOnly;
                set
                {
                    SetValue(ref isReadOnly, value, () =>
                    {
                        RemoveFilterCommand.IsEnabled = !value;
                        ToggleIsActiveCommand.IsEnabled = !value;
                    });
                }
            }

            public ICommandBase RemoveFilterCommand { get; }

            public ICommandBase ToggleIsActiveCommand { get; }

            public bool Match(AssetViewModel asset)
            {
                switch (Category)
                {
                    case FilterCategory.AssetName:
                        return ComputeTokens(Filter).All(x => asset.Name.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);

                    case FilterCategory.AssetTag:
                        return asset.Tags.Any(y => y.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0);

                    case FilterCategory.AssetType:
                        return string.Equals(asset.AssetType.FullName, Filter);
                }
                return false;
            }

            public bool Equals(AssetFilterViewModel other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Category == other.Category && string.Equals(Filter, other.Filter, StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;

                return Equals(obj as AssetFilterViewModel);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)Category * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Filter);
                }
            }

            public static bool operator ==(AssetFilterViewModel left, AssetFilterViewModel right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(AssetFilterViewModel left, AssetFilterViewModel right)
            {
                return !Equals(left, right);
            }
        }

        public static readonly IEnumerable<FilterCategory> AllFilterCategories = Enum.GetValues(typeof(FilterCategory)).Cast<FilterCategory>();

        private readonly ObservableSet<AssetViewModel> assets = new ObservableSet<AssetViewModel>();

        // /!\ FIXME: we need to rework that as we probably don't need that many lists
        private readonly ObservableList<AssetViewModel> filteredAssets = new ObservableList<AssetViewModel>();
        private readonly ObservableList<object> filteredContent = new ObservableList<object>();

        /// <remarks>
        /// <see cref="selectedAssets"/> is a sub-collection of <see cref="selectedContent"/>. It should always be read from and never directly updated, except in <see cref="SelectedContentCollectionChanged"/>.
        /// </remarks>
        private readonly ObservableList<AssetViewModel> selectedAssets = new ObservableList<AssetViewModel>();

        /// <summary>
        /// List of all selected items (e.g. in the asset view).
        /// </summary>
        private readonly ObservableList<object> selectedContent = new ObservableList<object>();

        private object singleSelectedContent;

        private readonly Lazy<AddAssetTemplateCollectionViewModel> addAssetTemplateCollection;
        private readonly List<AssetFilterViewModel> typeFilters = new List<AssetFilterViewModel>();
        private readonly Dictionary<FilterCategory, bool> availableFilterCategories;
        private readonly ObservableList<AssetFilterViewModel> availableAssetFilters = new ObservableList<AssetFilterViewModel>();
        private readonly ObservableSet<AssetFilterViewModel> currentAssetFilters = new ObservableSet<AssetFilterViewModel>();
        private string assetFilterPattern;
        private Func<AssetViewModel, bool> customFilter;

        private readonly IAssetDependencyManager dependencyManager;
        private readonly HashSet<DirectoryBaseViewModel> monitoredDirectories = new HashSet<DirectoryBaseViewModel>();
        private readonly SessionObjectPropertiesViewModel assetProperties;
        private DisplayAssetMode displayMode = InternalSettings.AssetViewDisplayMode.GetValue();
        private SortRule sortRule = InternalSettings.AssetViewSortRule.GetValue();
        private bool discardSelectionChanges;
        private bool refreshing;
        private IEnumerable<TemplateDescriptionViewModel> lastMatchingTemplates;

        private readonly FuncClipboardMonitor<bool> pasteMonitor = new FuncClipboardMonitor<bool>();

        public AssetCollectionViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] SessionViewModel session, [NotNull] IEnumerable<FilterCategory> filterCategories, SessionObjectPropertiesViewModel assetProperties = null)
            : base(serviceProvider)
        {
            refreshing = true;
            Session = session ?? throw new ArgumentNullException(nameof(session));
            dependencyManager = Session.DependencyManager;
            this.assetProperties = assetProperties;

            addAssetTemplateCollection = new Lazy<AddAssetTemplateCollectionViewModel>(() => new AddAssetTemplateCollectionViewModel(session));
            typeFilters.AddRange(AssetRegistry.GetPublicTypes().Where(type => type != typeof(Package)).Select(type => new AssetFilterViewModel(this, FilterCategory.AssetType, type.FullName, DisplayAttribute.GetDisplayName(type))));
            typeFilters.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.InvariantCultureIgnoreCase));
            availableFilterCategories = AllFilterCategories.ToDictionary(f => f, f => false);
            foreach (var cat in filterCategories)
            {
                availableFilterCategories[cat] = true;
            }

            ChangeDisplayAssetModeCommand = new AnonymousCommand<DisplayAssetMode>(serviceProvider, x => { DisplayAssetMode = x; });
            SortAssetsCommand = new AnonymousCommand<SortRule>(serviceProvider, x => { SortRule = x; UpdateCommands(); });
            SelectAssetCommand = new AnonymousCommand<AssetViewModel>(serviceProvider, x => SelectAssets(x.Yield()));
            DeleteContentCommand = new AnonymousTaskCommand(serviceProvider, () => DeleteContent(SelectedContent));
            RunAssetTemplateCommand = new AnonymousTaskCommand<ITemplateDescriptionViewModel>(serviceProvider, x => RunAssetTemplate(x, null));
            SelectFilesToCreateAssetCommand = new AnonymousTaskCommand(serviceProvider, SelectFilesToCreateAsset);
            ShowAddAssetDialogCommand = new AnonymousTaskCommand(serviceProvider, ShowAddAssetDialog);
            CutLocationsCommand = new AnonymousTaskCommand(serviceProvider, CutSelectedLocations, CanCopy);
            CopyLocationsCommand = new AnonymousTaskCommand(serviceProvider, CopySelectedLocations, CanCopy);
            CutContentCommand = new AnonymousTaskCommand(serviceProvider, CutSelectedContent, CanCopy);
            CopyContentCommand = new AnonymousTaskCommand(serviceProvider, CopySelectedContent, CanCopy);
            CopyAssetsRecursivelyCommand = new AnonymousTaskCommand(serviceProvider, CopySelectedAssetsRecursively, CanCopy);
            CopyAssetUrlCommand = new AnonymousCommand(ServiceProvider, CopyAssetUrl);
            PasteCommand = new AnonymousTaskCommand(serviceProvider, Paste, () => pasteMonitor.Get(CanPaste));
            AddAssetFilterCommand = new AnonymousCommand<AssetFilterViewModel>(serviceProvider, AddAssetFilter);
            ClearAssetFiltersCommand = new AnonymousCommand(serviceProvider, ClearAssetFilters);
            RefreshAssetFilterCommand = new AnonymousCommand<AssetFilterViewModel>(serviceProvider, RefreshAssetFilter);
            currentAssetFilters.CollectionChanged += (s,e) => RefreshFilters();
            SelectedLocations.CollectionChanged += SelectedLocationCollectionChanged;
            filteredAssets.CollectionChanged += FilteredAssetsCollectionChanged;
            selectedContent.CollectionChanged += SelectedContentCollectionChanged;
            refreshing = false;

            DependentProperties.Add(nameof(DisplayAssetMode), new[] { nameof(DisplayLocationContentRecursively) });
            DependentProperties.Add(nameof(SingleSelectedContent), new[] { nameof(SingleSelectedAsset) });
        }

        public SessionViewModel Session { get; }

        public IReadOnlyObservableCollection<AssetViewModel> Assets => assets;

        public int AssetCount => assets.Count;

        // FIXME: this property was added because for some reason DataGridEx has a lot of issues when displaying both assets and folders
        [Obsolete("Should not be used except by AssetViewUserControl GridView")]
        public IReadOnlyObservableCollection<AssetViewModel> FilteredAssets => filteredAssets;

        public IReadOnlyObservableCollection<object> FilteredContent => filteredContent;

        public IReadOnlyCollection<AssetFilterViewModel> TypeFilters => typeFilters;

        public IReadOnlyObservableCollection<AssetFilterViewModel> AvailableAssetFilters => availableAssetFilters;

        public IReadOnlyObservableCollection<AssetFilterViewModel> CurrentAssetFilters => currentAssetFilters;

        public string AssetFilterPattern { get => assetFilterPattern; set { SetValue(ref assetFilterPattern, value, () => UpdateAvailableAssetFilters(value)); } }

        public Func<AssetViewModel, bool> CustomFilter { get => customFilter; set => SetValue(ref customFilter, value, RefreshFilters); }

        public DisplayAssetMode DisplayAssetMode { get => displayMode; private set => SetValue(ref displayMode, value, UpdateLocations); }

        public SortRule SortRule { get => sortRule; private set => SetValue(ref sortRule, value, RefreshFilters); }

        public IReadOnlyObservableCollection<AssetViewModel> SelectedAssets => selectedAssets;

        public IReadOnlyObservableCollection<object> SelectedContent => selectedContent;

        public PackageViewModel SelectedAssetsPackage { get { var p = SelectedAssets.Select(x => x.Directory.Package).Distinct().ToArray(); return p.Length == 1 ? p[0] : null; } }

        /// <summary>
        /// List of selected locations (in the solution explorer).
        /// </summary>
        [NotNull]
        public ObservableList<object> SelectedLocations { get; } = new ObservableList<object>();

        public bool DisplayLocationContentRecursively => DisplayAssetMode == DisplayAssetMode.AssetInSelectedFolderAndSubFolder;

        public object SingleSelectedContent { get => singleSelectedContent; private set => SetValue(ref singleSelectedContent, value); }

        [CanBeNull]
        public AssetViewModel SingleSelectedAsset => SingleSelectedContent as AssetViewModel;

        public AddAssetTemplateCollectionViewModel AddAssetTemplateCollection => addAssetTemplateCollection.Value;

        [NotNull]
        public IEnumerable<IDirtiable> Dirtiables => Enumerable.Empty<IDirtiable>();

        [NotNull]
        public IEditorDialogService Dialogs => ServiceProvider.Get<IEditorDialogService>();

        public IEnumerable<TemplateDescriptionViewModel> LastMatchingTemplates { get => lastMatchingTemplates; set => SetValue(ref lastMatchingTemplates, value); }

        [NotNull]
        public ICommandBase ChangeDisplayAssetModeCommand { get; }

        [NotNull]
        public ICommandBase SortAssetsCommand { get; }

        [NotNull]
        public ICommandBase SelectAssetCommand { get; }

        [NotNull]
        public ICommandBase DeleteContentCommand { get; }

        [NotNull]
        public ICommandBase RunAssetTemplateCommand { get; }

        [NotNull]
        public ICommandBase SelectFilesToCreateAssetCommand { get; }

        [NotNull]
        public ICommandBase ShowAddAssetDialogCommand { get; }

        [NotNull]
        public ICommandBase CutLocationsCommand { get; }

        [NotNull]
        public ICommandBase CopyLocationsCommand { get; }

        [NotNull]
        public ICommandBase CutContentCommand { get; }

        [NotNull]
        public ICommandBase CopyContentCommand { get; }

        [NotNull]
        public ICommandBase CopyAssetsRecursivelyCommand { get; }

        [NotNull]
        public ICommandBase CopyAssetUrlCommand { get; }

        [NotNull]
        public ICommandBase PasteCommand { get; }

        [NotNull]
        public ICommandBase AddAssetFilterCommand { get; }

        [NotNull]
        public ICommandBase ClearAssetFiltersCommand { get; }

        [NotNull]
        public ICommandBase RefreshAssetFilterCommand { get; }

        public void ClearSelection()
        {
            selectedContent.Clear();
        }

        public void SelectAssets([ItemNotNull, NotNull] IEnumerable<AssetViewModel> assetsToSelect)
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
                var packageCategory = location as PackageCategoryViewModel;
                var package = location as PackageViewModel;
                var directory = location as DirectoryBaseViewModel;
                if (packageCategory != null && includeSubDirectoriesOfSelected)
                {
                    selectedDirectories.AddRange(packageCategory.Content.Select(x => x.AssetMountPoint).NotNull());
                }
                if (package != null)
                {
                    selectedDirectories.Add(package.AssetMountPoint);
                }
                if (directory != null)
                {
                    selectedDirectories.Add(directory);
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

        /// <summary>
        /// Deletes the given assets in a single transaction without asking for confirmation nor fixing broken references.
        /// Assets whose <see cref="AssetViewModel.CanDelete()"/> method returns <c>false</c> won't be deleted, unless <paramref name="forceDelete"/> is <c>true</c>.
        /// </summary>
        /// <param name="assetsToDelete">The list of assets to delete.</param>
        /// <param name="forceDelete">If <c>true</c> the asset whose <see cref="AssetViewModel.CanDelete()"/> method returns <c>false</c> will still be deleted</param>
        /// <returns>The number of assets that have been successfully deleted.</returns>
        internal int DeleteAssets(IEnumerable<AssetViewModel> assetsToDelete, bool forceDelete = false)
        {
            using (var transaction = Session.UndoRedoService.CreateTransaction())
            {
                var deletedAssets = new List<AssetViewModel>();
                foreach (var asset in assetsToDelete.Where(x => forceDelete || x.CanDelete()))
                {
                    if (asset.Directory == null)
                        throw new InvalidOperationException("The asset directory cannot be null before deleting an asset.");

                    if (!forceDelete && !asset.CanDelete())
                        continue;

                    // This must be done before we clear the Directory property of the asset
                    AssetDependenciesViewModel.NotifyAssetChanged(asset.Session, asset);

                    var oldDirectory = asset.Directory;

                    // It is important to set IsDeleted before clearing the directory, so the parent project can be marked as dirty
                    asset.IsDeleted = true;
                    asset.Directory.RemoveAsset(asset);
                    asset.Directory = null;

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
                Session.UndoRedoService.SetName(transaction, deletedAssets.Count == 1 ? $"Delete asset '{deletedAssets[0]}'" : $"Delete {deletedAssets.Count} assets");
                return deletedAssets.Count;
            }
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(AssetCollectionViewModel));

            pasteMonitor.Destroy();
            base.Destroy();
        }

        public async Task<List<AssetViewModel>> RunAssetTemplate(ITemplateDescriptionViewModel template, IEnumerable<UFile> files, PropertyContainer? customParameters = null)
        {
            if (template == null)
                return new List<AssetViewModel>();

            var loggerResult = new LoggerResult();

            var directory = await GetAssetCreationTargetFolder();
            if (directory == null)
                return new List<AssetViewModel>();

            var templateDescription = template.GetTemplate() as TemplateAssetDescription;
            if (templateDescription == null)
            {
                await Dialogs.MessageBox(Tr._p("Message", "Unable to use the selected template because it is not an asset template."), MessageBoxButton.OK, MessageBoxImage.Warning);
                return new List<AssetViewModel>();
            }
            var assetType = templateDescription.GetAssetType();
            // If the mount point of the current folder does not support this type of asset, try to select the first mount point that support it.
            directory = AssetViewModel.FindValidCreationLocation(assetType, directory, Session.CurrentProject);

            if (directory == null)
                return new List<AssetViewModel>();

            string name = string.Empty;
            if (templateDescription.RequireName)
            {
                name = templateDescription.DefaultOutputName ?? templateDescription.AssetTypeName;
            }

            return await InvokeAddAssetTemplate(loggerResult, name, directory, templateDescription, files, customParameters);
        }

        private async Task ShowAddAssetDialog()
        {
            var directory = await GetAssetCreationTargetFolder();
            if (directory == null)
                return;

            var templateDialog = ServiceProvider.Get<IEditorDialogService>().CreateAddAssetDialog(Session, directory);
            var result = await templateDialog.ShowModal();
            if (result == DialogResult.Ok)
            {
                var selectedTemplate = templateDialog.SelectedTemplate;
                if (selectedTemplate != null)
                {
                    await RunAssetTemplate(selectedTemplate, null);
                }
            }
        }

        private string ComputeNamespace(DirectoryBaseViewModel directory)
        {
            switch (directory)
            {
                case ProjectCodeViewModel projectCode:
                    return projectCode.Project.RootNamespace;
                case var directoryWithParent when directoryWithParent.Parent != null:
                    return $"{ComputeNamespace(directoryWithParent.Parent)}.{directoryWithParent.Name}";
                default:
                    return directory.Name;
            }
        }

        private async Task<List<AssetViewModel>> InvokeAddAssetTemplate(LoggerResult logger, string name, DirectoryBaseViewModel directory, TemplateAssetDescription templateDescription, IEnumerable<UFile> files, PropertyContainer? customParameters)
        {
            List<AssetViewModel> newAssets = new List<AssetViewModel>();

            var parameters = new AssetTemplateGeneratorParameters(directory.Path, files)
            {
                Name = name,
                Description = templateDescription,
                Package = directory.Package.Package,
                Logger = logger,
                Namespace = ComputeNamespace(directory),
            };

            if (customParameters.HasValue)
            {
                foreach (var tag in customParameters.Value)
                {
                    parameters.Tags[tag.Key] = tag.Value;
                }
            }

            var generator = TemplateManager.FindTemplateGenerator(parameters);
            if (generator == null)
            {
                await Dialogs.MessageBox(Tr._p("Message", "Unable to retrieve template generator for the selected template. Aborting."), MessageBoxButton.OK, MessageBoxImage.Error);
                return newAssets;
            }

            var workProgress = new WorkProgressViewModel(ServiceProvider, logger)
            {
                Title = Tr._p("Title", "Add asset…"),
                KeepOpen = KeepOpen.OnWarningsOrErrors,
                IsIndeterminate = true,
                IsCancellable = false, // The process is not cancellable at the beginning.
            };
            workProgress.RegisterProgressStatus(logger, true);

            try
            {
                var bufferBlock = new BufferBlock<IReadOnlyList<SourceFileChangedData>>();
                var cancel = new CancellationTokenSource();
                List<AssetItem> newAssetItems;

                using (Session.SourceTracker.SourceFileChanged.LinkTo(bufferBlock))
                {
                    // Run the template generator first. This method also takes care of showing the progress window.
                    await TemplateGeneratorHelper.RunTemplateGeneratorSafe(generator, parameters, workProgress);

                    var previousAssetItems = new HashSet<AssetItem>(directory.Package.Assets.Select(x => x.AssetItem));
                    newAssetItems = new List<AssetItem>(directory.Package.Package.Assets.Where(x => !previousAssetItems.Contains(x)));

                    // No asset to create, early exit.
                    if (newAssetItems.Count == 0)
                    {
                        logger.Info("Nothing to import.");
                        return newAssets;
                    }

                    // Collect all the source files that affect the assets, we want their hashes before creating view models.
                    var sources = new Dictionary<AssetId, HashSet<UFile>>();
                    var collector = new SourceFilesCollector();
                    foreach (var newAsset in newAssetItems)
                    {
                        var assetSources = collector.GetSourceFiles(newAsset.Asset).Where(x => x.Value).Select(x => x.Key).ToList();
                        if (assetSources.Count > 0)
                        {
                            sources.Add(newAsset.Id, new HashSet<UFile>(assetSources));
                        }
                    }

                    logger.Info($"Computing hashes of {sources.Sum(x => x.Value.Count)} source files...");
                    // From now the import become cancellable. If for some reason, the hashes of some source files is never computed,
                    // this process will wait forever until the user cancels.
                    workProgress.IsCancellable = true;
                    workProgress.CancelCommand = new AnonymousCommand(ServiceProvider, () => cancel.Cancel());

                    while (sources.Count > 0)
                    {
                        var changes = await bufferBlock.ReceiveAsync(cancel.Token);

                        if (cancel.IsCancellationRequested)
                        {
                            logger.Info("Operation cancelled");
                            break;
                        }

                        foreach (var change in changes)
                        {
                            // Filter out changes unrelated to our new assets.
                            if (!sources.ContainsKey(change.AssetId))
                                continue;

                            var assetItem = newAssetItems.First(x => x.Id == change.AssetId);
                            var assetSources = sources[assetItem.Id];

                            // We care only about source files changes
                            if (change.Type == SourceFileChangeType.SourceFile)
                            {
                                // Retrieve the currently stored hashes for this asset.
                                var assetHashes = SourceHashesHelper.GetAllHashes(assetItem.Asset);
                                foreach (var file in change.Files)
                                {
                                    // Update it with newly computed hashes
                                    var hash = Session.SourceTracker.GetCurrentHash(file);
                                    assetHashes[file] = hash;
                                    // Remove hashes that have been registered.
                                    assetSources.Remove(file);
                                    logger.Verbose($"Computed hash of {file} for asset {assetItem.Location}. {sources.Sum(x => x.Value.Count)} files remaining...");
                                }

                                // Push the changes we did to the stored hashes.
                                SourceHashesHelper.UpdateHashes(assetItem.Asset, assetHashes);

                                // Remove this asset from the list if we got all its hashes
                                if (assetSources.Count == 0)
                                    sources.Remove(assetItem.Id);
                            }
                        }
                    }
                }

                // If user cancelled, stops here and return an empty list.
                if (cancel.IsCancellationRequested)
                    return newAssets;

                // Actually create the transaction and the view models now.
                using (var transaction = Session.UndoRedoService.CreateTransaction())
                {
                    newAssets = newAssetItems.Select(assetItem => directory.Package.CreateAsset(directory, assetItem, true, logger)).ToList();
                    Session.UndoRedoService.SetName(transaction, newAssets.Count == 1 ? $"Create asset '{newAssets.First().Url}'" : $"Create {newAssets.Count} assets");
                }

                Session.CheckConsistency();
                if (parameters.RequestSessionSave)
                {
                    await Session.SaveSession();
                }

                SelectAssets(newAssets);
                return newAssets;
            }
            catch (Exception e)
            {
                logger.Error("There was a problem generating the asset", e);
                return newAssets;
            }
            finally
            {
                await workProgress.NotifyWorkFinished(false, logger.HasErrors);
            }
        }

        private async Task<DirectoryBaseViewModel> GetAssetCreationTargetFolder()
        {
            var directories = GetSelectedDirectories(false);
            var directoryCount = directories.Count;
            if (directoryCount > 1)
            {
                await Dialogs.MessageBox(Tr._p("Message", "Game Studio can't create assets in multiple locations. In the solution explorer, select a single directory or package to create the asset in."), MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
            if (directoryCount == 0)
            {
                await Dialogs.MessageBox(Tr._p("Message", "Game Studio can't create an asset here. In the solution explorer, select a directory or package to create the asset in."), MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            var directory = directories.First();
            if (!directory.Package.IsEditable)
            {
                await Dialogs.MessageBox(Tr._p("Message", "Game Studio can't create an asset here because the selected directory or package can't be edited. In the solution explorer, select a directory or package to create the asset in."), MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
            return directory;
        }

        private async Task CutSelectedLocations()
        {
            var directories = GetSelectedDirectories(false);
            await CutSelection(directories, null);
            UpdateCommands();
        }

        private bool CanCopy()
        {
            return ServiceProvider.TryGet<ICopyPasteService>() != null;
        }

        private void CopyAssetUrl()
        {
            if (SingleSelectedAsset == null)
                return;

            try
            {
                SafeClipboard.SetText(SingleSelectedAsset.Url);
            }
            catch (SystemException e)
            {
                // We don't provide feedback when copying fails.
                e.Ignore();
            }
        }

        private async Task CopySelectedLocations()
        {
            var directories = GetSelectedDirectories(false);
            await CopySelection(directories, null);
            UpdateCommands();
        }

        private async Task CutSelectedContent()
        {
            var directories = SelectedContent.OfType<DirectoryBaseViewModel>().ToList();
            await CutSelection(directories, SelectedAssets);
            UpdateCommands();
        }

        private async Task CopySelectedContent()
        {
            var directories = SelectedContent.OfType<DirectoryBaseViewModel>().ToList();
            await CopySelection(directories, SelectedAssets);
            UpdateCommands();
        }

        private async Task CopySelectedAssetsRecursively()
        {
            var assetsToCopy = new ObservableSet<AssetViewModel>();
            foreach (var asset in SelectedAssets)
            {
                assetsToCopy.Add(asset);
                assetsToCopy.AddRange(asset.Dependencies.RecursiveReferencedAssets.Where(a => a.IsEditable));
            }

            await CopySelection(null, assetsToCopy);
            UpdateCommands();
        }

        private async Task CutSelection(IReadOnlyCollection<DirectoryBaseViewModel> directories, IEnumerable<AssetViewModel> assetsToCut)
        {
            // Ensure all directories can be cut
            if (directories?.Any(d => !d.IsEditable) == true)
            {
                await Dialogs.MessageBox(Tr._p("Message", "Read-only folders can't be cut."), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var assetsToWrite = await GetCopyCollection(directories, assetsToCut);
            if (assetsToWrite == null || assetsToWrite.Count == 0)
            {
                return;
            }
            // Flatten to a list
            var assetList = assetsToWrite.SelectMany(x => x).ToList();
            foreach (var asset in assetList)
            {
                string error;
                if (!asset.CanDelete(out error))
                {
                    error = string.Format(Tr._p("Message", "The asset {0} can't be deleted. {1}{2}"), asset.Url, Environment.NewLine, error);
                    await Dialogs.MessageBox(error, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            // Copy
            if (!WriteToClipboard(assetsToWrite))
            {
                return;
            }

            using (var transaction = Session.UndoRedoService.CreateTransaction())
            {
                // Clear the selection at first to reduce view updates in the following actions
                ClearSelection();
                // Add an action item that will fix back the references in the referencers of the assets being cut, in case the
                var assetsToFix = PackageViewModel.GetReferencers(dependencyManager, Session, assetList.Select(x => x.AssetItem));
                var fixReferencesOperation = new FixAssetReferenceOperation(assetsToFix, true, false);
                Session.UndoRedoService.PushOperation(fixReferencesOperation);
                // Delete the assets
                DeleteAssets(assetList);
                if (directories != null)
                {
                    // Delete the directories
                    foreach (var directory in directories)
                    {
                        string error;
                        // Last-chance check (note that we already checked that the directories are not read-only)
                        if (!directory.CanDelete(out error))
                        {
                            error = string.Format(Tr._p("Message", "{0} can't be deleted. {1}{2}"), directory.Name, Environment.NewLine, error);
                            await Dialogs.MessageBox(error, MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        directory.Delete();
                    }
                }

                Session.UndoRedoService.SetName(transaction, "Cut selection");
            }
        }

        private async Task CopySelection(IReadOnlyCollection<DirectoryBaseViewModel> directories, IEnumerable<AssetViewModel> assetsToCopy)
        {
            var assetsToWrite = await GetCopyCollection(directories, assetsToCopy);
            if (assetsToWrite == null || assetsToWrite.Count == 0)
            {
                return;
            }
            WriteToClipboard(assetsToWrite);
        }

        /// <summary>
        /// Gets the whole collection of assets to be copied.
        /// </summary>
        /// <param name="directories">The collection of separate directories of assets.</param>
        /// <param name="assetsToCopy">The collection of assets in the current directory.</param>
        /// <remarks>Directories cannot be in the same hierarchy of one another.</remarks>
        /// <returns>The collection of assets to be copied, or null if the selection cannot be copied.</returns>
        private async Task<ICollection<IGrouping<string, AssetViewModel>>> GetCopyCollection(IReadOnlyCollection<DirectoryBaseViewModel> directories, IEnumerable<AssetViewModel> assetsToCopy)
        {
            var collection = new List<IGrouping<string, AssetViewModel>>();
            // First level assets will be copied as is
            if (assetsToCopy != null)
            {
                collection.AddRange(assetsToCopy.GroupBy(a => string.Empty));
            }
            if (directories != null)
            {
                // Check directory structure
                foreach (var directory in directories)
                {
                    var parent = directory.Parent;
                    while (parent != null)
                    {
                        if (directories.Contains(parent))
                        {
                            await Dialogs.MessageBox(Tr._p("Message", "Unable to cut or copy a selection that contains a folder and one of its subfolders."), MessageBoxButton.OK, MessageBoxImage.Information);
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
                        collection.AddRange(folder.Assets.GroupBy(a => folder.Path.Remove(0, directory.Parent?.Path.Length ?? 0)));
                    }
                }
            }

            return collection;
        }

        /// <summary>
        /// Consistency check. Makes sure <paramref name="assets"/> are indeed inside the given <paramref name="directory"/>.
        /// </summary>
        /// <param name="assets"></param>
        /// <param name="directory"></param>
        private static void EnsureDirectoryHierarchy(IEnumerable<AssetViewModel> assets, DirectoryBaseViewModel directory)
        {
            foreach (var asset in assets)
            {
                if (asset.AssetItem.Location.HasDirectory && (directory.Parent == null || !asset.Url.StartsWith(directory.Parent.Path)))
                {
                    throw new InvalidOperationException("One of the asset does not match the directory hierarchy.");
                }
            }
        }

        /// <summary>
        /// Actually writes the assets to the clipboard.
        /// </summary>
        /// <param name="assetsToWrite"></param>
        /// <returns></returns>
        private bool WriteToClipboard(IEnumerable<IGrouping<string, AssetViewModel>> assetsToWrite)
        {
            var assetCollection = new List<AssetItem>();
            assetCollection.AddRange(assetsToWrite.SelectMany(
                    grp => grp.Select(a => new AssetItem(UPath.Combine<UFile>(grp.Key, a.AssetItem.Location.GetFileNameWithoutExtension()), a.AssetItem.Asset))));
            try
            {
                var text = ServiceProvider.TryGet<ICopyPasteService>()?.CopyMultipleAssets(assetCollection);
                if (string.IsNullOrEmpty(text))
                    return false;

                SafeClipboard.SetText(text);
                return true;
            }
            catch (SystemException e)
            {
                // We don't provide feedback when copying fails.
                e.Ignore();
                return false;
            }
        }

        private bool CanPaste()
        {
            return ServiceProvider.TryGet<ICopyPasteService>()?.CanPaste(SafeClipboard.GetText(), typeof(List<AssetItem>), typeof(List<AssetItem>), typeof(List<AssetItem>)) ?? false;
        }

        private async Task Paste()
        {
            var directories = GetSelectedDirectories(false);
            if (directories.Count != 1)
            {
                await Dialogs.MessageBox(Tr._p("Message", "Select a valid asset folder to paste the selection to."), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            // If the selection is already a directory, paste into it
            var directory = SingleSelectedContent as DirectoryBaseViewModel ?? directories.First();
            var package = directory.Package;
            if (!package.IsEditable)
            {
                await Dialogs.MessageBox(Tr._p("Message", "This package or directory can't be modified."), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var text = SafeClipboard.GetText();
            if (string.IsNullOrWhiteSpace(text))
                return;

            var pastedAssets = new List<AssetItem>();
            pastedAssets = ServiceProvider.TryGet<ICopyPasteService>()?.DeserializeCopiedData(text, pastedAssets, typeof(List<AssetItem>)).Items.FirstOrDefault()?.Data as List<AssetItem>;
            if (pastedAssets == null)
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
                if (project != null)
                {
                    // Link source project
                    assetItem.SourceFolder = project.Package.RootDirectory;
                }

                // Resolve folders to paste collisions with those existing in a directory
                var assetLocationDir = assetItem.Location.FullPath;
                {
                    // Split path into two parts
                    int firstSeparator = assetLocationDir.IndexOf(DirectoryBaseViewModel.Separator, StringComparison.Ordinal);
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

        public void AddAssetFilter(AssetFilterViewModel filter)
        {
            if (filter == null)
                return;

            filter.IsActive = true;
            currentAssetFilters.Insert(0, filter);
        }

        public void ClearAssetFilters()
        {
            currentAssetFilters.RemoveWhere(f => !f.IsReadOnly);
        }

        public void RefreshAssetFilter(AssetFilterViewModel filter)
        {
            if (filter == null)
                return;

            refreshing = true;
            foreach (var f in currentAssetFilters.Where(f => f.Category == filter.Category).ToList())
            {
                RemoveAssetFilter(f);
            }
            refreshing = false;
            AddAssetFilter(filter);
        }

        public void RemoveAssetFilter(AssetFilterViewModel filter)
        {
            if (filter == null)
                return;

            filter.IsActive = false;
            currentAssetFilters.Remove(filter);
        }

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

        private void SelectedLocationCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // The transaction won't work if it's triggered from another thread
            Dispatcher.EnsureAccess();

            UpdateLocations();

            UpdateCommands();

            // TODO: we use assetProperties value to determine if it's the main asset collection view model. A proper boolean would be better
            if (assetProperties != null && SelectedLocations.Count == 1)
            {
                var package = SelectedLocations.OfType<PackageViewModel>().SingleOrDefault();
                if (package != null)
                {
                    assetProperties.TypeDescription = "Package";
                    assetProperties.Name = package.Name;
                    package.Properties.GenerateSelectionPropertiesAsync(package.Yield()).Forget();
                }
            }
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

            // TODO: we use assetProperties value to determine if it's the main asset collection view model. A proper boolean would be better
            if (assetProperties != null)
            {
                InternalSettings.AssetViewDisplayMode.SetValue(DisplayAssetMode);
            }
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

        private void AssetsCollectionInDirectoryChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // If the changes are too important, rebuild completely the collection.
            if (e.Action == NotifyCollectionChangedAction.Reset || (e.NewItems != null && e.NewItems.Count > 3) || (e.OldItems != null && e.OldItems.Count > 3))
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

        private void SubDirectoriesCollectionInDirectoryChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateLocations();
        }

        public async Task<bool> DeleteContent(IReadOnlyCollection<object> locations, bool skipConfirmation = false)
        {
            var result = await Session.DeleteItems(locations, skipConfirmation);
            UpdateCommands();
            return result;
        }

        private static string[] ComputeTokens(string pattern)
        {
            return pattern?.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
        }

        private void UpdateAvailableAssetFilters(string filterText)
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
                    if (filter.DisplayName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        availableAssetFilters.Add(filter);
                    }
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
                var assetNameComparer = new AnonymousComparer<AssetViewModel>((x, y) => nameComparer.Compare(x.Name, y.Name));
                IComparer<AssetViewModel> comparer;
                switch (sortRule)
                {
                    case SortRule.TypeOrderThenName:
                        comparer = new AnonymousComparer<AssetViewModel>((x, y) => { var r = -(DisplayAttribute.GetOrder(x.AssetType) ?? 0).CompareTo(DisplayAttribute.GetOrder(y.AssetType) ?? 0); return r == 0 ? assetNameComparer.Compare(x, y) : r; });
                        break;
                    case SortRule.DirtyThenName:
                        comparer = new AnonymousComparer<AssetViewModel>((x, y) => { var r = -x.IsDirty.CompareTo(y.IsDirty); return r == 0 ? assetNameComparer.Compare(x, y) : r; });
                        break;
                    case SortRule.ModificationDateThenName:
                        comparer = new AnonymousComparer<AssetViewModel>((x, y) => { var r = -x.AssetItem.ModifiedTime.CompareTo(y.AssetItem.ModifiedTime); return r == 0 ? assetNameComparer.Compare(x, y) : r; });
                        break;

                    default:
                        // Sort by name by default.
                        comparer = assetNameComparer;
                        break;
                }
                filteredList.Sort(comparer);
                filteredAssets.AddRange(filteredList);
                refreshing = false;

                // Force updating the filtered content
                UpdateFilteredContent();
            }

            // TODO: we use assetProperties value to determine if it's the main asset collection view model. A proper boolean would be better
            if (assetProperties != null)
            {
                InternalSettings.AssetViewSortRule.SetValue(SortRule);
            }
        }

        private bool Match(AssetViewModel asset)
        {
            if (CustomFilter != null && !CustomFilter(asset))
                return false;

            // Type filters are OR-ed
            var activeTypeFilters = currentAssetFilters.Where(f => f.Category == FilterCategory.AssetType && f.IsActive).ToList();
            if (activeTypeFilters.Count > 0 && !activeTypeFilters.Any(f => f.Match(asset)))
                return false;

            // Name and tag filters are AND-ed
            if (!currentAssetFilters.Where(f => f.Category != FilterCategory.AssetType && f.IsActive).All(f => f.Match(asset)))
                return false;

            return true;
        }

        private void SelectedContentCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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

            if (assetProperties != null)
            {
                assetProperties.UpdateTypeAndName(SelectedAssets, x => x.TypeDisplayName, x => x.Url, "assets");
                assetProperties.GenerateSelectionPropertiesAsync(SelectedAssets).Forget();
            }
        }

        private void UpdateSingleSelectedContent()
        {
            SingleSelectedContent = SelectedContent.Count == 1 ? SelectedContent.First() : null;
        }

        private void FilteredAssetsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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

            if (displayMode != DisplayAssetMode.AssetAndFolderInSelectedFolder)
            {
                filteredContent.AddRange(filteredAssets);
                return;
            }

            // Filter folders by name
            IEnumerable<object> folders = monitoredDirectories.SelectMany(d => d.SubDirectories)
                .Where(d => currentAssetFilters.Where(f => f.Category == FilterCategory.AssetName && f.IsActive)
                    .All(f => ComputeTokens(f.Filter).All(x => d.Name.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0)));
            filteredContent.AddRange(folders.Concat(filteredAssets));
        }

        private void UpdateCommands()
        {
            var atLeastOneAsset = SelectedAssets.Count > 0;
            var atLeastOneContent = SelectedContent.Count > 0;

            // TODO: Allow to cut asset mount point - but do not remove the mount point
            CutLocationsCommand.IsEnabled = SelectedLocations.All(x => x is DirectoryViewModel || x is PackageViewModel);
            // Can copy from asset mount point
            CopyLocationsCommand.IsEnabled = SelectedLocations.All(x => x is DirectoryBaseViewModel || x is PackageViewModel);
            PasteCommand.IsEnabled = SelectedLocations.Count == 1 && SelectedLocations.All(x => x is DirectoryBaseViewModel || x is PackageViewModel);

            CopyContentCommand.IsEnabled = atLeastOneContent;
            CutContentCommand.IsEnabled = atLeastOneContent;
            DeleteContentCommand.IsEnabled = atLeastOneContent;

            CopyAssetsRecursivelyCommand.IsEnabled = atLeastOneAsset;
            CopyAssetUrlCommand.IsEnabled = SingleSelectedAsset != null;
        }

        private async Task SelectFilesToCreateAsset()
        {
            var dialog = Dialogs.CreateFileOpenModalDialog();
            dialog.AllowMultiSelection = true;
            dialog.InitialDirectory = InternalSettings.FileDialogLastImportDirectory.GetValue();
            var result = await dialog.ShowModal();

            if (result == DialogResult.Ok && dialog.FilePaths.Count > 0)
            {
                List<UFile> files = dialog.FilePaths.Select(x => new UFile(x)).ToList();
                // Simulate a drop of file
                ((IAddChildViewModel)this).AddChildren(files, AddChildModifiers.None);
            }
        }

        /// <inheritdoc/>
        bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            var directories = GetSelectedDirectories(false);
            if (directories.Count != 1)
            {
                message = Tr._p("Message", "This location is invalid. Please select a package folder in the Solution explorer.");
                return false;
            }

            if (children.All(x => x is UFile))
            {
                message = Tr._p("Message", "Drop files");
                return true;
            }
            message = DragDropBehavior.InvalidDropAreaMessage;
            return false;
        }

        /// <inheritdoc/>
        async void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            var files = new List<UFile>();
            foreach (var file in children.Cast<UFile>())
            {
                ConstructFileList(file, ref files);
            }
            // Compute the list of compatible templates per file extension

            var extensionsList = new Dictionary<string, int>();
            foreach (var file in files)
            {
                var extension = file.GetFileExtension() ?? "";
                if (!extensionsList.ContainsKey(extension))
                    extensionsList[extension] = 1;
                else
                    ++extensionsList[extension];
            }
            var extensions = new Dictionary<string, List<TemplateAssetDescription>>();
            foreach (var extension in extensionsList.Keys)
            {
                var assetTemplates = Session.FindTemplates(TemplateScope.Asset).Cast<TemplateAssetDescription>();
                var matchingTemplates = assetTemplates.Where(x => x.ImportSource && x.GetSupportedExtensions().Contains(extension));
                extensions[extension] = matchingTemplates.OrderBy(x => x.Order).ToList();
            }

            // Ensure that all template collections matches. This allows to have different extension of the same type of asset (such as fbx/obj or png/tga/...) to still work, but not fbx/png.
            var firstList = extensions.First();
            bool templateMismatch = false;
            foreach (var extension in extensions)
            {
                if (!ArrayExtensions.ArraysReferenceEqual(firstList.Value, extension.Value))
                {
                    templateMismatch = true;
                    break;
                }
            }

            Dictionary<TemplateAssetDescription, int> templatesDictionary = null;
            if (templateMismatch)
            {
                // If we have a mismatch, we'll use a slightly different path. In this case we build a dictionary indicating the number of matching files per template
                // NOTE: This will break the order, but it's not a problem as long as AssetTemplatesViewModel rebuild the proper order.
                templatesDictionary = new Dictionary<TemplateAssetDescription, int>();
                foreach (var extension in extensionsList)
                {
                    foreach (var template in extensions[extension.Key])
                    {
                        if (!templatesDictionary.ContainsKey(template))
                            templatesDictionary[template] = extension.Value;
                        else
                            templatesDictionary[template] += extension.Value;
                    }
                }
            }

            var templates = firstList.Value;
            // Note: this should never happen since we have raw assets, litteraly everything can be imported.
            if (templates.Count == 0)
            {
                await Dialogs.MessageBox(Tr._p("Message", "These files aren't supported."), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var directory = GetSelectedDirectories(false).First();
            // Don't display the template selection window if there is no choice to make
            if (templates.Count == 1)
            {
                await RunAssetTemplate(new TemplateDescriptionViewModel(ServiceProvider, templates.Single()), files);
            }
            else
            {
                if (!templateMismatch)
                {
                    // No mismatch, the easy way
                    var dialog = Dialogs.CreateAssetTemplatesDialog(Session, directory, templates);

                    var result = await dialog.ShowModal();
                    if (result == DialogResult.Ok && dialog.SelectedTemplate != null)
                    {
                        await RunAssetTemplate(dialog.SelectedTemplate, files);
                    }
                }
                else
                {
                    // Mismatch, we pass additional information because we want to display some numbers
                    var dialog = Dialogs.CreateAssetTemplatesDialog(Session, directory, files.Count, templatesDictionary);

                    var result = await dialog.ShowModal();
                    if (result == DialogResult.Ok && dialog.SelectedTemplate != null)
                    {
                        // Rebuild a list of files that matches the selected template
                        files = files.Where(x => extensions[x.GetFileExtension()].Contains((TemplateAssetDescription)dialog.SelectedTemplate.GetTemplate())).ToList();
                        await RunAssetTemplate(dialog.SelectedTemplate, files);
                    }
                }
            }
        }

        private static void ConstructFileList(UFile file, ref List<UFile> fileList)
        {
            try
            {
                if (Directory.Exists(file))
                {
                    foreach (var subFile in Directory.GetFiles(file))
                    {
                        ConstructFileList(subFile, ref fileList);
                    }
                    foreach (var subDir in Directory.GetDirectories(file))
                    {
                        ConstructFileList(subDir, ref fileList);
                    }
                }
                else
                {
                    fileList.Add(file);
                }
            }
            catch (Exception e)
            {
                e.Ignore();
            }
        }
    }
}
