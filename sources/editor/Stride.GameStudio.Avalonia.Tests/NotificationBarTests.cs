// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.Core.Diagnostics;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Stride.Core.Assets.Editor.ViewModels.Progress;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The notification bar of the main view, which shows the background operations until they finish.
/// </summary>
public sealed class NotificationBarTests
{
    [AvaloniaFact]
    public async Task TheBarShowsAnOperationUntilItFinishes()
    {
        using var fixture = ShellSessionFixture.Create();
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var notifications = fixture.Main.Notifications!;
            var bar = window.GetVisualDescendants().OfType<ItemsControl>().Single(x => ReferenceEquals(x.ItemsSource, notifications.ActiveNotifications));
            Assert.False(bar.IsVisible);

            var work = new WorkProgressViewModel(fixture.ServiceProvider, new LoggerResult()) { Title = "Doing things", IsIndeterminate = true };
            notifications.AddNotification(work);
            await WindowTestHelper.PumpAsync();

            Assert.True(bar.IsVisible);
            Assert.Contains(bar.GetVisualDescendants().OfType<TextBlock>(), x => x.Text == "Doing things");
            Assert.Contains(bar.GetVisualDescendants().OfType<ProgressBar>(), x => x.IsIndeterminate);

            await work.NotifyWorkFinished(false, false);
            await WindowTestHelper.PumpAsync();

            Assert.False(bar.IsVisible);
            Assert.Empty(notifications.ActiveNotifications);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheDialogServiceShowsItsNotificationsInTheBar()
    {
        using var fixture = ShellSessionFixture.Create();
        var work = new WorkProgressViewModel(fixture.ServiceProvider, new LoggerResult()) { Title = "Through the dialog service" };

        fixture.ServiceProvider.Get<Services.EditorDialogService>().ShowNotification(work);

        Assert.Equal([work], fixture.Main.Notifications!.ActiveNotifications);
        await work.NotifyWorkFinished(false, false);
        Assert.Empty(fixture.Main.Notifications.ActiveNotifications);
    }
}
