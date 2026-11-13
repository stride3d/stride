// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Headless;
using Avalonia.Threading;

namespace Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;

/// <summary>
/// Runs the work queued on the dispatcher of Avalonia, for the tests of the views (the Game Studio tests link this file).
/// </summary>
public static class DispatcherPump
{
    /// <summary>
    /// Runs the work the view models queued on the dispatcher (the session updates its state and commands asynchronously), and
    /// a frame of the windows: the docking presents the content of its panels on the frames that follow a change.
    /// </summary>
    public static async Task PumpAsync()
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
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
}
