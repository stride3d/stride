// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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

        private readonly ThreadStart cachedTaskLoop;

        private readonly int maxThreadCount = Environment.ProcessorCount + 2;
        private readonly Queue<Action> workItems = new Queue<Action>();
        private readonly SemaphoreSlim workAvailable = new SemaphoreSlim(0, int.MaxValue);

        private SpinLock spinLock = new SpinLock();
        private int busyCount;
        private int aliveCount;

        public ThreadPool()
        {
            // Cache delegate to avoid pointless allocation
            cachedTaskLoop = ProcessWorkItems;
        }

        public void QueueWorkItem([NotNull] [Pooled] Action workItem)
        {
            PooledDelegateHelper.AddReference(workItem);

            bool taken = false;
            try
            {
                spinLock.Enter(ref taken);
                workItems.Enqueue(workItem);
            }
            finally
            {
                if( taken )
                {
                    spinLock.Exit();
                }
            }
            workAvailable.Release(1);

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
                new Thread(cachedTaskLoop)
                {
                    Name = $"{GetType().FullName} thread",
                    IsBackground = true,
                }.Start();
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
                    bool idleForTooLong = Stopwatch.GetTimestamp() - lastWorkTS > MaxIdleTimeTS;
                    // Wait for another work item to be available
                    if (idleForTooLong || workAvailable.Wait(MaxIdleTimeInMS) == false)
                    {
                        // No work given in the last MaxIdleTimeTS, close this task
                        return;
                    }
                    
                    bool taken = false;
                    Action workItem;
                    try
                    {
                        spinLock.Enter(ref taken);
                        // Semaphore and logic guarantees that at least one item is dequeue-able
                        workItem = workItems.Dequeue();
                    }
                    finally
                    {
                        if(taken)
                        {
                            spinLock.Exit();
                        }
                    }
                    
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
                    PooledDelegateHelper.Release(workItem);
                    lastWorkTS = Stopwatch.GetTimestamp();
                }
            }
            finally
            {
                Interlocked.Decrement(ref aliveCount);
            }
        }
    }
}
