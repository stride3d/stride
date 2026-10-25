// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModel.Progress;
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
}
