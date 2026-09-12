// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Interactivity;
using Stride.Core.CodeEditorSupport.VisualStudio;
using Stride.Launcher.Converters;
using Stride.Launcher.ViewModels;

namespace Stride.Launcher.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _subscribedVm;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedVm is not null)
            _subscribedVm.CloseRequested -= OnCloseRequested;

        _subscribedVm = DataContext as MainViewModel;

        if (_subscribedVm is not null)
            _subscribedVm.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
            _ = OnClosingAsync(vm);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // When the cross-platform Game Studio port (xplat-editor) lands, the Win32
        // HWND hand-off below needs to be replaced with a cross-platform IPC token
        // (e.g. a named-pipe path) passed via a generalised CLI argument. See
        // docs/launcher/port-status.md Phase 1 for the rationale.
        if (OperatingSystem.IsWindows())
        {
            var platformHandle = TryGetPlatformHandle();
            if (platformHandle is not null)
            {
                MainViewModel.WindowHandle = platformHandle.Handle;
            }
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        // Avalonia's OnClosing is synchronous. Cancel the close unconditionally,
        // run the async confirmation, then exit explicitly if the user confirms.
        e.Cancel = true;
        _ = OnClosingAsync(vm);
    }

    private static async Task OnClosingAsync(MainViewModel vm)
    {
        if (await vm.TryCloseAsync())
        {
            // Matches master's exit code. ShutdownMode is OnExplicitShutdown so we
            // can't rely on the main window close to terminate the process.
            Environment.Exit(1);
        }
    }


    private void FrameworkChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm
            && FrameworkSelector.SelectedItem is string framework
            && vm.PreferredFramework != framework)
        {
            vm.PreferredFramework = framework;
        }
    }

    private void EditorChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm
            && EditorSelector.SelectedItem is string editor
            && vm.PreferredEditor != editor)
        {
            vm.PreferredEditor = editor;
        }
    }

    private void OnOutlineSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems is [OutlineRow row])
        {
            ReleaseNotesView.ScrollToAnchor(row.Entry.Slug);
            OutlineToggle.IsChecked = false;
        }

        // Clear selection so re-clicking the same heading still raises this handler.
        OutlineList.SelectedItem = null;
    }

    private void VisualStudioDownloadPage_Button_Loaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && VisualStudioVersions.AvailableInstances
            .Any(ide => ide.InstallationVersion?.Major == 16 || ide.InstallationVersion?.Major == 17))
        {
            button.IsVisible = false;
        }
    }
}
