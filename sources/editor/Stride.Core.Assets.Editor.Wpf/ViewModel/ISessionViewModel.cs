// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel.Logs;
using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// The part of the session that the session objects (assets, packages, directories...) and their helpers use,
    /// so that they do not depend on the editor's <see cref="SessionViewModel"/>.
    /// </summary>
    public interface ISessionViewModel
    {
        /// <summary>
        /// Gets the service provider of the session.
        /// </summary>
        IViewModelServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets the dispatcher of the session.
        /// </summary>
        IDispatcherService Dispatcher { get; }

        /// <summary>
        /// Gets the path of the solution of the session.
        /// </summary>
        UFile SolutionPath { get; }

        /// <summary>
        /// Gets the current project of the session.
        /// </summary>
        ProjectViewModel CurrentProject { get; }

        /// <summary>
        /// Gets the packages of the session, grouped by category.
        /// </summary>
        IReadOnlyDictionary<string, PackageCategoryViewModel> PackageCategories { get; }

        /// <summary>
        /// Gets the packages of the session that are not local.
        /// </summary>
        IObservableCollection<PackageViewModel> StorePackages { get; }

        /// <summary>
        /// Gets the local packages (projects) of the session.
        /// </summary>
        IObservableCollection<PackageViewModel> LocalPackages { get; }

        /// <summary>
        /// Gets all the packages of the session.
        /// </summary>
        IEnumerable<PackageViewModel> AllPackages { get; }

        /// <summary>
        /// Gets all the assets of the session.
        /// </summary>
        IEnumerable<AssetViewModel> AllAssets { get; }

        /// <summary>
        /// Gets the dependency manager of the session.
        /// </summary>
        IAssetDependencyManager DependencyManager { get; }

        /// <summary>
        /// Gets the container of the property graphs of the assets.
        /// </summary>
        AssetPropertyGraphContainer GraphContainer { get; }

        /// <summary>
        /// Gets the node container used for the assets of the session.
        /// </summary>
        SessionNodeContainer AssetNodeContainer { get; }

        /// <summary>
        /// Gets the asset view that is currently active.
        /// </summary>
        IAssetCollectionViewModel ActiveAssetView { get; }

        /// <summary>
        /// Gets the properties panel of the asset view.
        /// </summary>
        ISessionObjectPropertiesViewModel AssetViewProperties { get; }

        /// <summary>
        /// Gets the tracker of the source files of the assets.
        /// </summary>
        IAssetSourceTrackerViewModel SourceTracker { get; }

        /// <summary>
        /// Gets the asset log of the session.
        /// </summary>
        IAssetLogViewModel AssetLog { get; }

        /// <summary>
        /// Gets whether the session is currently in a special context to fix up assets.
        /// </summary>
        bool IsInFixupAssetContext { get; }

        /// <summary>
        /// Gets whether any asset of the session declares <see cref="Asset.Replaces"/>.
        /// </summary>
        bool HasAssetReplacements { get; }

        /// <summary>
        /// Raised when the properties of some assets have changed.
        /// </summary>
        event EventHandler<AssetChangedEventArgs> AssetPropertiesChanged;

        /// <summary>
        /// Raised when the collection of deleted assets of a package has changed.
        /// </summary>
        event EventHandler<NotifyCollectionChangedEventArgs> DeletedAssetsChanged;

        /// <summary>
        /// Raised when the state of the session (selection, current project...) has changed.
        /// </summary>
        event EventHandler<SessionStateChangedEventArgs> SessionStateChanged;

        /// <summary>
        /// Gets an <see cref="AssetViewModel"/> instance of the asset which as the given identifier, if available.
        /// </summary>
        /// <param name="id">The identifier of the asset to look for.</param>
        /// <returns>An <see cref="AssetViewModel"/> that matches the given identifier if available. Otherwise, <c>null</c>.</returns>
        AssetViewModel GetAssetById(AssetId id);

        /// <summary>
        /// Gets the type of view model to use for the given asset.
        /// </summary>
        Type GetAssetViewModelType(AssetItem assetItem);

        /// <summary>
        /// Validates every <see cref="Asset.Replaces"/> declaration of the session.
        /// </summary>
        /// <param name="changedAsset">An asset whose messages are cleared when it no longer declares a replacement.</param>
        void CheckAssetReplacements(AssetViewModel changedAsset = null);

        /// <summary>
        /// Gets the package in which the given assets are marked as root by default.
        /// </summary>
        PackageViewModel GetDefaultRootAssetTarget(IReadOnlyCollection<AssetViewModel> assets);

        /// <summary>
        /// Toggles the given assets as root assets of the given package.
        /// </summary>
        void ToggleRootAsset(IReadOnlyCollection<AssetViewModel> assets, PackageViewModel target);

        /// <summary>
        /// Refreshes the root asset state of the selection.
        /// </summary>
        void RefreshRootAssetSelection();

        /// <summary>
        /// Registers an asset so it can be found using the <see cref="GetAssetById"/> method. This method is intended to be invoked only by <see cref="AssetViewModel"/>.
        /// </summary>
        /// <param name="asset">The asset to register.</param>
        void RegisterAsset(AssetViewModel asset);

        /// <summary>
        /// Unregisters an asset previously registered with <see cref="RegisterAsset"/>. This method is intended to be invoked only by <see cref="AssetViewModel"/>.
        /// </summary>
        /// <param name="asset">The asset to unregister.</param>
        void UnregisterAsset(AssetViewModel asset);

        /// <summary>
        /// Creates the view models of the projects that were added to the session.
        /// </summary>
        void ProcessAddedProjects(LoggerResult loggerResult, WorkProgressViewModel workProgress, bool packageAlreadyInSession);

        /// <summary>
        /// Removes the view models of the projects that were removed from the session.
        /// </summary>
        void ProcessRemovedProjects();

        /// <summary>
        /// Loads what a dependency that was just added brings to the projects.
        /// </summary>
        Task LoadAddedDependencies(LoggerResult loggerResult);
    }
}
