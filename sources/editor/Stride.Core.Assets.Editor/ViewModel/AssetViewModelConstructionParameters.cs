// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core.Presentation.ViewModel;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// This class is a container for the parameters needed to initialize the <see cref="AssetViewModel"/> class and its parent classes.
    /// </summary>
    public class AssetViewModelConstructionParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetViewModelConstructionParameters"/> class with the parameter to forward to the <see cref="AssetViewModel"/> constructor.
        /// </summary>
        /// <param name="serviceProvider">The service provider to use for asset view model.</param>
        /// <param name="directory">The directory containing the asset.</param>
        /// <param name="package">The project containing the asset, or in which to add the asset if it's a new asset.</param>
        /// <param name="assetItem">The <see cref="AssetItem"/> instance containing the asset with its current location.</param>
        /// <param name="container">The <see cref="NodeContainer"/> used to store the graph of properties of this asset.</param>
        /// <param name="canUndoRedoCreation">Indicates whether the creation of this view model will create a transaction in the undo/redo service.</param>
        internal AssetViewModelConstructionParameters(IViewModelServiceProvider serviceProvider, DirectoryBaseViewModel directory, Package package, AssetItem assetItem, SessionNodeContainer container, bool canUndoRedoCreation)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            if (directory.Package == null) throw new ArgumentException("The provided directory must be in a project when creating an asset.");
            if (package == null) throw new ArgumentNullException(nameof(package));
            if (assetItem == null) throw new ArgumentNullException(nameof(assetItem));
            if (container == null) throw new ArgumentNullException(nameof(container));

            Directory = directory;
            Package = package;
            AssetItem = assetItem;
            Container = container;
            ServiceProvider = serviceProvider;
            CanUndoRedoCreation = canUndoRedoCreation;
        }

        /// <summary>
        /// Gets the directory containing the asset to construct.
        /// </summary>
        internal DirectoryBaseViewModel Directory { get; }

        /// <summary>
        /// Gets the project containing the asset to construct.
        /// </summary>
        internal Package Package { get; }

        /// <summary>
        /// Gets the <see cref="AssetItem"/> instance representing the asset to construct.
        /// </summary>
        internal AssetItem AssetItem { get; }

        /// <summary>
        /// Gets the <see cref="NodeContainer"/> used to store the graph of properties of this asset.
        /// </summary>
        internal SessionNodeContainer Container { get; }

        /// <summary>
        /// Gets the action stack to provide to the base view model class.
        /// </summary>
        internal IViewModelServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets whether the creation of this asset can be undone/redone.
        /// </summary>
        internal bool CanUndoRedoCreation { get; }
    }
}
