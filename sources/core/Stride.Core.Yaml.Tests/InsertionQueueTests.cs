// Copyright (c) 2015 SharpYaml - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// -------------------------------------------------------------------------------
// SharpYaml is a fork of YamlDotNet https://github.com/aaubry/YamlDotNet
// published with the following license:
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2008, 2009, 2010, 2011, 2012 Antoine Aubry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Stride.Core.Yaml.Tests
{
    public class InsertionQueueTests
    {
        [Fact]
        public void ShouldThrowExceptionWhenDequeuingEmptyContainer()
        {
            var queue = CreateQueue();

            Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
        }

        [Fact]
        public void ShouldThrowExceptionWhenDequeuingContainerThatBecomesEmpty()
        {
            var queue = new InsertionQueue<int>();

            queue.Enqueue(1);
            queue.Dequeue();

            Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
        }

        [Fact]
        public void ShouldCorrectlyDequeueElementsAfterEnqueuing()
        {
            var queue = CreateQueue();

            WithTheRange(0, 10).Perform(queue.Enqueue);

            Assert.Equal(new List<int>() {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}, OrderOfElementsIn(queue));
        }

        [Fact]
        public void ShouldCorrectlyDequeueElementsWhenIntermixingEnqueuing()
        {
            var queue = CreateQueue();

            WithTheRange(0, 10).Perform(queue.Enqueue);
            PerformTimes(5, queue.Dequeue);
            WithTheRange(10, 15).Perform(queue.Enqueue);

            Assert.Equal(new List<int>() {5, 6, 7, 8, 9, 10, 11, 12, 13, 14}, OrderOfElementsIn(queue));
        }

        [Fact]
        public void ShouldThrowExceptionWhenDequeuingAfterInserting()
        {
            var queue = CreateQueue();

            queue.Enqueue(1);
            queue.Insert(0, 99);
            PerformTimes(2, queue.Dequeue);

            Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
        }

        [Fact]
        public void ShouldCorrectlyDequeueElementsWhenInserting()
        {
            var queue = CreateQueue();

            WithTheRange(0, 10).Perform(queue.Enqueue);
            queue.Insert(5, 99);

            Assert.Equal(new List<int>() {0, 1, 2, 3, 4, 99, 5, 6, 7, 8, 9}, OrderOfElementsIn(queue));
        }

        private static InsertionQueue<int> CreateQueue()
        {
            return new InsertionQueue<int>();
        }

        private IEnumerable<int> WithTheRange(int from, int to)
        {
            return Enumerable.Range(@from, to - @from);
        }

        private IEnumerable<int> OrderOfElementsIn(InsertionQueue<int> queue)
        {
            while (true)
            {
                if (queue.Count == 0)
                {
                    yield break;
                }
                yield return queue.Dequeue();
            }
        }

        private void PerformTimes(int times, Func<int> func)
        {
            WithTheRange(0, times).Perform(func);
        }
    }

    public static class EnumerableExtensions
    {
        public static void Perform<T>(this IEnumerable<T> withRange, Func<int> func)
        {
            withRange.Perform(x => func());
        }

        public static void Perform<T>(this IEnumerable<T> withRange, Action<T> action)
        {
            foreach (var element in withRange)
            {
                action(element);
            }
        }
    }
}
