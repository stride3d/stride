// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Stride.Core.Threading;
using Xunit;

namespace Stride.Core.Tests.Threading;

public class DispatcherTests
{
    public static TheoryData<string, int> SortCases() => new()
    {
        // Over 2,048 items the sort splits the list and sorts the parts on several threads
        { "random", 3000 },
        { "random", 100_000 },
        { "few keys", 10_000 },
        { "sorted", 10_000 },
        { "reversed", 10_000 },
        { "all equal", 10_000 },
        { "random", 100 },
        // The largest list sorted on one thread, and the smallest one split
        { "random", 2048 },
        { "random", 2049 },
        { "few keys", 2049 },
    };

    [Theory]
    [MemberData(nameof(SortCases))]
    public void Sort_Orders_The_Items(string kind, int count)
    {
        var random = new Random(count);
        var items = Enumerable.Range(0, count).Select(i => kind switch
        {
            "few keys" => random.Next(3),
            "sorted" => i,
            "reversed" => count - i,
            "all equal" => 7,
            _ => random.Next(),
        }).ToArray();
        var expected = items.Order().ToArray();

        Dispatcher.Sort(items, 0, items.Length, Comparer<int>.Default);

        Assert.Equal(expected, items);
    }

    [Fact]
    public void Sort_Of_A_Range_Leaves_The_Rest()
    {
        var random = new Random(5);
        var items = Enumerable.Range(0, 20_000).Select(_ => random.Next()).ToArray();
        var before = (int[])items.Clone();

        Dispatcher.Sort(items, 1000, 15_000, Comparer<int>.Default);

        Assert.Equal(before[..1000], items[..1000]);
        Assert.Equal(before[16_000..], items[16_000..]);
        Assert.Equal(before[1000..16_000].Order(), items[1000..16_000]);
    }

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
