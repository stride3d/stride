// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using Xenko.Core.Annotations;

namespace Xenko.Core.Threading
{
    /// <summary>
    /// Thread pool for scheduling actions.
    /// </summary>
    /// <remarks>
    /// Base on Stephen Toub's ManagedThreadPool
    /// </remarks>
    internal class ThreadPool
    {
        public static readonly ThreadPool Instance = new ThreadPool();

        private readonly int maxThreadCount = Environment.ProcessorCount + 2;
        private readonly Queue<Action> workItems = new Queue<Action>();
        private readonly ManualResetEvent workAvailable = new ManualResetEvent(false);

        private object _lock = new object();
        volatile private int activeThreadCount;
        volatile private int aliveThreadCount;

        public void QueueWorkItem([NotNull] [Pooled] Action workItem)
        {
            lock (_lock)
            {
                PooledDelegateHelper.AddReference(workItem);
                workItems.Enqueue(workItem);
                workAvailable.Set();
                if (activeThreadCount + 1 >= aliveThreadCount && aliveThreadCount < maxThreadCount)
                {
                    Interlocked.Increment(ref aliveThreadCount);
                    Task.Factory.StartNew(ProcessWorkItems, TaskCreationOptions.LongRunning);
                }
            }
        }

        private void ProcessWorkItems(object state)
        {
            try
            {
                long lastWork = Stopwatch.GetTimestamp();
                TimeSpan maxIdleTime = TimeSpan.FromSeconds(5);
                do
                {
                    Action workItem = null;

                    lock (_lock)
                    {
                        if (workItems.Count > 0)
                        {
                            workItem = workItems.Dequeue();
                            if (workItems.Count == 0)
                                workAvailable.Reset();
                        }
                    }

                    if (workItem != null)
                    {
                        Interlocked.Increment(ref activeThreadCount);
                        try
                        {
                            workItem.Invoke();
                        }
                        catch (Exception)
                        {
                            // Ignoring Exception
                        }
                        Interlocked.Decrement(ref activeThreadCount);
                        PooledDelegateHelper.Release(workItem);
                        lastWork = Stopwatch.GetTimestamp();
                    }

                    // Wait for another work item to be (potentially) available
                    workAvailable.WaitOne(maxIdleTime);
                } while (Utilities.ConvertRawToTimestamp(Stopwatch.GetTimestamp() - lastWork) < maxIdleTime);
            }
            finally
            {
                Interlocked.Decrement(ref aliveThreadCount);
            }
        }
    }
}
