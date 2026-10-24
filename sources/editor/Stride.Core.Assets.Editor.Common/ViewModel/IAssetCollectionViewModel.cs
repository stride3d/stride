// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// The part of an asset view (<c>AssetCollectionViewModel</c>) that the session objects use.
    /// </summary>
    public interface IAssetCollectionViewModel
    {
        /// <summary>
        /// Gets the assets that are currently displayed in the view.
        /// </summary>
        IReadOnlyObservableCollection<AssetViewModel> Assets { get; }

        /// <summary>
        /// Gets the assets that are currently selected.
        /// </summary>
        IReadOnlyObservableCollection<AssetViewModel> SelectedAssets { get; }

        /// <summary>
        /// Gets a command that selects the asset given as parameter.
        /// </summary>
        ICommandBase SelectAssetCommand { get; }

        /// <summary>
        /// Selects the given assets.
        /// </summary>
        void SelectAssets(IEnumerable<AssetViewModel> assetsToSelect);

        /// <summary>
        /// Deletes the given assets and directories.
        /// </summary>
        /// <returns><c>true</c> if the content was deleted, <c>false</c> if the deletion was cancelled.</returns>
        Task<bool> DeleteContent(IReadOnlyCollection<object> locations, bool skipConfirmation = false);
    }
}
