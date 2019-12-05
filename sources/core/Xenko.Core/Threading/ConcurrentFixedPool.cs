// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;

namespace Xenko.Core.Threading
{
    /// <summary>
    /// A stack of fixed size, that size must be a power of two,
    /// always contains at least one item.
    /// Fastest collection type under heavy concurrency.
    /// </summary>
    public class ConcurrentFixedPool<T> where T : class, new()
    {
        private class Segment
        {
            public T[] Items;
            public int Mask;
            public int Iterator;
        }

        private readonly Segment head;

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> must be a power of two</exception>
        public ConcurrentFixedPool(int size)
        {
            // Size must be a power of two for modulo and overflow of read/write indices to behave correctly
            if (size <= 0 || ((size & (size - 1)) != 0))
                throw new ArgumentOutOfRangeException(nameof(size), "Must be power of two");

            head = new Segment
            {
                Items = new T[size],
                Mask = size - 1,
            };
            Push(new T());
        }

        public T Pop()
        {
            var spin = new SpinWait();
            var localHead = head;
            var iterator = Interlocked.Decrement(ref localHead.Iterator);

            var index = iterator & localHead.Mask;

            T item;
            while ((item = Interlocked.Exchange(ref localHead.Items[index], null)) == null)
                spin.SpinOnce();
            
            // The entire logic works on the guarantee that there is
            // always at least one item within the collection.
            if (iterator <= 0)
                Push(new T());

            return item;
        }

        public void Push(T item)
        {
            var localHead = head;
            int iterator;

            var spin = new SpinWait();
            do
            {
                iterator = Volatile.Read(ref localHead.Iterator);
                if (iterator >= localHead.Items.Length)
                    return;
                if (Interlocked.CompareExchange(ref localHead.Iterator, iterator + 1, iterator) == iterator)
                    break;
                spin.SpinOnce();
            } while (true);

            var index = iterator & localHead.Mask;
            while (Interlocked.CompareExchange(ref localHead.Items[index], item, null) != null)
            {
                spin.SpinOnce();
            }
        }
    }
}