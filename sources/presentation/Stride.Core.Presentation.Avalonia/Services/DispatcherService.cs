// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Threading;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Presentation.Avalonia.Services;

/// <summary>
/// This class is the implementation of the <see cref="IDispatcherService"/> interface for Avalonia.
/// </summary>
// Note: this class is shared with the Launcher. Beware before adding new dependencies.
public sealed class DispatcherService : IDispatcherService
{
    private readonly Dispatcher dispatcher;

    /// <summary>
    /// Creates a new instance of the <see cref="DispatcherService"/> class using the dispatcher of the current thread.
    /// </summary>
    /// <returns></returns>
    public static DispatcherService Create()
    {
        return new DispatcherService(Dispatcher.UIThread);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DispatcherService"/> class using the associated dispatcher.
    /// </summary>
    /// <param name="dispatcher">The dispatcher to use for this instance of <see cref="DispatcherService"/>.</param>
    public DispatcherService(Dispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
    }

    /// <inheritdoc/>
    public void Invoke(Action callback)
    {
        if (CheckAccess())
            callback();
        else
            dispatcher.Invoke(callback);
    }

    /// <inheritdoc/>
    public TResult Invoke<TResult>(Func<TResult> callback)
    {
        return CheckAccess() ? callback() : dispatcher.Invoke(callback);
    }

    /// <inheritdoc/>
    public Task InvokeAsync(Action callback, CancellationToken token = default)
    {
        return dispatcher.InvokeAsync(callback, default, token).GetTask();
    }

    /// <inheritdoc/>
    public Task LowPriorityInvokeAsync(Action callback, CancellationToken token = default)
    {
        return dispatcher.InvokeAsync(callback, DispatcherPriority.ApplicationIdle, token).GetTask();
    }

    /// <inheritdoc/>
    public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, CancellationToken token = default)
    {
        return dispatcher.InvokeAsync(callback, default, token).GetTask();
    }

    /// <inheritdoc/>
    public Task InvokeTask(Func<Task> task, CancellationToken token = default)
    {
        return InvokeTask(dispatcher, task, token);
    }

    /// <inheritdoc/>
    public Task<TResult> InvokeTask<TResult>(Func<Task<TResult>> task, CancellationToken token = default)
    {
        return InvokeTask(dispatcher, task, token);
    }

    public static Task InvokeTask(Dispatcher dispatcher, Func<Task> task, CancellationToken token = default)
    {
        return dispatcher.InvokeAsync(task, default, token).GetTask().Unwrap();
    }

    public static Task<TResult> InvokeTask<TResult>(Dispatcher dispatcher, Func<Task<TResult>> task, CancellationToken token = default)
    {
        return dispatcher.InvokeAsync(task, default, token).GetTask().Unwrap();
    }

    /// <inheritdoc/>
    public bool CheckAccess()
    {
        return dispatcher.CheckAccess();
    }

    /// <inheritdoc/>
    public void EnsureAccess(bool inDispatcherThread = true)
    {
        if (inDispatcherThread && !CheckAccess())
            throw new InvalidOperationException("The current thread was expected to be the dispatcher thread.");
        if (!inDispatcherThread && CheckAccess())
            throw new InvalidOperationException("The current thread was expected to be different from the dispatcher thread.");
    }
}
