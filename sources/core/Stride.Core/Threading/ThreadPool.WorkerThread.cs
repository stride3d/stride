// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
namespace Stride.Core.Threading
{
    public partial class ThreadPool
    {
        /// <summary>
        /// The worker thread infrastructure for the CLR thread pool.
        /// </summary>
        private class WorkerThread
        {
            private readonly ThreadPool pool;
            /// <summary>
            /// Semaphore for controlling how many threads are currently working.
            /// </summary>
            private static readonly Semaphore Semaphore = new Semaphore(0, Environment.Is64BitProcess ? short.MaxValue : (short)1023, 70);
            
            public WorkerThread(ThreadPool poolParam) => pool = poolParam;

            public void MaybeAddWorkingWorker(int amount)
            {
                ThreadCounts counts = ThreadCounts.VolatileReadCounts(ref pool.separated.Counts);
                ThreadCounts newCounts;
                while (true)
                {
                    newCounts = counts;
                    newCounts.numProcessingWork = Math.Max(counts.numProcessingWork, Math.Min((short)(counts.numProcessingWork + amount), counts.numThreadsGoal));
                    newCounts.numExistingThreads = Math.Max(counts.numExistingThreads, newCounts.numProcessingWork);

                    if (newCounts == counts)
                    {
                        return;
                    }

                    ThreadCounts oldCounts = ThreadCounts.CompareExchangeCounts(ref pool.separated.Counts, newCounts, counts);

                    if (oldCounts == counts)
                    {
                        break;
                    }

                    counts = oldCounts;
                }

                int toCreate = newCounts.numExistingThreads - counts.numExistingThreads;
                int toRelease = newCounts.numProcessingWork - counts.numProcessingWork;

                if (toRelease > 0)
                {
                    Semaphore.Release(toRelease);
                }

                while (toCreate > 0)
                {
                    if (TryCreateWorkerThread())
                    {
                        toCreate--;
                    }
                    else
                    {
                        counts = ThreadCounts.VolatileReadCounts(ref pool.separated.Counts);
                        while (true)
                        {
                            newCounts = counts;
                            newCounts.numProcessingWork -= (short)toCreate;
                            newCounts.numExistingThreads -= (short)toCreate;

                            ThreadCounts oldCounts = ThreadCounts.CompareExchangeCounts(ref pool.separated.Counts, newCounts, counts);
                            if (oldCounts == counts)
                            {
                                break;
                            }
                            counts = oldCounts;
                        }
                        toCreate = 0;
                    }
                }
            }

            /// <summary>
            /// Returns if the current thread should stop processing work on the thread pool.
            /// A thread should stop processing work on the thread pool when work remains only when
            /// there are more worker threads in the thread pool than we currently want.
            /// </summary>
            /// <returns>Whether or not this thread should stop processing work even if there is still work in the queue.</returns>
            public bool ShouldStopProcessingWorkNow()
            {
                ThreadCounts counts = ThreadCounts.VolatileReadCounts(ref pool.separated.Counts);
                while (true)
                {
                    // When there are more threads processing work than the thread count goal, hill climbing must have decided
                    // to decrease the number of threads. Stop processing if the counts can be updated. We may have more
                    // threads existing than the thread count goal and that is ok, the cold ones will eventually time out if
                    // the thread count goal is not increased again. This logic is a bit different from the original CoreCLR
                    // code from which this implementation was ported, which turns a processing thread into a retired thread
                    // and checks for pending requests like RemoveWorkingWorker. In this implementation there are
                    // no retired threads, so only the count of threads processing work is considered.
                    if (counts.numProcessingWork <= counts.numThreadsGoal)
                    {
                        return false;
                    }

                    ThreadCounts newCounts = counts;
                    newCounts.numProcessingWork--;

                    ThreadCounts oldCounts = ThreadCounts.CompareExchangeCounts(ref pool.separated.Counts, newCounts, counts);

                    if (oldCounts == counts)
                    {
                        return true;
                    }
                    counts = oldCounts;
                }
            }

