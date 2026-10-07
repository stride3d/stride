// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Collections;

namespace Stride.Core.Tests;

public class TestPriorityLinkedQueue
{
    [Fact]
    public void TestInsertionAscending()
    {
        var priorityQueue = new PriorityNodeQueue<int>();
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
        var priorityQueue = new PriorityNodeQueue<int>();
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
        var priorityQueue = new PriorityNodeQueue<int>();
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
        var priorityQueue = new PriorityNodeQueue<int>();

        List<PriorityQueueNode<int>> nodes = [];
        for (int i = 0; i < 1000; ++i)
        {
            nodes.Add(priorityQueue.Enqueue(i));
        }

        priorityQueue.Remove(nodes[3]);
        priorityQueue.Remove(nodes[0]);
        priorityQueue.Remove(nodes[500]);
        priorityQueue.Remove(nodes[251]);
        priorityQueue.Remove(nodes[999]);

        Assert.Equal(1000 - 5, priorityQueue.Count);

        CheckPriorityQueue(priorityQueue);
    }

    /// <summary>
    /// The node filling the hole is bigger than its new children, so it has to move down.
    /// </summary>
    [Fact]
    public void TestRemovalMovingNodeDown()
    {
        var queue = new PriorityNodeQueue<int>();
        var nodes = Enqueue(queue, [0, 1, 4, 2, 5, 6, 7, 3]);

        queue.Remove(nodes[1]);

        Assert.Equal(-1, nodes[1].Index);
        CheckPriorityQueue(queue, [0, 2, 3, 4, 5, 6, 7]);
    }

    /// <summary>
    /// The node filling the hole is smaller than its new parent, so it has to move up.
    /// </summary>
    [Fact]
    public void TestRemovalMovingNodeUp()
    {
        var queue = new PriorityNodeQueue<int>();
        var nodes = Enqueue(queue, [0, 1, 4, 2, 5, 6, 7, 3]);

        queue.Remove(nodes[4]);

        Assert.Equal(-1, nodes[4].Index);
        CheckPriorityQueue(queue, [0, 1, 2, 3, 4, 6, 7]);
    }

    /// <summary>
    /// The node sitting last leaves no hole behind, so nothing moves.
    /// </summary>
    [Fact]
    public void TestRemovalOfTheLastNode()
    {
        var queue = new PriorityNodeQueue<int>();
        var nodes = Enqueue(queue, [0, 1, 4, 2, 5, 6, 7, 3]);

        queue.Remove(nodes[7]);

        Assert.Equal(-1, nodes[7].Index);
        CheckPriorityQueue(queue, [0, 1, 2, 4, 5, 6, 7]);
    }

    [Fact]
    public void TestRemovalWithDescendingComparer()
    {
        var queue = new PriorityNodeQueue<int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
        var nodes = Enqueue(queue, [0, 1, 4, 2, 5, 6, 7, 3]);

        queue.Remove(nodes[4]);

        CheckPriorityQueue(queue, [7, 6, 4, 3, 2, 1, 0]);
    }

    [Fact]
    public void TestRandomRemoval()
    {
        var random = new Random(0);
        var queue = new PriorityNodeQueue<int>();
        List<PriorityQueueNode<int>> nodes = [];
        for (int i = 0; i < 1000; ++i)
        {
            nodes.Add(queue.Enqueue(random.Next(100)));
        }

        var expected = nodes.Select(x => x.Value).ToList();
        for (int i = 0; i < 500; ++i)
        {
            var node = nodes[random.Next(nodes.Count)];
            queue.Remove(node);
            nodes.Remove(node);
            expected.Remove(node.Value);

            Assert.Equal(-1, node.Index);
            Assert.Equal(expected.Count, queue.Count);
        }

        expected.Sort();

        CheckPriorityQueue(queue, expected);
    }

    [Fact]
    public void TestClearReleasesNodes()
    {
        var queue = new PriorityNodeQueue<int>();
        var node = queue.Enqueue(5);

        queue.Clear();

        Assert.Equal(-1, node.Index);

        // A released node belongs to no queue anymore, so it can be queued again
        queue.Enqueue(node);

        Assert.Equal(5, queue.Peek());
    }

    private static List<PriorityQueueNode<int>> Enqueue(PriorityNodeQueue<int> priorityQueue, int[] values)
    {
        List<PriorityQueueNode<int>> nodes = [];
        foreach (var value in values)
        {
            nodes.Add(priorityQueue.Enqueue(value));
        }

        return nodes;
    }

    private static void CheckPriorityQueue(PriorityNodeQueue<int> priorityQueue)
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
    private static void CheckPriorityQueue(PriorityNodeQueue<int> priorityQueue, IEnumerable<int> expectedItems)
    {
        var items = new List<int>();
        while (!priorityQueue.Empty)
            items.Add(priorityQueue.Dequeue());

        Assert.Equal(expectedItems, items);
    }
}
