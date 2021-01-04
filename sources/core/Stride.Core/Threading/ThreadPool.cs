// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Stride.Core.Threading
{
    /// <summary>
    /// Thread pool for scheduling sub-millisecond actions, do not schedule long-running tasks.
    /// Can be instantiated and generates less garbage than dotnet's.
    /// </summary>
    public sealed partial class ThreadPool : IDisposable
    {
        /// <summary>
        /// The default instance that the whole process shares, use this one to avoid wasting process memory.
        /// </summary>
        public static ThreadPool Instance = new ThreadPool();
        
        private static readonly bool SingleCore;
        [ThreadStatic]
        private static bool isWorkedThread;
        /// <summary> Is the thread reading this property a worker thread </summary>
        public static bool IsWorkedThread => isWorkedThread;
        
        private readonly ConcurrentQueue<Action> workItems = new ConcurrentQueue<Action>();
        private readonly SemaphoreW semaphore;
        
        private long completionCounter;
        private int workScheduled, threadsBusy;
        private int disposing;
        private int leftToDispose;

        /// <summary> Amount of threads within this pool </summary>
        public readonly int WorkerThreadsCount;
        /// <summary> Amount of work waiting to be taken care of </summary>
        public int WorkScheduled => Volatile.Read(ref workScheduled);
        /// <summary> Amount of work completed </summary>
        public ulong CompletedWork => (ulong)Volatile.Read(ref completionCounter);
        /// <summary> Amount of threads currently executing work items </summary>
        public int ThreadsBusy => Volatile.Read(ref threadsBusy);

        public ThreadPool(int? threadCount = null)
        {
            semaphore = new SemaphoreW(spinCountParam:70);
            
            WorkerThreadsCount = threadCount ?? (Environment.ProcessorCount == 1 ? 1 : Environment.ProcessorCount - 1);
            leftToDispose = WorkerThreadsCount;
            for (int i = 0; i < WorkerThreadsCount; i++)
            {
                NewWorker();
            }
        }

        static ThreadPool()
        {
            SingleCore = Environment.ProcessorCount < 2;
        }

        /// <summary>
        /// Queue an action to run on one of the available threads,
        /// it is strongly recommended that the action takes less than a millisecond.
        /// </summary>
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

            if (disposing > 0)
            {
                throw new ObjectDisposedException(ToString());
            }

            Interlocked.Add(ref workScheduled, amount);
            for (int i = 0; i < amount; i++)
            {
                PooledDelegateHelper.AddReference(workItem);
                workItems.Enqueue(workItem);
            }
            semaphore.Release(amount);
        }

        /// <summary>
        /// Attempt to steal work from the threadpool to execute it from the calling thread.
        /// If you absolutely have to block inside one of the threadpool's thread for whatever
        /// reason do a busy loop over this function.
        /// </summary>
        public bool TryCooperate()
        {
            if (workItems.TryDequeue(out var workItem))
            {
                Interlocked.Increment(ref threadsBusy);
                Interlocked.Decrement(ref workScheduled);
                try
                {
                    workItem.Invoke();
                }
                finally
                {
                    PooledDelegateHelper.Release(workItem);
                    Interlocked.Decrement(ref threadsBusy);
                    Interlocked.Increment(ref completionCounter);
                }
                return true;
            }

            return false;
        }

        private void NewWorker()
        {
            new Thread(WorkerThreadScope)
            {
                Name = $"{GetType().FullName} thread",
                IsBackground = true,
                Priority = ThreadPriority.Highest,
            }.Start();
        }

        private void WorkerThreadScope()
        {
            isWorkedThread = true;
            try
            {
                do
                {
                    while (TryCooperate())
                    {
                        
                    }
                    
                    if (disposing > 0)
                    {
                        return;
                    }

                    semaphore.Wait();
                } while (true);
            }
            finally
            {
                if (disposing == 0)
                {
                    NewWorker();
                }
                else
                {
                    Interlocked.Decrement(ref leftToDispose);
                }
            }
        }



        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref disposing, 1, 0) == 1)
            {
                return;
            }
            
            semaphore.Release(WorkerThreadsCount);
            while (Volatile.Read(ref leftToDispose) != 0)
            {
                if (semaphore.SignalCount == 0)
                {
                    semaphore.Release(1);
                }
                Thread.Yield();
            }

            // Finish any work left
            while (TryCooperate())
            {
                
            }
        }
    }
}
