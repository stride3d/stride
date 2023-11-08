// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModels;

public enum KeepOpen
{
    Never,
    OnWarningsOrErrors,
    OnErrors,
    Always
}

public sealed class WorkProgressViewModel : DispatcherViewModel, IProgressViewModel
{
    private readonly CancellationTokenSource cts = new();
    private readonly AutoResetEvent resetEvent = new(false);
    private readonly SemaphoreSlim semaphore = new(1, 1);

    private (string?, double) next;

    private ICommandBase? cancelCommand;
    private bool isIndeterminate;
    private KeepOpen keepOpen;
    private double minimum = 0.0;
    private double maximum = 1.0;
    private string? progressMessage;
    private double progressValue;
    private IProgressStatus? registeredProgressStatus;
    private string title = "Work in progress...";
    private bool workDone;

    private bool windowWillOpen;
    private readonly TaskCompletionSource workProgressClosed = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkProgressViewModel"/> class with a single logger.
    /// </summary>
    /// <param name="serviceProvider">A service provider that can provide a <see cref="IDispatcherService"/> to use for this view model.</param>
    /// <param name="logger">The logger to monitor.</param>
    public WorkProgressViewModel(IViewModelServiceProvider serviceProvider, Logger logger)
        : this(serviceProvider, logger.Yield())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkProgressViewModel"/> class with multiple loggers.
    /// </summary>
    /// <param name="serviceProvider">A service provider that can provide a <see cref="IDispatcherService"/> to use for this view model.</param>
    /// <param name="loggers">The loggers to monitor.</param>
    public WorkProgressViewModel(IViewModelServiceProvider serviceProvider, IEnumerable<Logger> loggers)
        : base(serviceProvider)
    {
        Log = new LoggerViewModel(serviceProvider, loggers);
        Task.Run(() => UpdateAsync(cts.Token), cts.Token);
    }

    /// <summary>
    /// Gets or sets the cancel command to invoke when the user want to cancel the operation.
    /// </summary>
    public ICommandBase? CancelCommand
    {
        get => cancelCommand;
        set => SetValue(ref cancelCommand, value, nameof(CancelCommand), nameof(IsCancellable));
    }

    /// <summary>
    /// Gets whether this current work is cancellable.
    /// </summary>
    public bool IsCancellable => cancelCommand != null;

    /// <summary>
    /// Gets or sets whether the progresses are indeterminate.
    /// </summary>
    public bool IsIndeterminate { get { return isIndeterminate; } set { SetValue(ref isIndeterminate, value); } }

    /// <summary>
    /// Gets or sets the conditions under which the progress dialog must remain open after the work is finished.
    /// </summary>
    public KeepOpen KeepOpen { get { return keepOpen; } set { SetValue(ref keepOpen, value); } }

    /// <summary>
    /// The <see cref="LoggerViewModel"/> used to monitor <see cref="Logger"/> instances.
    /// </summary>
    public LoggerViewModel Log { get; }

    /// <summary>
    /// Gets or sets the minimum progress value.
    /// </summary>
    /// <remarks>This property is thread-safe.</remarks>
    public double Minimum
    {
        get => minimum;
        set => SetValue(ref minimum, value);
    }

    /// <summary>
    /// Gets or sets the maximum progress value.
    /// </summary>
    /// <remarks>This property is thread-safe.</remarks>
    public double Maximum
    {
        get => maximum;
        set => SetValue(ref maximum, value);
    }

    /// <summary>
    /// Gets or sets the current progress message.
    /// </summary>
    public string? ProgressMessage
    {
        get => progressMessage;
        set => SetValue(ref progressMessage, value);
    }

    /// <summary>
    /// Gets or sets the current progress value.
    /// </summary>
    /// <remarks>This property is thread-safe.</remarks>
    public double ProgressValue
    {
        get => progressValue;
        set => SetValue(ref progressValue, value);
    }

    /// <summary>
    /// Gets or sets the title of the work progress.
    /// </summary>
    public string Title
    {
        get => title;
        set => SetValue(ref title, value);
    }

    public bool WorkDone
    {
        get => workDone;
        set => SetValue(ref workDone, value);
    }

    /// <summary>
    /// Raised when the work is notified as being finished.
    /// </summary>
    public event EventHandler<WorkProgressNotificationEventArgs>? WorkFinished;

    /// <inheritdoc />
    public override void Destroy()
    {
        base.Destroy();

        try
        {
            cts.Cancel();
            resetEvent.Set();
        }
        finally
        {
            cts.Dispose();
            resetEvent.Dispose();
            semaphore.Dispose();
        }
    }

    public void NotifyWindowClosed()
    {
        if (workProgressClosed.TrySetResult())
        {
            Destroy();
        }
    }

