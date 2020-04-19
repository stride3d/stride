// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.Components.Status
{
    public class StatusViewModel : DispatcherViewModel
    {
        private const int DiscardableStatus = -1;
        private readonly SortedList<int, string> statusList = new SortedList<int, string>();
        private readonly SortedList<int, JobProgressViewModel> jobList = new SortedList<int, JobProgressViewModel>();
        private readonly object lockObject = new object();
        private int currentToken;
        private string currentStatus;
        private JobProgressViewModel currentJob;

        public StatusViewModel(IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public string CurrentStatus { get { return currentStatus; } set { SetValue(ref currentStatus, value); } }

        public JobProgressViewModel CurrentJob { get { return currentJob; } set { SetValue(ref currentJob, value); } }

        public int NotifyBackgroundJobStarted(string message, JobPriority priority, int count = -1)
        {
            return Dispatcher.Invoke(() =>
            {
                lock (lockObject)
                {
                    var job = new JobProgressViewModel(ServiceProvider, message, priority, count);
                    jobList.Add(++currentToken, job);
                    if (CurrentJob == null || CurrentJob.Priority <= priority)
                        CurrentJob = job;
                    return currentToken;
                }
            });
        }

        public void NotifyBackgroundJobProgress(int token, int progress, bool isAbsolute)
        {
            Dispatcher.Invoke(() =>
            {
                lock (lockObject)
                {
                    JobProgressViewModel job;
                    if (jobList.TryGetValue(token, out job))
                    {
                        if (isAbsolute)
                            jobList[token].Current = progress;
                        else
                            jobList[token].Current += progress;
                    }
                }
            });
        }

        public void NotifyBackgroundJobFinished(int token)
        {
            Dispatcher.Invoke(() =>
            {
                lock (lockObject)
                {
                    JobProgressViewModel job;
                    if (jobList.TryGetValue(token, out job))
                    {
                        jobList.Remove(token);
                        if (CurrentJob == job)
                        {
                            job = null;
                            foreach (JobPriority priority in Enum.GetValues(typeof(JobPriority)).Cast<JobPriority>().Reverse())
                            {
                                var nextJob = jobList.LastOrDefault(x => x.Value.Priority == priority).Value;
                                if (nextJob != null)
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

        public int PushDiscardableStatus(string message)
        {
            return Dispatcher.Invoke(() =>
            {
                lock (lockObject)
                {
                    statusList[-1] = message;
                    CurrentStatus = message;
                    return currentToken;
                }
            });
        }

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
}
