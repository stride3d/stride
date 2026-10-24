// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.Status;

public sealed class StatusViewModel : DispatcherViewModel
{
    private const int DiscardableStatus = -1;

    private JobProgressViewModel? currentJob;
    private string? currentStatus;
    private int currentToken;

    private readonly SortedList<int, JobProgressViewModel> jobList = [];
    private readonly SortedList<int, string> statusList = [];
    private readonly Lock lockObject = new();

    public StatusViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    public JobProgressViewModel? CurrentJob
    {
        get => currentJob;
        set => SetValue(ref currentJob, value);
    }

    public string? CurrentStatus
    {
        get => currentStatus;
        set => SetValue(ref currentStatus, value);
    }

    public void NotifyBackgroundJobFinished(int token)
    {
        Dispatcher.Invoke(() =>
        {
            lock (lockObject)
            {
                if (jobList.TryGetValue(token, out var job))
                {
                    jobList.Remove(token);
                    if (CurrentJob == job)
                    {
                        job = null;
                        foreach (var priority in Enum.GetValues<JobPriority>().Reverse())
                        {
                            var nextJob = jobList.LastOrDefault(x => x.Value.Priority == priority).Value;
                            if (nextJob is not null)
                            {
                                job = nextJob;
                            }
                        }
                        CurrentJob = job;
                    }
                }
            }
        });
    }

    public void NotifyBackgroundJobProgress(int token, int progress, bool isAbsolute)
    {
        Dispatcher.Invoke(() =>
        {
            lock (lockObject)
            {
                if (jobList.TryGetValue(token, out _))
                {
                    if (isAbsolute)
                        jobList[token].Current = progress;
                    else
                        jobList[token].Current += progress;
                }
            }
        });
    }

    public int NotifyBackgroundJobStarted(string message, JobPriority priority, int count = -1)
    {
        return Dispatcher.Invoke(() =>
        {
            lock (lockObject)
            {
                var job = new JobProgressViewModel(ServiceProvider, message, priority, count);
                jobList.Add(++currentToken, job);
                if (CurrentJob is null || CurrentJob.Priority <= priority)
                    CurrentJob = job;
                return currentToken;
            }
        });
    }

    public void PopStatus(int token)
    {
        Dispatcher.Invoke(() =>
        {
            lock (lockObject)
            {
                statusList.Remove(token);
                CurrentStatus = statusList.LastOrDefault().Value;
            }
        });
    }

    public int PushStatus(string message)
    {
        return Dispatcher.Invoke(() =>
        {
            lock (lockObject)
            {
                statusList.Add(++currentToken, message);
                CurrentStatus = message;
                return currentToken;
            }
        });
    }

    /// <summary>
    /// Shows a status that the next <see cref="PushDiscardableStatus"/> replaces and <see cref="DiscardStatus"/> removes, such as a hover hint.
    /// </summary>
    public int PushDiscardableStatus(string message)
    {
        return Dispatcher.Invoke(() =>
        {
            lock (lockObject)
            {
                statusList[DiscardableStatus] = message;
                CurrentStatus = message;
                return currentToken;
            }
        });
    }

    /// <summary>
    /// Removes the status shown by <see cref="PushDiscardableStatus"/>, if any.
    /// </summary>
    public void DiscardStatus()
    {
        Dispatcher.Invoke(() =>
        {
            lock (lockObject)
            {
                if (statusList.Remove(DiscardableStatus))
                    CurrentStatus = statusList.LastOrDefault().Value;
            }
        });
    }
}
