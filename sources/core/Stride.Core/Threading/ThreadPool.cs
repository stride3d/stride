// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Stride.Core.Annotations;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Stride.Core.Threading
{
    /// <summary>
    /// Thread pool for scheduling actions.
    /// Can be instantiated and generates less garbage than dotnet's.
    /// </summary>
    public sealed partial class ThreadPool
    {
        /// <summary>
        /// The default instance that the whole process shares, use this one to avoid wasting process memory.
        /// </summary>
        public static readonly ThreadPool Instance = new ThreadPool();

        private static short MaxPossibleThreadCount => Environment.Is64BitProcess ? short.MaxValue : (short)1023;

        private const int ThreadPoolThreadTimeoutMs = 20 * 1000;
        private const int CpuUtilizationHigh = 95;
        private const int CpuUtilizationLow = 80;

        private int cpuUtilization = 0;

        private short minThreads;
        private short maxThreads;

        [StructLayout(LayoutKind.Explicit, Size = CACHE_LINE_SIZE * 5)]
        private struct CacheLineSeparated
        {
            [FieldOffset(CACHE_LINE_SIZE * 1)]
            public ThreadCounts Counts;
            [FieldOffset(CACHE_LINE_SIZE * 2)]
            public int LastDequeueTime;
            [FieldOffset(CACHE_LINE_SIZE * 3)]
            public int PriorCompletionCount;
            [FieldOffset(CACHE_LINE_SIZE * 3 + sizeof(int))]
            public int PriorCompletedWorkRequestsTime;
            [FieldOffset(CACHE_LINE_SIZE * 3 + sizeof(int) * 2)]
            public int NextCompletedWorkRequestsTime;
        }

        private CacheLineSeparated separated;
        private long currentSampleStartTime;
        private long completionCounter;
        private int threadAdjustmentIntervalMs;

        private readonly object hillClimbingThreadAdjustmentLock = new object();
        private readonly GateThread gate;
        private readonly WorkerThread workers;
        private readonly HillClimbing hillClimber;
        private readonly ThreadRequests requests = new ThreadRequests();
        private readonly ConcurrentQueue<(object f, object p)> workItems = new ConcurrentQueue<(object, object)>();

        private volatile int numRequestedWorkers = 0;

        /// <summary>
        /// Amount of threads that this pool manages
        /// </summary>
        public int ThreadCount => ThreadCounts.VolatileReadCounts(ref separated.Counts).numExistingThreads;
        /// <summary>
        /// Amount of work completed
        /// </summary>
        public long CompletedWorkItemCount => Volatile.Read(ref completionCounter);
        /// <summary>
        /// Maximum amount of threads allowed
        /// </summary>
        public int GetMaxThreads() => maxThreads;

        /// <summary>
        /// Amount of threads waiting for work
        /// </summary>
        public int GetAvailableThreads()
        {
            ThreadCounts counts = ThreadCounts.VolatileReadCounts(ref separated.Counts);
            int count = maxThreads - counts.numProcessingWork;
            if (count < 0)
            {
                return 0;
            }

            return count;
        }

        public ThreadPool()
        {
            minThreads = (short)Environment.ProcessorCount;
            if (minThreads > MaxPossibleThreadCount)
            {
                minThreads = MaxPossibleThreadCount;
            }

            maxThreads = MaxPossibleThreadCount;
            if (maxThreads < minThreads)
            {
                maxThreads = minThreads;
            }

            separated = new CacheLineSeparated
            {
                Counts = new ThreadCounts
                {
                    numThreadsGoal = minThreads
                }
            };
            gate = new GateThread(this);
            workers = new WorkerThread(this);
            hillClimber = new HillClimbing(this);
        }

        public void QueueWorkItem([NotNull, Pooled] Action workItem, int amount = 1)
        {
            // Throw right here to help debugging
            if (workItem == null)
            {
                throw new NullReferenceException(nameof(workItem));
            }

            if (amount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            for (int i = 0; i < amount; i++)
            {
                PooledDelegateHelper.AddReference(workItem);
                workItems.Enqueue((workItem, null));
            }
            EnsureThreadRequested(amount);
        }

        public void QueueWorkItem(Action<object> workItem, object param, int amount = 1)
        {
            // Throw right here to help debugging
            if (workItem == null)
            {
                throw new NullReferenceException(nameof(workItem));
            }

            if (amount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            for (int i = 0; i < amount; i++)
            {
                PooledDelegateHelper.AddReference(workItem);
                workItems.Enqueue((workItem, param));
            }
            EnsureThreadRequested(amount);
        }

        /// <summary>
        /// Dispatches work items to this thread.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this thread did as much work as was available or its quantum expired.
        /// <c>false</c> if this thread stopped working early.
        /// </returns>
        private bool Dispatch()
        {
            //
            // Update our records to indicate that an outstanding request for a thread has now been fulfilled.
            // From this point on, we are responsible for requesting another thread if we stop working for any
            // reason, and we believe there might still be work in the queue.
            //
            // CoreCLR: Note that if this thread is aborted before we get a chance to request another one, the VM will
            // record a thread request on our behalf.  So we don't need to worry about getting aborted right here.
            //

            //
            // One of our outstanding thread requests has been satisfied.
            // Decrement the count so that future calls to EnsureThreadRequested will succeed.
            //
            // CoreCLR: Note that there is a separate count in the VM which has already been decremented
            // by the VM by the time we reach this point.
            //
            int count = requests.numOutstandingThreadRequests;
            while (count > 0)
            {
                int prev = Interlocked.CompareExchange(ref requests.numOutstandingThreadRequests, count - 1, count);
                if (prev == count)
                {
                    break;
                }

                count = prev;
            }

            //
            // Assume that we're going to need another thread if this one returns to the VM.  We'll set this to
            // false later, but only if we're absolutely certain that the queue is empty.
            //
            bool needAnotherThread = true;
            try
            {
                //
                // Loop until our quantum expires or there is no work.
                //
                while (true)
                {
                    if (workItems.TryDequeue(out var workItem) == false)
                    {
                        //
                        // No work.
                        // If we missed a steal, though, there may be more work in the queue.
                        // Instead of looping around and trying again, we'll just request another thread.  Hopefully the thread
                        // that owns the contended work-stealing queue will pick up its own workitems in the meantime,
                        // which will be more efficient than this thread doing it anyway.
                        //
                        needAnotherThread = false;

                        // Tell the VM we're returning normally, not because Hill Climbing asked us to return.
                        return true;
                    }

                    //
                    // If we found work, there may be more work.  Ask for another thread so that the other work can be processed
                    // in parallel.  Note that this will only ask for a max of #procs threads, so it's safe to call it for every dequeue.
                    //
                    EnsureThreadRequested(1);

                    //
                    // Execute the workitem outside of any finally blocks, so that it can be aborted if needed.
                    //
                    if(workItem.f is Action a)
                    {
                        try
                        {
                            a.Invoke();
                        }
                        finally
                        {
                            PooledDelegateHelper.Release(a);
                        }
                    }
                    else
                    {
                        (workItem.f as Action<object>).Invoke( workItem.p );
                    }


                    // Release refs
                    workItem = default;

                    //
                    // Notify the VM that we executed this workitem.  This is also our opportunity to ask whether Hill Climbing wants
                    // us to return the thread to the pool or not.
                    //
                    if (!NotifyWorkItemComplete())
                        return false;
                }
            }
            finally
            {
                //
                // If we are exiting for any reason other than that the queue is definitely empty, ask for another
                // thread to pick up where we left off.
                //
                if (needAnotherThread)
                    EnsureThreadRequested(1);
            }
        }

        private bool NotifyWorkItemComplete()
        {
            Interlocked.Increment(ref completionCounter);
            Volatile.Write(ref separated.LastDequeueTime, Environment.TickCount);

            if (ShouldAdjustMaxWorkersActive() && Monitor.TryEnter(hillClimbingThreadAdjustmentLock))
            {
                try
                {
                    AdjustMaxWorkersActive();
                }
                finally
                {
                    Monitor.Exit(hillClimbingThreadAdjustmentLock);
                }
            }

            return !workers.ShouldStopProcessingWorkNow();
        }
        
        private void EnsureThreadRequested(int amount)
        {
            //
            // If we have not yet requested #procs threads, then request a new thread.
            //
            // CoreCLR: Note that there is a separate count in the VM which has already been incremented
            // by the VM by the time we reach this point.
            //
            int count = requests.numOutstandingThreadRequests;
            var procCount = Environment.ProcessorCount;
            while (count < procCount)
            {
                var newC = Math.Min(count + amount, procCount);
                int prev = Interlocked.CompareExchange(ref requests.numOutstandingThreadRequests, newC, count);
                if (prev == count)
                {
                    RequestWorker(newC - prev);
                    break;
                }

                count = prev;
            }
        }

        //
        // This method must only be called if ShouldAdjustMaxWorkersActive has returned true, *and*
        // _hillClimbingThreadAdjustmentLock is held.
        //
        private void AdjustMaxWorkersActive()
        {
            Debug.Assert(Monitor.IsEntered(hillClimbingThreadAdjustmentLock));
            int currentTicks = Environment.TickCount;
            int totalNumCompletions = (int)Volatile.Read(ref completionCounter);
            int numCompletions = totalNumCompletions - separated.PriorCompletionCount;
            long startTime = currentSampleStartTime;
            long endTime = Stopwatch.GetTimestamp();
            long freq = Stopwatch.Frequency;

            double elapsedSeconds = (double)(endTime - startTime) / freq;

            if (elapsedSeconds * 1000 >= threadAdjustmentIntervalMs / 2)
            {
                ThreadCounts currentCounts = ThreadCounts.VolatileReadCounts(ref separated.Counts);
                int newMax;
                (newMax, threadAdjustmentIntervalMs) = hillClimber.Update(currentCounts.numThreadsGoal, elapsedSeconds, numCompletions);

                while (newMax != currentCounts.numThreadsGoal)
                {
                    ThreadCounts newCounts = currentCounts;
                    newCounts.numThreadsGoal = (short)newMax;

                    ThreadCounts oldCounts = ThreadCounts.CompareExchangeCounts(ref separated.Counts, newCounts, currentCounts);
                    if (oldCounts == currentCounts)
                    {
                        //
                        // If we're increasing the max, inject a thread.  If that thread finds work, it will inject
                        // another thread, etc., until nobody finds work or we reach the new maximum.
                        //
                        // If we're reducing the max, whichever threads notice this first will sleep and timeout themselves.
                        //
                        if (newMax > oldCounts.numThreadsGoal)
                        {
                            workers.MaybeAddWorkingWorker(1);
                        }

                        break;
                    }
                    else
                    {
                        if (oldCounts.numThreadsGoal > currentCounts.numThreadsGoal && oldCounts.numThreadsGoal >= newMax)
                        {
                            // someone (probably the gate thread) increased the thread count more than
                            // we are about to do.  Don't interfere.
                            break;
                        }

                        currentCounts = oldCounts;
                    }
                }

                separated.PriorCompletionCount = totalNumCompletions;
                separated.NextCompletedWorkRequestsTime = currentTicks + threadAdjustmentIntervalMs;
                Volatile.Write(ref separated.PriorCompletedWorkRequestsTime, currentTicks);
                currentSampleStartTime = endTime;
            }
        }

        private bool ShouldAdjustMaxWorkersActive()
        {
            // We need to subtract by prior time because Environment.TickCount can wrap around, making a comparison of absolute times unreliable.
            int priorTime = Volatile.Read(ref separated.PriorCompletedWorkRequestsTime);
            int requiredInterval = separated.NextCompletedWorkRequestsTime - priorTime;
            int elapsedInterval = Environment.TickCount - priorTime;
            if (elapsedInterval >= requiredInterval)
            {
                // Avoid trying to adjust the thread count goal if there are already more threads than the thread count goal.
                // In that situation, hill climbing must have previously decided to decrease the thread count goal, so let's
                // wait until the system responds to that change before calling into hill climbing again. This condition should
                // be the opposite of the condition in WorkerThread.ShouldStopProcessingWorkNow that causes
                // threads processing work to stop in response to a decreased thread count goal. The logic here is a bit
                // different from the original CoreCLR code from which this implementation was ported because in this
                // implementation there are no retired threads, so only the count of threads processing work is considered.
                ThreadCounts counts = ThreadCounts.VolatileReadCounts(ref separated.Counts);
                return counts.numProcessingWork <= counts.numThreadsGoal;
            }

            return false;
        }

        private void RequestWorker(int amount)
        {
            Interlocked.Add(ref numRequestedWorkers, amount);
            workers.MaybeAddWorkingWorker(amount);
            gate.EnsureRunning();
        }

        /// <summary>
        /// Tracks information on the number of threads we want/have in different states in our thread pool.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct ThreadCounts
        {
            /// <summary>
            /// Max possible thread pool threads we want to have.
            /// </summary>
            [FieldOffset(0)]
            public short numThreadsGoal;

            /// <summary>
            /// Number of thread pool threads that currently exist.
            /// </summary>
            [FieldOffset(2)]
            public short numExistingThreads;

            /// <summary>
            /// Number of threads processing work items.
            /// </summary>
            [FieldOffset(4)]
            public short numProcessingWork;

            [FieldOffset(0)]
            private long _asLong;

            public static ThreadCounts VolatileReadCounts(ref ThreadCounts counts)
            {
                return new ThreadCounts
                {
                    _asLong = Volatile.Read(ref counts._asLong)
                };
            }

            public static ThreadCounts CompareExchangeCounts(ref ThreadCounts location, ThreadCounts newCounts, ThreadCounts oldCounts)
            {
                ThreadCounts result = new ThreadCounts
                {
                    _asLong = Interlocked.CompareExchange(ref location._asLong, newCounts._asLong, oldCounts._asLong)
                };

                if (result == oldCounts)
                {
                    result.Validate();
                    newCounts.Validate();
                }

                return result;
            }

            public static bool operator ==(ThreadCounts lhs, ThreadCounts rhs) => lhs._asLong == rhs._asLong;

            public static bool operator !=(ThreadCounts lhs, ThreadCounts rhs) => lhs._asLong != rhs._asLong;

            public override bool Equals(object obj)
            {
                return obj is ThreadCounts counts && _asLong == counts._asLong;
            }

            public override int GetHashCode()
            {
                return (int)(_asLong >> 8) + numThreadsGoal;
            }

            private void Validate()
            {
                Debug.Assert(numThreadsGoal > 0, "Goal must be positive");
                Debug.Assert(numExistingThreads >= 0, "Number of existing threads must be non-zero");
                Debug.Assert(numProcessingWork >= 0, "Number of threads processing work must be non-zero");
                Debug.Assert(numProcessingWork <= numExistingThreads, $"Num processing work ({numProcessingWork}) must be less than or equal to Num existing threads ({numExistingThreads})");
            }
        }

        [StructLayout(LayoutKind.Sequential)] // enforce layout so that padding reduces false sharing
        private class ThreadRequests
        {
            private readonly PaddingFalseSharing pad1;

            public volatile int numOutstandingThreadRequests = 0;

            private readonly PaddingFalseSharing pad2;
        }

        /// <summary>Padding structure used to minimize false sharing</summary>
        [StructLayout(LayoutKind.Explicit, Size = CACHE_LINE_SIZE - sizeof(int))]
        private struct PaddingFalseSharing
        {
        }
        
        /// <summary>A size greater than or equal to the size of the most common CPU cache lines.</summary>
#if TARGET_ARM64
        public const int CACHE_LINE_SIZE = 128;
#else
        public const int CACHE_LINE_SIZE = 64;
#endif
    }
}