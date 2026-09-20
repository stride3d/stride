// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Collections;

namespace Stride.Core.Tests;

public class TestPriorityQueue
{
    [Fact]
    public void TestInsertionAscending()
    {
        var priorityQueue = new PriorityQueue<int>();
        for (int i = 0; i < 1000; ++i)
        {
            priorityQueue.Enqueue(i);
        }

        Assert.Equal(1000, priorityQueue.Count);
        Assert.Equal(0, priorityQueue.Peek());

        CheckPriorityQueue(priorityQueue);
    }

    [Fact]
    public void TestInsertionDescending()
    {
        var priorityQueue = new PriorityQueue<int>();
        for (int i = 0; i < 1000; ++i)
        {
            priorityQueue.Enqueue(999 - i);
        }

        Assert.Equal(1000, priorityQueue.Count);
        Assert.Equal(0, priorityQueue.Peek());

        CheckPriorityQueue(priorityQueue);
    }

    [Fact]
    public void TestInsertionRandom()
    {
        var priorityQueue = new PriorityQueue<int>();
        var random = new Random();
        for (int i = 0; i < 1000; ++i)
        {
            priorityQueue.Enqueue(random.Next());
        }

        Assert.Equal(1000, priorityQueue.Count);

        CheckPriorityQueue(priorityQueue);
    }

    [Fact]
    public void TestRemoval()
    {
        var priorityQueue = new PriorityQueue<int>();
        for (int i = 0; i < 1000; ++i)
        {
            priorityQueue.Enqueue(i);
        }

        priorityQueue.Remove(3);
        priorityQueue.Remove(0);
        priorityQueue.Remove(500);
        priorityQueue.Remove(251);
        priorityQueue.Remove(999);

        priorityQueue.Remove(1002);

        Assert.Equal(1000 - 5, priorityQueue.Count);

        CheckPriorityQueue(priorityQueue);
    }

    /// <summary>
    /// The item filling the hole is bigger than its new children, so it has to move down.
    /// </summary>
    [Fact]
    public void TestRemovalMovingItemDown()
    {
        var queue = new PriorityQueue<int>();
        foreach (var value in new[] { 0, 1, 4, 2, 5, 6, 7, 3 })
            queue.Enqueue(value);

        queue.Remove(1);

        CheckPriorityQueue(queue, [0, 2, 3, 4, 5, 6, 7]);
    }

    /// <summary>
    /// The item filling the hole is smaller than its new parent, so it has to move up.
    /// </summary>
    [Fact]
    public void TestRemovalMovingItemUp()
    {
        var queue = new PriorityQueue<int>();
        foreach (var value in new[] { 26, 38, 33, 54, 69, 64, 21 })
            queue.Enqueue(value);

        queue.Remove(54);

        CheckPriorityQueue(queue, [21, 26, 33, 38, 64, 69]);
    }

    /// <summary>
    /// The item sitting last leaves no hole behind, so nothing moves.
    /// </summary>
    [Fact]
    public void TestRemovalOfTheLastItem()
    {
        var queue = new PriorityQueue<int>();
        foreach (var value in new[] { 0, 1, 4, 2, 5, 6, 7, 3 })
            queue.Enqueue(value);

        queue.Remove(3);

        CheckPriorityQueue(queue, [0, 1, 2, 4, 5, 6, 7]);
    }

    [Fact]
    public void TestRemovalWithDescendingComparer()
    {
        var queue = new PriorityQueue<int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
        foreach (var value in new[] { 0, 1, 4, 2, 5, 6, 7, 3 })
            queue.Enqueue(value);

        queue.Remove(5);

        CheckPriorityQueue(queue, [7, 6, 4, 3, 2, 1, 0]);
    }

    [Fact]
    public void TestRandomRemoval()
    {
        var random = new Random(0);
        var queue = new PriorityQueue<int>();
        var expected = new List<int>();
        for (int i = 0; i < 1000; ++i)
        {
            var value = random.Next(100);
            queue.Enqueue(value);
            expected.Add(value);
        }

        for (int i = 0; i < 500; ++i)
        {
            var value = expected[random.Next(expected.Count)];
            queue.Remove(value);
            expected.Remove(value);

            Assert.Equal(expected.Count, queue.Count);
        }

        expected.Sort();

        CheckPriorityQueue(queue, expected);
    }

    private static void CheckPriorityQueue(PriorityQueue<int> priorityQueue)
    {
        int lastItem = int.MinValue;
        while (!priorityQueue.Empty)
        {
            var value = priorityQueue.Dequeue();
            Assert.True(value >= lastItem);
            lastItem = value;
        }
    }

    /// <summary>
    /// Drains the queue, which must return every expected item, in the order given.
    /// </summary>
    private static void CheckPriorityQueue(PriorityQueue<int> priorityQueue, IEnumerable<int> expectedItems)
    {
        var items = new List<int>();
        while (!priorityQueue.Empty)
            items.Add(priorityQueue.Dequeue());

        Assert.Equal(expectedItems, items);
    }
}
