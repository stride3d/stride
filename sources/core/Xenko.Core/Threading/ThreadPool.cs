// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;
using Xenko.Core.Annotations;

namespace Xenko.Core.Threading
{
    /// <summary>
    /// Thread pool for scheduling actions.
    /// </summary>
    internal class ThreadPool
    {
        public static readonly ThreadPool Instance = new ThreadPool();
        
        /// <summary>
        /// The amount of threads sharing the same semaphore and work collection.
        /// Decreasing this value provides better performances for very frequent short work
        /// but might create workload imbalance.
        /// Anything above 4 leads to very high amount of contention when enqueueing and de-queueing work.
        /// </summary>
        private const int ThreadPerGroup = 4;
        private const int MaxIdleTimeInMS = 5000;
        
        // Cache delegate to avoid pointless allocation
        private static readonly ParameterizedThreadStart cachedTaskLoop = ProcessWorkItems;
        
        private readonly ThreadGroup[] groups;
        private int nextBucket;
        
        public ThreadPool()
        {
            // Inconsistent performances when more threads are trying to be woken up than there is processors
            int maxTotalThreads = Environment.ProcessorCount <= 1 ? 1 : Environment.ProcessorCount - 1;
            groups = new ThreadGroup[maxTotalThreads / ThreadPerGroup + (maxTotalThreads % ThreadPerGroup > 0 ? 1 : 0)];
            int allocatedThreads = maxTotalThreads;
            for( int i = 0; i < groups.Length; i++ )
            {
                groups[i] = new ThreadGroup(allocatedThreads < ThreadPerGroup ? allocatedThreads : ThreadPerGroup);
                allocatedThreads -= ThreadPerGroup;
            }
        }

        public void QueueWorkItem([NotNull] [Pooled] Action workItem)
        {
            PooledDelegateHelper.AddReference(workItem);
            
            // Select next bucket to push work to, this is unbiased which might lead to
            // some work completing later than others, expected when doing any kind of
            // multi-threaded logic.
            // This design is far faster when the workload is properly distributed.
            // See 'ThreadPerGroup' for more info.
            ThreadGroup group = groups[Interlocked.Increment(ref nextBucket) % groups.Length];
            
            var node = new WorkNode(workItem);
            var previousNode = Interlocked.Exchange(ref group.WorkCollection, node);
            Interlocked.Exchange(ref node.previous, previousNode);
            Interlocked.Exchange(ref node.previousIsValid, 1);
            
            // Wake up / let one thread through
            group.Semaphore.Release(1);
            
            int alive;
            while((alive = Volatile.Read(ref group.AliveCount)) < group.MaxCount)
            {
                if(Interlocked.CompareExchange(ref group.AliveCount, alive + 1, alive) != alive)
                {
                    continue; // Compare increment failed, try again
                }
                // Spawn/re-spawn one thread per job until we reach max
                new Thread(cachedTaskLoop)
                {
                    Name = $"{GetType().FullName} thread",
                    IsBackground = true,
                    Priority = ThreadPriority.Highest
                }.Start(group);
                break;
            }
        }

        private static void ProcessWorkItems(object groupObj)
        {
            ThreadGroup group = (ThreadGroup)groupObj;
            try
            {
                SpinWait sw = new SpinWait();
                while (true)
                {
                    WorkNode n;
                    while((n = Volatile.Read(ref group.WorkCollection)) != null)
                    {
                        // Fetch latest (existing) enqueued work
                        
                        while(Volatile.Read(ref n.previousIsValid) != 1)
                        {
                            // wait for 'n.previous' to be set to a valid ref
                        }

                        if(Interlocked.CompareExchange(ref group.WorkCollection, n.previous, n) != n)
                        {
                            continue; // Failed to remove this work from the collection as it wasn't the latest, try again
                        }
                        
                        // Work removed from the collection, process it
                        try
                        {
                            n.work();
                        }
                        // Let exceptions fall into unhandled as we don't have any
                        // good mechanisms to pass it elegantly over to user-land yet
                        finally
                        {
                            PooledDelegateHelper.Release(n.work);
                        }
                    }

                    if(sw.NextSpinWillYield)
                    {
                        // Go back to system once we spun for long enough
                        
                        // Wait for another work item to be (potentially) available
                        if (group.Semaphore.Wait(MaxIdleTimeInMS) == false)
                        {
                            // No work given in the given time, close this thread
                            return;
                        }
                        // We received work, loop back and reset amount of spins required before returning to system
                        sw = new SpinWait();
                    }
                    else
                    {
                        // Spin a bunch to catch potential incoming work
                        sw.SpinOnce();
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref group.AliveCount);
            }
        }
        
        class WorkNode
        {
            public Action work;
            public WorkNode previous;
            public int previousIsValid;
            
            public WorkNode(Action workParam)
            {
                work = workParam;
            }
        }
        
        class ThreadGroup
        {
            public readonly int MaxCount;
            public int AliveCount;
            public readonly SemaphoreSlim Semaphore = new SemaphoreSlim(0, int.MaxValue);
            public WorkNode WorkCollection = null;
            public ThreadGroup(int maxCountParameter)
            {
                MaxCount = maxCountParameter;
            }
        }
    }
}