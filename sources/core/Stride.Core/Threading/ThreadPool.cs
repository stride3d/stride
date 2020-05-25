// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Stride.Core.Threading
{
    public sealed partial class ThreadPool
    {
        public static readonly ThreadPool Instance = new ThreadPool();
        
        private const int ThreadPoolThreadTimeoutMs = 20 * 1000;
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
        private readonly ThreadRequests Requests = new ThreadRequests();
        private readonly ConcurrentQueue<Action> WorkItems = new ConcurrentQueue<Action>();

        private volatile int _numRequestedWorkers = 0;

        public int ThreadCount => ThreadCounts.VolatileReadCounts(ref _separated.counts).numExistingThreads;
        public long CompletedWorkItemCount => Volatile.Read(ref _completionCounter);

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
        }

        public void QueueWorkItem(Action workItem, int amount = 1)
        {
            // Throw right here to help debugging
            if(workItem == null)
            {
                throw new NullReferenceException(nameof(workItem));
            }

            if(amount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            for( int i = 0; i < amount; i++ )
            {
                PooledDelegateHelper.AddReference(workItem);
                WorkItems.Enqueue(workItem);
                EnsureThreadRequested();
            }
        }

        /// <summary>
        /// Dispatches work items to this thread.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this thread did as much work as was available or its quantum expired.
        /// <c>false</c> if this thread stopped working early.
        /// </returns>
        public bool Dispatch()
        {
            //
            // Update our records to indicate that an outstanding request for a thread has now been fulfilled.
            // From this point on, we are responsible for requesting another thread if we stop working for any
            // reason, and we believe there might still be work in the queue.
            //
            // CoreCLR: Note that if this thread is aborted before we get a chance to request another one, the VM will
            // record a thread request on our behalf.  So we don't need to worry about getting aborted right here.
            //
            
            //MarkThreadRequestSatisfied();
            //
            // One of our outstanding thread requests has been satisfied.
            // Decrement the count so that future calls to EnsureThreadRequested will succeed.
            //
            // CoreCLR: Note that there is a separate count in the VM which has already been decremented
            // by the VM by the time we reach this point.
            //
            int count = Requests.numOutstandingThreadRequests;
            while( count > 0 )
            {
                int prev = Interlocked.CompareExchange( ref Requests.numOutstandingThreadRequests, count - 1, count );
                if( prev == count )
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
                while( true )
                {
                    if( WorkItems.TryDequeue( out Action workItem ) == false )
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
                    EnsureThreadRequested();

                    //
                    // Execute the workitem outside of any finally blocks, so that it can be aborted if needed.
                    //
                    try
                    {
                        workItem.Invoke();
                    }
                    finally
                    {
                        PooledDelegateHelper.Release(workItem);
                    }


                    // Release refs
                    workItem = null;

                    //
                    // Notify the VM that we executed this workitem.  This is also our opportunity to ask whether Hill Climbing wants
                    // us to return the thread to the pool or not.
                    //
                    if( ! NotifyWorkItemComplete() )
                        return false;
                }
            }
            finally
            {
                //
                // If we are exiting for any reason other than that the queue is definitely empty, ask for another
                // thread to pick up where we left off.
                //
                if( needAnotherThread )
                    EnsureThreadRequested();
            }
        }
        
        
        public void EnsureThreadRequested()
        {
            //
            // If we have not yet requested #procs threads, then request a new thread.
            //
            // CoreCLR: Note that there is a separate count in the VM which has already been incremented
            // by the VM by the time we reach this point.
            //
            int count = Requests.numOutstandingThreadRequests;
            while( count < Environment.ProcessorCount )
            {
                int prev = Interlocked.CompareExchange( ref Requests.numOutstandingThreadRequests, count + 1, count );
                if( prev == count )
                {
                    RequestWorker();
                    break;
                }

                count = prev;
            }
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

        [StructLayout(LayoutKind.Sequential)] // enforce layout so that padding reduces false sharing
        public class ThreadRequests
        {
            private readonly PaddingFalseSharing pad1;

            public volatile int numOutstandingThreadRequests = 0;

            private readonly PaddingFalseSharing pad2;
        }
        
        /// <summary>Padding structure used to minimize false sharing</summary>
        [ StructLayout( LayoutKind.Explicit, Size = PaddingFalseSharing.CACHE_LINE_SIZE - sizeof(int) ) ]
        public struct PaddingFalseSharing
        {
            /// <summary>A size greater than or equal to the size of the most common CPU cache lines.</summary>
            #if TARGET_ARM64
            public const int CACHE_LINE_SIZE = 128;
            #else
            public const int CACHE_LINE_SIZE = 64;
            #endif
        }
    }
}