            private void WorkerThreadStart()
            {
                while (true)
                {
                    while (WaitForRequest())
                    {
                        if (TakeActiveRequest())
                        {
                            Volatile.Write(ref pool.separated.LastDequeueTime, Environment.TickCount);
                            if (pool.Dispatch())
                            {
                                // If the queue runs out of work for us, we need to update the number of working workers to reflect that we are done working for now
                                RemoveWorkingWorker();
                            }
                        }
                        else
                        {
                            // If we woke up but couldn't find a request, we need to update the number of working workers to reflect that we are done working for now
                            RemoveWorkingWorker();
                        }
                    }

                    lock(pool.hillClimbingThreadAdjustmentLock)
                    {
                        // At this point, the thread's wait timed out. We are shutting down this thread.
                        // We are going to decrement the number of exisiting threads to no longer include this one
                        // and then change the max number of threads in the thread pool to reflect that we don't need as many
                        // as we had. Finally, we are going to tell hill climbing that we changed the max number of threads.
                        ThreadCounts counts = ThreadCounts.VolatileReadCounts(ref pool.separated.Counts);
                        while (true)
                        {
                            if (counts.numExistingThreads == counts.numProcessingWork)
                            {
                                // In this case, enough work came in that this thread should not time out and should go back to work.
                                break;
                            }

                            ThreadCounts newCounts = counts;
                            newCounts.numExistingThreads--;
                            newCounts.numThreadsGoal = Math.Max(pool.minThreads, Math.Min(newCounts.numExistingThreads, newCounts.numThreadsGoal));
                            ThreadCounts oldCounts = ThreadCounts.CompareExchangeCounts(ref pool.separated.Counts, newCounts, counts);
                            if (oldCounts == counts)
                            {
                                pool.hillClimber.ForceChange(newCounts.numThreadsGoal);
                                return;
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Waits for a request to work.
            /// </summary>
            /// <returns>If this thread was woken up before it timed out.</returns>
            private bool WaitForRequest() => Semaphore.Wait(ThreadPoolThreadTimeoutMs);

            /// <summary>
            /// Reduce the number of working workers by one, but maybe add back a worker (possibily this thread) if a thread request comes in while we are marking this thread as not working.
            /// </summary>
            private void RemoveWorkingWorker()
            {
                ThreadCounts currentCounts = ThreadCounts.VolatileReadCounts(ref pool.separated.Counts);
                while (true)
                {
                    ThreadCounts newCounts = currentCounts;
                    newCounts.numProcessingWork--;
                    ThreadCounts oldCounts = ThreadCounts.CompareExchangeCounts(ref pool.separated.Counts, newCounts, currentCounts);

                    if (oldCounts == currentCounts)
                    {
                        break;
                    }
                    currentCounts = oldCounts;
                }

                // It's possible that we decided we had thread requests just before a request came in,
                // but reduced the worker count *after* the request came in.  In this case, we might
                // miss the notification of a thread request.  So we wake up a thread (maybe this one!)
                // if there is work to do.
                if (pool.numRequestedWorkers > 0)
                {
                    MaybeAddWorkingWorker(1);
                }
            }

            private bool TakeActiveRequest()
            {
                int count = pool.numRequestedWorkers;
                while (count > 0)
                {
                    int prevCount = Interlocked.CompareExchange(ref pool.numRequestedWorkers, count - 1, count);
                    if (prevCount == count)
                    {
                        return true;
                    }
                    count = prevCount;
                }
                return false;
            }

            private bool TryCreateWorkerThread()
            {
                try
                {
                    Thread workerThread = new Thread(WorkerThreadStart);
                    workerThread.IsBackground = true;
                    workerThread.Start();
                }
                catch (ThreadStartException)
                {
                    return false;
                }
                catch (OutOfMemoryException)
                {
                    return false;
                }
                return true;
            }
        }
    }
}
