// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Xenko.Core.Annotations;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Serializers;

namespace Xenko.Core.Collections
{
    /// <summary>
    /// A collection that maintains the order of the elements  and lightter implementation of <see cref="System.Collections.ObjectModel.Collection{T}"/> with value types enumerators to avoid allocation in foreach loops, and various helper functions.
    /// </summary>
    /// <typeparam name="T">Type of elements of this collection </typeparam>
    [DataSerializer(typeof(ListAllSerializer<,>), Mode = DataSerializerGenericMode.TypeAndGenericArguments)]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    public class OrderedCollection<T> : ICollection<T>
    {
        private const int DefaultCapacity = 4;

        private readonly IComparer<T> comparer;
        private T[] items;
        private int size;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedCollection{T}"/> class.
        /// </summary>
        /// <param name="comparer">The comparer providing information about order between elements.</param>
        /// <exception cref="System.ArgumentNullException">If comparer is null</exception>
        public OrderedCollection([NotNull] IComparer<T> comparer)
        {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            this.comparer = comparer;
            items = ArrayHelper<T>.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedCollection{T}"/> class with a default capacity.
        /// </summary>
        /// <param name="comparer">The comparer.</param>
        /// <param name="capacity">The capacity.</param>
        public OrderedCollection([NotNull] IComparer<T> comparer, int capacity) : this(comparer)
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
        /// Gets the element <see cref="T"/> at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The element at the specified index</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">If index is out of range</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= size) throw new ArgumentOutOfRangeException(nameof(index));
                return items[index];
            }
        }

        public void Add(T item)
        {
            AddItem(item);
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
            var equalComparer = EqualityComparer<T>.Default;
            for (var i = 0; i < size; i++)
            {
                if (equalComparer.Equals(items[i], item))
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
            RemoteItem(index);
        }

        /// <summary>
        /// Clears all the items in this collection. Can be overriden.
        /// </summary>
        protected virtual void ClearItems()
        {
            if (size > 0)
            {
                Array.Clear(items, 0, size);
            }
            size = 0;
        }

        /// <summary>
        /// Adds an item to this collection. Can be overriden.
        /// </summary>
        /// <param name="item">The item.</param>
        protected virtual void AddItem(T item)
        {
            var index = Array.BinarySearch(items, 0, size, item, comparer);
            if (index < 0)
            {
                index = ~index; // insert at the end of the list
            }
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

        protected virtual void RemoteItem(int index)
        {
            size--;
            if (index < size)
            {
                Array.Copy(items, index + 1, items, index, size - index);
            }
            items[size] = default(T);
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
        public struct Enumerator : IEnumerator<T>
        {
            private readonly OrderedCollection<T> collection;
            private int index;
            private T current;

            internal Enumerator(OrderedCollection<T> collection)
            {
                this.collection = collection;
                index = 0;
                current = default(T);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (index < collection.size)
                {
                    current = collection.items[index];
                    index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                index = collection.size + 1;
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
