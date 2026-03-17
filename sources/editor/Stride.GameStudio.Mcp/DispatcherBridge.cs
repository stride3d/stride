// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.Presentation.Dialogs;
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

    /// <summary>
    /// Recent dialog messages that were suppressed during MCP execution.
    /// Visible via get_editor_status so agents can see dialog errors.
    /// </summary>
    public static ConcurrentQueue<string> RecentSuppressedDialogs { get; } = new();

    private const int MaxRecentDialogs = 20;

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
            return WithDialogSuppression(action);
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(DefaultTimeout);

        return await _dispatcher.InvokeAsync(() => WithDialogSuppression(action), cts.Token);
    }

    /// <summary>
    /// Executes an action on the UI thread without a return value.
    /// </summary>
    public async Task InvokeOnUIThread(Action action, CancellationToken cancellationToken = default)
    {
        if (_dispatcher.CheckAccess())
        {
            WithDialogSuppression(action);
            return;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(DefaultTimeout);

        await _dispatcher.InvokeAsync(() => WithDialogSuppression(action), cts.Token);
    }

    /// <summary>
    /// Executes an async task on the UI thread and returns the result.
    /// Uses a custom TCS to guarantee exception propagation and timeout —
    /// the built-in <c>DispatcherService.InvokeTask</c> swallows exceptions and hangs forever.
    /// </summary>
    public async Task<T> InvokeTaskOnUIThread<T>(Func<Task<T>> task, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(DefaultTimeout);

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cts.Token.Register(() => tcs.TrySetCanceled(cts.Token));

        // Schedule the async work on the dispatcher thread.
        // InvokeAsync completes once the synchronous part runs (the fire-and-forget kick-off).
        // The TCS is completed by the async continuation with result, cancellation, or exception.
        await _dispatcher.InvokeAsync(() =>
        {
            _ = ExecuteAndComplete(() => WithDialogSuppressionAsync(task), tcs);
        }, cts.Token);

        return await tcs.Task;
    }

    /// <summary>
    /// Executes an async task on the UI thread without a return value.
    /// Uses a custom TCS to guarantee exception propagation and timeout.
    /// </summary>
    public async Task InvokeTaskOnUIThread(Func<Task> task, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(DefaultTimeout);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cts.Token.Register(() => tcs.TrySetCanceled(cts.Token));

        await _dispatcher.InvokeAsync(() =>
        {
            _ = ExecuteAndComplete(async () =>
            {
                await WithDialogSuppressionAsync(task);
                return true;
            }, tcs);
        }, cts.Token);

        await tcs.Task;
    }

    /// <summary>
    /// Runs an async function and completes the TCS with result, cancellation, or exception.
    /// Guaranteed to set the TCS in all code paths.
    /// </summary>
    private static async Task ExecuteAndComplete<T>(Func<Task<T>> task, TaskCompletionSource<T> tcs)
    {
        try
        {
            tcs.TrySetResult(await task());
        }
        catch (OperationCanceledException ex)
        {
            tcs.TrySetCanceled(ex.CancellationToken);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
    }

    private static T WithDialogSuppression<T>(Func<T> action)
    {
        DialogService.SuppressDialogs = true;
        DialogService.SuppressedDialogMessages = new List<string>();
        try
        {
            var result = action();
            ThrowIfDialogsSuppressed();
            return result;
        }
        finally
        {
            DialogService.SuppressDialogs = false;
            DialogService.SuppressedDialogMessages = null;
        }
    }

    private static void WithDialogSuppression(Action action)
    {
        DialogService.SuppressDialogs = true;
        DialogService.SuppressedDialogMessages = new List<string>();
        try
        {
            action();
            ThrowIfDialogsSuppressed();
        }
        finally
        {
            DialogService.SuppressDialogs = false;
            DialogService.SuppressedDialogMessages = null;
        }
    }

    private static async Task<T> WithDialogSuppressionAsync<T>(Func<Task<T>> task)
    {
        DialogService.SuppressDialogs = true;
        DialogService.SuppressedDialogMessages = new List<string>();
        try
        {
            var result = await task();
            ThrowIfDialogsSuppressed();
            return result;
        }
        finally
        {
            DialogService.SuppressDialogs = false;
            DialogService.SuppressedDialogMessages = null;
        }
    }

    private static async Task WithDialogSuppressionAsync(Func<Task> task)
    {
        DialogService.SuppressDialogs = true;
        DialogService.SuppressedDialogMessages = new List<string>();
        try
        {
            await task();
            ThrowIfDialogsSuppressed();
        }
        finally
        {
            DialogService.SuppressDialogs = false;
            DialogService.SuppressedDialogMessages = null;
        }
    }

    private static void ThrowIfDialogsSuppressed()
    {
        var msgs = DialogService.SuppressedDialogMessages;
        if (msgs is { Count: > 0 })
        {
            foreach (var msg in msgs)
            {
                RecentSuppressedDialogs.Enqueue(msg);
                while (RecentSuppressedDialogs.Count > MaxRecentDialogs)
                    RecentSuppressedDialogs.TryDequeue(out _);
            }
            throw new McpDialogSuppressedException(msgs);
        }
    }
}
