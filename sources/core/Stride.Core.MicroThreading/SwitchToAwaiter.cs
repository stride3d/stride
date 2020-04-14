// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Xenko.Core.MicroThreading
{
#if NET45
    public class SwitchToAwaiter : INotifyCompletion
#else
    public class SwitchToAwaiter
#endif
    {
        private Scheduler scheduler;

        private MicroThread microThread;

        public SwitchToAwaiter(Scheduler scheduler)
        {
            this.scheduler = scheduler;
            this.microThread = null;
        }

        public bool IsCompleted
        {
            get { return false; }
        }

        public void OnCompleted(Action continuation)
        {
            microThread = scheduler.Add(() =>
            {
                continuation();
                return Task.FromResult(true);
            });
        }

        public IDisposable GetResult()
        {
            return new SwitchMicroThread(microThread);
        }

        public SwitchToAwaiter GetAwaiter()
        {
            return this;
        }

        private struct SwitchMicroThread : IDisposable
        {
            private MicroThread microThread;

            public SwitchMicroThread(MicroThread microThread)
            {
                this.microThread = microThread;
                //microThread.SynchronizationContext.IncrementTaskCount();
            }

            public void Dispose()
            {
                //microThread.SynchronizationContext.DecrementTaskCount();
            }
        }
    }
}
