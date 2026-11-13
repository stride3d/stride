// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Stride.Core.Presentation.Services;

namespace Stride.Assets.Editor.Tests.Helpers;

/// <summary>
/// A dispatcher with a thread of its own, as the UI thread or the game thread of the editor: the change propagators lock
/// both threads, which must not be the thread of the test. The awaits of the code it runs come back to its thread.
/// </summary>
internal sealed class ThreadDispatcherService : IDispatcherService, IDisposable
{
    private readonly BlockingCollection<Action> queue = [];
    private readonly Thread thread;

    public ThreadDispatcherService(string name)
    {
        thread = new Thread(Loop) { IsBackground = true, Name = name };
        thread.Start();
    }

    /// <summary>
    /// Runs an asynchronous test body on this thread.
    /// </summary>
    public Task Run(Func<Task> body) => InvokeTask(body);

    public void Dispose()
    {
        queue.CompleteAdding();
    }

    public bool CheckAccess() => Thread.CurrentThread == thread;

    public void EnsureAccess(bool inDispatcherThread = true)
    {
        if (CheckAccess() != inDispatcherThread)
            throw new InvalidOperationException(inDispatcherThread ? $"Not on the thread {thread.Name}." : $"On the thread {thread.Name}.");
    }

    public void Invoke(Action callback)
    {
        if (CheckAccess())
            callback();
        else
            InvokeAsync(callback).Wait();
    }

    public TResult Invoke<TResult>(Func<TResult> callback)
    {
        return CheckAccess() ? callback() : InvokeAsync(callback).Result;
    }

    public Task InvokeAsync(Action callback, CancellationToken token = default)
    {
        return InvokeAsync(() =>
        {
            callback();
            return 0;
        }, token);
    }

    public Task LowPriorityInvokeAsync(Action callback, CancellationToken token = default)
    {
        return InvokeAsync(callback, token);
    }

    public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, CancellationToken token = default)
    {
        var completion = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            queue.Add(Execute);
        }
        catch (InvalidOperationException)
        {
            // The dispatcher is disposed, as a game that went away
            completion.SetCanceled(CancellationToken.None);
        }
        return completion.Task;

        void Execute()
        {
            if (token.IsCancellationRequested)
            {
                completion.SetCanceled(token);
                return;
            }
            try
            {
                completion.SetResult(callback());
            }
            catch (Exception e)
            {
                completion.SetException(e);
            }
        }
    }

    public Task InvokeTask(Func<Task> task, CancellationToken token = default)
    {
        return InvokeAsync(task, token).Unwrap();
    }

    public Task<TResult> InvokeTask<TResult>(Func<Task<TResult>> task, CancellationToken token = default)
    {
        return InvokeAsync(task, token).Unwrap();
    }

    private void Loop()
    {
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(this));
        foreach (var action in queue.GetConsumingEnumerable())
        {
            action();
        }
    }

    private sealed class DispatcherSynchronizationContext(ThreadDispatcherService dispatcher) : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
            dispatcher.InvokeAsync(() => d(state));
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            dispatcher.Invoke(() => d(state));
        }

        public override SynchronizationContext CreateCopy() => this;
    }
}
