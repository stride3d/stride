// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

public sealed class SessionViewModel : DispatcherViewModel, ISessionViewModel
{
    private SessionObjectPropertiesViewModel activeProperties;
    private readonly ConcurrentDictionary<AssetId, AssetViewModel> assetIdMap = [];
    private ProjectViewModel? currentProject;
    private readonly Dictionary<PackageViewModel, PackageContainer> packageMap = [];
    private readonly PackageSession session;

    private SessionViewModel(IViewModelServiceProvider serviceProvider, PackageSession session, ILogger logger)
        : base(serviceProvider)
    {
        this.session = session;

        // Make sure plugins are initialized
        PluginService.EnsureInitialized(logger);

        // Initialize the node container used for asset properties
        AssetNodeContainer = new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } };

        // Initialize the asset collection view model
        AssetCollection = new AssetCollectionViewModel(this);

        // Initialize the asset collection view model
        EditorCollection = new EditorCollectionViewModel(this);

        activeProperties = AssetCollection.AssetViewProperties;

        // Initialize commands
        EditSelectedContentCommand = new AnonymousCommand(serviceProvider, OnEditSelectedContent);

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
    // FIXME: do we need both ActiveProperties and AssetCollection.AssetViewProperties?
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

    public IEnumerable<AssetViewModel> AllAssets => AllPackages.SelectMany(x => x.Assets);

    public IEnumerable<PackageViewModel> AllPackages => packageMap.Keys;

    public AssetCollectionViewModel AssetCollection { get; }

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

    public ThumbnailsViewModel Thumbnails { get; }

    public ICommandBase EditSelectedContentCommand { get; }

    internal IAssetsPluginService PluginService => ServiceProvider.Get<IAssetsPluginService>();

    internal IUndoRedoService? UndoRedoService => ServiceProvider.TryGet<IUndoRedoService>();

    /// <summary>
    /// Raised when some assets are modified.
    /// </summary>
    public event EventHandler<AssetChangedEventArgs>? AssetPropertiesChanged;

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

    public static async Task<SessionViewModel?> OpenSessionAsync(UFile path, PackageSessionResult sessionResult, IViewModelServiceProvider serviceProvider, CancellationToken token = default)
    {
        // TODO register a bunch of services
        //serviceProvider.RegisterService(new CopyPasteService());
        // Create the undo/redo service for this session. We use an initial size of 0 to prevent asset upgrade to be cancellable.
        var undoRedoService = new UndoRedoService(0);
        serviceProvider.RegisterService(undoRedoService);

        var sessionViewModel = await Task.Run(() =>
        {
            SessionViewModel? result = null;
            try
            {
                PackageSession.Load(path, sessionResult, new PackageLoadParameters
                {
                    CancelToken = token,
                    AutoCompileProjects = false,
                    LoadAssemblyReferences = false,
                    LoadMissingDependencies = false,
                });
                if (!token.IsCancellationRequested)
                {
                    result = new SessionViewModel(serviceProvider, sessionResult.Session, sessionResult);
                    
                    // Build asset view models
                    result.LoadAssetsFromPackages(token); 
                }
            }
            catch (Exception ex)
            {
                sessionResult.Error("There was a problem opening the solution.", ex);
                result = null;
            }
            
            return result;

        }, token);

        return sessionViewModel;
    }
    
    /// <inheritdoc />
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(SessionViewModel));

        Thumbnails.Destroy();

        base.Destroy();
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

    private PackageViewModel CreateProjectViewModel(PackageContainer packageContainer, bool packageAlreadyInSession)
    {
        switch (packageContainer)
        {
            case SolutionProject project:
                {
                    var packageContainerViewModel = new ProjectViewModel(this, project);
                    packageMap.Add(packageContainerViewModel, project);
                    if (!packageAlreadyInSession)
                        session.Projects.Add(project);
                    return packageContainerViewModel;
                }
            case StandalonePackage standalonePackage:
                {
                    var packageContainerViewModel = new PackageViewModel(this, standalonePackage);
                    packageMap.Add(packageContainerViewModel, standalonePackage);
                    if (!packageAlreadyInSession)
                        session.Projects.Add(standalonePackage);
                    return packageContainerViewModel;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(packageContainer));
        }
    }

    private void LoadAssetsFromPackages(CancellationToken token = default)
    {
        // Create directory and asset view models for each project
        foreach (var package in AllPackages)
        {
            if (token.IsCancellationRequested)
                return;

            package.LoadPackageInformation(token);
        }

        // This transaction is done to prevent action responding to undoRedoService.TransactionCompletion to occur during loading
        using var transaction = UndoRedoService?.CreateTransaction();
        ProcessAddedPackages(AllPackages).Forget();
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
        //SelectionIsRoot = ActiveAssetView.SelectedAssets.All(x => x.Dependencies.IsRoot);
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
        //UpdateSessionState();
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
