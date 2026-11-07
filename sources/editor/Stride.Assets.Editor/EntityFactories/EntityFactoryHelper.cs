// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.EntityFactories;

/// <summary>
/// Helpers for the entity factories that ask the user for an asset.
/// </summary>
/// <remarks>
/// The cross-platform counterpart of the WPF <c>EntityFactoryHelper</c>: the picker is the one of
/// <see cref="IEditorDialogService"/>.
/// </remarks>
internal static class EntityFactoryHelper
{
    /// <summary>
    /// Asks the user to pick an asset of one of the given types.
    /// </summary>
    /// <param name="session">The session to pick the asset from.</param>
    /// <param name="assetTypes">The accepted asset types.</param>
    /// <returns>The picked asset, or <c>null</c> when the user cancelled.</returns>
    public static Task<AssetViewModel?> PickupAsset(ISessionViewModel session, IEnumerable<Type> assetTypes)
    {
        var dialogService = session.ServiceProvider.Get<IEditorDialogService>();
        return dialogService.OpenAssetPickerAsync((SessionViewModel)session, assetTypes, message: "Select the asset to use for this entity");
    }
}
