// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Stride.Core.Annotations;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;

namespace Stride.Core.Collections
{
    /// <summary>
    /// Similar to <see cref="List{T}"/>, with direct access to underlying array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    [DataSerializer(typeof(ListAllSerializer<,>), Mode = DataSerializerGenericMode.TypeAndGenericArguments)]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    public class FastList<T> : IList<T>, IReadOnlyList<T>, ICollection<T>, IEnumerable<T>, IEnumerable
    {
        // Fields
        private const int DefaultCapacity = 4;

        /// <summary>
        /// Gets the items.
        /// </summary>
        [DataMemberIgnore]
        public T[] Items { get; private set; }

        private int size;

        public FastList()
        {
            Items = ArrayHelper<T>.Empty;
        }

        public FastList([NotNull] IEnumerable<T> collection)
        {
            var is2 = collection as ICollection<T>;
            if (is2 != null)
            {
                var count = is2.Count;
                Items = new T[count];
                is2.CopyTo(Items, 0);
                size = count;
            }
            else
            {
                size = 0;
                Items = new T[DefaultCapacity];
                using (var enumerator = collection.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Add(enumerator.Current);
                    }
                }
            }
        }

        public FastList(int capacity)
        {
            Items = new T[capacity];
        }

        public int Capacity
        {
            get { return Items.Length; }
            set
            {
                if (value != Items.Length)
                {
                    if (value > 0)
                    {
                        var destinationArray = new T[value];
                        if (size > 0)
                        {
                            Array.Copy(Items, 0, destinationArray, 0, size);
                        }
                        Items = destinationArray;
                    }
                    else
                    {
                        Items = ArrayHelper<T>.Empty;
                    }
                }
            }
        }

        #region IList<T> Members

        public void Add(T item)
        {
            if (size == Items.Length)
            {
                EnsureCapacity(size + 1);
            }
            Items[size++] = item;
        }

        public void IncreaseCapacity(int index)
        {
            EnsureCapacity(size + index);
            size += index;
        }

        public void Clear()
        {
            Clear(false);
        }

        public bool Contains(T item)
        {
            if (item == null)
            {
                for (var j = 0; j < size; j++)
                {
                    if (Items[j] == null)
                    {
                        return true;
                    }
                }
                return false;
            }
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < size; i++)
            {
                if (comparer.Equals(Items[i], item))
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(Items, 0, array, arrayIndex, size);
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(Items, item, 0, size);
        }

