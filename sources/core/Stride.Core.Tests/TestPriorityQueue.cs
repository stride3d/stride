// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xunit;
using Stride.Core.Collections;

namespace Stride.Core.Tests
{
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
    }
}
