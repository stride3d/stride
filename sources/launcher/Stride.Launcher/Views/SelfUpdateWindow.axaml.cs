// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Stride.Launcher;

public partial class SelfUpdateWindow : Window
{
    public SelfUpdateWindow()
    {
        InitializeComponent();

        if (Screens.ScreenFromWindow(this)?.WorkingArea is PixelRect area)
        {
            Width = Math.Min(Width, area.Width);
            Height = Math.Min(Height, area.Height);
        }

        // Allow closing only when Exit button is enabled.
        Closing += (sender, e) => e.Cancel = !ExitButton.IsEnabled;
    }

    /// <summary>
    /// Forcibly close the update window.
    /// </summary>
    public void ForceClose()
    {
        ExitButton.IsEnabled = true;
        Close();
    }

    /// <summary>
    /// Prevents window from being closed during a critical section of the update process.
    /// </summary>
    public void LockWindow()
    {
        ExitButton.IsEnabled = false;
    }

    private void ExitButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.cts.Cancel();
        }
        else
        {
            Environment.Exit(0);
        }
    }
}
