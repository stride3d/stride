// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Xenko.Core.Annotations;

namespace Xenko.Core.Threading
{
    /// <summary>
    /// Thread pool for scheduling actions.
    /// </summary>
    /// <remarks>
    /// Based on dotnet's ThreadPool and helios-io's DedicatedThreadPool
    /// </remarks>
    internal class ThreadPool
    {
        public static readonly ThreadPool Instance = new ThreadPool();
        
        private static readonly int ProcessorCount = Environment.ProcessorCount;
        
        private const int MaxIdleTimeInMS = 5000;
        private readonly long maxIdleTimeTS = (long)((double)Stopwatch.Frequency / 1000 * MaxIdleTimeInMS);
        private readonly ThreadStart cachedTaskLoop;

        private readonly int maxThreadCount = ProcessorCount + 2;
        private int busyCount;
        private int aliveCount;
        
        private readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();
        private int outstandingRequests;
        
        private readonly UnfairSemaphore semaphore = new UnfairSemaphore();
        
        public ThreadPool()
        {
            // Cache delegate to avoid pointless allocation
            cachedTaskLoop = ProcessWorkItems;
        }

        public void QueueWorkItem([NotNull] [Pooled] Action workItem)
        {
            PooledDelegateHelper.AddReference(workItem);
            queue.Enqueue(workItem);
                
            // There is a double counter here (_outstandingRequest and _semaphore)
            // Unfair semaphore does not support value bigger than short.MaxValue,
            // tring to Release more than short.MaxValue could fail miserably.

            // The _outstandingRequest counter ensure that we only request a
            // maximum of {ProcessorCount} to the semaphore.

            // It's also more efficient to have two counter, _outstandingRequests is
            // more lightweight than the semaphore.

            // This trick is borrowed from the .Net ThreadPool
            // https://github.com/dotnet/coreclr/blob/bc146608854d1db9cdbcc0b08029a87754e12b49/src/mscorlib/src/System/Threading/ThreadPool.cs#L568

            int count = Volatile.Read(ref outstandingRequests);
            while (count < ProcessorCount)
            {
                int prev = Interlocked.CompareExchange(ref outstandingRequests, count + 1, count);
                if (prev == count)
                {
                    semaphore.Release();
                    break;
                }
                count = prev;
            }
            
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
                    Priority = ThreadPriority.Highest
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
                    if(queue.TryDequeue(out Action workItem))
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
                        PooledDelegateHelper.Release(workItem);
                        lastWorkTS = Stopwatch.GetTimestamp();
                    }
                    else
                    {
                        bool idleForTooLong = Stopwatch.GetTimestamp() - lastWorkTS > maxIdleTimeTS;
                        // Wait for another work item to be available
                        if (idleForTooLong || WaitOne(MaxIdleTimeInMS) == false)
                        {
                            // No work given in the last MaxIdleTimeTS, close this task
                            return;
                        }
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref aliveCount);
            }
        }
        
        bool WaitOne(int milliseconds)
        {
            bool signaled = semaphore.Wait(milliseconds);
                
            int count = Volatile.Read(ref outstandingRequests);
            while (count > 0)
            {
                int prev = Interlocked.CompareExchange(ref outstandingRequests, count - 1, count);
                if (prev == count)
                {
                    break;
                }
                count = prev;
            }
                
            return signaled;
        }

        

        // This class has been translated from:
        // https://github.com/dotnet/coreclr/blob/97433b9d153843492008652ff6b7c3bf4d9ff31c/src/vm/win32threadpool.h#L124

        // UnfairSemaphore is a more scalable semaphore than Semaphore.  It prefers to release threads that have more recently begun waiting,
        // to preserve locality.  Additionally, very recently-waiting threads can be released without an addition kernel transition to unblock
        // them, which reduces latency.
        //
        // UnfairSemaphore is only appropriate in scenarios where the order of unblocking threads is not important, and where threads frequently
        // need to be woken.

        [StructLayout(LayoutKind.Sequential)]
        private class UnfairSemaphore
        {
            const int MaxWorker = 0x7FFF;

            // We track everything we care about in A 64-bit struct to allow us to 
            // do CompareExchanges on this for atomic updates.
            [StructLayout(LayoutKind.Explicit)]
            private struct SemaphoreState
            {
                //how many threads are currently spin-waiting for this semaphore?
                [FieldOffset(0)]
                public short Spinners;

                //how much of the semaphore's count is available to spinners?
                [FieldOffset(2)]
                public short CountForSpinners;

                //how many threads are blocked in the OS waiting for this semaphore?
                [FieldOffset(4)]
                public short Waiters;

                //how much count is available to waiters?
                [FieldOffset(6)]
                public short CountForWaiters;

                [FieldOffset(0)]
                public long RawData;
            }

            [StructLayout(LayoutKind.Explicit, Size = 64)]
            private struct CacheLinePadding { }

            // padding to ensure we get our own cache line
#pragma warning disable 169
            private readonly CacheLinePadding m_padding1;
#pragma warning restore 169
            private SemaphoreState m_state;
            private readonly SemaphoreSlim m_semaphore;
#pragma warning disable 169
            private readonly CacheLinePadding m_padding2;
#pragma warning restore 169

            private bool TryUpdateState(SemaphoreState newState, SemaphoreState currentState)
            {
                if (Interlocked.CompareExchange(ref m_state.RawData, newState.RawData, currentState.RawData) == currentState.RawData)
                {
                    Debug.Assert(newState.CountForSpinners <= MaxWorker, "CountForSpinners is greater than MaxWorker");
                    Debug.Assert(newState.CountForSpinners >= 0, "CountForSpinners is lower than zero");
                    Debug.Assert(newState.Spinners <= MaxWorker, "Spinners is greater than MaxWorker");
                    Debug.Assert(newState.Spinners >= 0, "Spinners is lower than zero");
                    Debug.Assert(newState.CountForWaiters <= MaxWorker, "CountForWaiters is greater than MaxWorker");
                    Debug.Assert(newState.CountForWaiters >= 0, "CountForWaiters is lower than zero");
                    Debug.Assert(newState.Waiters <= MaxWorker, "Waiters is greater than MaxWorker");
                    Debug.Assert(newState.Waiters >= 0, "Waiters is lower than zero");
                    Debug.Assert(newState.CountForSpinners + newState.CountForWaiters <= MaxWorker, "CountForSpinners + CountForWaiters is greater than MaxWorker");

                    return true;
                }

                return false;
            }

            public UnfairSemaphore()
            {
                m_semaphore = new SemaphoreSlim(0, int.MaxValue);
            }
            
            public void Release(short count = 1)
            {
                while (true)
                {
                    SemaphoreState currentState = GetCurrentState();
                    SemaphoreState newState = currentState;

                    short remainingCount = count;

                    // First, prefer to release existing spinners,
                    // because a) they're hot, and b) we don't need a kernel
                    // transition to release them.
                    short spinnersToRelease = Math.Max((short)0, Math.Min(remainingCount, (short)(currentState.Spinners - currentState.CountForSpinners)));
                    newState.CountForSpinners += spinnersToRelease;
                    remainingCount -= spinnersToRelease;

                    // Next, prefer to release existing waiters
                    short waitersToRelease = Math.Max((short)0, Math.Min(remainingCount, (short)(currentState.Waiters - currentState.CountForWaiters)));
                    newState.CountForWaiters += waitersToRelease;
                    remainingCount -= waitersToRelease;

                    // Finally, release any future spinners that might come our way
                    newState.CountForSpinners += remainingCount;

                    // Try to commit the transaction
                    if (TryUpdateState(newState, currentState))
                    {
                        // Now we need to release the waiters we promised to release
                        if (waitersToRelease > 0)
                            m_semaphore.Release(waitersToRelease);

                        break;
                    }
                }
            }
            
            public bool Wait(int millisecondTimeout = -1)
            {
                while (true)
                {
                    SemaphoreState currentCounts = GetCurrentState();
                    SemaphoreState newCounts = currentCounts;

                    // First, just try to grab some count.
                    if (currentCounts.CountForSpinners > 0)
                    {
                        --newCounts.CountForSpinners;
                        if (TryUpdateState(newCounts, currentCounts))
                            return true;
                    }
                    else
                    {
                        // No count available, become a spinner
                        ++newCounts.Spinners;
                        if (TryUpdateState(newCounts, currentCounts))
                            break;
                    }
                }

                //
                // Now we're a spinner.  
                //
                int numSpins = 0;
                const int spinLimitPerProcessor = 50;
                while (true)
                {
                    SemaphoreState currentCounts = GetCurrentState();
                    SemaphoreState newCounts = currentCounts;

                    if (currentCounts.CountForSpinners > 0)
                    {
                        --newCounts.CountForSpinners;
                        --newCounts.Spinners;
                        if (TryUpdateState(newCounts, currentCounts))
                            return true;
                    }
                    else
                    {
                        double spinnersPerProcessor = (double)currentCounts.Spinners / Environment.ProcessorCount;
                        int spinLimit = (int)((spinLimitPerProcessor / spinnersPerProcessor) + 0.5);
                        if (numSpins >= spinLimit)
                        {
                            --newCounts.Spinners;
                            ++newCounts.Waiters;
                            if (TryUpdateState(newCounts, currentCounts))
                                break;
                        }
                        else
                        {
                            //
                            // We yield to other threads using Thread.Sleep(0) rather than the more traditional Thread.Yield().
                            // This is because Thread.Yield() does not yield to threads currently scheduled to run on other
                            // processors.  On a 4-core machine, for example, this means that Thread.Yield() is only ~25% likely
                            // to yield to the correct thread in some scenarios.
                            // Thread.Sleep(0) has the disadvantage of not yielding to lower-priority threads.  However, this is ok because
                            // once we've called this a few times we'll become a "waiter" and wait on the Semaphore, and that will
                            // yield to anything that is runnable.
                            //
                            Thread.Sleep(0);
                            numSpins++;
                        }
                    }
                }

                //
                // Now we're a waiter
                //
                bool waitSucceeded = m_semaphore.Wait(millisecondTimeout);

                while (true)
                {
                    SemaphoreState currentCounts = GetCurrentState();
                    SemaphoreState newCounts = currentCounts;

                    --newCounts.Waiters;

                    if (waitSucceeded)
                        --newCounts.CountForWaiters;

                    if (TryUpdateState(newCounts, currentCounts))
                        return waitSucceeded;
                }
            }

            private SemaphoreState GetCurrentState()
            {
                // Volatile.Read of a long can get a partial read in x86 but the invalid
                // state will be detected in TryUpdateState with the CompareExchange.
                return new SemaphoreState
                {
                    RawData = Volatile.Read( ref m_state.RawData )
                };
            }
        }
    }
}