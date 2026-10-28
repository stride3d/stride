// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories
{
    /// <summary>
    /// Helpers for the entity factories that need the WPF editor's dialogs.
    /// </summary>
    public static class EntityFactoryHelper
    {
        /// <summary>
        /// Asks the user to pick an asset of one of the given types.
        /// </summary>
        /// <param name="session">The session to pick the asset from.</param>
        /// <param name="assetTypes">The accepted asset types.</param>
        /// <returns>The picked asset, or <c>null</c> when the user cancelled.</returns>
        public static async Task<AssetViewModel> PickupAsset([NotNull] ISessionViewModel session, [NotNull] IEnumerable<Type> assetTypes)
        {
            var dialogService = session.ServiceProvider.Get<IEditorDialogService>();
            var assetPicker = dialogService.CreateAssetPickerDialog((SessionViewModel)session);
            assetPicker.Message = "Select the asset to use for this entity";
            assetPicker.AcceptedTypes.AddRange(assetTypes);
            var result = await assetPicker.ShowModal();
            return result == Stride.Core.Presentation.Services.DialogResult.Ok ? assetPicker.SelectedAssets.FirstOrDefault() : null;
        }
    }
}
