// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Stride.Core.Assets.Editor.Internal;
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
    private readonly Dictionary<PackageViewModel, PackageContainer> packageMap = [];
    private readonly PackageSession session;

    private SessionViewModel(IViewModelServiceProvider serviceProvider, PackageSession session, ILogger logger)
        : base(serviceProvider)
    {
        this.session = session;

        // Gather all data from plugins
        var pluginService = ServiceProvider.Get<PluginService>();
        pluginService.RegisterSession(this, logger);

        // Initialize the node container used for asset properties
        AssetNodeContainer = new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } };

        // Initialize the asset collection view model
        AssetCollection = new AssetCollectionViewModel(this);

        // Initialize the asset collection view model
        EditorCollection = new EditorCollectionViewModel(this);

        ActiveProperties = AssetCollection.AssetViewProperties;

        // Initialize commands
        EditSelectedContentCommand = new AnonymousCommand(serviceProvider, OnEditSelectedContent);

        // Create package view models
        this.session.Projects.ForEach(x =>
        {
            var package = CreateProjectViewModel(x, true);
            AllPackages.Add(package);
        });

        GraphContainer = new AssetPropertyGraphContainer(AssetNodeContainer);
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
                //ActiveAssetsChanged?.Invoke(this, new ActiveAssetsChangedArgs(value?.GetRelatedAssets().ToList()));
            }
        }
    }

    public ObservableList<PackageViewModel> AllPackages { get; } = [];

    public AssetCollectionViewModel AssetCollection { get; }

    public AssetNodeContainer AssetNodeContainer { get; }

    public EditorCollectionViewModel EditorCollection { get; }

    public AssetPropertyGraphContainer GraphContainer { get; }

    public ICommandBase EditSelectedContentCommand { get; }

    internal Dictionary<Type, Type> AssetViewModelTypes { get; } = [];

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
    public Type GetAssetViewModelType(AssetItem assetItem)
    {
        var assetType = assetItem.Asset.GetType();
        return TypeHelpers.TryGetTypeOrBase(assetType, AssetViewModelTypes) ?? typeof(AssetViewModel<>);
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
