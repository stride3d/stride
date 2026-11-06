// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The notifications of the background operations (<see cref="NotificationsViewModel"/>).
/// </summary>
public sealed class TestNotifications
{
    private readonly ViewModelServiceProvider serviceProvider = new([new NullDispatcherService()]);

    [Fact]
    public async Task AnOperationShowsUntilItFinishes()
    {
        var notifications = new NotificationsViewModel(serviceProvider);
        var work = NewWork("Importing");

        notifications.AddNotification(work);
        Assert.Equal([work], notifications.ActiveNotifications);

        await work.NotifyWorkFinished(false, false);
        Assert.Empty(notifications.ActiveNotifications);
    }

    [Fact]
    public async Task AFailedOperationIsRemovedToo()
    {
        var notifications = new NotificationsViewModel(serviceProvider);
        var first = NewWork("First");
        var second = NewWork("Second");
        notifications.AddNotification(first);
        notifications.AddNotification(second);

        await first.NotifyWorkFinished(false, true);

        Assert.Equal([second], notifications.ActiveNotifications);
        Assert.True(first.HasFailed);
    }

    [Fact]
    public async Task AFinishedOperationIsNotShown()
    {
        var notifications = new NotificationsViewModel(serviceProvider);
        var work = NewWork("Done");
        await work.NotifyWorkFinished(false, false);

        notifications.AddNotification(work);

        Assert.Empty(notifications.ActiveNotifications);
    }

    [Fact]
    public void AnOperationIsShownOnce()
    {
        var notifications = new NotificationsViewModel(serviceProvider);
        var work = NewWork("Twice");

        notifications.AddNotification(work);
        notifications.AddNotification(work);

        Assert.Single(notifications.ActiveNotifications);
    }

    private WorkProgressViewModel NewWork(string title) => new(serviceProvider, new LoggerResult()) { Title = title };
}
