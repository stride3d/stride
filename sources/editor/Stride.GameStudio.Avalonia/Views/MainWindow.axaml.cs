// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Avalonia.Extensions;
using Stride.GameStudio.Avalonia.Settings;
using Stride.GameStudio.Avalonia.ViewModels;

namespace Stride.GameStudio.Avalonia.Views;

public partial class MainWindow : Window
{
    private TaskCompletionSource<bool>? closingTask;
    private bool closeConfirmed;
    private Size restoreBounds;
    private PixelPoint restorePosition;

    public MainWindow()
    {
        InitializeComponent();
        PositionChanged += (_, e) =>
        {
            if (WindowState == WindowState.Normal)
                restorePosition = e.Point;
        };
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
        if (closeConfirmed)
            return;
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

    /// <summary>
    /// The shortcuts of the Edit menu (Undo, Redo, Cut, Copy, Paste, Delete and Rename) go to the panel that has the focus. They
    /// run here, after the focused control has seen the key, so that a text box keeps its own Ctrl+Z and Ctrl+C: Avalonia
    /// evaluates the key bindings of the window before it raises the key event, so a <see cref="KeyBinding"/> would act on
    /// the session instead of the text.
    /// </summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        // Settings > Interface > Reset Game Studio layout: the sizes of the panels, as WPF resets its docking layout
        if (DataContext is MainViewModel mainViewModel)
            EditorSettings.ResetEditorLayout.Command = new AnonymousCommand(mainViewModel.ServiceProvider, View.ResetPanelSizes);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        View.EditRouter.TryHandleKey(e);
    }

    protected override void OnLoaded(RoutedEventArgs _)
    {
        // Notify the launcher that Game Studio is ready (no-op when not launched from the launcher).
        App.LauncherNotifier?.Invoke();
        App.LauncherNotifier = null; // fire once

        // Size the window to best fit the current screen size
        InitializeWindowSize();
        return;

        void InitializeWindowSize()
        {
            var previousWorkAreaWidth = GameStudioInternalSettings.WorkAreaWidth.GetValue();
            var previousWorkAreaHeight = GameStudioInternalSettings.WorkAreaHeight.GetValue();
            var wasWindowMaximized = GameStudioInternalSettings.WindowMaximized.GetValue();
            var (workArea, scaling) = this.GetWorkingArea();
            var previousPosition = new PixelPoint(GameStudioInternalSettings.WindowLeft.GetValue(), GameStudioInternalSettings.WindowTop.GetValue());
            // Kept for the next start while the window stays maximized
            restorePosition = previousPosition;

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
                // The window goes back where it was, if that is still on a screen; it is centered otherwise
                if (IsOnScreen(previousPosition, PixelSize.FromSize(new Size(Width, Height), scaling)))
                    Position = previousPosition;
                else
                    this.CenterToArea(workArea, scaling);
                WindowState = WindowState.Normal;
            }
        }
    }

    /// <summary>
    /// Whether enough of a window at this position shows on one of the screens to be moved with the mouse.
    /// </summary>
    private bool IsOnScreen(PixelPoint position, PixelSize size)
    {
        if (position.X == GameStudioInternalSettings.NoPosition || position.Y == GameStudioInternalSettings.NoPosition)
            return false;

        var bounds = new PixelRect(position, size);
        return Screens.All.Any(screen =>
        {
            var visible = screen.WorkingArea.Intersect(bounds);
            return visible.Width >= 100 && visible.Height >= 50;
        });
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
            // The session and its editors save or drop their unsaved changes, as the user chooses; the user can also cancel
            if (DataContext is MainViewModel mainViewModel && !await mainViewModel.CloseSessionAsync())
            {
                closingTask?.SetResult(false);
                return;
            }

            // TODO force all secondary windows to close

            // Internal settings
            SaveInternalSettings();

            // Exit the preview game before the process does (it renders on its own thread), and let the plugins leave
            (DataContext as MainViewModel)?.CloseSessionServices();

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
            var position = WindowState == WindowState.Normal ? Position : restorePosition;
            GameStudioInternalSettings.WindowLeft.SetValue(position.X);
            GameStudioInternalSettings.WindowTop.SetValue(position.Y);
            View.SavePanelSizes();
            // Write the settings file
            InternalSettings.SaveProfile();
        }

        void Shutdown()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                // Force shutdown
                lifetime.Shutdown();
            }
            else
            {
                // Without a desktop lifetime (a headless host), the window is just closed: the host owns the process
                closeConfirmed = true;
                Close();
            }
        }
    }
}
