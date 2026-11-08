// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// Runs the dispatcher calls inline, like <see cref="NullDispatcherService"/>, but one at a time, in a synchronization context that also
/// runs their continuations one at a time: the view models get the exclusive access a UI thread gives them.
/// </summary>
/// <remarks>
/// The session raises some of its events on the thread pool (the asset properties changed after an edit, undo or redo) and the builder in its
/// micro-threads, and the view models reacting to them dispatch their work. Run inline with no exclusion, that work races the other
/// dispatched work: the action history refreshes its transactions while an edit removes some, the references view enumerates the selection
/// while it changes. Run inline in the caller's context, an async method started there (the delayed refresh of the asset log) would post its
/// continuation to the micro-thread after it has completed ("MicroThread is already completed but still posting continuations"). These crash
/// the test host. A test that reads what dispatched work changes reads it through the dispatcher too.
/// </remarks>
/// <param name="strict">Whether only the dispatched work has the access of the UI thread, as in the editor. Its tests then change the session
/// through the dispatcher, and the view models fail on a change made elsewhere instead of racing the dispatched work now and then.</param>
public sealed class ExclusiveDispatcherService(bool strict = false) : IDispatcherService
{
    public bool CheckAccess() => !strict || ExclusiveContext.IsEntered;

    public void EnsureAccess(bool inDispatcherThread = true)
    {
        if (strict && CheckAccess() != inDispatcherThread)
            throw new InvalidOperationException(inDispatcherThread ? "Not in the exclusive dispatcher context." : "In the exclusive dispatcher context.");
    }

    public void Invoke(Action callback) => ExclusiveContext.Run(() => { callback(); return true; });

    public TResult Invoke<TResult>(Func<TResult> callback) => ExclusiveContext.Run(callback);

    public Task InvokeAsync(Action callback, CancellationToken token = default)
    {
        Invoke(callback);
        return Task.CompletedTask;
    }

    public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, CancellationToken token = default) => Task.FromResult(ExclusiveContext.Run(callback));

    public Task InvokeTask(Func<Task> task, CancellationToken token = default) => ExclusiveContext.Run(task);

    public Task<TResult> InvokeTask<TResult>(Func<Task<TResult>> task, CancellationToken token = default) => ExclusiveContext.Run(task);

    public Task LowPriorityInvokeAsync(Action callback, CancellationToken token = default) => InvokeAsync(callback, token);
}

/// <summary>
/// A synchronization context that runs everything posted to it on the thread pool, one at a time.
/// </summary>
internal sealed class ExclusiveContext : SynchronizationContext
{
    private static readonly ExclusiveContext Instance = new();
    private static readonly object Gate = new();

    public static bool IsEntered => Monitor.IsEntered(Gate);

    public static TResult Run<TResult>(Func<TResult> callback)
    {
        // The lock is reentrant: a dispatcher call made from a dispatched one runs inline, as on a UI thread
        lock (Gate)
        {
            var context = Current;
            SetSynchronizationContext(Instance);
            try
            {
                return callback();
            }
            finally
            {
                SetSynchronizationContext(context);
            }
        }
    }

    public override void Post(SendOrPostCallback d, object? state) => ThreadPool.QueueUserWorkItem(_ => Run(() => { d(state); return true; }));

    public override void Send(SendOrPostCallback d, object? state) => Run(() => { d(state); return true; });

    public override SynchronizationContext CreateCopy() => this;
}
