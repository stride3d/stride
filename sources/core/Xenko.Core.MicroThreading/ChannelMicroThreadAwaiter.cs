// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Xenko.Core.MicroThreading
{
    public class ChannelMicroThreadAwaiter<T> : ICriticalNotifyCompletion
    {
        private static List<ChannelMicroThreadAwaiter<T>> pool = new List<ChannelMicroThreadAwaiter<T>>();

        private bool isCompleted = false;

        internal MicroThread MicroThread;
        internal Action Continuation;
        internal T Result;

        public bool SuppressCancellationExceptions = false;

        public static ChannelMicroThreadAwaiter<T> New(MicroThread microThread)
        {
            lock (pool)
            {
                if (pool.Count > 0)
                {
                    var index = pool.Count - 1;
                    var lastItem = pool[index];
                    pool.RemoveAt(index);

                    lastItem.MicroThread = microThread;

                    return lastItem;
                }

                return new ChannelMicroThreadAwaiter<T>(microThread);
            }
        }

        public ChannelMicroThreadAwaiter(MicroThread microThread)
        {
            MicroThread = microThread;
        }

        public ChannelMicroThreadAwaiter<T> GetAwaiter()
        {
            return this;
        }

        public void OnCompleted(Action continuation)
        {
            Continuation = continuation;
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            Continuation = continuation;
        }

        public T GetResult()
        {
            // Check Task Result (exception, etc...)
            if (SuppressCancellationExceptions == false)
                MicroThread.CancellationToken.ThrowIfCancellationRequested();

            var result = Result;

            // After result has been taken, we can reuse this item, so put it in the pool
            // We mitigate pool size, but another approach than hard limit might be interesting
            lock (pool)
            {
                if (pool.Count < 4096)
                {
                    isCompleted = false;
                    SuppressCancellationExceptions = false;
                    MicroThread = null;
                    Continuation = null;
                    Result = default(T);
                }

                pool.Add(this);
            }

            return result;
        }

        public bool IsCompleted
        {
            get { return isCompleted || (MicroThread != null && MicroThread.IsOver); }
            set { isCompleted = value; }
        }
    }
}
