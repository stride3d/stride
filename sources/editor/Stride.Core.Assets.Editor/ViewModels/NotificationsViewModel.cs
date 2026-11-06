// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// The notifications of the background operations in progress: each one shows until it finishes.
/// </summary>
public sealed class NotificationsViewModel : DispatcherViewModel, INotificationService
{
    private readonly ObservableList<WorkProgressViewModel> activeNotifications = [];

    public NotificationsViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    /// <inheritdoc/>
    public IReadOnlyObservableList<WorkProgressViewModel> ActiveNotifications => activeNotifications;

    /// <inheritdoc/>
    public void AddNotification(WorkProgressViewModel workProgress)
    {
        ArgumentNullException.ThrowIfNull(workProgress);

        Dispatcher.Invoke(() =>
        {
            // The work may have finished before it reached the UI thread
            if (workProgress.WorkDone || activeNotifications.Contains(workProgress))
                return;

            workProgress.WorkFinished += OnWorkFinished;
            activeNotifications.Add(workProgress);
        });
    }

    private void OnWorkFinished(object? sender, WorkProgressNotificationEventArgs e)
    {
        // WorkFinished is raised on the UI thread (WorkProgressViewModel.NotifyWorkFinished dispatches it)
        e.WorkProgress.WorkFinished -= OnWorkFinished;
        activeNotifications.Remove(e.WorkProgress);
    }
}
