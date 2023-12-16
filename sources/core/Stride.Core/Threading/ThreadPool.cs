// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable CS8500 // Pointer not constrained to managed type

using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
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

        private static readonly ProfilingKey ProcessWorkItemKey = new ProfilingKey($"{nameof(ThreadPool)}.ProcessWorkItem");

        private readonly ConcurrentQueue<Work> workItems = new ConcurrentQueue<Work>();
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
        public unsafe void QueueWorkItem([NotNull, Pooled] Action workItem, int amount = 1)
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
                workItems.Enqueue(new()
                {
                    WorkHandler = &Adapter,
                    Action = workItem
                });
            }
            semaphore.Release(amount);

            static void Adapter(in Work workItem)
            {
                try
                {
                    workItem.Action();
                }
                finally
                {
                    PooledDelegateHelper.Release(workItem.Action);
                }
            }
        }

        /// <summary>
        /// Queue some work item to run on one of the available threads,
        /// it is strongly recommended that the action takes less than a millisecond.
        /// Additionally, the parameter provided must be fixed from this call onward until the action has finished executing
        /// </summary>
        public unsafe void QueueUnsafeWorkItem<T>(T* parameter, int amount = 1) where T : IJob
        {
            if (parameter == null)
            {
                throw new NullReferenceException(nameof(parameter));
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
                workItems.Enqueue(new()
                {
                    DataPtr = parameter,
                    WorkHandler = &WorkHandler<T>
                });
            }
            semaphore.Release(amount);
        }

        /// <summary>
        /// We want generics for optimal performance and flexibility, but we would rather have one collection with all work items instead of one collection per generics -
        /// we can use a typed generic function pointer to take care of casting the data to the right type and process it appropriately
        /// </summary>
        static unsafe void WorkHandler<T>(in Work workItem) where T : IJob
        {
            // Do not make this function local to 'QueueUnsafeWorkItem', its mangled name can make debugging confusing

            // The IWork constraint should help the JIT inline the following call
            ((T*)workItem.DataPtr)->Execute();
        }

        /// <summary>
        /// Attempt to steal work from the threadpool to execute it from the calling thread.
        /// If you absolutely have to block inside one of the threadpool's thread for whatever
        /// reason do a busy loop over this function.
        /// </summary>
        public unsafe bool TryCooperate()
        {
            if (workItems.TryDequeue(out var workItem))
            {
                Interlocked.Increment(ref threadsBusy);
                Interlocked.Decrement(ref workScheduled);
                try
                {
                    using (Profiler.Begin(ProcessWorkItemKey))
                        workItem.WorkHandler(workItem);
                }
                finally
                {
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

        public interface IJob
        {
            /// <summary>
            /// This structure will be shared across all threads that have been scheduled for a given job; each threads read and write to the same region in memory
            /// </summary>
            void Execute();
        }

        unsafe struct Work
        {
            public void* DataPtr;
            public delegate*<in Work, void> WorkHandler;
            public Action Action;
        }
    }
}
