// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Stride.Core.Threading;
using Xunit;

namespace Stride.Core.Tests.Threading;

public class DispatcherTests
{
    [Fact]
    public void Slow_Items_Run_On_Several_Threads()
    {
        if (Dispatcher.MaxDegreeOfParallelism <= 1 || Environment.ProcessorCount <= 1)
            return;

        // Each helper wakes the next one, so a job of slow items still spreads past the caller
        var threads = new ConcurrentDictionary<int, bool>();
        Dispatcher.For(0, Dispatcher.MaxDegreeOfParallelism * 4, _ =>
        {
            threads.TryAdd(Environment.CurrentManagedThreadId, true);
            Thread.Sleep(1);
        });
        Assert.True(threads.Count > 1, $"{threads.Count} thread(s)");
    }

    [Fact]
    public void Many_Short_And_Nested_Jobs_Cover_Their_Ranges_Once()
    {
        const int jobs = 2000, size = 50;
        var hits = new int[jobs * size];
        Dispatcher.For(0, jobs, job =>
            Dispatcher.For(job * size, (job + 1) * size, index => Interlocked.Increment(ref hits[index])));
        for (int job = 0; job < jobs; job++)
            Dispatcher.For(job * size, (job + 1) * size, index => Interlocked.Increment(ref hits[index]));
        Assert.All(hits, hit => Assert.Equal(2, hit));
    }
}
