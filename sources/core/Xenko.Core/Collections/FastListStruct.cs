// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xenko.Core.Annotations;

namespace Xenko.Core.Collections
{
    public struct FastListStruct<T> : IEnumerable<T>
    {
        private static readonly T[] EmptyArray = new T[0];

        public int Count;

        /// <summary>
        /// Gets the items.
        /// </summary>
        public T[] Items;

        public T this[int index]
        {
            get { return Items[index]; }
            set
            {
                Items[index] = value;
            }
        }

        public FastListStruct([NotNull] FastList<T> fastList)
        {
            Count = fastList.Count;
            Items = fastList.Items;
        }

        public FastListStruct([NotNull] T[] array)
        {
            Count = array.Length;
            Items = array;
        }

        public FastListStruct(int capacity)
        {
            Count = 0;
            Items = capacity == 0 ? EmptyArray : new T[capacity];
        }

        public void Add(T item)
        {
            if (Count == Items.Length)
                EnsureCapacity(Count + 1);
            Items[Count++] = item;
        }

        public void AddRange(FastListStruct<T> items)
        {
            for (var i = 0; i < items.Count; i++)
            {
                Add(items[i]);
            }
        }

        public void Insert(int index, T item)
        {
            if (Count == Items.Length)
            {
                EnsureCapacity(Count + 1);
            }
            if (index < Count)
            {
                for (var i = Count; i > index; --i)
                {
                    Items[i] = Items[i - 1];
                }
            }
            Items[index] = item;
            Count++;
        }

        public void RemoveAt(int index)
        {
            Count--;
            if (index < Count)
            {
                Array.Copy(Items, index + 1, Items, index, Count - index);
            }
            Items[Count] = default(T);
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        public void Clear()
        {
            Count = 0;
        }

        [NotNull]
        public T[] ToArray()
        {
            var destinationArray = new T[Count];
            Array.Copy(Items, 0, destinationArray, 0, Count);
            return destinationArray;            
        }

        public void EnsureCapacity(int newCapacity)
        {
            if (Items.Length < newCapacity)
            {
                var newSize = Items.Length * 2;
                if (newSize < newCapacity)
                    newSize = newCapacity;

                var destinationArray = new T[newSize];
                Array.Copy(Items, 0, destinationArray, 0, Count);
                Items = destinationArray;
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(Items, Count);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(Items, Count);
        }
        
        public Enumerator GetEnumerator()
        {
            return new Enumerator(Items, Count);
        }

        public static implicit operator FastListStruct<T>([NotNull] FastList<T> fastList)
        {
            return new FastListStruct<T>(fastList);
        }

        public static implicit operator FastListStruct<T>([NotNull] T[] array)
        {
            return new FastListStruct<T>(array);
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(Items, item, 0, Count);
        }

        /// <summary>
        /// Remove an item by swapping it with the last item and removing it from the last position. This function prevents to shift values from the list on removal but does not maintain order.
        /// </summary>
        /// <param name="index">Index of the item to remove.</param>
        public void SwapRemoveAt(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));

            if (index < Count - 1)
            {
                Items[index] = Items[Count - 1];
            }

            RemoveAt(Count - 1);
        }

        #region Nested type: Enumerator

        [StructLayout(LayoutKind.Sequential)]
        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private readonly T[] items;
            private readonly int count;
            private int index;
            private T current;

            internal Enumerator(T[] items, int count)
            {
                this.items = items;
                this.count = count;
                index = 0;
                current = default(T);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (index < count)
                {
                    current = items[index];
                    index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                index = count + 1;
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
