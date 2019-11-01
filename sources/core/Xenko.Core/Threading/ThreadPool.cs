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
        
        private const int MaxIdleTimeInMS = 5000;
        private readonly ThreadStart cachedTaskLoop;
        
        // Inconsistent performances when more threads are trying to be woken up than there is processors
        private readonly int maxThreadCount = Environment.ProcessorCount < 2 ? 1 : Environment.ProcessorCount - 1;
        private int aliveCount;
        
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(0, int.MaxValue);
        
        private WorkNode workCollection = null;
        
        public ThreadPool()
        {
            // Cache delegate to avoid pointless allocation
            cachedTaskLoop = ProcessWorkItems;
        }

        public void QueueWorkItem([NotNull] [Pooled] Action workItem)
        {
            PooledDelegateHelper.AddReference(workItem);
            
            var node = new WorkNode(workItem);
            var previousNode = Interlocked.Exchange(ref workCollection, node);
            Interlocked.Exchange(ref node.previous, previousNode);
            Interlocked.Exchange(ref node.previousIsValid, 1);
            
            semaphore.Release(1);
            
            int alive;
            while((alive = Volatile.Read(ref aliveCount)) < maxThreadCount)
            {
                if(Interlocked.CompareExchange(ref aliveCount, alive + 1, alive) != alive)
                {
                    continue; // Compare increment failed, try again
                }
                // Spawn/re-spawn one thread per job until we reach max
                new Thread(cachedTaskLoop)
                {
                    Name = $"{GetType().FullName} thread",
                    IsBackground = true,
                    Priority = ThreadPriority.Highest
                }.Start();
                break;
            }
        }

        private void ProcessWorkItems()
        {
            try
            {
                SpinWait sw = new SpinWait();
                while (true)
                {
                    WorkNode n;
                    while((n = Volatile.Read(ref workCollection)) != null)
                    {
                        // Fetch latest (existing) enqueued work
                        
                        while(Volatile.Read(ref n.previousIsValid) != 1)
                        {
                            // wait for 'n.previous' to be set to a valid ref
                        }
                        
                        if(Interlocked.CompareExchange(ref workCollection, n.previous, n) != n)
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
                        // Go back to system once we spun for long enough and wait for another work item to be (potentially) available
                        if (semaphore.Wait(MaxIdleTimeInMS) == false)
                        {
                            // No work given in the last MaxIdleTimeTS, close this thread
                            return;
                        }
                        // We received work, loop back and reset amount of spins required before yielding
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
                Interlocked.Decrement(ref aliveCount);
            }
        }
        
        class WorkNode
        {
            public Action work;
            public WorkNode previous;
            public int previousIsValid;
            
            public WorkNode( Action workParam )
            {
                work = workParam;
            }
        }
    }
}