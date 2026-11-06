// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Stride.Core.Presentation.Services;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// Blocks the threads of several dispatchers (the UI thread and the game thread) while the code that holds the lock
/// accesses objects owned by both.
/// </summary>
internal sealed class DispatcherLock : IDisposable
{
    private readonly struct DispatcherState
    {
        public DispatcherState(IDispatcherService dispatcher)
        {
            ArgumentNullException.ThrowIfNull(dispatcher);
            if (dispatcher.CheckAccess()) throw new InvalidOperationException("A dispatcher lock must be created from a different thread that the dispatchers it should lock");
            Dispatcher = dispatcher;
            Locked = new TaskCompletionSource<int>();
            Unlocked = new TaskCompletionSource<int>();
        }

        public readonly IDispatcherService Dispatcher;
        public readonly TaskCompletionSource<int> Locked;
        public readonly TaskCompletionSource<int> Unlocked;
    }

    private readonly List<DispatcherState> dispatcherStates;

    private DispatcherLock(IEnumerable<IDispatcherService> dispatchers)
    {
        dispatcherStates = dispatchers.Select(x => new DispatcherState(x)).ToList();
    }

    /// <summary>
    /// Unlocks all the dispatchers.
    /// </summary>
    public void Dispose()
    {
        foreach (var dispatcher in dispatcherStates)
        {
            dispatcher.Unlocked.SetResult(0);
        }
    }

    /// <summary>
    /// Locks the given dispatchers, one after the other when <paramref name="lockSequentially"/> is <c>true</c>.
    /// </summary>
    /// <returns>The lock, which releases the dispatchers when disposed.</returns>
    public static async Task<DispatcherLock> Lock(bool lockSequentially, params IDispatcherService[] dispatchers)
    {
        ArgumentNullException.ThrowIfNull(dispatchers);

        var lok = new DispatcherLock(dispatchers);
        foreach (var dispatcher in lok.dispatcherStates)
        {
            dispatcher.Dispatcher.InvokeAsync(() =>
            {
                // This dispatcher is now locked...
                dispatcher.Locked.SetResult(0);
                // ... and blocked until it's unlocked
                dispatcher.Unlocked.Task.Wait();
            }).Forget();

            if (lockSequentially)
            {
                // Wait for this dispatcher to be locked before proceeding, if sequential locking was requested.
                await dispatcher.Locked.Task;
            }
        }

        // Wait for all the dispatcher to be locked.
        await Task.WhenAll(lok.dispatcherStates.Select(x => x.Locked.Task).Cast<Task>());
        return lok;
    }
}
