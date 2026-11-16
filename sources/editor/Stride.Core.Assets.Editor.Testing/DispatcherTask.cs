// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Stride.Core.Assets.Editor.Testing;

/// <summary>
/// A task that, awaited, continues its caller in the exclusive dispatcher when it ended there, as the open of a session fixture does:
/// the test then goes on as on the UI thread of the editor, until it ends. A task that ended elsewhere (on the thread of a
/// <c>ThreadDispatcherService</c>) continues its caller as a <see cref="Task{TResult}"/> does.
/// </summary>
/// <remarks>
/// A helper that wraps such an open returns a <see cref="DispatcherTask{TResult}"/> too (<c>async DispatcherTask&lt;T&gt;</c>): as a
/// <see cref="Task{TResult}"/>, it would continue its caller on the thread pool, out of the dispatcher.
/// </remarks>
[AsyncMethodBuilder(typeof(DispatcherTaskMethodBuilder<>))]
public readonly struct DispatcherTask<TResult>
{
    private readonly Task<TResult> task;
    // Set by the thread that ends the task: the caller may see that it ended only later, from another thread
    private readonly StrongBox<bool>? endedInDispatcher;

    /// <summary>
    /// Wraps a task that does not end in the exclusive dispatcher: awaited, it continues its caller as a <see cref="Task{TResult}"/> does.
    /// </summary>
    public DispatcherTask(Task<TResult> task) : this(task, null)
    {
    }

    internal DispatcherTask(Task<TResult> task, StrongBox<bool>? endedInDispatcher)
    {
        this.task = task;
        this.endedInDispatcher = endedInDispatcher;
    }

    public Awaiter GetAwaiter() => new(task, endedInDispatcher);

    public readonly struct Awaiter(Task<TResult> task, StrongBox<bool>? endedInDispatcher) : INotifyCompletion
    {
        // A caller out of the dispatcher always goes through OnCompleted, which continues it where the task ended
        public bool IsCompleted => task.IsCompleted && ExclusiveContext.IsEntered;

        public void OnCompleted(Action continuation)
        {
            var context = SynchronizationContext.Current;
            var ended = endedInDispatcher;
            task.ContinueWith(_ =>
            {
                if (ended is { Value: true })
                    ExclusiveContext.Instance.Post(_ => continuation(), null);
                else if (context is not null)
                    context.Post(_ => continuation(), null);
                else
                    ThreadPool.QueueUserWorkItem(_ => continuation());
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public TResult GetResult() => task.GetAwaiter().GetResult();
    }
}

/// <summary>
/// Creates the <see cref="DispatcherTask{TResult}"/> of the opens.
/// </summary>
public static class DispatcherTask
{
    /// <summary>
    /// Runs the asynchronous open in the exclusive dispatcher: awaited, it continues its caller there.
    /// </summary>
    public static DispatcherTask<TResult> InDispatcher<TResult>(Func<Task<TResult>> open) => ExclusiveContext.Run(() => InDispatcherAsync(open()));

    // Awaited in the dispatcher, the open ends there
    private static async DispatcherTask<TResult> InDispatcherAsync<TResult>(Task<TResult> open) => await open;
}

/// <summary>
/// The builder of the <c>async</c> methods that return a <see cref="DispatcherTask{TResult}"/>.
/// </summary>
public struct DispatcherTaskMethodBuilder<TResult>
{
    private AsyncTaskMethodBuilder<TResult> builder;
    // A box: the state machine and its builder are copied when the method first awaits
    private StrongBox<bool> endedInDispatcher;

    public static DispatcherTaskMethodBuilder<TResult> Create() => new() { builder = AsyncTaskMethodBuilder<TResult>.Create(), endedInDispatcher = new() };

    public DispatcherTask<TResult> Task => new(builder.Task, endedInDispatcher);

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine => builder.Start(ref stateMachine);

    public void SetStateMachine(IAsyncStateMachine stateMachine) => builder.SetStateMachine(stateMachine);

    public void SetResult(TResult result)
    {
        endedInDispatcher.Value = ExclusiveContext.IsEntered;
        builder.SetResult(result);
    }

    public void SetException(Exception exception)
    {
        endedInDispatcher.Value = ExclusiveContext.IsEntered;
        builder.SetException(exception);
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
        => builder.AwaitOnCompleted(ref awaiter, ref stateMachine);

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
        => builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
}
