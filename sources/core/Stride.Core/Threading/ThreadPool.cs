// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Stride.Core.Annotations;
using System;
using System.Collections.Concurrent;
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
        
        [ThreadStatic]
        static bool isCurrentAWorker;
        
        private readonly ConcurrentQueue<Action> workItems = new ConcurrentQueue<Action>();
        private readonly SemaphoreW semaphore;
        
        private long completionCounter;

        /// <summary> Amount of work completed </summary>
        public ulong CompletedWorkItemCount => (ulong)Volatile.Read(ref completionCounter);
        /// <summary> Is the thread reading this property a worker thread </summary>
        public static bool IsCurrentAWorker => isCurrentAWorker;

        public ThreadPool()
        {
            var threadCount = Environment.ProcessorCount;
            for (int i = 0; i < threadCount; i++)
            {
                NewWorker();
            }
            semaphore = new SemaphoreW(0, int.MaxValue, 70);
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
                workItems.Enqueue(workItem);
            }
            semaphore.Release(amount);
        }

        public bool TryCooperate()
        {
            if (workItems.TryDequeue(out var workItem))
            {
                try
                {
                    workItem.Invoke();
                }
                finally
                {
                    PooledDelegateHelper.Release(workItem);
                }

                Interlocked.Increment(ref completionCounter);
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
            isCurrentAWorker = true;
            try
            {
                while (true)
                {
                    while (TryCooperate())
                    {
                
                    }
                    semaphore.Wait();
                }
            }
            finally
            {
                NewWorker();
            }
        }
    }
}