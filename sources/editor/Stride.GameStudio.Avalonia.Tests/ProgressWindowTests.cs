// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.GameStudio.Avalonia.Services;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Stride.Core.Assets.Editor.ViewModels.Progress;
using MessageBox = Stride.Core.Presentation.Avalonia.Windows.MessageBox;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The progress window of a work: it waits for the message boxes that are up, which it would hide.
/// </summary>
public sealed class ProgressWindowTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TheProgressWindowWaitsForAnOpenMessageBox(bool delayed)
    {
        var serviceProvider = new ViewModelServiceProvider([DispatcherService.Create()]);
        var dialogs = new EditorDialogService(serviceProvider);
        var messageBoxes = new List<MessageBox>();
        var progressWindows = new List<ProgressWindow>();
        using var messageBoxHandler = Window.WindowOpenedEvent.AddClassHandler<MessageBox>((window, _) => messageBoxes.Add(window));
        using var progressHandler = Window.WindowOpenedEvent.AddClassHandler<ProgressWindow>((window, _) => progressWindows.Add(window));
        // A question asked while a session loads (the upgrade of its packages)
        var answer = dialogs.MessageBoxAsync("Upgrade?", [new DialogButtonInfo { Content = "Upgrade", Result = 1, IsDefault = true }]);
        await WindowTestHelper.WaitUntilAsync(() => messageBoxes.Count == 1, "the message box");
        var work = new WorkProgressViewModel(serviceProvider, new LoggerResult()) { Title = "Opening session...", IsIndeterminate = true };
        try
        {
            if (delayed)
                dialogs.ShowProgressWindow(work, 0);
            else
                dialogs.ShowProgressWindow(work);
            await Task.Delay(300);
            await WindowTestHelper.PumpAsync();

            Assert.Empty(progressWindows);

            messageBoxes[0].Close();
            await answer;
            await WindowTestHelper.WaitUntilAsync(() => progressWindows.Count == 1, "the progress window");
        }
        finally
        {
            messageBoxes[0].Close();
            await work.NotifyWorkFinished(false, false);
        }
        await WindowTestHelper.WaitUntilAsync(() => !progressWindows[0].IsVisible, "the progress window to close");
    }
}
