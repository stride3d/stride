// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Diagnostics;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Components.Transactions;
using Stride.Core.Assets.Editor.Internal;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.Logs;
using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Packages;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Reflection;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// The session of the editor: the packages, their assets, the asset view (<see cref="AssetCollection"/>) and the asset editors (<see cref="EditorCollection"/>).
/// </summary>
public sealed partial class SessionViewModel : DirtiableEditableViewModel, IAssetFinder, ISessionViewModel
{
    public static readonly string StorePackageCategoryName = Tr._("External packages");
    public static readonly string LocalPackageCategoryName = Tr._("Local packages");
    public const string SolutionExtension = ".slnx";
    /// <summary>The UI toolkit of this editor; only the Editor companion packages for it are loaded.</summary>
    public const string EditorToolkitName = EditorToolkits.Avalonia;
    public const int SaveIrreversibleSourceFileOperationsMessageCount = 5;

    private readonly ConcurrentDictionary<AssetId, AssetViewModel> assetIdMap = [];
    private readonly Dictionary<string, PackageCategoryViewModel> packageCategories = [];
    private readonly Dictionary<PackageViewModel, PackageContainer> packageMap = [];
    private readonly ObservableList<RootAssetTargetViewModel> rootAssetTargets = [];
    private readonly PackageSession session;
    private readonly IUndoRedoService undoRedoService;
    private Dictionary<AssetId, AssetViewModel> replacedTargets = [];

    private SessionObjectPropertiesViewModel activeProperties;
    private ProjectViewModel? currentProject;
    private PackageViewModel? rootAssetDefaultTarget;
    private string rootAssetToggleHeader = string.Empty;
    private bool selectionIsRoot;
    private bool hasUnsavedChanges;
    private bool isInFixupAssetContext;
    private bool sessionStateUpdating;
    // Non-zero while a raise is scheduled
    private int assetTypesChangedPending;

    private readonly IDebugPage? assetNodesDebugPage;
    private readonly IDebugPage? quantumDebugPage;
    private readonly IDebugPage? undoRedoStackPage;

    private SessionViewModel(IViewModelServiceProvider serviceProvider, PackageSession session, IMainViewModel main, ILogger logger)
        : base(serviceProvider)
    {
        this.session = session;
        Main = main;
        undoRedoService = ServiceProvider.Get<IUndoRedoService>();

        // Gather all data from plugins
        PluginService.RegisterSession(this, logger);

        // Create the service that handles property documentation
        serviceProvider.RegisterService(new UserDocumentationService(this));

        // Initialize the node container used for asset properties
        AssetNodeContainer = new SessionNodeContainer(this) { NodeBuilder = { NodeFactory = new AssetNodeFactory() } };

        // Initialize the asset collection view model of the main asset view (it owns the properties of the selected assets)
        AssetCollection = new AssetCollectionViewModel(this, isMainView: true);
        activeProperties = AssetCollection.AssetViewProperties;

        // Initialize the editor collection view model
        EditorCollection = new EditorCollectionViewModel(this);

        // Initialize debug pages
        if (serviceProvider.TryGet<IEditorDebugService>() is { } debugService)
        {
            assetNodesDebugPage = debugService.CreateAssetNodesDebugPage(this, "Asset nodes visualizer");
            quantumDebugPage = debugService.CreateLogDebugPage(GlobalLogger.GetLogger(GraphViewModel.DefaultLoggerName), "Quantum log");
            undoRedoStackPage = debugService.CreateUndoRedoDebugPage(undoRedoService, "Undo/redo stack");
        }
        ActionHistory = new ActionHistoryViewModel(this);

        SelectionService?.RegisterSelectionScope(id => GetAssetById(id.AssetId), o => o is AssetViewModel asset ? new AbsoluteId(asset.Id, Guid.Empty) : null, AssetCollection.SelectedContent);
        AssetCollection.SelectedAssets.CollectionChanged += SelectedAssetsCollectionChanged;
        AssetCollection.SelectedContent.CollectionChanged += (_, _) => UpdateSessionState();
        AssetCollection.SelectedLocations.CollectionChanged += (_, _) => UpdateSessionState();

        // Initialize logs
        AssetLog = new AssetLogViewModel(ServiceProvider, this);

        // Initialize the reference view model related to the main asset view
        References = new ReferencesViewModel(this);

        // The tags of the selected assets, in the header of the property grid
        AssetTags = new TagsViewModel(AssetCollection);

        // Construct package categories
        var localPackageName = session.SolutionPath != null ? string.Format(Tr._(@"Solution '{0}'"), session.SolutionPath.GetFileNameWithoutExtension()) : LocalPackageCategoryName;
        packageCategories.Add(LocalPackageCategoryName, new PackageCategoryViewModel(localPackageName, this));
        packageCategories.Add(StorePackageCategoryName, new PackageCategoryViewModel(StorePackageCategoryName, this));
        LocalPackages.CollectionChanged += LocalPackagesCollectionChanged;
        // Asset assemblies add or remove asset types
        AssemblyRegistry.AssemblyRegistered += AssetAssembliesChanged;
        AssemblyRegistry.AssemblyUnregistered += AssetAssembliesChanged;

        // Initialize commands
        SaveSessionCommand = new AnonymousTaskCommand(ServiceProvider, SaveSessionAsync);
        AddExistingProjectCommand = new AnonymousTaskCommand(ServiceProvider, AddExistingProjectAsync);
        NewProjectCommand = new AnonymousTaskCommand(ServiceProvider, NewProjectAsync) { IsEnabled = !string.IsNullOrWhiteSpace(session.SolutionPath) };
        ActivatePackagePropertiesCommand = new AnonymousTaskCommand(ServiceProvider, async () =>
        {
            var directory = AssetCollection.GetSelectedDirectories(false).FirstOrDefault();
            if (directory != null)
                await directory.Package.Properties.GenerateSelectionPropertiesAsync(directory.Package.Yield());
        });
        NewDirectoryCommand = new AnonymousTaskCommand<IEnumerable<object>>(ServiceProvider, CreateNewFolderInDirectoriesAsync);
        RenameDirectoryOrPackageCommand = new AnonymousCommand<IEnumerable>(ServiceProvider, x => RenameDirectoryOrPackage(x.Cast<object>().LastOrDefault()));
        DeleteSelectedSolutionItemsCommand = new AnonymousTaskCommand(ServiceProvider, async () => await DeleteItems(AssetCollection.SelectedLocations));
        SetCurrentProjectCommand = new AnonymousCommand<object>(ServiceProvider, SetCurrentProject);
        EditSelectedContentCommand = new AnonymousCommand(ServiceProvider, EditSelectedContent);
        ToggleIsRootOnSelectedAssetCommand = new AnonymousCommand(ServiceProvider, () => ToggleRootAsset(AssetCollection.SelectedAssets, RootAssetDefaultTarget));
        PreviousSelectionCommand = new AnonymousCommand(ServiceProvider, () => { SelectionService?.NavigateBackward(); UpdateSelectionCommands(); });
        NextSelectionCommand = new AnonymousCommand(ServiceProvider, () => { SelectionService?.NextSelection(); UpdateSelectionCommands(); });

        // This event must be subscribed before we create the package view models
        PackageCategories.ForEach(x => x.Value.Content.CollectionChanged += PackageCollectionChanged);

        // Create package view models
        this.session.Projects.ForEach(x => CreateProjectViewModel(x, true));

        // Initialize other sub view models
        Thumbnails = new ThumbnailsViewModel(this);

        GraphContainer = new AssetPropertyGraphContainer(AssetNodeContainer);

        // Register the node container to the copy/paste service.
        if (ServiceProvider.TryGet<CopyPasteService>() is { } copyPasteService)
            copyPasteService.PropertyGraphContainer = GraphContainer;
    }

