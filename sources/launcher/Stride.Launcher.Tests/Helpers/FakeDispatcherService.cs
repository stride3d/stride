using Stride.Core.Presentation.Services;

namespace Stride.Launcher.Tests.Helpers;

internal sealed class FakeDispatcherService : IDispatcherService
{
    public void Invoke(Action callback) => callback();

    public TResult Invoke<TResult>(Func<TResult> callback) => callback();

    public Task InvokeAsync(Action callback, CancellationToken token = default)
    {
        callback();
        return Task.CompletedTask;
    }

    public Task LowPriorityInvokeAsync(Action callback, CancellationToken token = default)
    {
        callback();
        return Task.CompletedTask;
    }

    public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, CancellationToken token = default)
        => Task.FromResult(callback());

    public Task InvokeTask(Func<Task> task, CancellationToken token = default) => task();

    public Task<TResult> InvokeTask<TResult>(Func<Task<TResult>> task, CancellationToken token = default) => task();

    public bool CheckAccess() => true;

    public void EnsureAccess(bool inDispatcherThread = true) { }
}
