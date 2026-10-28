// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Threading;

namespace Stride.GameStudio.Avalonia.Tests.Helpers;

internal static class WindowTestHelper
{
    /// <summary>
    /// Runs the work the view models queued on the dispatcher (the session updates its state and commands asynchronously).
    /// </summary>
    public static async Task PumpAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
    }

    /// <summary>
    /// Pumps the dispatcher until the condition holds, or fails after the timeout: the session's history and properties update
    /// asynchronously after a change.
    /// </summary>
    public static async Task WaitUntilAsync(Func<bool> condition, string what, int timeoutMilliseconds = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Timed out waiting for {what}.");
            await Task.Delay(25);
            await PumpAsync();
        }
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