        public void Insert(int index, T item)
        {
            if (size == Items.Length)
            {
                EnsureCapacity(size + 1);
            }
            if (index < size)
            {
                Array.Copy(Items, index, Items, index + 1, size - index);
            }
            Items[index] = item;
            size++;
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
            size--;
            if (index < size)
            {
                Array.Copy(Items, index + 1, Items, index, size - index);
            }
            Items[size] = default(T);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public int Count => size;

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= size) throw new ArgumentOutOfRangeException(nameof(index));
                return Items[index];
            }
            set
            {
                if (index < 0 || index >= size) throw new ArgumentOutOfRangeException(nameof(index));
                Items[index] = value;
            }
        }

        bool ICollection<T>.IsReadOnly => false;

        #endregion

        /// <summary>
        /// Clears this list with a fast-clear option.
        /// </summary>
        /// <param name="fastClear">if set to <c>true</c> this method only resets the count elements but doesn't clear items referenced already stored in the list.</param>
        public void Clear(bool fastClear)
        {
            Resize(0, fastClear);
        }

        public void Resize(int newSize, bool fastClear)
        {
            if (size < newSize)
            {
                EnsureCapacity(newSize);
            }
            else if (!fastClear && size - newSize > 0)
            {
                Array.Clear(Items, newSize, size - newSize);
            }

            size = newSize;
        }

        public void AddRange([NotNull] IEnumerable<T> collection)
        {
            InsertRange(size, collection);
        }

        [NotNull]
        public ReadOnlyCollection<T> AsReadOnly()
        {
            return new ReadOnlyCollection<T>(this);
        }

        public int BinarySearch(T item)
        {
            return BinarySearch(0, Count, item, null);
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return BinarySearch(0, Count, item, comparer);
        }

        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            return Array.BinarySearch(Items, index, count, item, comparer);
        }

        public void CopyTo([NotNull] T[] array)
        {
            CopyTo(array, 0);
        }

        public void CopyTo(int index, [NotNull] T[] array, int arrayIndex, int count)
        {
            Array.Copy(Items, index, array, arrayIndex, count);
        }

        public void EnsureCapacity(int min)
        {
            if (Items.Length < min)
            {
                var num = (Items.Length == 0) ? DefaultCapacity : (Items.Length * 2);
                if (num < min)
                {
                    num = min;
                }
                Capacity = num;
            }
        }

        public bool Exists(Predicate<T> match)
        {
            return FindIndex(match) != -1;
        }

        public T Find(Predicate<T> match)
        {
            for (var i = 0; i < size; i++)
            {
                if (match(Items[i]))
                {
                    return Items[i];
                }
            }
            return default(T);
        }

        [NotNull]
        public FastList<T> FindAll(Predicate<T> match)
        {
            var list = new FastList<T>();
            for (var i = 0; i < size; i++)
            {
                if (match(Items[i]))
                {
                    list.Add(Items[i]);
                }
            }
            return list;
        }

        public int FindIndex(Predicate<T> match)
        {
            return FindIndex(0, size, match);
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return FindIndex(startIndex, size - startIndex, match);
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            var num = startIndex + count;
            for (var i = startIndex; i < num; i++)
            {
                if (match(Items[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public T FindLast(Predicate<T> match)
        {
            for (var i = size - 1; i >= 0; i--)
            {
                if (match(Items[i]))
                {
                    return Items[i];
                }
            }
            return default(T);
        }

        public int FindLastIndex(Predicate<T> match)
        {
            return FindLastIndex(size - 1, size, match);
        }

        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return FindLastIndex(startIndex, startIndex + 1, match);
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            var num = startIndex - count;
            for (var i = startIndex; i > num; i--)
            {
                if (match(Items[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public void ForEach(Action<T> action)
        {
            for (var i = 0; i < size; i++)
            {
                action(Items[i]);
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        [NotNull]
        public FastList<T> GetRange(int index, int count)
        {
            var list = new FastList<T>(count);
            Array.Copy(Items, index, list.Items, 0, count);
            list.size = count;
            return list;
        }

        public int IndexOf(T item, int index)
        {
            return Array.IndexOf(Items, item, index, size - index);
        }

        public int IndexOf(T item, int index, int count)
        {
            return Array.IndexOf(Items, item, index, count);
        }

        public void InsertRange(int index, [NotNull] IEnumerable<T> collection)
        {
            var is2 = collection as ICollection<T>;
            if (is2 != null)
            {
                var count = is2.Count;
                if (count > 0)
                {
                    EnsureCapacity(size + count);
                    if (index < size)
                    {
                        Array.Copy(Items, index, Items, index + count, size - index);
                    }
                    if (this == is2)
                    {
                        Array.Copy(Items, 0, Items, index, index);
                        Array.Copy(Items, index + count, Items, index * 2, size - index);
                    }
                    else
                    {
                        is2.CopyTo(Items, index);
                    }
                    size += count;
                }
            }
            else
            {
                using (var enumerator = collection.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Insert(index++, enumerator.Current);
                    }
                }
            }
        }

        private static bool IsCompatibleObject(object value)
        {
            return value is T || (value == null && default(T) == null);
        }

        public int LastIndexOf(T item)
        {
            if (size == 0)
            {
                return -1;
            }
            return LastIndexOf(item, size - 1, size);
        }

        public int LastIndexOf(T item, int index)
        {
            return LastIndexOf(item, index, index + 1);
        }

        public int LastIndexOf(T item, int index, int count)
        {
            if (size == 0)
            {
                return -1;
            }
            return Array.LastIndexOf(Items, item, index, count);
        }

        public int RemoveAll(Predicate<T> match)
        {
            var index = 0;
            while ((index < size) && !match(Items[index]))
            {
                index++;
            }
            if (index >= size)
            {
                return 0;
            }
            var num2 = index + 1;
            while (num2 < size)
            {
                while ((num2 < size) && match(Items[num2]))
                {
                    num2++;
                }
                if (num2 < size)
                {
                    Items[index++] = Items[num2++];
                }
            }
            Array.Clear(Items, index, size - index);
            var num3 = size - index;
            size = index;
            return num3;
        }

        public void RemoveRange(int index, int count)
        {
            if (count > 0)
            {
                size -= count;
                if (index < size)
                {
                    Array.Copy(Items, index + count, Items, index, size - index);
                }
                Array.Clear(Items, size, count);
            }
        }

        public void Reverse()
        {
            Reverse(0, Count);
        }

        public void Reverse(int index, int count)
        {
            Array.Reverse(Items, index, count);
        }

        public void Sort()
        {
            Sort(0, Count, null);
        }

        public void Sort(IComparer<T> comparer)
        {
            Sort(0, Count, comparer);
        }

        //public void Sort(Comparison<T> comparison)
        //{
        //    if (this._size > 0)
        //    {
        //        IComparer<T> comparer = new Array.FunctorComparer<T>(comparison);
        //        Array.Sort<T>(this.Items, 0, this._size, comparer);
        //    }
        //}

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            Array.Sort(Items, index, count, comparer);
        }

        [NotNull]
        public T[] ToArray()
        {
            var destinationArray = new T[size];
            Array.Copy(Items, 0, destinationArray, 0, size);
            return destinationArray;
        }

        public void TrimExcess()
        {
            var num = (int)(Items.Length * 0.9);
            if (size < num)
            {
                Capacity = size;
            }
        }

        public bool TrueForAll(Predicate<T> match)
        {
            for (var i = 0; i < size; i++)
            {
                if (!match(Items[i]))
                {
                    return false;
                }
            }
            return true;
        }

        #region Nested type: Enumerator

        [StructLayout(LayoutKind.Sequential)]
        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private readonly FastList<T> list;
            private int index;
            private T current;

            internal Enumerator(FastList<T> list)
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
                    current = list.Items[index];
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

        #endregion
    }
}
