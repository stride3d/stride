// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Components.AddAssets;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Services;

internal class EditorDialogService : DialogService, IEditorDialogService, IProgressWindowService
{
    private DebugWindow? debugWindow;
    private readonly IViewModelServiceProvider serviceProvider;

    public EditorDialogService(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider.Get<IDispatcherService>())
    {
        this.serviceProvider = serviceProvider;
    }

    /// <summary>
    /// The notification bar; a host without one shows nothing.
    /// </summary>
    private INotificationService? Notifications => serviceProvider.TryGet<INotificationService>();

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
        ArgumentNullException.ThrowIfNull(workProgress);
        if (!workProgress.WorkDone || workProgress.ShouldStayOpen())
        {
            workProgress.NotifyWindowWillOpen();
            OpenProgressWindow(workProgress);
        }
        else
        {
            workProgress.NotifyWindowClosed();
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// As in WPF, the window opens when the delay is over while the work goes on, or as soon as the work finishes when it should stay
    /// open (its errors); it does not open at all when the work finishes before the delay without a reason to stay open.
    /// </remarks>
    public void ShowProgressWindow(WorkProgressViewModel workProgress, int minDelay)
    {
        ArgumentNullException.ThrowIfNull(workProgress);
        // The work waits for the window to close (NotifyWorkFinished) from now on
        workProgress.NotifyWindowWillOpen();
        ShowProgressWindowAfterDelayAsync(workProgress, minDelay).Forget();
    }

    private async Task ShowProgressWindowAfterDelayAsync(WorkProgressViewModel workProgress, int minDelay)
    {
        var workFinished = new TaskCompletionSource();
        void OnWorkFinished(object? sender, WorkProgressNotificationEventArgs e) => workFinished.TrySetResult();
        workProgress.WorkFinished += OnWorkFinished;
        try
        {
            if (workProgress.WorkDone)
                workFinished.TrySetResult();
            if (minDelay > 0)
                await Task.WhenAny(Task.Delay(minDelay), workFinished.Task);
        }
        finally
        {
            workProgress.WorkFinished -= OnWorkFinished;
        }

        await Dispatcher.InvokeAsync(() =>
        {
            if (!workProgress.WorkDone || workProgress.ShouldStayOpen())
                OpenProgressWindow(workProgress);
            else
                workProgress.NotifyWindowClosed();
        });
    }

    /// <summary>
    /// Opens the progress window of a work (which <see cref="WorkProgressViewModel.NotifyWindowWillOpen"/> has been told about).
    /// </summary>
    private void OpenProgressWindow(WorkProgressViewModel workProgress)
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

            // A dialog needs a visible owner; before the main window shows (or without one), the progress window stands alone
            if (ActiveWindow is { IsVisible: true } owner) progressWindow.ShowDialog(owner);
            else progressWindow.Show();
        });
    }

    public void ShowNotification(WorkProgressViewModel workProgress)
    {
        Notifications?.AddNotification(workProgress);
    }

    public void ClearKeyboardFocus()
    {
        // A text box commits its edit when it loses the focus (the property grid's do): move the focus away from it, as WPF
        // moves it to the main window. Other controls keep it, such as the code editor, where the user goes on typing.
        Dispatcher.Invoke(() =>
        {
            if (MainWindow is { FocusManager: { } focusManager } window && focusManager.GetFocusedElement() is TextBox)
            {
                if (!window.Focus())
                    focusManager.Focus(null);
            }
        });
    }

    public async Task<AssetViewModel?> OpenAssetPickerAsync(SessionViewModel session, IEnumerable<Type> acceptedTypes, Func<AssetViewModel, bool>? filter = null, AssetViewModel? initialAsset = null, DirectoryBaseViewModel? initialLocation = null, string? message = null)
    {
        if (MainWindow is null) return null;

        return await Dispatcher.InvokeTask(async () =>
        {
            var picker = new AssetPickerViewModel(session, acceptedTypes, filter, initialAsset, initialLocation) { Message = message };
            var window = new AssetPickerWindow { DataContext = picker };
            var picked = await window.ShowDialog<bool>(MainWindow);
            return picked ? picker.SelectedAsset : null;
        });
    }

    public async Task<ITemplateDescriptionViewModel?> ShowAddAssetDialogAsync(AddAssetTemplateCollectionViewModel templates)
    {
        ArgumentNullException.ThrowIfNull(templates);
        if (MainWindow is null) return null;

        return await Dispatcher.InvokeTask(async () =>
        {
            var window = new AddItemWindow { DataContext = templates };
            return await window.ShowDialog<ITemplateDescriptionViewModel?>(MainWindow);
        });
    }

    public async Task<TemplateDescriptionViewModel?> ShowAssetTemplatesDialogAsync(AssetTemplatesViewModel templates)
    {
        ArgumentNullException.ThrowIfNull(templates);
        if (MainWindow is null) return null;

        return await Dispatcher.InvokeTask(async () =>
        {
            var window = new ItemTemplatesWindow { DataContext = templates };
            return await window.ShowDialog<TemplateDescriptionViewModel?>(MainWindow);
        });
    }

    public async Task<NewPackageParameters?> ShowNewProjectDialogAsync(SessionViewModel session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (MainWindow is null) return null;

        return await Dispatcher.InvokeTask(async () =>
        {
            var templates = new NewProjectTemplateCollectionViewModel(serviceProvider, session);
            try
            {
                var window = new NewProjectWindow { DataContext = templates };
                var accepted = await window.ShowDialog<bool>(MainWindow);
                return accepted ? templates.GetNewPackageParameters() : null;
            }
            finally
            {
                templates.Destroy();
            }
        });
    }

    public async Task<bool> ShowProjectSelectionDialogAsync(NewOrOpenSessionTemplateCollectionViewModel templates)
    {
        ArgumentNullException.ThrowIfNull(templates);
        if (MainWindow is null) return false;

        return await Dispatcher.InvokeTask(async () =>
        {
            var window = new ProjectSelectionWindow { DataContext = templates };
            return await window.ShowDialog<bool>(MainWindow);
        });
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
