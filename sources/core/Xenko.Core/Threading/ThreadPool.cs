// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xenko.Core.Annotations;

namespace Xenko.Core.Threading
{
    /// <summary>
    /// Thread pool for scheduling actions.
    /// </summary>
    /// <remarks>
    /// Base on Stephen Toub's ManagedThreadPool
    /// </remarks>
    internal class ThreadPool
    {
        public static readonly ThreadPool Instance = new ThreadPool();

        private readonly int maxThreadCount = Environment.ProcessorCount + 2;
        private readonly ConcurrentQueue<Action> workItems = new ConcurrentQueue<Action>();
        private EventWaitHandle waiter = new EventWaitHandle(false, EventResetMode.AutoReset);
        private int poolCount;

        public void QueueWorkItem([NotNull] [Pooled] Action workItem)
        {
            PooledDelegateHelper.AddReference(workItem);
            workItems.Enqueue(workItem);
            waiter.Set();
            if (poolCount < maxThreadCount)
            {
                poolCount++;
                Task.Factory.StartNew(ProcessWorkItems, TaskCreationOptions.LongRunning);
            }
        }

        private void ProcessWorkItems(object state)
        {
            while(true)
            {
                if (workItems.TryDequeue(out Action workItem)) {
                    try {
                        workItem.Invoke();
                    } catch(Exception e) { /* ignore exception */ }
                    PooledDelegateHelper.Release(workItem);
                };

                if (waiter.WaitOne(5000) == false) {
                    poolCount--;
                    return;
                }
            }
        }
    }
}
