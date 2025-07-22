// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Specialized;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Components.Transactions;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModels;

public sealed partial class SessionViewModel : DispatcherViewModel, ISessionViewModel
{
    public static readonly string StorePackageCategoryName = Tr._("External packages");
    public static readonly string LocalPackageCategoryName = Tr._("Local packages");

    private SessionObjectPropertiesViewModel activeProperties;
    private readonly ConcurrentDictionary<AssetId, AssetViewModel> assetIdMap = [];
    private ProjectViewModel? currentProject;
    private readonly Dictionary<string, PackageCategoryViewModel> packageCategories = [];
    private readonly Dictionary<PackageViewModel, PackageContainer> packageMap = [];
    private readonly PackageSession session;
    private bool sessionStateUpdating;

    private readonly IDebugPage? assetNodesDebugPage;
    private readonly IDebugPage? quantumDebugPage;
    private readonly IDebugPage? undoRedoStackPage;

    private SessionViewModel(IViewModelServiceProvider serviceProvider, PackageSession session, IMainViewModel main, ILogger logger)
        : base(serviceProvider)
    {
        this.session = session;
        Main = main;

        // Make sure plugins are initialized
        PluginService.EnsureInitialized(logger);

        // Initialize the node container used for asset properties
        AssetNodeContainer = new SessionNodeContainer(serviceProvider) { NodeBuilder = { NodeFactory = new AssetNodeFactory() } };

        // Initialize the asset collection view model
        AssetCollection = new AssetCollectionViewModel(this);

        // Initialize the asset collection view model
        EditorCollection = new EditorCollectionViewModel(this);

        // Initialize debug pages
        var debugService = serviceProvider.Get<IEditorDebugService>();
        assetNodesDebugPage = debugService.CreateAssetNodesDebugPage(this, "Asset nodes visualizer");
        quantumDebugPage = debugService.CreateLogDebugPage(GlobalLogger.GetLogger(GraphViewModel.DefaultLoggerName), "Quantum log");
        if (ActionService is { } actionService)
        {
            undoRedoStackPage = debugService.CreateUndoRedoDebugPage(actionService, "Undo/redo stack");
            ActionHistory = new ActionHistoryViewModel(this);
        }

        ActiveProperties = AssetCollection.AssetViewProperties;
        SelectionService?.RegisterSelectionScope(id => GetAssetById(id.AssetId), o =>
        {
            if (o is AssetViewModel asset)
            {
                return new AbsoluteId(asset.Id, Guid.Empty);
            }
            return null;
        }, AssetCollection.SelectedContent);
        // FIXME xplat-editor
        //AssetCollection.SelectedAssets.CollectionChanged += SelectedAssetsCollectionChanged;
        AssetCollection.SelectedContent.CollectionChanged += (s, e) => UpdateSessionState();
        AssetCollection.SelectedLocations.CollectionChanged += (s, e) => UpdateSessionState();

        // Initialize logs
        AssetLog = new AssetLogViewModel(ServiceProvider, this);

        // Initialize the reference view model related to the main asset view
        References = new ReferencesViewModel(this);

        // Construct package categories
        var localPackageName = session.SolutionPath != null ? string.Format(Tr._(@"Solution '{0}'"), session.SolutionPath.GetFileNameWithoutExtension()) : LocalPackageCategoryName;
        packageCategories.Add(LocalPackageCategoryName, new PackageCategoryViewModel(localPackageName, this));
        packageCategories.Add(StorePackageCategoryName, new PackageCategoryViewModel(StorePackageCategoryName, this));
        LocalPackages.CollectionChanged += LocalPackagesCollectionChanged;

        // Initialize commands
        EditSelectedContentCommand = new AnonymousCommand(serviceProvider, OnEditSelectedContent);
        PreviousSelectionCommand = new AnonymousCommand(serviceProvider, () => { SelectionService?.NavigateBackward(); UpdateSelectionCommands(); });
        NextSelectionCommand = new AnonymousCommand(serviceProvider, () => { SelectionService?.NextSelection(); UpdateSelectionCommands(); });

        // This event must be subscribed before we create the package view models
        PackageCategories.ForEach(x => x.Value.Content.CollectionChanged += PackageCollectionChanged);

        // Create package view models
        this.session.Projects.ForEach(x => CreateProjectViewModel(x, true));

        // Initialize other sub view models
        Thumbnails = new ThumbnailsViewModel(this);

        GraphContainer = new AssetPropertyGraphContainer(AssetNodeContainer);

        // Initialize session itself in plugins
        foreach (var plugin in PluginService.Plugins)
        {
            plugin.InitializeSession(this);
        }
    }

