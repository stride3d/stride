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
