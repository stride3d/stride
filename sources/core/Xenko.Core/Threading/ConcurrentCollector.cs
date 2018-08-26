// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Xenko.Core.Annotations;

namespace Xenko.Core.Threading
{
    public class ConcurrentCollectorCache<T>
    {
        private readonly int capacity;
        private readonly List<T> cache = new List<T>();
        private ConcurrentCollector<T> currentCollection;

        public ConcurrentCollectorCache(int capacity)
        {
            this.capacity = capacity;
        }

        public void Add([NotNull] ConcurrentCollector<T> collection, T item)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            if (currentCollection != collection || cache.Count > capacity)
            {
                if (currentCollection != null)
                {
                    currentCollection.AddRange(cache);
                    cache.Clear();
                }
                currentCollection = collection;
            }

            cache.Add(item);
        }

        public void Flush()
        {
            if (currentCollection != null)
            {
                currentCollection.AddRange(cache);
                cache.Clear();
            }
            currentCollection = null;
        }
    }

    public static class ConcurrentCollectorExtensions
    {
        public static void Add<T>([NotNull] this ConcurrentCollector<T> collection, T item, [NotNull] ConcurrentCollectorCache<T> cache)
        {
            cache.Add(collection, item);
        }
    }

    /// <summary>
    /// A collector that allows for concurrent adding of items, as well as non-thread-safe clearing and accessing of the underlying colletion.
    /// </summary>
    /// <typeparam name="T">The element type in the collection.</typeparam>
    public class ConcurrentCollector<T> : IReadOnlyList<T>
    {
        private const int DefaultCapacity = 16;

        private class Segment
        {
            public T[] Items;
            public int Offset;
            public Segment Previous;
            public Segment Next;
        }

        private readonly object resizeLock = new object();
        private readonly Segment head;
        private Segment tail;
        private int count;

        public ConcurrentCollector(int capacity = DefaultCapacity)
        {
            tail = head = new Segment { Items = new T[capacity] };
        }

        public T[] Items
        {
            get
            {
                if (head != tail)
                    throw new InvalidOperationException();

                return head.Items;
            }
        }

        /// <summary>
        /// Consolidates all added items into a single consecutive array. It is an error to access Items after adding elements, but before closing.
        /// </summary>
        public void Close()
        {
            if (head.Next != null)
            {
                var newItems = new T[tail.Offset + tail.Items.Length];

                var segment = head;
                while (segment != null)
                {
                    Array.Copy(segment.Items, 0, newItems, segment.Offset, segment.Items.Length);
                    segment = segment.Next;
                }

                head.Items = newItems;
                head.Next = null;

                tail = head;
            }
        }

        public int Add(T item)
        {
            var index = Interlocked.Increment(ref count) - 1;

            var segment = tail;
            if (index >= segment.Offset + segment.Items.Length)
            {
                lock (resizeLock)
                {
                    if (index >= tail.Offset + tail.Items.Length)
                    {
                        tail.Next = new Segment
                        {
                            Items = new T[segment.Items.Length * 2],
                            Offset = segment.Offset + segment.Items.Length,
                            Previous = tail,
                        };

                        tail = tail.Next;
                    }

                    segment = tail;
                }
            }

            while (index < segment.Offset)
            {
                segment = segment.Previous;
            }

            segment.Items[index - segment.Offset] = item;

            return index;
        }

        public void AddRange([NotNull] IReadOnlyList<T> collection)
        {
            var newCount = Interlocked.Add(ref count, collection.Count);

            var segment = tail;
            if (newCount >= segment.Offset + segment.Items.Length)
            {
                lock (resizeLock)
                {
                    if (newCount >= tail.Offset + tail.Items.Length)
                    {
                        var capacity = tail.Offset + tail.Items.Length;
                        var size = Math.Max(capacity, newCount - capacity);

                        tail.Next = new Segment
                        {
                            Items = new T[size],
                            Offset = capacity,
                            Previous = tail,
                        };

                        tail = tail.Next;
                    }

                    segment = tail;
                }
            }

            // Find the segment containing the last index
            while (newCount <= segment.Offset)
                segment = segment.Previous;

            var destinationIndex = newCount - segment.Offset - 1;
            for (var sourceIndex = collection.Count - 1; sourceIndex >= 0; sourceIndex--)
            {
                if (destinationIndex < 0)
                {
                    segment = segment.Previous;
                    destinationIndex = segment.Items.Length - 1;
                }

                segment.Items[destinationIndex] = collection[sourceIndex];
                destinationIndex--;
            }
        }

        public void Clear(bool fastClear)
        {
            Close();
            if (!fastClear && count > 0)
            {
                Array.Clear(Items, 0, count);
            }
            count = 0;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public int Count => count;

        public T this[int index]
        {
            get
            {
                return Items[index];
            }
            set
            {
                Items[index] = value;
            }
        }

        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private readonly ConcurrentCollector<T> list;
            private int index;
            private T current;

            internal Enumerator(ConcurrentCollector<T> list)
            {
                this.list = list;
                index = 0;
                current = default(T);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                var list = this.list;
                if (index < list.count)
                {
                    current = list.Items[index];
                    index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                index = list.count + 1;
                current = default(T);
                return false;
            }

            public T Current => current;

            object IEnumerator.Current => Current;

            void IEnumerator.Reset()
            {
                index = 0;
                current = default(T);
            }
        }
    }
}
