// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Services;

public sealed class NullDispatcherService : IDispatcherService
{
    bool IDispatcherService.CheckAccess()
    {
        return true;
    }

    void IDispatcherService.EnsureAccess(bool inDispatcherThread)
    {
    }

    void IDispatcherService.Invoke(Action callback)
    {
        callback();
    }

    TResult IDispatcherService.Invoke<TResult>(Func<TResult> callback)
    {
        return callback();
    }

    Task IDispatcherService.InvokeAsync(Action callback, CancellationToken token)
    {
        callback();
        return Task.CompletedTask;
    }

    Task<TResult> IDispatcherService.InvokeAsync<TResult>(Func<TResult> callback, CancellationToken token)
    {
        var result = callback();
        return Task.FromResult(result);
    }

    Task IDispatcherService.InvokeTask(Func<Task> task, CancellationToken token)
    {
        return task();
    }

    Task<TResult> IDispatcherService.InvokeTask<TResult>(Func<Task<TResult>> task, CancellationToken token)
    {
        return task();
    }

    Task IDispatcherService.LowPriorityInvokeAsync(Action callback, CancellationToken token)
    {
        callback();
        return Task.CompletedTask;
    }
}
