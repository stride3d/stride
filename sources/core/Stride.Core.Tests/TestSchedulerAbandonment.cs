// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using Stride.Core.MicroThreading;

namespace Stride.Core.Tests;

public class TestSchedulerAbandonment
{
    [Fact]
    public void LateContinuationOnAbandonedSchedulerIsIgnored()
    {
        var scheduler = new Scheduler();
        var tcs = new TaskCompletionSource<int>();
        var resumedAfterAwait = false;
        var microThread = scheduler.Add(async () =>
        {
            await tcs.Task;
            resumedAfterAwait = true;
        });

        // Run until the microthread suspends on the await, then abandon the scheduler
        scheduler.Run();
        Assert.Equal(MicroThreadState.Running, microThread.State);

        // Completing the awaited task posts a continuation to the abandoned scheduler; it must not throw
        tcs.SetResult(0);

        Assert.Null(scheduler.RunningMicroThread);
        Assert.False(resumedAfterAwait);
    }
}
