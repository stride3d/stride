// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;

namespace Stride.GameStudio.Avalonia.Tests.Helpers;

internal static class WindowTestHelper
{
    /// <summary>
    /// Runs the work the view models queued on the dispatcher (the session updates its state and commands asynchronously), and
    /// a frame of the windows: the docking presents the content of its panels on the frames that follow a change.
    /// </summary>
    public static Task PumpAsync() => DispatcherPump.PumpAsync();

    /// <summary>
    /// Shows a window and lets it present its content: the docked panels of the main window appear on its first frame.
    /// </summary>
    public static async Task ShowAsync(this Window window)
    {
        window.Show();
        await PumpAsync();
    }

    /// <summary>
    /// Pumps the dispatcher until the condition holds, or fails after the timeout: the session's history and properties update
    /// asynchronously after a change.
    /// </summary>
    public static Task WaitUntilAsync(Func<bool> condition, string what, int timeoutMilliseconds = 5000) => DispatcherPump.WaitUntilAsync(condition, what, timeoutMilliseconds);

    /// <summary>
    /// Clicks the middle of a control with the left mouse button, as a user would: unlike raising its Click event, a click runs the
    /// command of a button.
    /// </summary>
    public static async Task ClickAsync(this Window window, Control control)
    {
        // The mouse input first runs the pending jobs and renders, which may lay the control out elsewhere: aim once they ran
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)
            ?? throw new InvalidOperationException("The control is not in the window.");
        // When the input is slow (a busy machine), the tooltip of the control opens between the move and the press: in the small
        // headless window it can cover the pointer and take the press, and the control is not clicked
        ToolTip.SetServiceEnabled(window, false);
        try
        {
            window.MouseMove(center);
            window.MouseDown(center, MouseButton.Left);
            window.MouseUp(center, MouseButton.Left);
        }
        finally
        {
            window.ClearValue(ToolTip.ServiceEnabledProperty);
        }
        await PumpAsync();
    }

    /// <summary>
    /// Closes a window and waits for it to be closed: the main window saves its settings first and closes when the dispatcher is
    /// idle, and a window left open renders after the test's application is gone.
    /// </summary>
    public static async Task CloseAsync(this Window window)
    {
        window.Close();
        // ApplicationIdle runs after ContextIdle, the priority of the deferred close
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
    }
}
