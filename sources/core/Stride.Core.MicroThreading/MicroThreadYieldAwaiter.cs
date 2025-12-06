// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Stride.Core.MicroThreading;

public readonly struct MicroThreadYieldAwaiter : INotifyCompletion
{
    private readonly MicroThread microThread;

    public MicroThreadYieldAwaiter(MicroThread microThread)
    {
        this.microThread = microThread;
    }

    public readonly MicroThreadYieldAwaiter GetAwaiter()
    {
        return this;
    }

    public readonly bool IsCompleted
    {
        get
        {
            if (microThread.IsOver)
                return true;

            return microThread.Scheduler.HasNoEntriesScheduled();
        }
    }

    public readonly void GetResult()
    {
        microThread.CancellationToken.ThrowIfCancellationRequested();
    }

    public readonly void OnCompleted(Action continuation)
    {
        microThread.ScheduleContinuation(ScheduleMode.Last, continuation);
    }
}
