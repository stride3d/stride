// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Xenko.Core.Collections;

namespace Xenko.Core.Tests
{
    [TestFixture]
    public class TestPriorityLinkedQueue
    {
        [Test]
        public void TestInsertionAscending()
        {
            var priorityQueue = new PriorityNodeQueue<int>();
            for (int i = 0; i < 1000; ++i)
            {
                priorityQueue.Enqueue(i);
            }

            Assert.That(priorityQueue.Count, Is.EqualTo(1000));
            Assert.That(priorityQueue.Peek(), Is.EqualTo(0));

            CheckPriorityQueue(priorityQueue);
        }

        [Test]
        public void TestInsertionDescending()
        {
            var priorityQueue = new PriorityNodeQueue<int>();
            for (int i = 0; i < 1000; ++i)
            {
                priorityQueue.Enqueue(999 - i);
            }

            Assert.That(priorityQueue.Count, Is.EqualTo(1000));
            Assert.That(priorityQueue.Peek(), Is.EqualTo(0));

            CheckPriorityQueue(priorityQueue);
        }

        [Test]
        public void TestInsertionRandom()
        {
            var priorityQueue = new PriorityNodeQueue<int>();
            var random = new Random();
            for (int i = 0; i < 1000; ++i)
            {
                priorityQueue.Enqueue(random.Next());
            }

            Assert.That(priorityQueue.Count, Is.EqualTo(1000));

            CheckPriorityQueue(priorityQueue);
        }

        [Test]
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

            Assert.That(priorityQueue.Count, Is.EqualTo(1000 - 5));

            CheckPriorityQueue(priorityQueue);
        }

        private static void CheckPriorityQueue(PriorityNodeQueue<int> priorityQueue)
        {
            int lastItem = int.MinValue;
            while (!priorityQueue.Empty)
            {
                var value = priorityQueue.Dequeue();
                Assert.That(value, Is.GreaterThanOrEqualTo(lastItem));
                lastItem = value;
            }
        }
    }
}
