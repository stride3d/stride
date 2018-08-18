// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Xenko.Core.Collections
{
    /// <summary>
    /// List of items, stored sequentially and sorted by an implicit invariant key that are extracted from items by implementing <see cref="GetKeyForItem"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="T">The type of the item in the collection.</typeparam>
    public abstract class KeyedSortedList<TKey, T> : ICollection<T>, ICollection
    {
        protected FastListStruct<T> items = new FastListStruct<T>(1);
        private readonly IComparer<TKey> comparer;

        protected KeyedSortedList() : this(null)
        {
        }

        protected KeyedSortedList(IComparer<TKey> comparer)
        {
            if (comparer == null)
                comparer = Comparer<TKey>.Default;

            this.comparer = comparer;
        }

        /// <summary>
        /// Extracts the key for the specified element.
        /// </summary>
        /// <param name="item">The element from which to extract the key.</param>
        /// <returns>The key for the specified item.</returns>
        protected abstract TKey GetKeyForItem(T item);

        /// <summary>
        /// Called every time an item should be added at a given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        protected virtual void InsertItem(int index, T item)
        {
            items.Insert(index, item);
        }

        /// <summary>
        /// Called every time an item should be removed at a given index.
        /// </summary>
        /// <param name="index">The index.</param>
        protected virtual void RemoveItem(int index)
        {
            items.RemoveAt(index);
        }

        /// <summary>
        /// Sorts again this list (in case keys were mutated).
        /// </summary>
        public void Sort()
        {
            Array.Sort(items.Items, 0, items.Count, new Comparer(this));
        }

        /// <inheritdoc/>
        public void Add(T item)
        {
            var key = GetKeyForItem(item);

            var index = BinarySearch(key);
            if (index >= 0)
                throw new InvalidOperationException("An item with the same key has already been added.");

            InsertItem(~index, item);
        }

        public bool ContainsKey(TKey key)
        {
            return BinarySearch(key) >= 0;
        }

        public bool Remove(TKey key)
        {
            var index = BinarySearch(key);
            if (index < 0)
                return false;

            RemoveItem(index);

            return true;
        }

        public T this[int index]
        {
            get { return items[index]; }
            set { items[index] = value; }
        }

        public T this[TKey key]
        {
            get
            {
                var index = BinarySearch(key);
                if (index < 0)
                    throw new KeyNotFoundException();
                return items[index];
            }
            set
            {
                var index = BinarySearch(key);
                if (index >= 0)
                    items[index] = value;
                else
                    items.Insert(~index, value);
            }
        }

        public bool TryGetValue(TKey key, out T value)
        {
            var index = BinarySearch(key);
            if (index < 0)
            {
                value = default(T);
                return false;
            }

            value = items[index];
            return true;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            items.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(T item)
        {
            return items.Contains(item);
        }

        /// <inheritdoc/>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            foreach (var item in items)
            {
                array[arrayIndex++] = item;
            }
        }

        /// <inheritdoc/>
        bool ICollection<T>.Remove(T item)
        {
            return items.Remove(item);
        }

        /// <inheritdoc/>
        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            foreach (var item in items)
            {
                ((IList)array)[arrayIndex++] = item;
            }
        }
        
        /// <inheritdoc/>
        public int Count => items.Count;

        /// <inheritdoc/>
        object ICollection.SyncRoot => this;

        /// <inheritdoc/>
        bool ICollection.IsSynchronized => false;

        /// <inheritdoc/>
        bool ICollection<T>.IsReadOnly => false;

        public int IndexOf(T item)
        {
            return items.IndexOf(item);
        }

//        /// <inheritdoc/>
//        public void RemoveAt(int index)
//        {
//            Items.RemoveAt(index);
//        }

        public void Remove(T item)
        {
            items.Remove(item);
        }

        /// <inheritdoc/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(items);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(items);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(items);
        }

        public int BinarySearch(TKey searchKey)
        {
            var values = items.Items;
            var start = 0;
            var end = items.Count - 1;

            while (start <= end)
            {
                var middle = start + ((end - start) >> 1);
                var itemKey = GetKeyForItem(values[middle]);

                var compareResult = comparer.Compare(itemKey, searchKey);

                if (compareResult == 0)
                {
                    return middle;
                }
                if (compareResult < 0)
                {
                    start = middle + 1;
                }
                else
                {
                    end = middle - 1;
                }
            }
            return ~start;
        }

        private struct Comparer : IComparer<T>
        {
            private readonly KeyedSortedList<TKey, T> list;

            internal Comparer(KeyedSortedList<TKey, T> list)
            {
                this.list = list;
            }

            public int Compare(T x, T y)
            {
                return list.comparer.Compare(list.GetKeyForItem(x), list.GetKeyForItem(y));
            }
        }
 
        #region Nested type: Enumerator

        [StructLayout(LayoutKind.Sequential)]
        public struct Enumerator : IEnumerator<T>
        {
            private readonly FastListStruct<T> list;
            private int index;
            private T current;

            internal Enumerator(FastListStruct<T> list)
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
                if (index < list.Count)
                {
                    current = list.Items[index];
                    index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                index = list.Count + 1;
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

        #endregion
    }
}
