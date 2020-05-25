// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace SubSystem.Threading
{
    /// <summary>
    /// A thread-pool run and managed on the CLR.
    /// </summary>
    public sealed partial class ThreadPool
    {
        private const int ThreadPoolThreadTimeoutMs = 20 * 1000; // If you change this make sure to change the timeout times in the tests.
        private static short MaxPossibleThreadCount => Environment.Is64BitProcess ? short.MaxValue : (short)1023;
        
        private const int CpuUtilizationHigh = 95;
        private const int CpuUtilizationLow = 80;
        private int _cpuUtilization = 0;

        private short _minThreads;
        private short _maxThreads;

        [StructLayout(LayoutKind.Explicit, Size = CacheLineSize * 5)]
        private struct CacheLineSeparated
        {
#if TARGET_ARM64
            private const int CacheLineSize = 128;
#else
            private const int CacheLineSize = 64;
#endif
            [FieldOffset(CacheLineSize * 1)]
            public ThreadCounts counts;
            [FieldOffset(CacheLineSize * 2)]
            public int lastDequeueTime;
            [FieldOffset(CacheLineSize * 3)]
            public int priorCompletionCount;
            [FieldOffset(CacheLineSize * 3 + sizeof(int))]
            public int priorCompletedWorkRequestsTime;
            [FieldOffset(CacheLineSize * 3 + sizeof(int) * 2)]
            public int nextCompletedWorkRequestsTime;
        }

        private CacheLineSeparated _separated;
        private long _currentSampleStartTime;
        private long _completionCounter;
        private int _threadAdjustmentIntervalMs;

        private readonly LowLevelLock _hillClimbingThreadAdjustmentLock = new LowLevelLock();
        private readonly GateThread Gate;
        private readonly WorkerThread Workers;
        private readonly HillClimbing HillClimber;
        private readonly WorkQueue s_workQueue;

        private volatile int _numRequestedWorkers = 0;

        public ThreadPool()
        {
            _minThreads = (short)Environment.ProcessorCount;
            if (_minThreads > MaxPossibleThreadCount)
            {
                _minThreads = MaxPossibleThreadCount;
            }

            _maxThreads = MaxPossibleThreadCount;
            if (_maxThreads < _minThreads)
            {
                _maxThreads = _minThreads;
            }

            _separated = new CacheLineSeparated
            {
                counts = new ThreadCounts
                {
                    numThreadsGoal = _minThreads
                }
            };
            Gate = new GateThread(this);
            Workers = new WorkerThread(this);
            HillClimber = new HillClimbing(this);
            s_workQueue = new WorkQueue(this);
        }

        public void QueueUserWorkItem(Action callBack)
        {
            if (callBack == null)
            {
                throw new NullReferenceException(nameof(callBack));
            }

            s_workQueue.Enqueue(callBack);
        }

        public int GetMaxThreads() => _maxThreads;

        public int GetAvailableThreads()
        {
            ThreadCounts counts = ThreadCounts.VolatileReadCounts(ref _separated.counts);
            int count = _maxThreads - counts.numProcessingWork;
            if (count < 0)
            {
                return 0;
            }
            return count;
        }

        public int ThreadCount => ThreadCounts.VolatileReadCounts(ref _separated.counts).numExistingThreads;
        public long CompletedWorkItemCount => Volatile.Read(ref _completionCounter);

        private bool NotifyWorkItemComplete()
        {
            Interlocked.Increment(ref _completionCounter);
            Volatile.Write(ref _separated.lastDequeueTime, Environment.TickCount);

            if (ShouldAdjustMaxWorkersActive() && _hillClimbingThreadAdjustmentLock.TryAcquire())
            {
                try
                {
                    AdjustMaxWorkersActive();
                }
                finally
                {
                    _hillClimbingThreadAdjustmentLock.Release();
                }
            }

            return !Workers.ShouldStopProcessingWorkNow();
        }

        //
        // This method must only be called if ShouldAdjustMaxWorkersActive has returned true, *and*
        // _hillClimbingThreadAdjustmentLock is held.
        //
        private void AdjustMaxWorkersActive()
        {
            _hillClimbingThreadAdjustmentLock.VerifyIsLocked();
            int currentTicks = Environment.TickCount;
            int totalNumCompletions = (int)Volatile.Read(ref _completionCounter);
            int numCompletions = totalNumCompletions - _separated.priorCompletionCount;
            long startTime = _currentSampleStartTime;
            long endTime = Stopwatch.GetTimestamp();
            long freq = Stopwatch.Frequency;

            double elapsedSeconds = (double)(endTime - startTime) / freq;

            if (elapsedSeconds * 1000 >= _threadAdjustmentIntervalMs / 2)
            {
                ThreadCounts currentCounts = ThreadCounts.VolatileReadCounts(ref _separated.counts);
                int newMax;
                (newMax, _threadAdjustmentIntervalMs) = HillClimber.Update(currentCounts.numThreadsGoal, elapsedSeconds, numCompletions);

                while (newMax != currentCounts.numThreadsGoal)
                {
                    ThreadCounts newCounts = currentCounts;
                    newCounts.numThreadsGoal = (short)newMax;

                    ThreadCounts oldCounts = ThreadCounts.CompareExchangeCounts(ref _separated.counts, newCounts, currentCounts);
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
                            Workers.MaybeAddWorkingWorker();
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
                _separated.priorCompletionCount = totalNumCompletions;
                _separated.nextCompletedWorkRequestsTime = currentTicks + _threadAdjustmentIntervalMs;
                Volatile.Write(ref _separated.priorCompletedWorkRequestsTime, currentTicks);
                _currentSampleStartTime = endTime;
            }
        }

        private bool ShouldAdjustMaxWorkersActive()
        {
            // We need to subtract by prior time because Environment.TickCount can wrap around, making a comparison of absolute times unreliable.
            int priorTime = Volatile.Read(ref _separated.priorCompletedWorkRequestsTime);
            int requiredInterval = _separated.nextCompletedWorkRequestsTime - priorTime;
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
                ThreadCounts counts = ThreadCounts.VolatileReadCounts(ref _separated.counts);
                return counts.numProcessingWork <= counts.numThreadsGoal;
            }
            return false;
        }

        public void RequestWorker()
        {
            Interlocked.Increment(ref _numRequestedWorkers);
            Workers.MaybeAddWorkingWorker();
            Gate.EnsureRunning();
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
                return obj is ThreadCounts counts && this._asLong == counts._asLong;
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
    }
}
