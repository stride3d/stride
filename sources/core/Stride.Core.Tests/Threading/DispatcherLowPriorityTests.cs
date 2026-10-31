// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Threading;
using Xunit;

namespace Stride.Core.Tests.Threading;

public class DispatcherLowPriorityTests
{
    private const int Count = 1000;

    [Fact]
    public async Task ForBatched_Covers_The_Range_Once()
    {
        var hits = new int[Count];

        await DispatcherLowPriority.ForBatchedAsync(Count, (start, endExclusive) =>
        {
            for (int i = start; i < endExclusive; i++)
                Interlocked.Increment(ref hits[i]);
        });

        Assert.All(hits, hit => Assert.Equal(1, hit));
    }

    [Fact]
    public async Task Empty_Range_Does_Not_Call_The_Callback()
    {
        var calls = new int[1];

        await DispatcherLowPriority.ForBatchedAsync(0, (start, endExclusive) =>
        {
            for (int i = start; i < endExclusive; i++)
                Interlocked.Increment(ref calls[0]);
        });

        Assert.Equal(0, calls[0]);
    }

    [Fact]
    public async Task Range_Smaller_Than_Pool()
    {
        var hits = new int[1];
        
        Assert.True(DispatcherLowPriority.MaxDegreeOfParallelism > 1);

        await DispatcherLowPriority.ForBatchedAsync(1, (start, endExclusive) =>
        {
            for (int i = start; i < endExclusive; i++)
                Interlocked.Increment(ref hits[i]);
        });

        Assert.Equal(1, hits[0]);
    }

    [Fact]
    public async Task Exception_Is_Rethrown_To_The_Caller()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await DispatcherLowPriority.ForBatchedAsync(Count, (start, endExclusive) =>
        {
            for (int i = start; i < endExclusive; i++)
            {
                if (i == Count / 2)
                    throw new InvalidOperationException();
            }
        }));
    }
}
