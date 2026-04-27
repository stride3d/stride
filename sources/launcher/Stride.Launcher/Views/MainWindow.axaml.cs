// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Launcher.ViewModels;

namespace Stride.Launcher.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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
}
