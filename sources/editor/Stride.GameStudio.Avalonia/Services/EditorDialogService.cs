// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.GameStudio.Avalonia.Views;
using Stride.Core.Assets.Editor.Avalonia;

namespace Stride.GameStudio.Avalonia.Services;

internal class EditorDialogService : DialogService, IEditorDialogService
{
    private DebugWindow? debugWindow;
    private readonly IViewModelServiceProvider serviceProvider;

    public EditorDialogService(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider.Get<IDispatcherService>())
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task ShowAboutWindowAsync()
    {
        if (MainWindow is null) return;

        await Dispatcher.InvokeTask(async () =>
        {
            await new AboutWindow().ShowDialog(MainWindow);
        });
    }

    public async Task ShowDebugWindowAsync()
    {
        await Dispatcher.InvokeAsync(() =>
        {
            if (debugWindow is null)
            {
                debugWindow = new DebugWindow
                {
                    ViewModel = new DebugWindowViewModel(serviceProvider)
                };
                debugWindow.Show();
                debugWindow.Closed += (_, _) => debugWindow = null;
            }
            else
            {
                if (debugWindow.WindowState == WindowState.Minimized)
                {
                    debugWindow.WindowState = WindowState.Normal;
                }
                debugWindow.Activate();
            }
        });
    }

    public void ShowProgressWindow(WorkProgressViewModel workProgress)
    {
        if (!workProgress.WorkDone || workProgress.ShouldStayOpen())
        {
            Dispatcher.Invoke(() =>
            {
                var progressWindow = new ProgressWindow
                {
                    DataContext = workProgress
                };
                workProgress.WorkFinished += (_, _) =>
                {
                    if (!workProgress.ShouldStayOpen()) progressWindow.Close();
                };
                progressWindow.Closing += (_, e) =>
                {
                    if (!workProgress.WorkDone) e.Cancel = true;
                };
                progressWindow.Closed += (_, _) =>
                {
                    workProgress.NotifyWindowClosed();
                };
                workProgress.NotifyWindowWillOpen();
                
                if (MainWindow is not null) progressWindow.ShowDialog(MainWindow);
                else progressWindow.Show();
            });
        }
        else
        {
            workProgress.NotifyWindowClosed();
        }
    }

    public void ShowProjectSelectionWindow()
    {
        var project = new ProjectSelectionWindow()
        {
            DataContext = new NewOrOpenSessionTemplateCollectionViewModel(serviceProvider)
        };
        project.Show();
    }

    public async Task ShowSettingsWindowAsync()
    {
        if (MainWindow is null) return;

        await Dispatcher.InvokeTask(async () =>
        {
            await new SettingsWindow
            {
                DataContext = new SettingsViewModel(serviceProvider, EditorSettings.SettingsContainer.CurrentProfile)
            }.ShowDialog(MainWindow);
        });
    }
}
