// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;

namespace Stride.Core.Threading
{
    /// <summary>
    /// A concurrent object pool.
    /// </summary>
    /// <remarks>
    /// Circular buffer segments are used as storage. When full, new segments are added as tail. Items are only appended to the tail segment.
    /// When the head segment is empty, it will be discarded. After stabilizing, only a single segment exists at a time, causing no further segment allocations or locking.
    /// </remarks>
    /// <typeparam name="T">The pooled item type</typeparam>
    public class ConcurrentPool<T>
        where T : class
    {
        private class Segment
        {
            /// <summary>
            /// The array of items. Length must be a power of two.
            /// </summary>
            public readonly T[] Items;

            /// <summary>
            /// A bit mask for calculation of (Low % Items.Length) and (High % Items.Length)
            /// </summary>
            public readonly int Mask;

            /// <summary>
            /// The read index for Release. It is only ever incremented and safe to overflow.
            /// </summary>
            public int Low;

            /// <summary>
            /// The write index for Acquire. It is only ever incremented and safe to overflow.
            /// </summary>
            public int High;

            /// <summary>
            /// The current number of stored items, used to check when to change head and tail segments.
            /// When it reaches zero, the segment can be safely discarded.
            /// </summary>
            public int Count;

            /// <summary>
            /// The next segment to draw from, after this one is emptied.
            /// </summary>
            public Segment Next;

            public Segment(int size)
            {
                // Size must be a power of two for modulo and overflow of read/write indices to behave correctly
                if (size <= 0 || ((size & (size - 1)) != 0))
                    throw new ArgumentOutOfRangeException(nameof(size), "Must be power of two");

                Items = new T[size];
                Mask = size - 1;
            }
        }

        private const int DefaultCapacity = 4;

        private readonly object resizeLock = new object();
        private readonly Func<T> factory;
        private Segment head;
        private Segment tail;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentPool{T}"/> class.
        /// </summary>
        /// <param name="factory">The factory method for creating new items, should the pool be empty.</param>
        public ConcurrentPool(Func<T> factory)
        {
            head = tail = new Segment(DefaultCapacity);
            this.factory = factory;
        }

        /// <summary>
        /// Draws an item from the pool.
        /// </summary>
        public T Acquire()
        {
            while (true)
            {
                var localHead = head;
                var count = localHead.Count;

                if (count == 0)
                {
                    // If first segment is empty, but there is at least one other, move the head forward.
                    if (localHead.Next != null)
                    {
                        lock (resizeLock)
                        {
                            if (head.Next != null && head.Count == 0)
                            {
                                head = head.Next;
                            }
                        }
                    }
                    else
                    {
                        // If there was only one segment and it was empty, create a new item.
                        return factory();
                    }
                }
                else if (Interlocked.CompareExchange(ref localHead.Count, count - 1, count) == count)
                {
                    // If there were any items and we could reserve one of them, move the
                    // read index forward and get the index of the item we can acquire.
                    var localLow = Interlocked.Increment(ref localHead.Low) - 1;

                    // Modulo Items.Length to calculate the actual index.
                    var index = localLow & localHead.Mask;

                    // Take the item. Spin until the slot has been written by pending calls to Release.
                    T item;
                    var spinWait = new SpinWait();
                    while ((item = Interlocked.Exchange(ref localHead.Items[index], null)) == null)
                    {
                        spinWait.SpinOnce();
                    }

                    return item;
                }
            }
        }

        /// <summary>
        /// Releases an item back to the pool.
        /// </summary>
        /// <param name="item">The item to release to the pool.</param>
        public void Release(T item)
        {
            while (true)
            {
                var localTail = tail;
                var count = localTail.Count;

                // If the segment was full, allocate and append a new, bigger one.
                if (count == localTail.Items.Length)
                {
                    lock (resizeLock)
                    {
                        if (tail.Next == null && count == tail.Items.Length)
                        {
                            tail = tail.Next = new Segment(tail.Items.Length << 1);
                        }
                    }
                }
                else if (Interlocked.CompareExchange(ref localTail.Count, count + 1, count) == count)
                {
                    // If there was space for another item and we were able to reserve it, move the
                    // write index forward and get the index of the slot we can write into.
                    var localHigh = Interlocked.Increment(ref localTail.High) - 1;

                    // Modulo Items.Length to calculate the actual index.
                    var index = localHigh & localTail.Mask;

                    // Write the item. Spin until the slot has been cleared by pending calls to Acquire.
                    var spinWait = new SpinWait();
                    while (Interlocked.CompareExchange(ref localTail.Items[index], item, null) != null)
                    {
                        spinWait.SpinOnce();
                    }

                    return;
                }
            }
        }
    }
}