    /// <summary>
    /// Gets the currently active <see cref="SessionObjectPropertiesViewModel"/>.
    /// </summary>
    // FIXME xplat-editor do we need both ActiveProperties and AssetCollection.AssetViewProperties?
    public SessionObjectPropertiesViewModel ActiveProperties
    {
        get => activeProperties;
        set
        {
            if (SetValueUncancellable(ref activeProperties, value))
            {
                ActiveAssetsChanged?.Invoke(this, new ActiveAssetsChangedArgs(value.GetRelatedAssets().ToList()));
            }
        }
    }

    public ActionHistoryViewModel ActionHistory { get; }

    /// <summary>
    /// Gets all assets contained in this session.
    /// </summary>
    /// <remarks>
    /// Some assets in the session might not be accessible to some other assets/packages if they are located in another package that is not a dependency
    /// to the asset/package. To safely retrieve all assets accessible from a specific package, use <see cref="PackageViewModel.AllAssets"/>.
    /// </remarks>
    public IEnumerable<AssetViewModel> AllAssets => AllPackages.SelectMany(x => x.Assets);

    public IEnumerable<PackageViewModel> AllPackages => PackageCategories.Values.SelectMany(x => x.Content);

    // TODO: Properly temporary, until unification of AssemblyContainer and AssemblyRegistry since we now have only a single session at a time
    public AssemblyContainer AssemblyContainer => session.AssemblyContainer;

    /// <summary>
    /// Gets the main asset view of the session.
    /// </summary>
    public AssetCollectionViewModel AssetCollection { get; }

    public AssetLogViewModel AssetLog { get; }

    public SessionNodeContainer AssetNodeContainer { get; }

