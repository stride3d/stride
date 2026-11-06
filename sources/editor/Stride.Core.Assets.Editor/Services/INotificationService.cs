// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Presentation.Collections;

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// Shows the progress of background operations without blocking the editor: a bar under the session area lists them until
/// they finish, where a progress window would be modal.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Gets the operations in progress, in the order they started.
    /// </summary>
    IReadOnlyObservableList<WorkProgressViewModel> ActiveNotifications { get; }

    /// <summary>
    /// Shows an operation until it finishes (<see cref="WorkProgressViewModel.NotifyWorkFinished"/>). A finished operation is
    /// not shown.
    /// </summary>
    /// <param name="workProgress">The progress of the operation.</param>
    void AddNotification(WorkProgressViewModel workProgress);
}
