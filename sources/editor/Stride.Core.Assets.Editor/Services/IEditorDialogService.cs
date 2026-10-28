// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Services;

public interface IEditorDialogService : IDialogService
{
    void ShowProgressWindow(WorkProgressViewModel workProgress);

    /// <summary>
    /// Shows the progress of a background operation in the notification bar (see <see cref="INotificationService"/>), without
    /// blocking the editor as <see cref="ShowProgressWindow"/> does.
    /// </summary>
    /// <param name="workProgress">The progress of the operation.</param>
    void ShowNotification(WorkProgressViewModel workProgress);

    /// <summary>
    /// Moves the keyboard focus away from the control that has it, so that an edit in progress (a text box of the property
    /// grid) is validated, for instance before the session is saved. A host without keyboard focus does nothing.
    /// </summary>
    void ClearKeyboardFocus()
    {
    }

    /// <summary>
    /// Shows the asset picker (see <see cref="AssetPickerViewModel"/>) and waits for the user to pick an asset.
    /// </summary>
    /// <param name="session">The session whose assets are listed.</param>
    /// <param name="acceptedTypes">The asset types to list (and their derived types); empty lists every type.</param>
    /// <param name="filter">An additional condition on the listed assets, or <c>null</c>.</param>
    /// <param name="initialAsset">The asset selected when the picker opens, or <c>null</c>.</param>
    /// <param name="initialLocation">The folder selected when the picker opens and there is no initial asset, or <c>null</c>.</param>
    /// <returns>The picked asset, or <c>null</c> when the user cancelled.</returns>
    Task<AssetViewModel?> OpenAssetPickerAsync(SessionViewModel session, IEnumerable<Type> acceptedTypes, Func<AssetViewModel, bool>? filter = null, AssetViewModel? initialAsset = null, DirectoryBaseViewModel? initialLocation = null);
}
