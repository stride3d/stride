// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;

namespace Stride.Core.Assets.Presentation.ViewModels
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
        /// Gets the content (assets and folders) that is currently displayed in the view, once filtered.
        /// </summary>
        IReadOnlyObservableCollection<object> FilteredContent { get; }

        /// <summary>
        /// Gets a command that selects the asset given as parameter.
        /// </summary>
        ICommandBase SelectAssetCommand { get; }

        /// <summary>
        /// Selects the given assets.
        /// </summary>
        void SelectAssets(IEnumerable<AssetViewModel> assetsToSelect);

        /// <summary>
        /// Replaces the assets of the view with the given ones (a view that lists given assets, such as the references of the selection).
        /// </summary>
        void UpdateAssetsCollection(ICollection<AssetViewModel> newAssets);

        /// <summary>
        /// Deletes the given assets and directories.
        /// </summary>
        /// <param name="locations">The assets and directories to delete.</param>
        /// <param name="skipConfirmation">If <c>true</c>, no confirmation is asked.</param>
        /// <param name="deletedOutside">If <c>true</c>, the files of these assets were deleted outside the editor.</param>
        /// <returns><c>true</c> if the content was deleted, <c>false</c> if the deletion was cancelled.</returns>
        Task<bool> DeleteContent(IReadOnlyCollection<object> locations, bool skipConfirmation = false, bool deletedOutside = false);

        /// <summary>
        /// Runs an asset template in the folder where the view creates assets, and selects the new assets.
        /// </summary>
        /// <param name="template">The asset template to run.</param>
        /// <param name="files">The source files to import, or <c>null</c> when the template asks for them itself.</param>
        /// <returns>The new assets; none when nothing was created.</returns>
        Task<List<AssetViewModel>> RunAssetTemplateAsync(TemplateDescription template, IList<UFile> files);
    }
}
