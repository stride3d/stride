// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.CompilerServices;

namespace Stride.Core.MicroThreading
{
    public struct MicroThreadYieldAwaiter : INotifyCompletion
    {
        private readonly MicroThread microThread;

        public MicroThreadYieldAwaiter(MicroThread microThread)
        {
            this.microThread = microThread;
        }

        public MicroThreadYieldAwaiter GetAwaiter()
        {
            return this;
        }

        public bool IsCompleted
        {
            get
            {
                if (microThread.IsOver)
                    return true;

                lock (microThread.Scheduler.ScheduledEntries)
                {
                    return microThread.Scheduler.ScheduledEntries.Count == 0;
                }
            }
        }

        public void GetResult()
        {
            microThread.CancellationToken.ThrowIfCancellationRequested();
        }

        public void OnCompleted(Action continuation)
        {
            microThread.ScheduleContinuation(ScheduleMode.Last, continuation);
        }
    }
}
