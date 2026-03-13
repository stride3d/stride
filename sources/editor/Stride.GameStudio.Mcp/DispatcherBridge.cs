// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.Presentation.Services;

namespace Stride.GameStudio.Mcp;

/// <summary>
/// Bridges MCP tool handler threads (background) to the WPF dispatcher thread
/// where all editor state must be accessed.
/// </summary>
public sealed class DispatcherBridge
{
    private readonly IDispatcherService _dispatcher;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    public DispatcherBridge(IDispatcherService dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <summary>
    /// Executes an action on the UI thread and returns the result.
    /// This is the primary way MCP tools access editor state.
    /// </summary>
    public async Task<T> InvokeOnUIThread<T>(Func<T> action, CancellationToken cancellationToken = default)
    {
        if (_dispatcher.CheckAccess())
        {
            return action();
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(DefaultTimeout);

        return await _dispatcher.InvokeAsync(action, cts.Token);
    }

    /// <summary>
    /// Executes an action on the UI thread without a return value.
    /// </summary>
    public async Task InvokeOnUIThread(Action action, CancellationToken cancellationToken = default)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
            return;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(DefaultTimeout);

        await _dispatcher.InvokeAsync(action, cts.Token);
    }

    /// <summary>
    /// Executes an async task on the UI thread and returns the result.
    /// </summary>
    public async Task<T> InvokeTaskOnUIThread<T>(Func<Task<T>> task, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(DefaultTimeout);

        return await _dispatcher.InvokeTask(task, cts.Token);
    }

    /// <summary>
    /// Executes an async task on the UI thread without a return value.
    /// </summary>
    public async Task InvokeTaskOnUIThread(Func<Task> task, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(DefaultTimeout);

        await _dispatcher.InvokeTask(task, cts.Token);
    }
}
