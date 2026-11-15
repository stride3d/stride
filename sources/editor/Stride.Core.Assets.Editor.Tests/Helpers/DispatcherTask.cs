// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

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
public readonly struct DispatcherTask<TResult>(Task<TResult> task)
{
    public Awaiter GetAwaiter() => new(task);

    public readonly struct Awaiter(Task<TResult> task) : INotifyCompletion
    {
        // A caller out of the dispatcher always goes through OnCompleted, which knows where the task ended
        public bool IsCompleted => task.IsCompleted && ExclusiveContext.IsEntered;

        public void OnCompleted(Action continuation)
        {
            var context = SynchronizationContext.Current;
            // Run where the task ends, to know whether it ended in the dispatcher
            task.ContinueWith(_ =>
            {
                if (ExclusiveContext.IsEntered)
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
    public static DispatcherTask<TResult> InDispatcher<TResult>(Func<Task<TResult>> open) => new(ExclusiveContext.Run(open));
}

/// <summary>
/// The builder of the <c>async</c> methods that return a <see cref="DispatcherTask{TResult}"/>.
/// </summary>
public struct DispatcherTaskMethodBuilder<TResult>
{
    private AsyncTaskMethodBuilder<TResult> builder;

    public static DispatcherTaskMethodBuilder<TResult> Create() => new() { builder = AsyncTaskMethodBuilder<TResult>.Create() };

    public DispatcherTask<TResult> Task => new(builder.Task);

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine => builder.Start(ref stateMachine);

    public void SetStateMachine(IAsyncStateMachine stateMachine) => builder.SetStateMachine(stateMachine);

    public void SetResult(TResult result) => builder.SetResult(result);

    public void SetException(Exception exception) => builder.SetException(exception);

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
        => builder.AwaitOnCompleted(ref awaiter, ref stateMachine);

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
        => builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
}
