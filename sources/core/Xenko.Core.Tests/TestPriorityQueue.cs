// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using NUnit.Framework;
using Xenko.Core.Collections;

namespace Xenko.Core.Tests
{
    [TestFixture]
    public class TestPriorityQueue
    {
        [Test]
        public void TestInsertionAscending()
        {
            var priorityQueue = new PriorityQueue<int>();
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
            var priorityQueue = new PriorityQueue<int>();
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
            var priorityQueue = new PriorityQueue<int>();
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

            Assert.That(priorityQueue.Count, Is.EqualTo(1000 - 5));

            CheckPriorityQueue(priorityQueue);
        }

        private static void CheckPriorityQueue(PriorityQueue<int> priorityQueue)
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
