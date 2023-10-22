// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Presentation.ViewModels;

public interface ISessionViewModel
{
    SessionObjectPropertiesViewModel? ActiveProperties { get; set; }

    AssetNodeContainer AssetNodeContainer { get; }

    AssetPropertyGraphContainer GraphContainer { get; }

    IViewModelServiceProvider ServiceProvider { get; }

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