    /// <summary>
    /// Gets the properties of the objects selected in the main asset view.
    /// </summary>
    public SessionObjectPropertiesViewModel AssetViewProperties => AssetCollection.AssetViewProperties;

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
            SetValueUncancellable(ref currentProject, value, () => UpdateCurrentProject(oldValue, value));
        }
    }

    /// <summary>
    /// Gets the dependency manager associated to this session.
    /// </summary>
    public IAssetDependencyManager DependencyManager => session.DependencyManager;

    /// <summary>
    /// Gets the asset editors of the session.
    /// </summary>
    public EditorCollectionViewModel EditorCollection { get; }

    public AssetPropertyGraphContainer GraphContainer { get; }

    /// <summary>
    /// Gets whether any asset of the session declares <see cref="Asset.Replaces"/>.
    /// </summary>
    public bool HasAssetReplacements => AllAssets.Any(x => x.Asset.Replaces != null);

    public bool IsEditorInitialized { get; private set; }

    /// <summary>
    /// Gets whether the session is currently in a special context to fix up assets.
    /// </summary>
    /// <seealso cref="CreateAssetFixupContext"/>
    public bool IsInFixupAssetContext
    {
        get => isInFixupAssetContext;
        internal set
        {
            GraphContainer.PropagateChangesFromBase = !value;
            SetValue(ref isInFixupAssetContext, value);
            ActiveProperties.RefreshSelectedPropertiesAsync().Forget();
        }
    }

    public IObservableCollection<PackageViewModel> LocalPackages => PackageCategories[LocalPackageCategoryName].Content;

    public IMainViewModel Main { get; }

    public IReadOnlyDictionary<string, PackageCategoryViewModel> PackageCategories => packageCategories;

    public ReferencesViewModel References { get; }

    /// <summary>
    /// Gets the tags of the assets selected in the asset view.
    /// </summary>
    public TagsViewModel AssetTags { get; }

    /// <summary>
    /// Gets the package the root-asset toggle targets for the current selection (see <see cref="GetDefaultRootAssetTarget"/>).
    /// </summary>
    public PackageViewModel? RootAssetDefaultTarget { get => rootAssetDefaultTarget; private set => SetValueUncancellable(ref rootAssetDefaultTarget, value); }

    /// <summary>
    /// Gets the packages the current selection can be rooted in, each with its own toggle.
    /// </summary>
    public IReadOnlyObservableList<RootAssetTargetViewModel> RootAssetTargets => rootAssetTargets;

    /// <summary>
    /// Gets the header of the root-asset toggle menu item, naming its target package.
    /// </summary>
    public string RootAssetToggleHeader { get => rootAssetToggleHeader; private set => SetValueUncancellable(ref rootAssetToggleHeader, value); }

    public Task SaveCompletion => session.SaveCompletion;

    /// <summary>
    /// Gets whether the session has changes that are not saved, as <see cref="HasUnsavedAssets"/> tells, kept current for the views
    /// (the <c>*</c> of the window title).
    /// </summary>
    /// <seealso cref="UpdateHasUnsavedChanges"/>
    public bool HasUnsavedChanges { get => hasUnsavedChanges; private set => SetValueUncancellable(ref hasUnsavedChanges, value); }

    public bool SelectionIsRoot { get => selectionIsRoot; internal set { SetValueUncancellable(ref selectionIsRoot, value); UpdateSessionState(); } }

    public UFile SolutionPath => session.SolutionPath;

    public AssetSourceTrackerViewModel? SourceTracker { get; private set; }

    public IObservableCollection<PackageViewModel> StorePackages => PackageCategories[StorePackageCategoryName].Content;

    /// <summary>
    /// Gets the packages suggested as dependencies, in addition to the loaded ones.
    /// </summary>
    public List<PackageName> SuggestedPackages { get; } = [];

    public ThumbnailsViewModel Thumbnails { get; }

    public ICommandBase ActivatePackagePropertiesCommand { get; }

    public ICommandBase AddExistingProjectCommand { get; }

    public ICommandBase DeleteSelectedSolutionItemsCommand { get; }

    public ICommandBase EditSelectedContentCommand { get; }

    public ICommandBase NewDirectoryCommand { get; }

    /// <summary>
    /// Adds a project to the session from a template (the new project window); enabled when the session has a solution.
    /// </summary>
    public ICommandBase NewProjectCommand { get; }

    public ICommandBase NextSelectionCommand { get; }

    public ICommandBase PreviousSelectionCommand { get; }

    public ICommandBase RenameDirectoryOrPackageCommand { get; }

    public ICommandBase SaveSessionCommand { get; }

    public ICommandBase SetCurrentProjectCommand { get; }

    public ICommandBase ToggleIsRootOnSelectedAssetCommand { get; }

    internal PackageSession PackageSession => session;

    internal IAssetsPluginService PluginService => ServiceProvider.Get<IAssetsPluginService>();

    internal SelectionService? SelectionService => ServiceProvider.TryGet<SelectionService>();

    private IDialogService Dialogs => ServiceProvider.Get<IDialogService>();

    /// <summary>
    /// Raised when some assets are modified.
    /// </summary>
    public event EventHandler<AssetChangedEventArgs>? AssetPropertiesChanged;

    /// <summary>
    /// Raised whenever <see cref="CheckAssetReplacements"/> runs, i.e. the session's replacement declarations may have changed.
    /// </summary>
    public event EventHandler? AssetReplacementsChanged;

    /// <summary>
    /// Raised when the asset types and templates of the session may have changed (packages or asset assemblies changed).
    /// </summary>
    public event EventHandler? AssetTypesChanged;

    /// <summary>
    /// Raised when the active assets collection changed.
    /// </summary>
    public event EventHandler<ActiveAssetsChangedArgs>? ActiveAssetsChanged;

    /// <summary>
    /// Raised when some assets are deleted or undeleted.
    /// </summary>
    public event EventHandler<NotifyCollectionChangedEventArgs>? DeletedAssetsChanged;

    /// <summary>
    /// Raised when the session state changed (e.g. current package).
    /// </summary>
    public event EventHandler<SessionStateChangedEventArgs>? SessionStateChanged;

    #region ISessionViewModel
    IAssetCollectionViewModel ISessionViewModel.ActiveAssetView => AssetCollection;

    ISessionObjectPropertiesViewModel ISessionViewModel.AssetViewProperties => AssetViewProperties;

    IAssetSourceTrackerViewModel ISessionViewModel.SourceTracker => SourceTracker!;

    IAssetLogViewModel ISessionViewModel.AssetLog => AssetLog;

    void ISessionViewModel.NotifyAssetPropertiesChanged(IReadOnlyCollection<AssetViewModel> assets) => NotifyAssetPropertiesChanged(assets);

    void ISessionViewModel.ProcessAddedProjects(LoggerResult loggerResult, WorkProgressViewModel workProgress, bool packageAlreadyInSession) => ProcessAddedProjects(loggerResult, workProgress, packageAlreadyInSession);

    void ISessionViewModel.ProcessRemovedProjects() => ProcessRemovedProjects();

    Task ISessionViewModel.LoadAddedDependencies(LoggerResult loggerResult) => LoadAddedDependenciesAsync(loggerResult);

    /// <inheritdoc />
    public AssetViewModel? GetAssetById(AssetId id)
    {
        assetIdMap.TryGetValue(id, out var result);
        return result;
    }

    /// <inheritdoc />
    public Type GetAssetViewModelType(AssetItem assetItem)
    {
        var assetType = assetItem.Asset.GetType();
        return PluginService.GetAssetViewModelType(assetType) ?? typeof(AssetViewModel<>);
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

    public void CheckConsistency()
    {
        AllPackages.ForEach(x => x.CheckConsistency());
    }

    /// <summary>
    /// Validates every <see cref="Asset.Replaces"/> declaration of the session, reporting problems
    /// in the asset log. Declarations cross-reference each other (duplicates, targets), so any
    /// change re-validates them all; <paramref name="changedAsset"/> additionally gets its
    /// messages cleared when it no longer declares anything (cleared or deleted).
    /// </summary>
    public void CheckAssetReplacements(AssetViewModel? changedAsset = null)
    {
        var newReplaced = new Dictionary<AssetId, AssetViewModel>();
        foreach (var asset in AllAssets)
        {
            if (asset.Asset.Replaces is { } target)
            {
                CheckAssetReplacement(asset);
                if (GetAssetById(target.Id) is { } targetVm)
                    newReplaced.TryAdd(targetVm.Id, asset);
            }
        }
        // Refresh the reverse "Replaced by" indicator: clear targets that are no longer replaced,
        // then set the current ones.
        foreach (var id in replacedTargets.Keys)
        {
            if (!newReplaced.ContainsKey(id))
                GetAssetById(id)?.UpdateReplacedBy(null);
        }
        foreach (var (id, replacer) in newReplaced)
            GetAssetById(id)?.UpdateReplacedBy(replacer);
        replacedTargets = newReplaced;

        if (changedAsset != null && (changedAsset.IsDeleted || changedAsset.Asset.Replaces == null))
            AssetLog.ClearMessages(LogKey.Get(changedAsset.Id, AssetLogViewModel.AssetReplacementLogName));
        AssetReplacementsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the packages the given assets can be rooted in: editable local packages that have all of them in scope.
    /// </summary>
    public IReadOnlyList<PackageViewModel> GetRootAssetTargets(IReadOnlyCollection<AssetViewModel> assets)
    {
        if (assets.Count == 0)
            return [];
        return LocalPackages.Where(package => package.IsEditable && assets.All(package.IsInScope)).ToList();
    }

    /// <summary>
    /// Gets the package the root-asset toggle targets by default for the given assets: the package owning them when it is
    /// a valid target, else the game library the current project builds (the topmost library among its dependencies), else
    /// the current project. Rooting in the current project (a platform head) is a deliberate per-executable choice.
    /// </summary>
    public PackageViewModel? GetDefaultRootAssetTarget(IReadOnlyCollection<AssetViewModel> assets)
    {
        var targets = GetRootAssetTargets(assets);
        if (targets.Count == 0)
            return null;

        var owners = assets.Select(x => x.Directory.Package).Distinct().ToList();
        if (owners.Count == 1 && targets.Contains(owners[0]))
            return owners[0];

        if (CurrentProject != null)
        {
            var libraries = targets.Where(x => x != CurrentProject && x is not ProjectViewModel { Type: ProjectType.Executable } && CurrentProject.PackageContainer.FlattenedDependencies.Any(d => d.Package == x.Package)).ToList();
            var topmost = libraries.Where(x => !libraries.Any(other => other != x && other.PackageContainer.FlattenedDependencies.Any(d => d.Package == x.Package))).ToList();
            if (topmost.Count == 1)
                return topmost[0];
            if (targets.Contains(CurrentProject))
                return CurrentProject;
        }
        return targets[0];
    }

    /// <summary>
    /// Toggles the given assets as root assets of the given package: they all become roots, unless they already all are,
    /// in which case they all stop being roots of that package.
    /// </summary>
    public void ToggleRootAsset(IReadOnlyCollection<AssetViewModel> assets, PackageViewModel? target)
    {
        if (target == null)
            return;
        var toggleable = assets.Where(x => !x.Dependencies.ForcedRoot && target.IsInScope(x)).ToList();
        if (toggleable.Count == 0)
            return;
        var newValue = !toggleable.All(x => target.RootAssets.Contains(x));
        using var transaction = undoRedoService.CreateTransaction();
        foreach (var asset in toggleable)
        {
            if (newValue)
                target.RootAssets.Add(asset);
            else
                target.RootAssets.Remove(asset);
        }
        undoRedoService.SetName(transaction, "Change root assets");
    }

    /// <summary>
    /// Recomputes the root-asset state of the current selection: its default target, the toggle header and the per-package targets.
    /// </summary>
    public void RefreshRootAssetSelection()
    {
        var selectedAssets = AssetCollection.SelectedAssets;
        var targets = GetRootAssetTargets(selectedAssets);
        var defaultTarget = GetDefaultRootAssetTarget(selectedAssets);
        rootAssetTargets.Clear();
        rootAssetTargets.AddRange(targets.Select(x =>
        {
            var rootedCount = selectedAssets.Count(asset => x.RootAssets.Contains(asset));
            return new RootAssetTargetViewModel(this, x, x == defaultTarget, rootedCount == selectedAssets.Count, rootedCount > 0 && rootedCount < selectedAssets.Count);
        }));
        RootAssetDefaultTarget = defaultTarget;
        var isRoot = defaultTarget != null && selectedAssets.All(x => defaultTarget.RootAssets.Contains(x));
        RootAssetToggleHeader = defaultTarget == null
            ? Tr._p("Menu", "Include in build as root asset")
            : string.Format(isRoot ? Tr._p("Menu", "Don't include in build as root asset (in {0})") : Tr._p("Menu", "Include in build as root asset (in {0})"), defaultTarget.Name);
        SelectionIsRoot = isRoot;
    }

    /// <summary>
    /// Notifies the session that a property of some assets has been changed.
    /// </summary>
    /// <remarks>
    /// Since notifications will be raised asynchronously, <paramref name="assets"/> collection should not be modified after it has been passed to this method.
    /// If necessary, caller must provide a copy.
    /// </remarks>
    public void NotifyAssetPropertiesChanged(IReadOnlyCollection<AssetViewModel> assets)
    {
        NotifyAssetPropertiesChangedAsync(assets).Forget();
    }

    /// <inheritdoc cref="NotifyAssetPropertiesChanged"/>
    public async Task NotifyAssetPropertiesChangedAsync(IReadOnlyCollection<AssetViewModel> assets)
    {
        var tasks = assets.Select(x => AssetDependenciesViewModel.NotifyAssetChanged(x.Session, x)).ToList();
        await Task.WhenAll(tasks);
        // We raise this event from a task because it will trigger heavy work on the different subscribers
        await Task.Run(() => AssetPropertiesChanged?.Invoke(this, new AssetChangedEventArgs(assets)));
    }

    /// <summary>
    /// Creates the view models of the projects that were added to the session.
    /// </summary>
    internal void ProcessAddedProjects(LoggerResult loggerResult, WorkProgressViewModel workProgress, bool packageAlreadyInSession)
    {
        var newPackages = new List<PackageViewModel>();
        foreach (var package in session.Projects.Where(x => !packageMap.ContainsValue(x)))
        {
            var viewModel = CreateProjectViewModel(package, packageAlreadyInSession);
            viewModel.LoadPackageInformation(loggerResult, workProgress);
            newPackages.Add(viewModel);
        }
        ProcessAddedPackages(newPackages).Forget();
    }

    /// <summary>
    /// Removes the view models of the projects that were removed from the session.
    /// </summary>
    internal void ProcessRemovedProjects()
    {
        foreach (var package in packageMap.Where(x => !session.Projects.Contains(x.Value)).ToList())
        {
            // Unregister the removed package's assets (id + graph)
            foreach (var asset in package.Key.Assets.ToList())
                asset.MarkAsDeleted();
            LocalPackages.Remove(package.Key);
            packageMap.Remove(package.Key);
        }
    }

    /// <summary>
    /// Loads what a dependency just added (and restored) brings to the projects: its packages, their companions,
    /// assemblies and assets, as opening the session would have.
    /// </summary>
    internal async Task LoadAddedDependenciesAsync(LoggerResult loggerResult)
    {
        var workProgress = new WorkProgressViewModel(ServiceProvider, loggerResult)
        {
            Title = Tr._p("Title", "Adding dependency..."),
            KeepOpen = KeepOpen.OnWarningsOrErrors,
            IsIndeterminate = true,
            IsCancellable = false
        };
        workProgress.RegisterProgressStatus(loggerResult, true);
        var cancellationSource = new CancellationTokenSource();

        ServiceProvider.Get<IEditorDialogService>().ShowProgressWindow(workProgress);

        await Task.Run(() =>
        {
            try
            {
                session.LoadAddedDependencies(loggerResult, CreatePackageLoadParameters(ServiceProvider, cancellationSource.Token));
            }
            catch (Exception e)
            {
                loggerResult.Error(Tr._p("Log", "There was a problem loading the new dependencies."), e);
            }
        }, cancellationSource.Token);

        workProgress.Minimum = 0;
        workProgress.ProgressValue = 0;
        workProgress.Maximum = session.Projects.Where(x => !packageMap.ContainsValue(x)).Sum(x => x.Package.Assets.Count);
        ProcessAddedProjects(loggerResult, workProgress, true);

        await workProgress.NotifyWorkFinished(cancellationSource.IsCancellationRequested, loggerResult.HasErrors);
    }
    #endregion // ISessionViewModel

    #region IAssetFinder
    /// <inheritdoc/>
    AssetItem? IAssetFinder.FindAsset(AssetId assetId) => session.FindAsset(assetId);

    /// <inheritdoc/>
    AssetItem? IAssetFinder.FindAsset(UFile location) => session.FindAsset(location);

    /// <inheritdoc/>
    AssetItem? IAssetFinder.FindAssetFromProxyObject(object? proxyObject) => session.FindAssetFromProxyObject(proxyObject);
    #endregion // IAssetFinder

    /// <inheritdoc />
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(SessionViewModel));

        SourceTracker?.Destroy();
        ActionHistory.Destroy();
        Thumbnails.Destroy();
        AssetLog.Destroy();
        AssetTags.Destroy();

        if (ServiceProvider.TryGet<IEditorDebugService>() is { } debugService)
        {
            debugService.UnregisterDebugPage(undoRedoStackPage);
            debugService.UnregisterDebugPage(assetNodesDebugPage);
            debugService.UnregisterDebugPage(quantumDebugPage);
        }
        // Unregister collection
        SelectionService?.UnregisterSelectionScope(AssetCollection.SelectedContent);
        AssetCollection.SelectedAssets.CollectionChanged -= SelectedAssetsCollectionChanged;
        AssemblyRegistry.AssemblyRegistered -= AssetAssembliesChanged;
        AssemblyRegistry.AssemblyUnregistered -= AssetAssembliesChanged;

        base.Destroy();
    }

    /// <summary>
    /// Notifies the session that the plugins have initialized their views for it.
    /// </summary>
    public void PluginsInitialized()
    {
        IsEditorInitialized = true;
    }

    /// <summary>
    /// Gets an <see cref="AssetViewModel"/> instance of the asset which has the given URL, if available.
    /// </summary>
    /// <param name="url">The URL of the asset to look for.</param>
    /// <returns>An <see cref="AssetViewModel"/> that matches the given URL if available. Otherwise, <c>null</c>.</returns>
    public AssetViewModel? GetAssetByUrl(string url)
    {
        var assetItem = session.FindAsset(new UFile(url));
        return assetItem != null ? GetAssetById(assetItem.Id) : null;
    }

    /// <summary>
    /// True when assets under this namespace resolve by bare URL for the open game: its own
    /// packages, explicit usings and global-using dependencies.
    /// </summary>
    public bool IsAssetNamespaceInScope(string assetNamespace)
    {
        if (session.AssetNamespaceUsings.Contains(assetNamespace))
            return true;
        foreach (var package in AllPackages)
        {
            if (package.Package.Container?.AssetNamespace is { } candidate
                && string.Equals(candidate, assetNamespace, StringComparison.OrdinalIgnoreCase)
                && !package.Package.IsReadOnly)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Creates a <see cref="IDisposable"/> object that represents a context where most of the standard mechanisms relying on property changes are disabled, such as
    /// property changes notifications, creation of operations, and propagation of properties between a base and a derived asset.
    /// </summary>
    /// <returns>A disposable object disabling normal mechanisms relying on property change.</returns>
    /// <remarks>This method should be used only for advanced modifications of assets such as global fixups, that still need to rely on undo/redo.</remarks>
    public IDisposable CreateAssetFixupContext()
    {
        return new FixupAssetContext(undoRedoService, x => IsInFixupAssetContext = x);
    }

    public IEnumerable<TemplateDescription> FindTemplates(TemplateScope asset)
    {
        return TemplateManager.FindTemplates(asset, session);
    }

    /// <summary>
    /// Gets the packages a dependency can be picked from: the loaded packages and the suggested ones not loaded yet.
    /// </summary>
    public Task<IEnumerable<PickablePackageViewModel>> SuggestPackagesToAddAsync()
    {
        var packages = new List<PickablePackageViewModel>();
        packages.AddRange(LocalPackages.Select(x => new LoadedPickablePackageViewModel(x)));
        packages.AddRange(StorePackages.Select(x => new LoadedPickablePackageViewModel(x)));

        // Note: later, we could even go further and add a NuGet search UI rather than a hardcoded list
        foreach (var packageInfo in SuggestedPackages)
        {
            // Make sure this package is not already in the list
            if (packages.Any(x => x.Name == packageInfo.Id))
                continue;

            packages.Add(new UnloadedPickablePackageViewModel(this, packageInfo));
        }

        return Task.FromResult<IEnumerable<PickablePackageViewModel>>(packages);
    }

    #region Save/close
    public async Task<bool> SaveSessionAsync()
    {
        Dispatcher.EnsureAccess();

        var success = false;

        CheckConsistency();

        // Ensure any edition is validated by triggering lost focus, etc.
        ServiceProvider.Get<IEditorDialogService>().ClearKeyboardFocus();

        var sessionResult = new LoggerResult();
        var workProgress = new WorkProgressViewModel(ServiceProvider, sessionResult)
        {
            Title = Tr._p("Title", "Saving session..."),
            KeepOpen = KeepOpen.OnWarningsOrErrors,
            IsIndeterminate = true,
            IsCancellable = false,
        };
        workProgress.RegisterProgressStatus(sessionResult, true);

        // The editors that keep changes outside their asset (the text of a script) save them first: they may ask the user,
        // which must happen before the progress window shows; their errors go to the log of the save
        await EditorCollection.SaveEditorChangesAsync(sessionResult);

        // Prepare packages to be saved by setting their dirty flag correctly
        foreach (var package in LocalPackages)
        {
            package.PreparePackageForSaving();
        }

        ServiceProvider.Get<IEditorDialogService>().ShowProgressWindow(workProgress);

        // Ensure saving is finished
        await Task.Run(() =>
        {
            try
            {
                var saveParameters = PackageSaveParameters.Default();

                AllAssets.ForEach(x => x.PrepareSave(sessionResult));
                session.Save(sessionResult, saveParameters);

                success = true;
            }
            catch (Exception e)
            {
                sessionResult.Error(string.Format(Tr._p("Log", "There was a problem saving the solution. {0}"), e.Message), e);
            }
        });

        // Fail on errors in the session result
        if (sessionResult.HasErrors)
            success = false;

        // Notify that the task is finished
        await workProgress.NotifyWorkFinished(false, sessionResult.HasErrors);

        // Update view model (in case "Source" is updated due to "Keep source side by side" feature)
        await ActiveProperties.RefreshSelectedPropertiesAsync();

        // Notify assets view model that their underlying assets has been saved
        foreach (var asset in AllPackages.Where(project => !project.Package.IsReadOnly).SelectMany(package => package.Assets))
        {
            // Note: we use AssetItem.IsDirty rather than AssetViewModel.IsDirty since OnSessionSaved() might be the place where we update AssetViewModel.IsDirty
            if (!asset.AssetItem.IsDirty)
                asset.OnSessionSaved();
        }

        if (success)
        {
            ActionHistory.NotifySave();
            AllPackages.ForEach(x => x.OnSessionSaved());
        }

        UpdateSessionState();
        UpdateHasUnsavedChanges();

        return success;
    }

    /// <summary>
    /// Saves the session without any window (no progress window, no dialog), for when the UI can no longer render.
    /// The view models are not updated: the caller is about to exit.
    /// </summary>
    /// <returns>The log of the save; an error means the session was not (fully) saved.</returns>
    public LoggerResult SaveSessionWithoutUI()
    {
        Dispatcher.EnsureAccess();

        var sessionResult = new LoggerResult();
        try
        {
            CheckConsistency();
            foreach (var package in LocalPackages)
            {
                package.PreparePackageForSaving();
            }

            AllAssets.ForEach(x => x.PrepareSave(sessionResult));
            session.Save(sessionResult, PackageSaveParameters.Default());
        }
        catch (Exception e)
        {
            sessionResult.Error(string.Format(Tr._p("Log", "There was a problem saving the solution. {0}"), e.Message), e);
        }
        return sessionResult;
    }

    /// <summary>
    /// Attempts to close the session. If the session has unsaved changes, ask the user whether to save it or not.
    /// </summary>
    /// <param name="allowCancel">
    /// Whether the user can cancel the close. A forced close (a lost graphics device) offers only to save or not.
    /// </param>
    /// <returns><c>true</c> if the session can be closed, <c>false</c> if the user cancelled.</returns>
    public async Task<bool> CloseAsync(bool allowCancel = true)
    {
        // Check if either view model is dirty, or any LocalPackage.Assets is (since some view models such as scripts don't make package dirty)
        if (HasUnsavedAssets())
        {
            var captions = new List<string> { Tr._p("Button", "Save"), Tr._p("Button", "Don't save") };
            if (allowCancel)
                captions.Add(Tr._p("Button", "Cancel"));
            var buttons = DialogButtons.Create(captions, 1, allowCancel ? 3 : null);

            int result;
            do
            {
                result = await Dialogs.MessageBoxAsync(Tr._p("Message", "The project has unsaved changes. Do you want to save it?"), buttons, MessageBoxImage.Question);
            }
            // A forced close needs an answer: closing the box asks again
            while (!allowCancel && result == 0);

            switch (result)
            {
                case 0:
                case 3:
                    // Cancel
                    return false;

                case 1:
                    await SaveSessionAsync();
                    // session saving has been cancelled, aborting
                    if (HasUnsavedAssets())
                    {
                        if (!allowCancel)
                        {
                            await Dialogs.MessageBoxAsync(Tr._p("Message", "Some assets couldn't be saved. Their changes are lost."), MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        }
                        await Dialogs.MessageBoxAsync(Tr._p("Message", "Some assets couldn't be saved. Check the assets and try again."), MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    break;
            }
        }

        return true;
    }

    public bool HasUnsavedAssets()
    {
        return IsDirty || LocalPackages.Any(package => package.IsDirty || package.Assets.Any(asset => asset.IsDirty));
    }

    /// <summary>
    /// Updates <see cref="HasUnsavedChanges"/> once dirty flags changed: the action history calls it after each change, undo and
    /// redo, the save after it ran, and an editor when its unsaved edits outside the history change (the text of a script).
    /// </summary>
    public void UpdateHasUnsavedChanges()
    {
        Dispatcher.Invoke(() => HasUnsavedChanges = HasUnsavedAssets());
    }
    #endregion // Save/close

    #region Projects and packages
    public async Task<PackageViewModel?> AddExistingProjectAsync(UFile projectPath)
    {
        var loggerResult = new LoggerResult();

        var workProgress = new WorkProgressViewModel(ServiceProvider, loggerResult)
        {
            Title = Tr._p("Title", "Importing project..."),
            KeepOpen = KeepOpen.OnWarningsOrErrors,
            IsIndeterminate = true,
            IsCancellable = false
        };
        workProgress.RegisterProgressStatus(loggerResult, true);
        // Note: this task is note safely cancellable
        // TODO: Remove the cancellation token if possible.
        var cancellationSource = new CancellationTokenSource();

        ServiceProvider.Get<IEditorDialogService>().ShowProgressWindow(workProgress);

        Package? package = null;
        await Task.Run(() =>
        {
            try
            {
                package = session.AddExistingProject(projectPath, loggerResult, CreatePackageLoadParameters(ServiceProvider, cancellationSource.Token)).Package;
            }
            catch (Exception e)
            {
                loggerResult.Error(Tr._p("Log", "There was a problem importing the package."), e);
            }

        }, cancellationSource.Token);

        // Set the range of the work in progress according to the created assets
        workProgress.Minimum = 0;
        workProgress.ProgressValue = 0;
        workProgress.Maximum = session.Projects.OfType<SolutionProject>().Where(x => !packageMap.ContainsValue(x)).Sum(x => x.Package.Assets.Count);

        using (var transaction = undoRedoService.CreateTransaction())
        {
            ProcessAddedProjects(loggerResult, workProgress, true);
            undoRedoService.SetName(transaction, $"Import project '{projectPath.GetFileNameWithoutExtension()}'");
        }

        // Notify that the task is finished
        await workProgress.NotifyWorkFinished(cancellationSource.IsCancellationRequested, loggerResult.HasErrors);

        return AllPackages.FirstOrDefault(x => x.Package == package);
    }

    /// <summary>
    /// Asks for a template and adds the project it creates to the session, as the WPF "New project..." does.
    /// </summary>
    public async Task NewProjectAsync()
    {
        var loggerResult = new LoggerResult();

        // Display the template dialog to let the user select which template he want to use.
        var parameters = await ServiceProvider.Get<IEditorDialogService>().ShowNewProjectDialogAsync(this);
        if (parameters?.TemplateDescription is null)
            return;

        var workProgress = new WorkProgressViewModel(ServiceProvider, loggerResult)
        {
            Title = Tr._p("Title", "Creating project..."),
            KeepOpen = KeepOpen.OnWarningsOrErrors,
            IsIndeterminate = true,
            IsCancellable = false
        };
        workProgress.RegisterProgressStatus(loggerResult, true);

        var generatorParameters = new SessionTemplateGeneratorParameters
        {
            Name = parameters.OutputName,
            OutputDirectory = parameters.OutputDirectory,
            Description = parameters.TemplateDescription,
            Session = session,
            Logger = loggerResult,
        };

        var generator = TemplateManager.FindTemplateGenerator(generatorParameters);
        if (generator is null)
        {
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(Tr._p("Message", "Unable to retrieve template generator for the selected template. Aborting."), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        await TemplateGeneratorHelper.RunTemplateGeneratorSafe(generator, generatorParameters, workProgress);

        // Set the range of the work in progress according to the created assets
        workProgress.Minimum = 0;
        workProgress.ProgressValue = 0;
        workProgress.Maximum = session.Projects.OfType<SolutionProject>().Where(x => !packageMap.ContainsValue(x)).Sum(x => x.Package.Assets.Count);

        // This action is uncancellable - In case of errors, we still try to create view models to match what is currently in the PackageSession, but the template has responsibility to clean itself up in case of failure.
        ProcessAddedProjects(loggerResult, workProgress, true);
        // A template can also reference a new project from an existing one (Add plugin)
        foreach (var package in LocalPackages)
            package.AddMissingDependencyViewModels();

        if (CurrentProject is null)
            AutoSelectCurrentProject();

        await workProgress.NotifyWorkFinished(false, loggerResult.HasErrors);
    }

    public async Task<bool> DeleteItems(IReadOnlyCollection<object> locations, bool skipConfirmation = false)
    {
        // Skip empty
        if (locations.Count == 0)
            return true;

        var directoriesToDelete = locations.OfType<DirectoryBaseViewModel>().ToList();
        var assetsToDelete = locations.OfType<AssetViewModel>().ToList();
        var projectsToDelete = locations.OfType<ProjectViewModel>().ToList();
        var dependenciesToDelete = locations.OfType<PackageReferenceViewModel>().ToList();
        var packagesToDelete = locations.OfType<PackageViewModel>().ToList();

        // Ask confirmation (for assets and directories)
        if ((assetsToDelete.Count > 0 || directoriesToDelete.Count > 0) && !skipConfirmation && EditorSettings.AskBeforeDeletingAssets.GetValue())
        {
            var messageParts = new List<string>();
            if (assetsToDelete.Count == 1)
                messageParts.Add("this asset");
            else if (assetsToDelete.Count > 1)
                messageParts.Add($"these {assetsToDelete.Count} assets");
            if (directoriesToDelete.Count == 1)
                messageParts.Add("this folder");
            else if (directoriesToDelete.Count > 1)
                messageParts.Add($"these {directoriesToDelete.Count} folders");

            var message = $"Are you sure you want to delete {string.Join(" and ", messageParts)}?";
            var checkedMessage = Tr._p("Settings", "Always delete without asking");
            var buttons = DialogButtons.Create([Tr._p("Button", "Delete"), Tr._p("Button", "Cancel")], 1, 2);
            var result = await Dialogs.CheckedMessageBoxAsync(message, false, checkedMessage, buttons, MessageBoxImage.Question);
            if (result.Result != 1)
                return false;

            if (result.IsChecked == true)
            {
                EditorSettings.AskBeforeDeletingAssets.SetValue(false);
                EditorSettings.Save();
            }
        }

        // Collect actual assets to delete (including those from the sub directories)
        var allAssetsToDelete = new HashSet<AssetViewModel>(assetsToDelete);
        allAssetsToDelete.UnionWith(directoriesToDelete.BreadthFirst(x => x.SubDirectories).SelectMany(x => x.Assets));

        // Check deletion
        foreach (var asset in allAssetsToDelete)
        {
            if (!asset.CanDelete(out var error))
            {
                error = string.Format(Tr._p("Message", "Stride can't delete the {0} asset. {1}{2}"), asset.Url, Environment.NewLine, error);
                await Dialogs.MessageBoxAsync(error, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        foreach (var directory in directoriesToDelete)
        {
            if (!directory.CanDelete(out var error))
            {
                error = string.Format(Tr._p("Message", "Stride can't delete the {0} folder. {1}{2}"), directory.Name, Environment.NewLine, error);
                await Dialogs.MessageBoxAsync(error, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        using (var transaction = undoRedoService.CreateTransaction())
        {
            var systemPackageWarningDisplayed = false;
            var confirmationPackageDisplayed = false;
            var confirmationProjectDisplayed = false;
            var confirmationDependencyDisplayed = false;
            var packagesDeleted = 0;
            var assetsDeleted = 0;
            var directoriesDeleted = 0;
            var projectsDeleted = 0;
            var dependencyDeleted = 0;
            var message = string.Empty;

            // Fix asset references
            if (allAssetsToDelete.Count > 0)
            {
                var hashIds = new HashSet<AssetId>();
                hashIds.AddRange(allAssetsToDelete.Select(x => x.Id));
                var hasReferencesToFix =
                    allAssetsToDelete
                        .SelectMany(x => DependencyManager.ComputeDependencies(x.AssetItem.Id, AssetDependencySearchOptions.In, ContentLinkType.Reference)?.LinksIn ?? [])
                        .Where(x => !hashIds.Contains(x.Item.Id))
                        .Any(x => GetAssetById(x.Item.Id) != null);
                if (hasReferencesToFix)
                {
                    // FIXME xplat-editor: port the fix-references dialog (FixReferencesViewModel); until then the deletion is refused rather than leaving broken references
                    await Dialogs.MessageBoxAsync(Tr._p("Message", "Other assets reference the assets to delete. Fixing those references is not available yet: remove the references first."), MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
            }

            // Delete selected packages
            foreach (var selectedPackage in packagesToDelete)
            {
                // Note: this should never happen. UI rules should ensure that the user cannot attempt a system package deletion.
                if (selectedPackage.Package.IsReadOnly && !systemPackageWarningDisplayed)
                {
                    await Dialogs.MessageBoxAsync(Tr._p("Message", "Stride can't delete the system package."), MessageBoxButton.OK, MessageBoxImage.Information);
                    systemPackageWarningDisplayed = true;
                    continue;
                }

                if (!confirmationPackageDisplayed)
                {
                    var buttons = DialogButtons.Create([Tr._p("Button", "Delete"), Tr._p("Button", "Cancel")], 1, 2);
                    var result = await Dialogs.MessageBoxAsync(Tr._p("Message", "Are you sure you want to delete this package? The package files will remain on the disk."), buttons, MessageBoxImage.Question);
                    if (result != 1)
                        break;
                }
                confirmationPackageDisplayed = true;
                message = $"Delete package '{selectedPackage.Name}'";
                selectedPackage.Delete();
                ++packagesDeleted;
            }

            // Delete package dependencies for deleted package in first pass
            var referencesToDeletedPackages = AllPackages.Except(packagesToDelete)
                .SelectMany(p => p.Dependencies.Content)
                .Where(r => packagesToDelete.Contains(r.Target)).ToList();
            foreach (var dependency in referencesToDeletedPackages)
            {
                message = $"Delete dependency '{dependency.Name}'";
                dependency.Delete();
                ++dependencyDeleted;
            }

            // Delete selected package dependencies
            foreach (var selectedDependency in dependenciesToDelete.Except(referencesToDeletedPackages))
            {
                if (!confirmationDependencyDisplayed)
                {
                    var buttons = DialogButtons.Create([Tr._p("Button", "Delete"), Tr._p("Button", "Cancel")], 1, 2);
                    var result = await Dialogs.MessageBoxAsync(Tr._p("Message", "Are you sure you want to delete this dependency?"), buttons, MessageBoxImage.Question);
                    if (result != 1)
                        break;
                }
                confirmationDependencyDisplayed = true;
                message = $"Delete dependency '{selectedDependency.Name}'";
                selectedDependency.Delete();
                ++dependencyDeleted;
            }

            // Delete assets
            if (assetsToDelete.Count > 0)
            {
                assetsDeleted = AssetCollection.DeleteAssets(assetsToDelete);
            }

            // Delete directories
            foreach (var directory in directoriesToDelete)
            {
                // We check if one of the recursive parent of the directory is in the list of things to delete.
                // If so, we skip it, this it is/will be deleted via its parent.
                var parent = directory.Parent;
                while (parent != null)
                {
                    if (locations.Contains(parent))
                        break;
                    parent = parent.Parent;
                }

                if (parent != null)
                    continue;

                message = $"Delete directory '{directory.Path}'";
                directory.Delete();
                ++directoriesDeleted;
            }

            // Delete asset mount points
            if (locations.OfType<AssetMountPointViewModel>().Any())
            {
                // We can't delete a root directory
                await Dialogs.MessageBoxAsync(Tr._p("Message", "Asset root folders can't be deleted."), MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Delete projects
            foreach (var project in projectsToDelete)
            {
                if (!confirmationProjectDisplayed)
                {
                    var buttons = DialogButtons.Create([Tr._p("Button", "Delete"), Tr._p("Button", "Cancel")], 1, 2);
                    var result = await Dialogs.MessageBoxAsync(Tr._p("Message", "Are you sure you want to delete these projects?"), buttons, MessageBoxImage.Question);
                    if (result != 1)
                        break;
                }
                confirmationProjectDisplayed = true;
                message = $"Delete project '{project.Name}'";
                project.Delete();
                ++projectsDeleted;
            }

            // Set transaction name
            var totalDeleted = packagesDeleted + dependencyDeleted + assetsDeleted + directoriesDeleted + projectsDeleted;
            if (totalDeleted > 1)
            {
                var parts = new List<string>();
                if (packagesDeleted > 0)
                    parts.Add($"{packagesDeleted} package{(packagesDeleted > 1 ? "s" : "")}");
                if (dependencyDeleted > 0)
                    parts.Add($"{dependencyDeleted} dependencie{(dependencyDeleted > 1 ? "s" : "")}");
                if (directoriesDeleted > 0)
                    parts.Add($"{directoriesDeleted} directorie{(directoriesDeleted > 1 ? "s" : "")}");
                if (assetsDeleted > 0)
                    parts.Add($"{assetsDeleted} asset{(assetsDeleted > 1 ? "s" : "")}");
                if (projectsDeleted > 0)
                    parts.Add($"{projectsDeleted} project{(projectsDeleted > 1 ? "s" : "")}");
                message = $"Delete {string.Join("/", parts)}";
            }
            undoRedoService.SetName(transaction, message);
        }
        CheckConsistency();

        return true;
    }

    /// <summary>
    /// Gets the package that contains the given item of the solution explorer (package, asset, folder, dependency...).
    /// </summary>
    public static PackageViewModel? GetContainerPackage(object? item)
    {
        return item switch
        {
            PackageViewModel package => package,
            AssetViewModel asset => asset.Directory?.Package,
            DirectoryBaseViewModel directory => directory.Package,
            DependencyCategoryViewModel depsCategory => depsCategory.Parent,
            PackageReferenceViewModel reference => reference.Referencer,
            _ => null,
        };
    }

    private async Task AddExistingProjectAsync()
    {
        var file = await Dialogs.OpenFilePickerAsync(session.SolutionPath?.GetFullDirectory(), [new FilePickerFilter("Visual Studio C# project") { Patterns = ["*.csproj"] }]);
        if (file is not null)
        {
            await AddExistingProjectAsync(file);
        }
    }

    private void AutoSelectCurrentProject()
    {
        // The executable of the editor's own platform, which Start project can run, then the Windows one as in the WPF editor
        var executableProjects = LocalPackages
            .OfType<ProjectViewModel>()
            .Where(x => x.Type == ProjectType.Executable)
            .ToList();

        PackageViewModel? selectedProject = SelectExecutable(executableProjects, Platform.Type)
            ?? SelectExecutable(executableProjects, PlatformType.Windows);
        selectedProject ??= LocalPackages.FirstOrDefault();

        if (selectedProject != null)
        {
            SetCurrentProject(selectedProject);
        }

        static ProjectViewModel? SelectExecutable(List<ProjectViewModel> executableProjects, PlatformType platform)
        {
            var candidates = executableProjects.Where(x => x.Platform == platform).ToList();
            // Prefer solutions with an existing *.sdpkg
            return candidates.FirstOrDefault(x => x.PackageContainer is SolutionProject { IsImplicitProject: false })
                ?? candidates.FirstOrDefault();
        }
    }

    private async Task CreateNewFolderInDirectoriesAsync(IEnumerable<object> selectedItems)
    {
        var createdDirectories = new List<DirectoryViewModel>();

        DirectoryViewModel? createdDirectory = null;
        var invalidSelectedItem = false;
        var locations = selectedItems.ToList();
        foreach (var selectedItem in locations)
        {
            using var transaction = undoRedoService.CreateTransaction();
            var selectedPackage = GetContainerPackage(selectedItem);
            if (selectedItem is DirectoryBaseViewModel selectedDirectory)
            {
                if (selectedDirectory.Package.IsEditable)
                {
                    createdDirectory = selectedDirectory.CreateSubDirectory(true);
                }
            }
            else if (selectedPackage != null)
            {
                if (selectedPackage.IsEditable)
                {
                    createdDirectory = selectedPackage.AssetMountPoint.CreateSubDirectory(true);
                }
            }
            else
            {
                invalidSelectedItem = true;
            }

            if (createdDirectory != null)
            {
                createdDirectories.Add(createdDirectory);
                var parentPath = !string.IsNullOrEmpty(createdDirectory.Parent.Path) ? createdDirectory.Parent.Path : createdDirectory.Parent.Name;
                undoRedoService.SetName(transaction, $@"Create folder ""{createdDirectory.Name}"" in ""{parentPath}""");
            }
        }

        // If an error occurred while creating folders, inform the user.
        if (invalidSelectedItem)
        {
            await Dialogs.MessageBoxAsync(locations.Count == 1
                ? Tr._p("Message", "Folders can only be created in the Assets hierarchy of a package.")
                : Tr._p("Message", "Game Studio can't create a folder in some of the locations you selected. Folders can only be created in the Assets hierarchy of a package."));
        }
        // If no directory are selected, create in the first package of the session.
        else if (createdDirectory == null)
        {
            var firstPackage = LocalPackages.FirstOrDefault();
            if (firstPackage != null)
            {
                using var transaction = undoRedoService.CreateTransaction();
                createdDirectory = firstPackage.AssetMountPoint.CreateSubDirectory(true);
                createdDirectories.Add(createdDirectory);
                undoRedoService.SetName(transaction, $"Create directory '{createdDirectory.Name}' in '{createdDirectory.Parent.Path}'");
            }
        }

        // Select created directories
        if (createdDirectory != null)
        {
            AssetCollection.SelectedLocations.Clear();
            AssetCollection.SelectedLocations.AddRange(createdDirectories);
        }

        CheckConsistency();
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

    private void LoadAssetsFromPackages(LoggerResult loggerResult, WorkProgressViewModel workProgress, CancellationToken? cancellationToken = null)
    {
        ArgumentNullException.ThrowIfNull(workProgress);
        workProgress.Minimum = 0;
        workProgress.ProgressValue = 0;
        workProgress.Maximum = session.Packages.Sum(x => x.Assets.Count);

        // Create directory and asset view models for each project
        foreach (var package in AllPackages)
        {
            if (cancellationToken is { IsCancellationRequested: true })
                return;

            package.LoadPackageInformation(loggerResult, workProgress, cancellationToken);
        }

        // Create actions corresponding to potential upgrades/fixes that occurred during the loading process.
        var upgradedAssets = new List<IDirtiable>();
        upgradedAssets.AddRange(AllPackages.Where(x => x.HasBeenUpgraded));
        upgradedAssets.AddRange(AllPackages.SelectMany(x => x.Assets).Where(x => x.HasBeenUpgraded));
        if (upgradedAssets.Count > 0)
        {
            // This transaction is done on a zero-sized stack just to flag assets as dirty. It is made to be uncancellable.
            using var transaction = undoRedoService.CreateTransaction();
            upgradedAssets.ForEach(x => undoRedoService.PushOperation(new AssetsUpgradeOperation(x)));
            undoRedoService.SetName(transaction, $"Upgrade assets ({upgradedAssets.Count})");
        }

        SourceTracker = new AssetSourceTrackerViewModel(this);
        UpdateSessionState();

        // This transaction is done to prevent action responding to undoRedoService.TransactionCompletion to occur during loading
        using (undoRedoService.CreateTransaction())
        {
            ProcessAddedPackages(AllPackages).Forget();
        }
    }

    private void LoadDocumentation()
    {
        // Load documentation into cache
        var documentationService = ServiceProvider.Get<UserDocumentationService>();
        foreach (var packageAssembly in LocalPackages.SelectMany(p => p.LoadedAssemblies))
        {
            if (packageAssembly.Assembly != null)
                Task.Run(() => documentationService.CacheAssemblyDocumentation(packageAssembly.Assembly));
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

    private static void RenameDirectoryOrPackage(object? selectedDirectoryOrPackage)
    {
        (selectedDirectoryOrPackage as PackageViewModel)?.RenameCommand.Execute();
        (selectedDirectoryOrPackage as DirectoryViewModel)?.RenameCommand.Execute();
    }

    private void SetCurrentProject(object? selectedItem)
    {
        // The context menu of the solution explorer passes the selected locations
        if (selectedItem is IEnumerable items and not string)
            selectedItem = items.Cast<object>().LastOrDefault();
        if (selectedItem is not ProjectViewModel project)
        {
            // Editor.MessageBox(Resources.Strings.SessionViewModel.SelectExecutableAsCurrentProject, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        CurrentProject = project;
        AllAssets.ForEach(x => x.Dependencies.NotifyRootAssetChange(false));
        RefreshRootAssetSelection();
    }

    private void UpdateCurrentProject(ProjectViewModel? oldValue, ProjectViewModel? newValue)
    {
        if (oldValue != null)
        {
            oldValue.IsCurrentProject = false;
        }
        if (newValue != null)
        {
            newValue.IsCurrentProject = true;
        }
        if (AssetCollection != null)
            RefreshRootAssetSelection();
        UpdateSessionState();
    }
    #endregion // Projects and packages

    #region Asset replacements
    private void CheckAssetReplacement(AssetViewModel asset)
    {
        var logKey = LogKey.Get(asset.Id, AssetLogViewModel.AssetReplacementLogName);
        AssetLog.ClearMessages(logKey);
        if (asset.Asset.Replaces is not { } target)
            return;

        var targetItem = asset.AssetItem.Package?.FindAsset(target);
        var error = AssetReplacementAnalysis.ValidateDeclaration(asset.AssetItem, targetItem);
        if (error == null)
        {
            var duplicate = AllAssets.FirstOrDefault(x => x != asset && x.Asset.Replaces is { } other && string.Equals(other.Location, target.Location, StringComparison.OrdinalIgnoreCase));
            if (duplicate != null)
                error = $"[{duplicate.Url}] also replaces [{target.Location}]; only one replacement per URL is allowed.";
        }

        if (error != null)
            AssetLog.GetLogger(logKey)!.Log(new AssetSerializableLogMessage(asset.Id, asset.Url, LogMessageType.Warning, error));
    }
    #endregion // Asset replacements

    #region Events
    private void AssetAssembliesChanged(object? sender, AssemblyRegisteredEventArgs e)
    {
        if (!e.Categories.Contains(AssemblyCommonCategories.Assets))
            return;

        // Raise AssetTypesChanged once for a burst of asset assembly changes, such as an assembly reload
        if (Interlocked.Exchange(ref assetTypesChangedPending, 1) != 0)
            return;

        Dispatcher.InvokeAsync(() =>
        {
            Interlocked.Exchange(ref assetTypesChangedPending, 0);
            RaiseAssetTypesChanged();
        }).Forget();
    }

    private void LocalPackagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            session.Projects.RemoveWhere(x => !x.Package.IsReadOnly);
        }
        if (e.NewItems != null)
        {
            // When a PackageViewModel is built, we will add it before the Package instance is added to the package map.
            // So we can't assume that the view model will always exists in the packageMap.
            packageMap.Where(x => e.NewItems.Cast<PackageViewModel>().Contains(x.Key)).ForEach(x => session.Projects.Add(x.Value));
        }
        e.OldItems?.Cast<PackageViewModel>().Select(x => packageMap[x]).ForEach(x => session.Projects.Remove(x));
        // The root-asset menu lists the local packages
        if (AssetCollection != null)
            RefreshRootAssetSelection();

        // A package brings its own asset templates
        RaiseAssetTypesChanged();
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

    private void RaiseAssetTypesChanged()
    {
        AssetTypesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SelectedAssetsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshRootAssetSelection();
    }
    #endregion // Events

    #region Session state
    internal void UpdateSessionState()
    {
        sessionStateUpdating = true;
        Dispatcher.InvokeAsync(DoUpdateSessionState);
    }

    private void DoUpdateSessionState()
    {
        if (!sessionStateUpdating)
            return;

        var packageSelected = false;
        var projectSelected = false;
        var directorySelected = false;
        var canDelete = AssetCollection.SelectedLocations.Count > 0;
        var canRename = AssetCollection.SelectedLocations.Count > 0;
        foreach (var location in AssetCollection.SelectedLocations.OfType<SessionObjectViewModel>())
        {
            if (location is PackageViewModel { IsEditable: true } package)
            {
                packageSelected = true;
                projectSelected = package is ProjectViewModel;
            }
            if (location is DirectoryBaseViewModel)
            {
                directorySelected = true;
            }
            if (location is PackageReferenceViewModel)
            {
                canRename = false;
            }
            if (!location.IsEditable)
            {
                canRename = false;
                canDelete = false;
            }
        }

        var asset = AssetCollection.SingleSelectedAsset;

        NewProjectCommand.IsEnabled = !string.IsNullOrWhiteSpace(SolutionPath);
        SetCurrentProjectCommand.IsEnabled = projectSelected;
        DeleteSelectedSolutionItemsCommand.IsEnabled = canDelete;
        RenameDirectoryOrPackageCommand.IsEnabled = canRename;
        NewDirectoryCommand.IsEnabled = packageSelected || directorySelected;
        ActivatePackagePropertiesCommand.IsEnabled = packageSelected || directorySelected;
        EditSelectedContentCommand.IsEnabled = AssetCollection.SingleSelectedContent is DirectoryViewModel || asset is { IsEditable: true } && PluginService.HasEditorView(this, asset.GetType());
        ToggleIsRootOnSelectedAssetCommand.IsEnabled = RootAssetDefaultTarget != null && AssetCollection.SelectedAssets.Count > 0 && AssetCollection.SelectedAssets.All(x => !x.Dependencies.ForcedRoot);
        UpdateSelectionCommands();

        SessionStateChanged?.Invoke(this, new SessionStateChangedEventArgs());
        sessionStateUpdating = false;
    }

    private void UpdateSelectionCommands()
    {
        PreviousSelectionCommand.IsEnabled = SelectionService?.CanGoBack ?? false;
        NextSelectionCommand.IsEnabled = SelectionService?.CanGoForward ?? false;
    }
    #endregion // Session state

    #region Commands
    private void EditSelectedContent()
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
