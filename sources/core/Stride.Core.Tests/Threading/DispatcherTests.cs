// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using Stride.Core.Threading;
using Xunit;

namespace Stride.Core.Tests.Threading;

public class DispatcherTests
{
    private const int Count = 1000;

    public static TheoryData<string, IReadOnlyList<int>> Collections()
    {
        var items = Enumerable.Range(0, Count).ToArray();
        var collector = new ConcurrentCollector<int>();
        foreach (var item in items)
            collector.Add(item);
        collector.Close();

        return new()
        {
            { "array", items },
            { "List", new List<int>(items) },
            { "ConcurrentCollector", collector },
            { "IReadOnlyList", new ReadOnlyCollection<int>(items) },
        };
    }

    [Theory]
    [MemberData(nameof(Collections))]
    public void ForEach_Visits_Each_Item_Once(string name, IReadOnlyList<int> collection)
    {
        var hits = new int[Count];
        Dispatcher.ForEach(collection, hits, static (hits, item) => Interlocked.Increment(ref hits[item]));
        Assert.True(hits.All(hit => hit == 1), name);
    }

    [Fact]
    public void ForEach_Reads_A_Covariant_Array()
    {
        IReadOnlyList<object> collection = Enumerable.Range(0, Count).Select(i => i.ToString()).ToArray();
        var hits = new int[Count];
        Dispatcher.ForEach(collection, hits, static (hits, item) => Interlocked.Increment(ref hits[int.Parse((string)item)]));
        Assert.All(hits, hit => Assert.Equal(1, hit));
    }

    [Fact]
    public void For_Visits_Each_Index_Once()
    {
        const int from = 5;
        var hits = new int[Count];
        Dispatcher.For(from, from + Count, hits, static (hits, index) => Interlocked.Increment(ref hits[index - from]));
        Assert.All(hits, hit => Assert.Equal(1, hit));
    }

    [Fact]
    public void ForBatched_Covers_The_Range_Once()
    {
        var hits = new int[Count];
        Dispatcher.ForBatched(Count, hits, static (hits, from, toExclusive) =>
        {
            for (int i = from; i < toExclusive; i++)
                Interlocked.Increment(ref hits[i]);
        });
        Assert.All(hits, hit => Assert.Equal(1, hit));
    }

    [Fact]
    public void Empty_Range_Does_Not_Call_The_Callback()
    {
        var calls = new int[1];
        Dispatcher.For(3, 3, calls, static (calls, _) => Interlocked.Increment(ref calls[0]));
        Dispatcher.ForEach(Array.Empty<int>(), calls, static (calls, _) => Interlocked.Increment(ref calls[0]));
        Assert.Equal(0, calls[0]);
    }

    [Fact]
    public void Exception_Is_Rethrown_To_The_Caller()
    {
        var items = Enumerable.Range(0, Count).ToList();
        Assert.Throws<InvalidOperationException>(() => Dispatcher.ForEach(items, 500, static (failingItem, item) =>
        {
            if (item == failingItem)
                throw new InvalidOperationException();
        }));
    }
}
