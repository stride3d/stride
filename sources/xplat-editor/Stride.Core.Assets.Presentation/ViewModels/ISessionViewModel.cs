// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Presentation.ViewModels;

public interface ISessionViewModel
{
    IEnumerable<AssetViewModel> AllAssets { get; }

    IEnumerable<PackageViewModel> AllPackages { get; }

    AssetNodeContainer AssetNodeContainer { get; }

    ProjectViewModel? CurrentProject { get; }

    IAssetDependencyManager DependencyManager { get; }

    IDispatcherService Dispatcher { get; }

    AssetPropertyGraphContainer GraphContainer { get; }

    IObservableCollection<PackageViewModel> LocalPackages { get; }

    IReadOnlyDictionary<string, PackageCategoryViewModel> PackageCategories { get; }

    IViewModelServiceProvider ServiceProvider { get; }

    IObservableCollection<PackageViewModel> StorePackages { get; }

    event EventHandler<AssetChangedEventArgs>? AssetPropertiesChanged;

    event EventHandler<SessionStateChangedEventArgs>? SessionStateChanged;

    /// <summary>
    /// Gets an <see cref="AssetViewModel"/> instance of the asset which as the given identifier, if available.
    /// </summary>
    /// <param name="id">The identifier of the asset to look for.</param>
    /// <returns>An <see cref="AssetViewModel"/> that matches the given identifier if available. Otherwise, <c>null</c>.</returns>
    AssetViewModel? GetAssetById(AssetId id);

    Type GetAssetViewModelType(AssetItem assetItem);

    /// <summary>
    /// Register an asset so it can be found using the <see cref="GetAssetById"/> method. This method is intended to be invoked only by <see cref="AssetViewModel"/>.
    /// </summary>
    /// <param name="asset">The asset to register.</param>
    void RegisterAsset(AssetViewModel asset);

    /// <summary>
    /// Unregister an asset previously registered with <see cref="RegisterAsset"/>. This method is intended to be invoked only by <see cref="AssetViewModel"/>.
    /// </summary>
    /// <param name="asset">The asset to register.</param>
    void UnregisterAsset(AssetViewModel asset);
}
