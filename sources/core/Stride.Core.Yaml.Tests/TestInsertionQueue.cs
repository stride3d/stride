// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Yaml.Tests;

public class TestInsertionQueue
{
    [Fact]
    public void TestInsertionQueueInitiallyEmpty()
    {
        var queue = new InsertionQueue<int>();

        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void TestInsertionQueueEnqueue()
    {
        var queue = new InsertionQueue<int>();

        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);

        Assert.Equal(3, queue.Count);
    }

    [Fact]
    public void TestInsertionQueueDequeue()
    {
        var queue = new InsertionQueue<int>();
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);

        Assert.Equal(1, queue.Dequeue());
        Assert.Equal(2, queue.Count);
        Assert.Equal(2, queue.Dequeue());
        Assert.Equal(1, queue.Count);
        Assert.Equal(3, queue.Dequeue());
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void TestInsertionQueueDequeueEmptyThrows()
    {
        var queue = new InsertionQueue<int>();

        Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
    }

    [Fact]
    public void TestInsertionQueueDequeueAfterEmptyThrows()
    {
        var queue = new InsertionQueue<int>();
        queue.Enqueue(1);
        queue.Dequeue();

        Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
    }

    [Fact]
    public void TestInsertionQueueInsertAtBeginning()
    {
        var queue = new InsertionQueue<int>();
        queue.Enqueue(1);
        queue.Enqueue(3);

        queue.Insert(0, 0);

        Assert.Equal(3, queue.Count);
        Assert.Equal(0, queue.Dequeue());
        Assert.Equal(1, queue.Dequeue());
        Assert.Equal(3, queue.Dequeue());
    }

    [Fact]
    public void TestInsertionQueueInsertInMiddle()
    {
        var queue = new InsertionQueue<int>();
        queue.Enqueue(1);
        queue.Enqueue(3);

        queue.Insert(1, 2);

        Assert.Equal(3, queue.Count);
        Assert.Equal(1, queue.Dequeue());
        Assert.Equal(2, queue.Dequeue());
        Assert.Equal(3, queue.Dequeue());
    }

    [Fact]
    public void TestInsertionQueueInsertAtEnd()
    {
        var queue = new InsertionQueue<int>();
        queue.Enqueue(1);
        queue.Enqueue(2);

        queue.Insert(2, 3);

        Assert.Equal(3, queue.Count);
        Assert.Equal(1, queue.Dequeue());
        Assert.Equal(2, queue.Dequeue());
        Assert.Equal(3, queue.Dequeue());
    }

    [Fact]
    public void TestInsertionQueueWithStrings()
    {
        var queue = new InsertionQueue<string>();
        queue.Enqueue("a");
        queue.Enqueue("b");
        queue.Enqueue("c");

        Assert.Equal(3, queue.Count);
        Assert.Equal("a", queue.Dequeue());
        Assert.Equal("b", queue.Dequeue());
        Assert.Equal("c", queue.Dequeue());
    }

    [Fact]
    public void TestInsertionQueueMixedOperations()
    {
        var queue = new InsertionQueue<int>();

        queue.Enqueue(1);
        queue.Enqueue(2);
        Assert.Equal(1, queue.Dequeue());
        queue.Enqueue(3);
        queue.Insert(0, 0);

        Assert.Equal(3, queue.Count);
        Assert.Equal(0, queue.Dequeue());
        Assert.Equal(2, queue.Dequeue());
        Assert.Equal(3, queue.Dequeue());
    }

    [Fact]
    public void TestInsertionQueueCountAfterOperations()
    {
        var queue = new InsertionQueue<int>();

        Assert.Equal(0, queue.Count);
        queue.Enqueue(1);
        Assert.Equal(1, queue.Count);
        queue.Enqueue(2);
        Assert.Equal(2, queue.Count);
        queue.Dequeue();
        Assert.Equal(1, queue.Count);
        queue.Insert(0, 0);
        Assert.Equal(2, queue.Count);
        queue.Dequeue();
        Assert.Equal(1, queue.Count);
        queue.Dequeue();
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void TestInsertionQueueWithNullValues()
    {
#nullable enable
        var queue = new InsertionQueue<string?>();
        queue.Enqueue(null);
        queue.Enqueue("test");
        queue.Enqueue(null);

        Assert.Equal(3, queue.Count);
        Assert.Null(queue.Dequeue());
        Assert.Equal("test", queue.Dequeue());
        Assert.Null(queue.Dequeue());
#nullable disable
    }

    [Fact]
    public void TestInsertionQueueLargeNumberOfItems()
    {
        var queue = new InsertionQueue<int>();
        const int count = 1000;

        for (int i = 0; i < count; i++)
        {
            queue.Enqueue(i);
        }

        Assert.Equal(count, queue.Count);

        for (int i = 0; i < count; i++)
        {
            Assert.Equal(i, queue.Dequeue());
        }

        Assert.Equal(0, queue.Count);
    }
}
