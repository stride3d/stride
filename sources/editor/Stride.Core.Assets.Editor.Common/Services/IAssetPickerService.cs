// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// The asset picker of an editor's dialog service, which the shared commands and view models (the property grid's pick button,
    /// the fix references dialog) open without depending on the editor.
    /// </summary>
    public interface IAssetPickerService
    {
        /// <summary>
        /// Opens the asset picker as a modal dialog and returns the picked asset.
        /// </summary>
        /// <param name="session">The session whose assets are listed.</param>
        /// <param name="acceptedTypes">The types of asset that can be picked; empty to accept any asset.</param>
        /// <param name="filter">An additional filter on the listed assets, or <c>null</c>.</param>
        /// <param name="initialAsset">The asset selected when the picker opens, or <c>null</c>.</param>
        /// <param name="initialLocation">The folder shown when the picker opens if there is no <paramref name="initialAsset"/>, or <c>null</c>.</param>
        /// <param name="message">A message shown at the top of the picker, or <c>null</c>.</param>
        /// <returns>The picked asset, or <c>null</c> if the picker was cancelled.</returns>
        Task<AssetViewModel> OpenAssetPickerAsync(ISessionViewModel session, IEnumerable<Type> acceptedTypes, Func<AssetViewModel, bool> filter = null, AssetViewModel initialAsset = null, DirectoryBaseViewModel initialLocation = null, string message = null);
    }
}
