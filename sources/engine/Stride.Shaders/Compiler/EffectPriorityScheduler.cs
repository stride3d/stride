// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xenko.Core.Collections;

namespace Xenko.Shaders.Compiler
{
    /// <summary>
    /// A <see cref="TaskScheduler"/> with control over concurrency and priority, useful with <see cref="EffectCompilerCache"/>.
    /// </summary>
    public class EffectPriorityScheduler : TaskScheduler, IDisposable
    {
        private static object lockObject = new object();
        private ThreadPriority threadPriority;
        private Thread[] threads;
        private readonly int maximumConcurrencyLevel;
        private readonly PriorityQueue<QueuedTask> taskPriorityQueue;
        private readonly System.Collections.Generic.SortedList<int, PriorityGroupScheduler> priorityGroups = new System.Collections.Generic.SortedList<int, PriorityGroupScheduler>();
        private readonly AutoResetEvent notifyQueuedTaskEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent notifyHighPriorityQueuedTaskEvent = new AutoResetEvent(false);
        private readonly ManualResetEvent notifyThreadExitEvent = new ManualResetEvent(false);
        private bool threadShouldExit;

        public EffectPriorityScheduler(ThreadPriority threadPriority, int maximumConcurrencyLevel)
        {
            if (maximumConcurrencyLevel == 0)
                throw new ArgumentOutOfRangeException("maximumConcurrencyLevel");

            this.taskPriorityQueue = new PriorityQueue<QueuedTask>(new TaskPriorityComparer());

            this.threadPriority = threadPriority;
            this.maximumConcurrencyLevel = maximumConcurrencyLevel;
        }

        public int QueuedTaskCount { get { return taskPriorityQueue.Count; } }

        /// <inheritdoc/>
        public override int MaximumConcurrencyLevel
        {
            get { return maximumConcurrencyLevel; }
        }

        /// <inheritdoc/>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        /// <inheritdoc/>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            // Note: used by debugger only
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the or create a task scheduler for the given priority.
        /// </summary>
        /// <param name="priority">The priority.</param>
        /// <returns></returns>
        public TaskScheduler GetOrCreatePriorityGroup(int priority)
        {
            lock (priorityGroups)
            {
                PriorityGroupScheduler result;
                if (!priorityGroups.TryGetValue(priority, out result))
                {
                    priorityGroups.Add(priority, result = new PriorityGroupScheduler(this, priority));
                }

                return result;
            }
        }

        /// <inheritdoc/>
        protected override void QueueTask(Task task)
        {
            QueueTask(new QueuedTask(task, null));
        }

        private void QueueTask(QueuedTask task)
        {
            // Add task to priority queue
            lock (taskPriorityQueue)
            {
                taskPriorityQueue.Enqueue(task);
            }
            if (task.Scheduler != null && task.Scheduler.Priority < 0)
                notifyHighPriorityQueuedTaskEvent.Set();
            else
                notifyQueuedTaskEvent.Set();

            // If necessary, create threads
            if (threads == null)
            {
                lock (lockObject)
                {
                    if (threads == null)
                    {
                        var waitHandles = new WaitHandle[] { notifyThreadExitEvent, notifyHighPriorityQueuedTaskEvent, notifyQueuedTaskEvent };
                        var waitHandlesHighPriorityOnly = new WaitHandle[] { notifyThreadExitEvent, notifyHighPriorityQueuedTaskEvent };
                        threads = new Thread[maximumConcurrencyLevel];
                        for (int i = 0; i < maximumConcurrencyLevel; i++)
                        {
                            int threadIndex = i;
                            threads[i] = new Thread(() =>
                            {
                                while (!threadShouldExit)
                                {
                                    // TODO: ResetEvent, and exit signal
                                    var t = default(QueuedTask);
                                    lock (taskPriorityQueue)
                                    {
                                        if (!taskPriorityQueue.Empty)
                                        {
                                            t = taskPriorityQueue.Dequeue();
                                        }
                                    }

                                    if (t.Task != null)
                                    {
                                        // High priority task (<0) gets an above normal thread priority
                                        var priority = t.Scheduler?.Priority ?? 0;
                                        if (priority < 0)
                                            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

                                        if (t.Scheduler != null)
                                            t.Scheduler.TryExecuteTaskInternal(t.Task);
                                        else
                                            TryExecuteTask(t.Task);

                                        if (priority < 0)
                                            Thread.CurrentThread.Priority = ThreadPriority.Normal;
                                    }
                                    else
                                    {
                                        // If more than one thread, one will be dedicated to high-priority tasks
                                        if (threadIndex == 0 && maximumConcurrencyLevel > 1)
                                            WaitHandle.WaitAny(waitHandlesHighPriorityOnly);
                                        else
                                            WaitHandle.WaitAny(waitHandles);
                                    }
                                }
                            })
                            {
                                Name = string.Format("PriorityScheduler: {0}", i),
                                Priority = threadPriority,
                                IsBackground = true,
                            };
                            threads[i].Start();
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            lock (lockObject)
            {
                if (threads != null)
                {
                    // Notify all threads that we're done
                    threadShouldExit = true;
                    notifyThreadExitEvent.Set();

                    // Should we wait for threads to finish (or maybe we should just bail out?)
                    //foreach (var thread in threads)
                    //{
                    //    thread.Join();
                    //}
                }
            }
        }

        /// <summary>
        /// Internal task scheduler for a specific priority level.
        /// </summary>
        class PriorityGroupScheduler : TaskScheduler
        {
            private readonly EffectPriorityScheduler parent;

            public int Priority { get; }

            public PriorityGroupScheduler(EffectPriorityScheduler parent, int priority)
            {
                this.parent = parent;
                this.Priority = priority;
            }

            protected override void QueueTask(Task task)
            {
                // Enqueue task
                parent.QueueTask(new QueuedTask(task, this));
            }

            internal bool TryExecuteTaskInternal(Task task)
            {
                return TryExecuteTask(task);
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return false;
            }

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                // Note: used by debugger only
                throw new NotImplementedException();
            }
        }

        class TaskPriorityComparer : Comparer<QueuedTask>
        {
            public override int Compare(QueuedTask x, QueuedTask y)
            {
                var priorityX = x.Scheduler != null ? x.Scheduler.Priority : 0;
                var priorityY = y.Scheduler != null ? y.Scheduler.Priority : 0;
                return priorityX.CompareTo(priorityY);
            }
        }

        struct QueuedTask
        {
            public QueuedTask(Task task, PriorityGroupScheduler scheduler)
            {
                Task = task;
                Scheduler = scheduler;
            }

            public Task Task;
            public PriorityGroupScheduler Scheduler;
        }
    }
}
