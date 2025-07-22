// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Avalonia.Extensions;
using Stride.GameStudio.Avalonia.Settings;

namespace Stride.GameStudio.Avalonia.Views;

public partial class MainWindow : Window
{
    private TaskCompletionSource<bool>? closingTask;
    private Size restoreBounds;

    public MainWindow()
    {
        InitializeComponent();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.ShutdownRequested += OnShutdownRequested;
        }

        return;

        void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
        {
            // We need to run async stuff before closing, so let's always cancel the shutdown at first.
            e.Cancel = true;
            // This method will shutdown the application if the session has been successfully closed.
            SaveAndClose().Forget();
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        switch (e.CloseReason)
        {
            case WindowCloseReason.ApplicationShutdown:
            case WindowCloseReason.OSShutdown:
                return;

            default:
                // We need to run async stuff before closing, so let's always cancel the close at first.
                e.Cancel = true;
                // This method will shutdown the application if the session has been successfully closed.
                SaveAndClose().Forget();
                break;
        }
    }

    protected override void OnLoaded(RoutedEventArgs _)
    {
        // Size the window to best fit the current screen size
        InitializeWindowSize();
        return;

        void InitializeWindowSize()
        {
            var previousWorkAreaWidth = GameStudioInternalSettings.WorkAreaWidth.GetValue();
            var previousWorkAreaHeight = GameStudioInternalSettings.WorkAreaHeight.GetValue();
            var wasWindowMaximized = GameStudioInternalSettings.WindowMaximized.GetValue();
            var (workArea, scaling) = this.GetWorkingArea();

            if (wasWindowMaximized || previousWorkAreaWidth > workArea.Width || previousWorkAreaHeight > workArea.Height)
            {
                // Resolution has changed (and is now smaller), let's make the window fill all available space.
                this.FillArea(workArea, scaling);
                WindowState = WindowState.Maximized;
            }
            else
            {
                // Load state
                var previousWindowWidth = GameStudioInternalSettings.WindowWidth.GetValue();
                var previousWindowHeight = GameStudioInternalSettings.WindowHeight.GetValue();
                // Set window size
                Width = Math.Min(previousWindowWidth, workArea.Width);
                Height = Math.Min(previousWindowHeight, workArea.Height);
                // Window is centered by default
                this.CenterToArea(workArea, scaling);
                WindowState = WindowState.Normal;
            }
        }
    }

    protected override void OnResized(WindowResizedEventArgs e)
    {
        base.OnResized(e);
        switch (e.Reason)
        {
            case WindowResizeReason.Layout:
            case WindowResizeReason.User:
                restoreBounds = e.ClientSize;
                break;
        }
    }

    private async Task SaveAndClose()
    {
        try
        {
            // TODO asks editors to save their modified assets
            // if cancel: closingTask?.SetResult(false); return;

            // TODO force all editors and secondary windows to close

            // Internal settings
            SaveInternalSettings();

            closingTask?.SetResult(true);
            // Shutdown after all other operations have completed
            await Dispatcher.UIThread.InvokeAsync(Shutdown, DispatcherPriority.ContextIdle);
        }
        finally
        {
            closingTask = null;
        }

        return;

        void SaveInternalSettings()
        {
            var (workArea, _) = this.GetWorkingArea();
            // Save state
            GameStudioInternalSettings.WorkAreaWidth.SetValue((int)workArea.Width);
            GameStudioInternalSettings.WorkAreaHeight.SetValue((int)workArea.Height);
            GameStudioInternalSettings.WindowWidth.SetValue((int)Math.Max(800, WindowState == WindowState.Maximized ? restoreBounds.Width : Bounds.Width));
            GameStudioInternalSettings.WindowHeight.SetValue((int)Math.Max(600, WindowState == WindowState.Maximized ? restoreBounds.Height : Bounds.Height));
            GameStudioInternalSettings.WindowMaximized.SetValue(WindowState == WindowState.Maximized);
            // Write the settings file
            InternalSettings.SaveProfile();
        }

        static void Shutdown()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                // Force shutdown
                lifetime.Shutdown();
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }
}
