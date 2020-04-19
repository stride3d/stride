// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xunit;
using Stride.Core.Collections;

namespace Stride.Core.Tests
{
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

            List<PriorityQueueNode<int>> nodes = new List<PriorityQueueNode<int>>();
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
    }
}
