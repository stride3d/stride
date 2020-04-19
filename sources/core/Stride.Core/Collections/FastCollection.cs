// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;

namespace Stride.Core.Collections
{
    /// <summary>
    /// Faster and lighter implementation of <see cref="System.Collections.ObjectModel.Collection{T}"/> with value types enumerators to avoid allocation in foreach loops, and various helper functions.
    /// </summary>
    /// <typeparam name="T">Type of elements of this collection </typeparam>
    [DataSerializer(typeof(ListAllSerializer<,>), Mode = DataSerializerGenericMode.TypeAndGenericArguments)]
    [DebuggerTypeProxy(typeof(CollectionDebugView))]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    public class FastCollection<T> : IList<T>, IReadOnlyList<T>
    {
        private const int DefaultCapacity = 4;

        private T[] items;
        private int size;

        public FastCollection()
        {
            items = ArrayHelper<T>.Empty;
        }

        public FastCollection([NotNull] IEnumerable<T> collection)
        {
            var is2 = collection as ICollection<T>;
            if (is2 != null)
            {
                var count = is2.Count;
                items = new T[count];
                is2.CopyTo(items, 0);
                size = count;
            }
            else
            {
                size = 0;
                items = new T[DefaultCapacity];
                using (var enumerator = collection.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Add(enumerator.Current);
                    }
                }
            }
        }

        public FastCollection(int capacity)
        {
            items = new T[capacity];
        }

        public int Capacity
        {
            get { return items.Length; }
            set
            {
                if (value != items.Length)
                {
                    if (value > 0)
                    {
                        var destinationArray = new T[value];
                        if (size > 0)
                        {
                            Array.Copy(items, 0, destinationArray, 0, size);
                        }
                        items = destinationArray;
                    }
                    else
                    {
                        items = ArrayHelper<T>.Empty;
                    }
                }
            }
        }

        public int Count => size;

        /// <summary>
        /// Gets or sets the element <see cref="T"/> at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <value>
        /// The element <see cref="T"/>.
        /// </value>
        /// <returns>The element at the specified index</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">If index is out of range</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= size) throw new ArgumentOutOfRangeException(nameof(index));
                return items[index];
            }
            set
            {
                if (index < 0 || index >= size) throw new ArgumentOutOfRangeException(nameof(index));
                SetItem(index, value);
            }
        }

        public void Add(T item)
        {
            InsertItem(size, item);
        }

        public void Clear()
        {
            ClearItems();
        }

        public bool Contains(T item)
        {
            if (item == null)
            {
                for (var j = 0; j < size; j++)
                {
                    if (items[j] == null)
                    {
                        return true;
                    }
                }
                return false;
            }
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < size; i++)
            {
                if (comparer.Equals(items[i], item))
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(items, 0, array, arrayIndex, size);
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(items, item, 0, size);
        }

        public void Insert(int index, T item)
        {
            if (index < 0 || index > size) throw new ArgumentOutOfRangeException(nameof(index));
            InsertItem(index, item);
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

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= size) throw new ArgumentOutOfRangeException(nameof(index));
            RemoveItem(index);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Adds the elements of the specified source to the end of <see cref="FastCollection{T}"/>.
        /// </summary>
        /// <param name="itemsArgs">The items to add to this collection.</param>
        public void AddRange<TE>([NotNull] TE itemsArgs) where TE : IEnumerable<T>
        {
            foreach (var item in itemsArgs)
            {
                Add(item);
            }
        }

        /// <summary>
        /// Inline Enumerator used directly by foreach.
        /// </summary>
        /// <returns>An enumerator of this collection</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public void Sort()
        {
            Sort(0, Count, null);
        }

        public void Sort(IComparer<T> comparer)
        {
            Sort(0, Count, comparer);
        }

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            Array.Sort(items, index, count, comparer);
        }

        protected virtual void ClearItems()
        {
            if (size > 0)
            {
                Array.Clear(items, 0, size);
            }
            size = 0;
        }

        protected virtual void InsertItem(int index, T item)
        {
            if (size == items.Length)
            {
                EnsureCapacity(size + 1);
            }
            if (index < size)
            {
                Array.Copy(items, index, items, index + 1, size - index);
            }
            items[index] = item;
            size++;
        }

        protected virtual void RemoveItem(int index)
        {
            size--;
            if (index < size)
            {
                Array.Copy(items, index + 1, items, index, size - index);
            }
            items[size] = default(T);
        }

        protected virtual void SetItem(int index, T item)
        {
            items[index] = item;
        }

        bool ICollection<T>.IsReadOnly => false;

        public void EnsureCapacity(int min)
        {
            if (items.Length < min)
            {
                var num = (items.Length == 0) ? DefaultCapacity : (items.Length * 2);
                if (num < min)
                {
                    num = min;
                }
                Capacity = num;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private readonly FastCollection<T> list;
            private int index;
            private T current;

            internal Enumerator(FastCollection<T> list)
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
                if (index < list.size)
                {
                    current = list.items[index];
                    index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                index = list.size + 1;
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
