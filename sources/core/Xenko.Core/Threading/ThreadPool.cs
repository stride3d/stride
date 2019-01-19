// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
        private const long MaxIdleTimeInTicks = 5 * TimeSpan.TicksPerSecond;

        public static readonly ThreadPool Instance = new ThreadPool();

        private readonly int maxThreadCount = Environment.ProcessorCount + 2;
        private readonly Queue<Action> workItems = new Queue<Action>();
        private readonly ManualResetEvent workAvailable = new ManualResetEvent(false);

        private SpinLock spinLock = new SpinLock();
        private int workingCount;
        /// <summary> Usage only within <see cref="spinLock"/> </summary>
        private int aliveCount;

        public void QueueWorkItem([NotNull] [Pooled] Action workItem)
        {
            var lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);

                PooledDelegateHelper.AddReference(workItem);
                workItems.Enqueue(workItem);
                workAvailable.Set();

                var curWorkingCount = Interlocked.CompareExchange(ref workingCount, 0, 0);
                if (curWorkingCount + 1 >= aliveCount && aliveCount < maxThreadCount)
                {
                    aliveCount++;
                    Task.Factory.StartNew(ProcessWorkItems, TaskCreationOptions.LongRunning);
                }
            }
            finally
            {
                if (lockTaken)
                    spinLock.Exit(true);
            }
        }

        private void ProcessWorkItems(object state)
        {
            long lastWork = Stopwatch.GetTimestamp();
            TimeSpan maxIdleTime = TimeSpan.FromTicks(MaxIdleTimeInTicks);
            while(true)
            {
                Action workItem = null;
                var lockTaken = false;
                bool idleForTooLong = Utilities.ConvertRawToTimestamp(Stopwatch.GetTimestamp() - lastWork) < maxIdleTime;
                try
                {
                    spinLock.Enter(ref lockTaken);

                    if (workItems.Count > 0)
                    {
                        workItem = workItems.Dequeue();
                        if (workItems.Count == 0)
                            workAvailable.Reset();
                    }
                    else if (idleForTooLong)
                    {
                        aliveCount--;
                        return;
                    }
                }
                finally
                {
                    if (lockTaken)
                        spinLock.Exit(true);
                }

                if (workItem != null)
                {
                    Interlocked.Increment(ref workingCount);
                    try
                    {
                        workItem.Invoke();
                    }
                    catch (Exception)
                    {
                        // Ignoring Exception
                    }
                    Interlocked.Decrement(ref workingCount);
                    PooledDelegateHelper.Release(workItem);
                    lastWork = Stopwatch.GetTimestamp();
                }

                // Wait for another work item to be (potentially) available
                workAvailable.WaitOne(maxIdleTime);
            }
        }
    }
}
