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
    /// <remarks>
    /// When more than <see cref="PoolSize"/> jobs have been scheduled,
    /// new threads will have to be spawned to avoid user-side deadlocks.
    /// </remarks>
    internal class ThreadPool
    {
        /// <summary> Instance shared across the program </summary>
        public static readonly ThreadPool Instance = new ThreadPool();
        
        /// <summary> Maximum amount of idle threads waiting for work. </summary>
        public readonly int PoolSize;
        
        /// <summary>
        /// OS semaphores are close to two times faster to signal multiple
        /// threads than doing the same thing through monitor like
        /// SemaphoreSlim and other reset events are doing.
        /// </summary>
        private readonly Semaphore semaphore;
        private readonly ConcurrentPool<object> jobs;
        private int spinningCount;
        private int spinToRelease;
        private int sleeping;

        public ThreadPool()
        {
            PoolSize = Environment.ProcessorCount * 4;
            semaphore = new Semaphore(0, int.MaxValue);
            jobs = new ConcurrentPool<object>(() => null);
        }

        /// <summary> Schedule an action to be run on one of the <see cref="ThreadPool"/>'s threads </summary>
        /// <exception cref="ArgumentNullException"><see cref="workItem"/> is null</exception>
        public void QueueWorkItem([NotNull, Pooled] Action workItem, int amount = 1)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }
            
            Dispatch(workItem, amount);
        }

        /// <summary> Schedule a job to be run on one of or multiple <see cref="ThreadPool"/>'s threads </summary>
        /// <exception cref="ArgumentNullException"><see cref="jobParam"/> is null</exception>
        public void DispatchJob([NotNull] IConcurrentJob jobParam, int amount = 1)
        {
            if (jobParam == null)
            {
                throw new ArgumentNullException(nameof(jobParam));
            }

            Dispatch(jobParam, amount);
        }

        private void Dispatch(object job, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                jobs.Release(job);
            }
            SignalThreads(amount, out int leftToSpawn);
            for (int i = 0; i < leftToSpawn; i++)
            {
                new PooledThread(this);
            }
        }

        private bool TryEnterSleep()
        {
            if ((Volatile.Read(ref sleeping) + Volatile.Read(ref spinningCount)) >= PoolSize)
                return false;
            
            Interlocked.Increment(ref spinningCount);
            var spin = new SpinWait();
            while (spin.NextSpinWillYield == false)
            {
                int spinRelease = Volatile.Read(ref spinToRelease);
                if (spinRelease > 0 && Interlocked.CompareExchange(ref spinToRelease, spinRelease - 1, spinRelease) == spinRelease)
                {
                    Interlocked.Decrement(ref spinningCount);
                    return true;
                }

                spin.SpinOnce();
            }
            
            Interlocked.Increment(ref sleeping);
            Interlocked.Decrement(ref spinningCount);

            semaphore.WaitOne();
            return true;
        }
        
        private void SignalThreads(int releaseCount, out int leftThatCouldntBeReleased)
        {
            if (releaseCount < 1)
                throw new ArgumentOutOfRangeException(nameof(releaseCount));

            var spin = new SpinWait();
            Interlocked.Add(ref spinToRelease, releaseCount);
            while (spin.NextSpinWillYield == false && Volatile.Read(ref spinningCount) > 0)
            {
                spin.SpinOnce();
            }
            
            // Retrieve amount that wasn't released
            releaseCount = Interlocked.Exchange(ref spinToRelease, 0);
            if (releaseCount == 0)
            {
                leftThatCouldntBeReleased = 0;
                return;
            }
            
            spin = new SpinWait();
            int semaphoreToRelease;
            while (true)
            {
                var sleepingSample = Volatile.Read(ref sleeping);
                semaphoreToRelease = sleepingSample > releaseCount ? releaseCount : sleepingSample;
                if (Interlocked.CompareExchange(ref sleeping, sleepingSample - semaphoreToRelease, sleepingSample) == sleepingSample)
                    break;
                spin.SpinOnce();
            }

            semaphore.Release(semaphoreToRelease);

            leftThatCouldntBeReleased = releaseCount - semaphoreToRelease;
        }

        private class PooledThread
        {
            private static int nextThreadId;
            
            private readonly Thread thread;
            private ThreadPool pool;

            public PooledThread(ThreadPool poolParam)
            {
                pool = poolParam;
                thread = new Thread(Loop)
                {
                    Name = $"{GetType().Name} thread #{Interlocked.Increment(ref nextThreadId)}",
                    IsBackground = true,
                    Priority = ThreadPriority.Highest,
                };
                thread.Start();
            }

            private void Loop()
            {
                do
                {
                    // Logic guarantees one job per initialization and signal
                    object scheduledOp = pool.jobs.Acquire();
                    if (scheduledOp == null)
                        throw new NullReferenceException("Missing job in pool");
                        
                    if (scheduledOp is IConcurrentJob pooledJob)
                    {
                        pooledJob.Work();
                    }
                    else if (scheduledOp is Action action)
                    {
                        try
                        {
                            action.Invoke();
                        }
                        finally
                        {
                            PooledDelegateHelper.Release(action);
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid job type: {scheduledOp}");
                    }

                    if (pool.TryEnterSleep() == false)
                        return;
                } while (true);
            }
        }
        
        /// <summary>
        /// Simple interface to run on <see cref="ThreadPool"/>'s threads,
        /// mostly used to store data to pass to delegate invoked within <see cref="Work()"/>.
        /// </summary>
        internal interface IConcurrentJob
        {
            /// <summary> Called once from any thread to perform work </summary>
            void Work();
        }
    }
}