    /// <summary>
    /// Gets the currently active <see cref="SessionObjectPropertiesViewModel"/>.
    /// </summary>
    // FIXME xplat-editor do we need both ActiveProperties and AssetCollection.AssetViewProperties?
    public SessionObjectPropertiesViewModel ActiveProperties
    {
        get { return activeProperties; }
        set
        {
            if (SetValue(ref activeProperties, value))
            {
                // FIXME xplat-editor
                //ActiveAssetsChanged?.Invoke(this, new ActiveAssetsChangedArgs(value?.GetRelatedAssets().ToList()));
            }
        }
    }

    public ActionHistoryViewModel? ActionHistory { get; }

    public IUndoRedoService? ActionService => ServiceProvider.TryGet<IUndoRedoService>();

    public IEnumerable<AssetViewModel> AllAssets => AllPackages.SelectMany(x => x.Assets);

    public IEnumerable<PackageViewModel> AllPackages => PackageCategories.Values.SelectMany(x => x.Content);

    public AssetCollectionViewModel AssetCollection { get; }

    public AssetLogViewModel AssetLog { get; }

    public AssetNodeContainer AssetNodeContainer { get; }

    /// <summary>
    /// Gets the current active project for build/startup operations.
    /// </summary>
    // TODO: this property should become cancellable to maintain action stack consistency! Undoing a "mark as root" operation after changing the current package wouldn't work.
    public ProjectViewModel? CurrentProject
    {
        get => currentProject;
        private set
        {
            var oldValue = currentProject;
            //SetValueUncancellable(ref currentProject, value, () => UpdateCurrentProject(oldValue, value));
            SetValue(ref currentProject, value, () => UpdateCurrentProject(oldValue, value));
        }
    }

    /// <summary>
    /// Gets the dependency manager associated to this session.
    /// </summary>
    public IAssetDependencyManager DependencyManager => session.DependencyManager;

    public EditorCollectionViewModel EditorCollection { get; }

    public AssetPropertyGraphContainer GraphContainer { get; }

    public IObservableCollection<PackageViewModel> LocalPackages => PackageCategories[LocalPackageCategoryName].Content;

    public IMainViewModel Main { get; }

    public IReadOnlyDictionary<string, PackageCategoryViewModel> PackageCategories => packageCategories;

    public ReferencesViewModel References { get; }

    public UFile SolutionPath => session.SolutionPath;

    public IAssetSourceTrackerViewModel SourceTracker { get; private set; }

    public IObservableCollection<PackageViewModel> StorePackages => PackageCategories[StorePackageCategoryName].Content;

    public ThumbnailsViewModel Thumbnails { get; }

    public ICommandBase EditSelectedContentCommand { get; }

    public ICommandBase NextSelectionCommand { get; }

    public ICommandBase PreviousSelectionCommand { get; }

    internal PackageSession PackageSession => session;

    internal IAssetsPluginService PluginService => ServiceProvider.Get<IAssetsPluginService>();

    internal SelectionService? SelectionService => ServiceProvider.TryGet<SelectionService>();

    /// <summary>
    /// Raised when some assets are modified.
    /// </summary>
    public event EventHandler<AssetChangedEventArgs>? AssetPropertiesChanged;

    /// <summary>
    /// Raised when some assets are deleted or undeleted.
    /// </summary>
    public event EventHandler<NotifyCollectionChangedEventArgs?> DeletedAssetsChanged;

    /// <summary>
    /// Raised when the session state changed (e.g. current package).
    /// </summary>
    public event EventHandler<SessionStateChangedEventArgs>? SessionStateChanged;

