// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.ViewModels;

public sealed class SessionViewModel : DispatcherViewModel, ISessionViewModel
{
    private readonly ConcurrentDictionary<AssetId, AssetViewModel> assetIdMap = [];
    private readonly Dictionary<PackageViewModel, PackageContainer> packageMap = [];
    private readonly PackageSession session;

    private SessionViewModel(IViewModelServiceProvider serviceProvider, PackageSession session)
        : base(serviceProvider)
    {
        this.session = session;

        AssetCollection = new AssetCollectionViewModel(this);

        // Create package view models
        this.session.Projects.ForEach(x =>
        {
            var package = CreateProjectViewModel(x, true);
            AllPackages.Add(package);
        });
    }

    public ObservableList<PackageViewModel> AllPackages { get; } = [];

    public AssetCollectionViewModel AssetCollection { get; }

    internal Dictionary<Type, Type> AssetViewModelTypes { get; } = [];

    /// <inheritdoc />
    public AssetViewModel? GetAssetById(AssetId id)
    {
        assetIdMap.TryGetValue(id, out var result);
        return result;
    }

    public static async Task<SessionViewModel?> OpenSessionAsync(UFile path, IViewModelServiceProvider serviceProvider, CancellationToken token = default)
    {
        // TODO register a bunch of services
        //serviceProvider.RegisterService(new CopyPasteService());

        var sessionViewModel = await Task.Run(() =>
        {
            SessionViewModel? result = null;
            try
            {
                var sessionResult = PackageSession.Load(path, new PackageLoadParameters
                {
                    CancelToken = token,
                    AutoCompileProjects = false,
                    LoadAssemblyReferences = false,
                    LoadMissingDependencies = false,
                });
                if (!token.IsCancellationRequested)
                {
                    result = new SessionViewModel(serviceProvider, sessionResult.Session);
                    
                    // Build asset view models
                    result.LoadAssetsFromPackages(token); 
                }
            }
            catch (Exception)
            {
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
        Type? assetViewModelType;
        do
        {
            if (AssetViewModelTypes.TryGetValue(assetType, out assetViewModelType))
                break;

            assetViewModelType = typeof(AssetViewModel<>);
            assetType = assetType.BaseType;
        } while (assetType != null);

        return assetViewModelType;
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
}
