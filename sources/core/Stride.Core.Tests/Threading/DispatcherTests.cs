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

    [Fact]
    public void Sort_Rethrows_A_Comparer_Exception_Thrown_On_A_Helper()
    {
        if (Dispatcher.MaxDegreeOfParallelism <= 1)
            return;

        var random = new Random(3);
        var items = Enumerable.Range(0, 1_000_000).Select(_ => random.Next()).ToArray();
        var caller = Environment.CurrentManagedThreadId;
        // Throws on the helpers only: the caller goes on with its parts until it sees the exception
        var comparer = Comparer<int>.Create((x, y) => Environment.CurrentManagedThreadId != caller ? throw new InvalidOperationException() : x.CompareTo(y));

        // Thrown by a partition as it is, wrapped by Array.Sort in an InvalidOperationException
        Assert.Throws<InvalidOperationException>(() => Dispatcher.Sort(items, 0, items.Length, comparer));
    }

    [Fact]
    public void Sort_Stops_Its_Helpers_Before_It_Rethrows()
    {
        if (Dispatcher.MaxDegreeOfParallelism <= 1)
            return;

        var random = new Random(4);
        var items = Enumerable.Range(0, 1_000_000).Select(_ => random.Next()).ToArray();
        var caller = Environment.CurrentManagedThreadId;
        var helperStarted = 0;
        // Throws on the caller once a helper works on the list
        var comparer = Comparer<int>.Create((x, y) =>
        {
            if (Environment.CurrentManagedThreadId != caller)
                Volatile.Write(ref helperStarted, 1);
            else if (Volatile.Read(ref helperStarted) != 0)
                throw new InvalidOperationException();
            return x.CompareTo(y);
        });

        Assert.Throws<InvalidOperationException>(() => Dispatcher.Sort(items, 0, items.Length, comparer));

        // No thread writes to the list once Sort has thrown
        var after = (int[])items.Clone();
        Thread.Sleep(100);
        Assert.Equal(after, items);

        // And the next sort is not slowed or broken by them
        Dispatcher.Sort(items, 0, items.Length, Comparer<int>.Default);
        Assert.Equal(after.Order(), items);
    }
}