    /// <inheritdoc />
    public AssetViewModel? GetAssetById(AssetId id)
    {
        assetIdMap.TryGetValue(id, out var result);
        return result;
    }

    /// <inheritdoc />
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(SessionViewModel));

        ActionHistory?.Destroy();
        AssetLog.Destroy();
        Thumbnails.Destroy();

        var debugService = ServiceProvider.Get<IEditorDebugService>();
        debugService.UnregisterDebugPage(undoRedoStackPage);
        debugService.UnregisterDebugPage(assetNodesDebugPage);
        debugService.UnregisterDebugPage(quantumDebugPage);
        // Unregister collection
        SelectionService?.UnregisterSelectionScope(AssetCollection.SelectedContent);

        base.Destroy();
    }

    /// <inheritdoc />
    public Type GetAssetViewModelType(AssetItem assetItem)
    {
        var assetType = assetItem.Asset.GetType();
        return PluginService.GetAssetViewModelType(assetType) ?? typeof(AssetViewModel<>);
    }

    /// <summary>
    /// Notifies the session that a property of some assets has been changed.
    /// </summary>
    /// <remarks>
    /// Since notifications will be raised asynchronously, <paramref name="assets"/> collection should not be modified after it has been passed to this method.
    /// If necessary, caller must provide a copy.
    /// </remarks>
    public async Task NotifyAssetPropertiesChangedAsync(IReadOnlyCollection<AssetViewModel> assets)
    {
        var tasks = assets.Select(x => AssetDependenciesViewModel.NotifyAssetChanged(x.Session, x)).ToList();
        await Task.WhenAll(tasks);
        // We raise this event from a task because it will trigger heavy work on the different subscribers
        await Task.Run(() => AssetPropertiesChanged?.Invoke(this, new AssetChangedEventArgs(assets)));
    }

    /// <inheritdoc />
    public void RegisterAsset(AssetViewModel asset)
    {
        ((IDictionary<AssetId, AssetViewModel>)assetIdMap).Add(asset.Id, asset);
    }

    /// <inheritdoc />
    public void UnregisterAsset(AssetViewModel asset)
    {
        ((IDictionary<AssetId, AssetViewModel>)assetIdMap).Remove(asset.Id);
    }

    private void AutoSelectCurrentProject()
    {
        var currentProject = LocalPackages.OfType<ProjectViewModel>().FirstOrDefault(/* FIXME sxplat-editor x => x.Type == ProjectType.Executable && x.Platform == PlatformType.Windows*/) ?? LocalPackages.FirstOrDefault();
        if (currentProject != null)
        {
            SetCurrentProject(currentProject);
        }
    }

    private PackageViewModel CreateProjectViewModel(PackageContainer packageContainer, bool packageAlreadyInSession)
    {
        switch (packageContainer)
        {
            case SolutionProject project:
                {
                    var packageContainerViewModel = new ProjectViewModel(this, project, packageAlreadyInSession);
                    packageMap.Add(packageContainerViewModel, project);
                    if (!packageAlreadyInSession)
                        session.Projects.Add(project);
                    return packageContainerViewModel;
                }
            case StandalonePackage standalonePackage:
                {
                    var packageContainerViewModel = new PackageViewModel(this, standalonePackage, packageAlreadyInSession);
                    packageMap.Add(packageContainerViewModel, standalonePackage);
                    if (!packageAlreadyInSession)
                        session.Projects.Add(standalonePackage);
                    return packageContainerViewModel;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(packageContainer));
        }
    }

    private void LoadAssetsFromPackages(IProgressViewModel? progressVM, CancellationToken token = default)
    {
        if (progressVM is not null)
        {
            progressVM.Minimum = 0;
            progressVM.ProgressValue = 0;
            progressVM.Maximum = session.Packages.Sum(x => x.Assets.Count);
        }
        double progress = 0.0;

        // Create directory and asset view models for each project
        foreach (var package in AllPackages)
        {
            if (token.IsCancellationRequested)
                return;

            package.LoadPackageInformation(progressVM, ref progress, token);
        }

        // Create actions corresponding to potential upgrades/fixes that occurred during the loading process.
        // FIXME xplat-editor

        SourceTracker = new AssetSourceTrackerViewModel(this);
        UpdateSessionState();

        // This transaction is done to prevent action responding to undoRedoService.TransactionCompletion to occur during loading
        using var transaction = ActionService?.CreateTransaction();
        ProcessAddedPackages(AllPackages).Forget();
    }

    private void LocalPackagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            session.Projects.RemoveWhere(x => !x.Package.IsSystem);
        }
        if (e.NewItems != null)
        {
            // When a PackageViewModel is built, we will add it before the Package instance is added to the package map.
            // So we can't assume that the view model will always exists in the packageMap.
            packageMap.Where(x => e.NewItems.Cast<PackageViewModel>().Contains(x.Key)).ForEach(x => session.Projects.Add(x.Value));
        }
        e.OldItems?.Cast<PackageViewModel>().Select(x => packageMap[x]).ForEach(x => session.Projects.Remove(x));
    }

    private void PackageCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        e.NewItems?.Cast<PackageViewModel>().ForEach(x => x.DeletedAssets.CollectionChanged += DeletedAssetChanged);
        e.OldItems?.Cast<PackageViewModel>().ForEach(x => x.DeletedAssets.CollectionChanged -= DeletedAssetChanged);
        return;

        void DeletedAssetChanged(object? _, NotifyCollectionChangedEventArgs ev)
        {
            DeletedAssetsChanged?.Invoke(this, ev);
        }
    }

    private async Task ProcessAddedPackages(IEnumerable<PackageViewModel> packages)
    {
        var packageList = packages.ToList();
        // We must refresh asset bases after all packages have been added, because we might have cross-packages references here.
        packageList.SelectMany(x => x.Assets).ForEach(x => x.Initialize());
        await AssetDependenciesViewModel.TriggerInitialReferenceBuild(this);
        await Dispatcher.InvokeAsync(() => packageList.ForEach(x => Thumbnails.StartInitialBuild(x)));
    }

    private void SetCurrentProject(object selectedItem)
    {
        if (selectedItem is not ProjectViewModel project)
        {
            // Editor.MessageBox(Resources.Strings.SessionViewModel.SelectExecutableAsCurrentProject, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        CurrentProject = project;
        AllAssets.ForEach(x => x.Dependencies.NotifyRootAssetChange(false));
        // FIXME xplat-editor
        //SelectionIsRoot = AssetCollection.SelectedAssets.All(x => x.Dependencies.IsRoot);
    }

    private void UpdateCurrentProject(ProjectViewModel? oldValue, ProjectViewModel? newValue)
    {
        //if (oldValue != null)
        //{
        //    oldValue.IsCurrentProject = false;
        //}
        //if (newValue != null)
        //{
        //    newValue.IsCurrentProject = true;
        //}
        //ToggleIsRootOnSelectedAssetCommand.IsEnabled = CurrentProject != null;
        UpdateSessionState();
    }

    private void UpdateSelectionCommands()
    {
        PreviousSelectionCommand.IsEnabled = SelectionService?.CanGoBack ?? false;
        NextSelectionCommand.IsEnabled = SelectionService?.CanGoForward ?? false;
    }

    private void UpdateSessionState()
    {
        sessionStateUpdating = true;
        Dispatcher.InvokeAsync(DoUpdateSessionState);

        void DoUpdateSessionState()
        {
            if (!sessionStateUpdating)
                return;

            UpdateSelectionCommands();

            sessionStateUpdating = false;
        }
    }

    #region Commands
    private void OnEditSelectedContent()
    {
        // Cannot edit multi-selection
        if (AssetCollection.SingleSelectedContent == null)
            return;

        // Asset
        var asset = AssetCollection.SingleSelectedAsset;
        if (asset != null)
        {
            EditorCollection.OpenAssetEditor(asset);
        }

        // Folder
        if (AssetCollection.SingleSelectedContent is DirectoryViewModel folder)
        {
            AssetCollection.SelectedLocations.Clear();
            AssetCollection.SelectedLocations.Add(folder);
        }
    }
    #endregion // Commands
}
