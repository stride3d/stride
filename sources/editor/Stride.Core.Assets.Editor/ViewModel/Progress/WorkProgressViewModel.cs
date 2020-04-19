// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.ViewModel.Progress
{
    /// <summary>
    /// A thread-safe view model used to display progress and log of a background operation.
    /// </summary>
    public class WorkProgressViewModel : DispatcherViewModel
    {
        private readonly object asyncUpdateLock = new object();
        private string title = "Work in progress...";
        private string progressMessage;
        private bool isIndeterminate;
        private double minimum;
        private double maximum;
        private double progressValue;
        private bool isCancellable;
        private bool isCancelled;
        private bool hasFailed;
        private KeepOpen keepOpen;
        private bool workDone;
        private IProgressStatus registeredProgressStatus;
        private bool progressStatusUpdateAsync;
        private string nextProgressMessage;
        private double nextProgressValue;
        private bool asyncUpdate;
        private bool windowWillOpen;
        private readonly TaskCompletionSource<int> workProgressClosed = new TaskCompletionSource<int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkProgressViewModel"/> class with a single logger.
        /// </summary>
        /// <param name="serviceProvider">A service provider that can provide a <see cref="IDispatcherService"/> to use for this view model.</param>
        /// <param name="logger">The logger to monitor.</param>
        public WorkProgressViewModel(IViewModelServiceProvider serviceProvider, Logger logger)
            : base(serviceProvider)
        {
            Log = new LoggerViewModel(serviceProvider, logger);
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
        }

        /// <summary>
        /// Gets or sets the title of the work progress.
        /// </summary>
        public string Title { get { return title; } set { SetValue(ref title, value); } }

        /// <summary>
        /// Gets or sets the current progress message.
        /// </summary>
        /// <remarks>This property is thread-safe.</remarks>
        public string ProgressMessage { get { return progressMessage; } set { SetValue(ref progressMessage, value); } }

        /// <summary>
        /// Gets or sets whether the progresses are indeterminate.
        /// </summary>
        public bool IsIndeterminate { get { return isIndeterminate; } set { SetValue(ref isIndeterminate, value); } }

        /// <summary>
        /// Gets or sets the minimum progress value.
        /// </summary>
        /// <remarks>This property is thread-safe.</remarks>
        public double Minimum { get { return minimum; } set { SetValue(ref minimum, value); } }

        /// <summary>
        /// Gets or sets the maximum progress value.
        /// </summary>
        /// <remarks>This property is thread-safe.</remarks>
        public double Maximum { get { return maximum; } set { SetValue(ref maximum, value); } }

        /// <summary>
        /// Gets or sets the current progress value.
        /// </summary>
        /// <remarks>This property is thread-safe.</remarks>
        public double ProgressValue { get { return progressValue; } set { SetValue(ref progressValue, value, () => RaiseEvent(ProgressValueChanged)); } }

        /// <summary>
        /// Gets or sets whether this current work is cancellable.
        /// </summary>
        public bool IsCancellable { get { return isCancellable; } set { SetValue(ref isCancellable, value); } }

        /// <summary>
        /// Gets whether this current work has been cancelled.
        /// </summary>
        public bool IsCancelled { get { return isCancelled; } private set { SetValue(ref isCancelled, value); } }
        
        /// <summary>
        /// Gets whether this current work has failed.
        /// </summary>
        public bool HasFailed { get { return hasFailed; } private set { SetValue(ref hasFailed, value); } }

        /// <summary>
        /// Gets or sets the conditions under which the progress dialog must remain open after the work is finished.
        /// </summary>
        /// <remarks>This property is thread-safe.</remarks>
        public KeepOpen KeepOpen { get { return keepOpen; } set { SetValue(ref keepOpen, value); } }

        /// <summary>
        /// Gets whether the work is finished.
        /// </summary>
        /// <remarks>This property is thread-safe.</remarks>
        public bool WorkDone { get { return workDone; } private set { SetValue(ref workDone, value); } }

        /// <summary>
        /// Gets the <see cref="LoggerViewModel"/> used to monitor <see cref="Logger"/> instances.
        /// </summary>
        public LoggerViewModel Log { get; }

        /// <summary>
        /// Gets or sets the cancel command to invoke when the user want to cancel the operation.
        /// </summary>
        public ICommandBase CancelCommand { get; set; }

        /// <summary>
        /// Raised when the value of the <see cref="ProgressValue"/> property changes.
        /// </summary>
        public event EventHandler<WorkProgressNotificationEventArgs> ProgressValueChanged;

        /// <summary>
        /// Raised when the work is notified as being finished.
        /// </summary>
        public event EventHandler<WorkProgressNotificationEventArgs> WorkFinished;

        /// <summary>
        /// Updates the <see cref="ProgressMessage"/> and <see cref="ProgressValue"/> asynchronously. The order of consecutive updates is assured.
        /// </summary>
        /// <param name="newProgressMessage">The new progress message to set.</param>
        /// <param name="newProgressValue">The new progress value to set.</param>
        /// <remarks>Uses this method to reduce overhead of frequent progress updates.</remarks>
        public void UpdateProgressAsync(string newProgressMessage, double newProgressValue)
        {
            lock (asyncUpdateLock)
            {
                nextProgressMessage = newProgressMessage;
                nextProgressValue = newProgressValue;
                asyncUpdate = true;
                Dispatcher.InvokeAsync(AsyncUpdate);
            }
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
            IsCancelled = cancelled;
            HasFailed = !cancelled && error;
            Log.Flush();
            Dispatcher.Invoke(() => RaiseEvent(WorkFinished));
            if (registeredProgressStatus != null)
            {
                registeredProgressStatus.ProgressChanged -= ProgressChanged;
                registeredProgressStatus = null;
            }
            return windowWillOpen ? workProgressClosed.Task : Task.CompletedTask;
        }

        /// <summary>
        /// Registers an instance of the <see cref="IProgressStatus"/> interface to automatically updates the work progress.
        /// </summary>
        /// <param name="progressStatus">The instance of <see cref="IProgressStatus"/> to register.</param>
        /// <param name="updateAsync">Indicate whether progress update must be processed asynchronously via the <see cref="UpdateProgressAsync"/> method.</param>
        public void RegisterProgressStatus(IProgressStatus progressStatus, bool updateAsync)
        {
            registeredProgressStatus = progressStatus;
            progressStatusUpdateAsync = updateAsync;
            progressStatus.ProgressChanged += ProgressChanged;
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

            switch (KeepOpen)
            {
                case KeepOpen.Never:
                    return false;
                case KeepOpen.OnWarningsOrErrors:
                    return Log.HasWarnings || Log.HasErrors;
                case KeepOpen.OnErrors:
                    return Log.HasErrors;
                case KeepOpen.Always:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal void NotifyWindowWillOpen()
        {
            if (windowWillOpen)
                throw new InvalidOperationException("This WorkProgressViewModel is already associated to a progress window");

            windowWillOpen = true;
        }

        internal void NotifyWindowClosed()
        {
            workProgressClosed.TrySetResult(0);
        }

        /// <summary>
        /// Helper method to raise event.
        /// </summary>
        /// <param name="handler">The event handler to raise.</param>
        private void RaiseEvent(EventHandler<WorkProgressNotificationEventArgs> handler)
        {
            handler?.Invoke(this, new WorkProgressNotificationEventArgs(this));
        }

        /// <summary>
        /// Invoked when progress updates come from a registered <see cref="IProgressStatus"/>
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ProgressChanged(object sender, ProgressStatusEventArgs e)
        {
            // If the minimum, maximum, or the is indeterminate values have changed, we have to update them synchronously
            if (IsIndeterminate == e.HasKnownSteps || Math.Abs(Minimum - 0) > double.Epsilon || Math.Abs(Maximum - e.StepCount) > double.Epsilon)
            {
                Dispatcher.Invoke(() =>
                    {
                        IsIndeterminate = !e.HasKnownSteps;
                        Minimum = 0;
                        Maximum = e.StepCount;
                        ProgressValue = e.CurrentStep;
                        ProgressMessage = e.Message;
                    });
            }
            else
            {
                if (progressStatusUpdateAsync)
                {
                    UpdateProgressAsync(e.Message, e.CurrentStep);
                }
                else
                {
                    Dispatcher.Invoke(() =>
                        {
                            ProgressValue = e.CurrentStep;
                            ProgressMessage = e.Message;
                        });
                }
            }
        }

        /// <summary>
        /// Invoked to update progress asynchronously via <see cref="UpdateProgressAsync"/>
        /// </summary>
        private void AsyncUpdate()
        {
            if (asyncUpdate)
            {
                lock (asyncUpdateLock)
                {
                    ProgressMessage = nextProgressMessage;
                    ProgressValue = nextProgressValue;
                    asyncUpdate = false;
                }
            }
        }
    }
}
