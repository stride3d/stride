// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
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
        
        /// <summary> Is the thread reading this property a worker thread </summary>
        public static bool IsWorkerThread => isWorkerThread;
        
        private static readonly bool SingleCore;
        [ThreadStatic]
        private static bool isWorkerThread;
        
        private readonly ConcurrentQueue<Action> workItems = new ConcurrentQueue<Action>();
        private readonly ISemaphore semaphore;
        
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
            int spinCount = 70;
            if(RuntimeInformation.ProcessArchitecture is Architecture.Arm or Architecture.Arm64)
            {
                // Dotnet:
                // On systems with ARM processors, more spin-waiting seems to be necessary to avoid perf regressions from incurring
                // the full wait when work becomes available soon enough. This is more noticeable after reducing the number of
                // thread requests made to the thread pool because otherwise the extra thread requests cause threads to do more
                // busy-waiting instead and adding to contention in trying to look for work items, which is less preferable.
                spinCount *= 4;
            }
            try
            {
                semaphore = new DotnetLifoSemaphore(spinCount);
            }
            catch
            {
                // For net6+ this should not happen, logging instead of throwing as this is just a performance regression
                if(Environment.Version.Major >= 6)
                    Console.Out?.WriteLine($"{typeof(ThreadPool).FullName}: Falling back to suboptimal semaphore");
                
                semaphore = new SemaphoreW(spinCountParam:70);
            }

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
            isWorkerThread = true;
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
            semaphore.Dispose();
            while (Volatile.Read(ref leftToDispose) != 0)
            {
                Thread.Yield();
            }

            // Finish any work left
            while (TryCooperate())
            {
                
            }
        }



        private interface ISemaphore : IDisposable
        {
            public void Release( int Count );
            public void Wait( int timeout = - 1 );
        }



        private sealed class DotnetLifoSemaphore : ISemaphore
        {
            private readonly IDisposable semaphore;
            private readonly Func<int, bool, bool> wait;
            private readonly Action<int> release;
            
            public DotnetLifoSemaphore(int spinCount)
            {
                Type lifoType = Type.GetType("System.Threading.LowLevelLifoSemaphore");
                semaphore = Activator.CreateInstance(lifoType, new object[]{ 0, short.MaxValue, spinCount, new Action( () => {} ) }) as IDisposable;
                wait = lifoType.GetMethod("Wait", BindingFlags.Instance | BindingFlags.Public).CreateDelegate<Func<int, bool, bool>>(semaphore);
                release = lifoType.GetMethod("Release", BindingFlags.Instance | BindingFlags.Public).CreateDelegate<Action<int>>(semaphore);
            }



            public void Dispose() => semaphore.Dispose();
            public void Release(int count) => release(count);
            public void Wait(int timeout = -1) => wait(timeout, true);
        }
    }
}