    public void NotifyWindowWillOpen()
    {
        if (windowWillOpen)
            throw new InvalidOperationException($"This {nameof(WorkProgressViewModel)} is already associated to a progress window.");

        windowWillOpen = true;
    }

    /// <summary>
    /// Notifies that the current work is finished (or cancelled), regardless of the <see cref="ProgressValue"/> property.
    /// </summary>
    /// <param name="cancelled">Indicates whether the work has been cancelled.</param>
    /// <param name="error">Indicates whether an error occurred.</param>
    /// <returns>A task that completes when the related window has been closed if it has been opened, or immediately otherwise.</returns>
    public Task NotifyWorkFinished(bool cancelled, bool error)
    {
        WorkDone = true;

        ProgressValue = Maximum;
        ProgressMessage = cancelled ? Tr._("Operation cancelled.") : error ? Tr._("Operation failed.") : Tr._("Operation completed.");

        Log.Flush();

        if (registeredProgressStatus is not null)
        {
            registeredProgressStatus.ProgressChanged -= ProgressChanged;
            registeredProgressStatus = null;
        }

        RaiseEvent(WorkFinished);
        return windowWillOpen ? workProgressClosed.Task : Task.CompletedTask;
    }

    /// <summary>
    /// Registers an instance of the <see cref="IProgressStatus"/> interface to automatically updates the work progress.
    /// </summary>
    /// <param name="progressStatus">The instance of <see cref="IProgressStatus"/> to register.</param>
    public void RegisterProgressStatus(IProgressStatus progressStatus)
    {
        registeredProgressStatus = progressStatus;
        registeredProgressStatus.ProgressChanged += ProgressChanged;
        Minimum = 0;
        Maximum = 1;
    }

    /// <summary>
    /// Indicates whether the progress window should stay open.
    /// This method throws an exception if invoked when <see cref="WorkDone"/> is false.
    /// </summary>
    /// <returns><c>true</c> if the window should stay open, <c>false</c> otherwise.</returns>
    public bool ShouldStayOpen()
    {
        if (!WorkDone)
        {
            throw new InvalidOperationException("Invoking ShouldStayOpen before the work is finished.");
        }

        return KeepOpen switch
        {
            KeepOpen.Never => false,
            KeepOpen.OnWarningsOrErrors => Log.HasWarnings || Log.HasErrors,
            KeepOpen.OnErrors => Log.HasErrors,
            KeepOpen.Always => true,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    /// <summary>
    /// Updates the <see cref="ProgressMessage"/> and <see cref="ProgressValue"/>. The order of consecutive updates is assured.
    /// </summary>
    /// <param name="newProgressMessage">The new progress message to set.</param>
    /// <param name="newProgressValue">The new progress value to set.</param>
    /// <remarks>Uses this method to reduce overhead of frequent progress updates.</remarks>
    public void UpdateProgress(string newProgressMessage, double newProgressValue)
    {
        semaphore.Wait();
        next = (newProgressMessage, newProgressValue);
        semaphore.Release();

        resetEvent.Set();
    }

    /// <summary>
    /// Invoked when progress updates come from a registered <see cref="IProgressStatus"/>
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void ProgressChanged(object? sender, ProgressStatusEventArgs e)
    {
        // If the minimum, maximum, or the is indeterminate values have changed, we have to update them synchronously
        if (IsIndeterminate == e.HasKnownSteps || Math.Abs(Minimum - 0) > double.Epsilon || Math.Abs(Maximum - e.StepCount) > double.Epsilon)
        {
            IsIndeterminate = !e.HasKnownSteps;
            Minimum = 0;
            Maximum = e.StepCount;
            ProgressValue = e.CurrentStep;
            ProgressMessage = e.Message;
        }
        else
        {
            UpdateProgress(e.Message, e.CurrentStep);
        }
    }

    /// <summary>
    /// Helper method to raise event.
    /// </summary>
    /// <param name="handler">The event handler to raise.</param>
    private void RaiseEvent(EventHandler<WorkProgressNotificationEventArgs>? handler)
    {
        Dispatcher.Invoke(() =>
        {
            handler?.Invoke(this, new WorkProgressNotificationEventArgs(this));
        });
    }

    private async Task UpdateAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                resetEvent.WaitOne();
                if (token.IsCancellationRequested) break;

                await semaphore.WaitAsync(token);
                var (message, value) = next;
                semaphore.Release();

                // Reduce overhead of progress update
                await Dispatcher.LowPriorityInvokeAsync(() =>
                {
                    ProgressMessage = message;
                    ProgressValue = value;
                }, token);
            }
        }
        catch (OperationCanceledException) { }
    }
}
