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

        private readonly ConcurrentFixedPool<PooledThread> pool;

        public ThreadPool()
        {
            int nextPow2 = Environment.ProcessorCount * 4;
            if (nextPow2 >= 1073741824)
            {
                nextPow2 = 1073741824;
            }
            else if (nextPow2 <= 0)
            {
                nextPow2 = 1;
            }
            else
            {
                nextPow2--;
                nextPow2 |= nextPow2 >> 1;
                nextPow2 |= nextPow2 >> 2;
                nextPow2 |= nextPow2 >> 4;
                nextPow2 |= nextPow2 >> 8;
                nextPow2 |= nextPow2 >> 16;
                nextPow2++;
            }

            PoolSize = nextPow2;
            pool = new ConcurrentFixedPool<PooledThread>(PoolSize);
        }

        /// <summary> Schedule an action to be run on one of the <see cref="ThreadPool"/>'s threads </summary>
        /// <exception cref="ArgumentNullException"><see cref="workItem"/> is null</exception>
        public void QueueWorkItem([NotNull, Pooled] Action workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }
            
            pool.Pop().Signal(workItem, pool);
        }

        /// <summary> Schedule a job to be run on one of or multiple <see cref="ThreadPool"/>'s threads </summary>
        /// <exception cref="ArgumentNullException"><see cref="jobParam"/> is null</exception>
        public void DispatchJob([NotNull] IConcurrentJob jobParam)
        {
            if (jobParam == null)
            {
                throw new ArgumentNullException(nameof(jobParam));
            }
            
            pool.Pop().Signal(jobParam, pool);
        }

        private class PooledThread
        {
            private const int SIGNAL_IDLE = 0;
            private const int SIGNAL_WORK = 1;
            private const int SIGNAL_SLEEPING = 2;
            
            private static int nextThreadId;
            
            private readonly Thread thread;
            private readonly ManualResetEventSlim mre = new ManualResetEventSlim(true);
            private ConcurrentFixedPool<PooledThread> pool;
            private object job;
            private bool init;
            private int currentSignal;

            public PooledThread()
            {
                thread = new Thread(Loop)
                {
                    Name = $"{GetType().Name} thread #{Interlocked.Increment(ref nextThreadId)}",
                    IsBackground = true,
                    Priority = ThreadPriority.Highest,
                };
            }

            public void Signal(object newJob, ConcurrentFixedPool<PooledThread> poolParam)
            {
                Interlocked.Exchange(ref job, newJob);
                if (Interlocked.Exchange(ref currentSignal, SIGNAL_WORK) == SIGNAL_SLEEPING)
                {
                    mre.Set();
                }

                if (init == false)
                {
                    init = true;
                    pool = poolParam;
                    thread.Start();
                }
            }

            private void Loop()
            {
                do
                {
                    var scheduledOp = Interlocked.Exchange(ref job, null);
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

                    Interlocked.Exchange(ref currentSignal, SIGNAL_IDLE);
                    // We're idle, push this thread onto the stack to wait for signal
                    pool.Push(this);
                    
                    SpinWait sw = new SpinWait();
                    do
                    {
                        if (Volatile.Read(ref currentSignal) == SIGNAL_WORK)
                        {
                            break;
                        }

                        if (sw.NextSpinWillYield)
                        {
                            mre.Reset();
                            // Check for last-minute signal to work, otherwise sleep
                            int previousSignal = Interlocked.CompareExchange(ref currentSignal, SIGNAL_SLEEPING, SIGNAL_IDLE);
                            if (previousSignal == SIGNAL_WORK)
                            {
                                break;
                            }

                            mre.Wait();
                            break;
                        }
                        else
                        {
                            sw.SpinOnce();
                        }
                    } while (true);
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