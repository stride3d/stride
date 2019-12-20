// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Xenko.Core.Annotations;

namespace Xenko.Core.Threading
{
    /// <summary>
    /// Thread pool for scheduling actions.
    /// </summary>
    internal class ThreadPool : IDisposable
    {
        /// <summary>
        /// Instance shared across the program.
        /// </summary>
        public static readonly ThreadPool Instance = new ThreadPool();
        
        /// <summary>
        /// Used to assign names to threads.
        /// </summary>
        private static int nextThreadId;
        
        public readonly int PoolSize;

        private readonly IColl<object> queue;
        
        /// <summary>
        /// Reference used to put to sleep and signal threads
        /// </summary>
        private readonly object poolSyncObj = new object();
        
        /// <summary>
        /// Cached delegate to avoid allocating when assigning to new threads.
        /// </summary>
        private readonly ThreadStart cachedThreadLoop;
        
        /// <summary>
        /// Amount of threads waiting for a pulse.
        /// </summary>
        private int idles;
        
        private int disposed;

        public ThreadPool()
        {
            PoolSize = Environment.ProcessorCount-1;
            if (PoolSize >= 13)
                queue = new HighContentionQueue<object>();
            else
                queue = new LowContentionQueue<object>();
                
            cachedThreadLoop = ThreadLoop;
            for (int i = 0; i < PoolSize; i++)
                SpawnThread();
        }

        /// <summary> Signals threads to shutdown asap </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
                return;

            Dispatch(new ShutdownSignal(), PoolSize);
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
            if (amount < 1)
                throw new ArgumentOutOfRangeException(nameof(amount));
            
            queue.Enqueue(job, amount);

            // Only attempt to enter if the lock is not busy,
            // The owner will take care of pulsing other threads if we couldn't enter
            if (Monitor.TryEnter(poolSyncObj))
            {
                try
                {
                    if (idles == 0)
                        return;
            
                    idles--;
                    // Don't pulse all, it takes too much time, let pool threads pulse instead
                    Monitor.Pulse(poolSyncObj);
                }
                finally
                {
                    Monitor.Exit(poolSyncObj);
                }
            }
        }

        private void ThreadLoop()
        {
            try
            {
                do
                {
                    switch (WaitOne())
                    {
                        case IConcurrentJob pooledJob:
                            pooledJob.Work();
                            break;
                        case Action action:
                            try
                            {
                                action.Invoke();
                            }
                            finally
                            {
                                PooledDelegateHelper.Release(action);
                            }
                            break;
                        case ShutdownSignal _:
                            return;
                        case null:
                            throw new NullReferenceException("Missing job in pool");
                        case object obj:
                            throw new ArgumentException($"Invalid job type: {obj.GetType()}");
                    }
                } while (true);
            }
            finally
            {
                // Let this thread throw if it needs to,
                // ensure that there's another one to take its place
                SpawnThread();
            }
        }

        private object WaitOne()
        {
            SpinWait sw = new SpinWait();
            do
            {
                if (queue.TryDequeue(out var item))
                    return item;

                sw.SpinOnce();
                
                if (queue.LatestItemCount == 0 && Monitor.TryEnter(poolSyncObj))
                {
                    try
                    {
                        sw = new SpinWait();
                        if (queue.LatestItemCount == 0)
                        {
                            idles++;
                            Monitor.Wait(poolSyncObj);
                        }
                        
                        if (idles > 0)
                        {
                            idles = 0;
                            Monitor.PulseAll(poolSyncObj);
                        }
                    }
                    finally
                    {
                        Monitor.Exit(poolSyncObj);
                    }
                }
            } while (true);
        }

        private void SpawnThread()
        {
            new Thread(cachedThreadLoop)
            {
                Name = $"{GetType().Name} thread #{Interlocked.Increment(ref nextThreadId)}",
                IsBackground = true,
                Priority = ThreadPriority.Highest,
            }.Start();
        }
        
        /// <summary> Comp-exchange around a ConcurrentQueue </summary>
        private class HighContentionQueue<T> : IColl<T>
        {
            public int LatestItemCount => Volatile.Read(ref queueSize);
            
            /// <summary>
            /// Items scheduled to run on the pool.
            /// Using a lock around a queue leads to better performance on processors with 8 and less HW threads,
            /// needs to be investigated further.
            /// </summary>
            private readonly ConcurrentQueue<T> queue = new ConcurrentQueue<T>();
        
            /// <summary>
            /// Amount of items in the queue, used to lighten the load on <see cref="queue"/>.
            /// </summary>
            private int queueSize;

            public void Enqueue(T item, int amount)
            {
                Interlocked.Add(ref queueSize, amount);
                for (int i = 0; i < amount; i++)
                    queue.Enqueue(item);
            }

            public bool TryDequeue(out T item)
            {
                item = default;
                SpinWait sw = new SpinWait();
                do
                {
                    int sample;
                    while ((sample = Volatile.Read(ref queueSize)) > 0)
                    {
                        if (Interlocked.CompareExchange(ref queueSize, sample - 1, sample) == sample)
                        {
                            sw = new SpinWait();
                            while (queue.TryDequeue(out item) == false)
                            {
                                // Should resolve shortly as we increase queueSize before adding items to the queue
                                sw.SpinOnce();
                            }
                            return true;
                        }
                        sw.SpinOnce();
                    }

                    if (sw.NextSpinWillYield)
                        return false;
                    
                    sw.SpinOnce();
                } while (true);
            }
        }
        
        /// <summary> Spinlock around a queue </summary>
        private class LowContentionQueue<T> : IColl<T>
        {
            public int LatestItemCount => Volatile.Read(ref queueSize);
            
            /// <summary>
            /// Items scheduled to run on the pool.
            /// Using a lock around a queue leads to better performance on processors with 8 and less HW threads,
            /// needs to be investigated further.
            /// </summary>
            private readonly Queue<T> queue = new Queue<T>();
        
            /// <summary>
            /// Amount of items in the queue, used to lighten the load on <see cref="queue"/>.
            /// </summary>
            private int queueSize;
            
            private SpinLock sLock = new SpinLock();

            public void Enqueue(T item, int amount)
            {
                bool entered = false;
                try
                {
                    sLock.Enter(ref entered);
                    Interlocked.Add(ref queueSize, amount);
                    for (int i = 0; i < amount; i++)
                        queue.Enqueue(item);
                }
                finally
                {
                    sLock.Exit(true);
                }
            }

            public bool TryDequeue(out T item)
            {
                bool found = false;
                item = default;
                bool entered = false;
                try
                {
                    sLock.Enter(ref entered);
                    if (queue.Count > 0)
                    {
                        found = true;
                        item = queue.Dequeue();
                    }
                }
                finally
                {
                    sLock.Exit(true);
                }

                if (found)
                {
                    Interlocked.Decrement(ref queueSize);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        interface IColl<T>
        {
            int LatestItemCount { get; }
            void Enqueue(T item, int amount);
            bool TryDequeue(out T item);
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
            
        /// <summary>
        /// When queued, ensure that the thread dequeuing that job shutdowns by itself.
        /// </summary>
        private class ShutdownSignal { }
    }
}
