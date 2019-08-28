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
        private const int MaxIdleTimeInMS = 5000;
        private readonly long MaxIdleTimeTS = (long)((double)Stopwatch.Frequency / 1000 * MaxIdleTimeInMS);

        public static readonly ThreadPool Instance = new ThreadPool();

        private readonly Action<object> cachedTaskLoop;

        private readonly int maxThreadCount = Environment.ProcessorCount + 2;
        private readonly Queue<Action> workItems = new Queue<Action>();
        private readonly ManualResetEvent workAvailable = new ManualResetEvent(false);

        private SpinLock spinLock = new SpinLock();
        private int busyCount;
        private int aliveCount;

        public ThreadPool()
        {
            // Cache delegate to avoid pointless allocation
            cachedTaskLoop = (o) => ProcessWorkItems();
        }

        public void QueueWorkItem([NotNull] [Pooled] Action workItem)
        {
            bool lockTaken = false;
            bool startNewTask = false;
            try
            {
                spinLock.Enter(ref lockTaken);

                PooledDelegateHelper.AddReference(workItem);
                workItems.Enqueue(workItem);
                workAvailable.Set();

                // We're only locking when potentially increasing aliveCount as we
                // don't want to go above our maximum amount of threads.
                int curBusyCount = Interlocked.CompareExchange(ref busyCount, 0, 0);
                int curAliveCount = Interlocked.CompareExchange(ref aliveCount, 0, 0);
                if (curBusyCount + 1 >= curAliveCount && curAliveCount < maxThreadCount)
                {
                    // Start threads as busy otherwise only one thread will be created 
                    // when calling this function multiple times in a row
                    Interlocked.Increment(ref busyCount);
                    Interlocked.Increment(ref aliveCount);
                    startNewTask = true;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    spinLock.Exit(true);
                }
            }
            // No point in wasting spins on the lock while creating the task
            if (startNewTask)
            {
                Task.Factory.StartNew(cachedTaskLoop, null, TaskCreationOptions.LongRunning);
            }
        }

        private void ProcessWorkItems()
        {
            Interlocked.Decrement(ref busyCount);
            try
            {
                long lastWorkTS = Stopwatch.GetTimestamp();
                while (true)
                {
                    Action workItem = null;
                    bool lockTaken = false;
                    try
                    {
                        spinLock.Enter(ref lockTaken);
                        if (workItems.Count > 0)
                        {
                            workItem = workItems.Dequeue();
                            if (workItems.Count == 0)
                            {
                                workAvailable.Reset();
                            }
                        }
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            spinLock.Exit(true);
                        }
                    }
                    
                    if (workItem == null)
                    {
                        bool idleForTooLong = Stopwatch.GetTimestamp() - lastWorkTS > MaxIdleTimeTS;
                        // Wait for another work item to be (potentially) available
                        if (idleForTooLong || workAvailable.WaitOne(MaxIdleTimeInMS) == false)
                        {
                            // No work given in the last MaxIdleTimeTS, close this task
                            return;
                        }
                    }
                    else
                    {
                        Interlocked.Increment(ref busyCount);
                        try
                        {
                            workItem();
                        }
                        // Let exceptions fall into unhandled as we don't have any
                        // good mechanisms to pass it elegantly over to user-land yet
                        finally
                        {
                            Interlocked.Decrement(ref busyCount);
                        }
                        lastWorkTS = Stopwatch.GetTimestamp();
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref aliveCount);
            }
        }
    }
}
