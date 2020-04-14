// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;

namespace Stride.Core.Shaders.Utility
{
    /// <summary>
    /// An ordered set
    /// </summary>
    /// <typeparam name="T">Type of the element in the set</typeparam>
    [DataSerializer(typeof(ListAllSerializer<,>), Mode = DataSerializerGenericMode.TypeAndGenericArguments)]
    public class OrderedSet<T> : ISet<T>, IList<T>
    {
        #region Constants and Fields

        private HashSet<T> hashSet;

        private List<T> listSet;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "OrderedSet&lt;T&gt;" /> class.
        /// </summary>
        public OrderedSet()
        {
            hashSet = new HashSet<T>();
            listSet = new List<T>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedSet&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="items">The items.</param>
        public OrderedSet(IEnumerable<T> items)
        {
            hashSet = new HashSet<T>();
            listSet = new List<T>();
            UnionWith(items);
        }

        #endregion

        #region Public Properties

        /// <inheritdoc />
        public int Count
        {
            get
            {
                return hashSet.Count;
            }
        }

        /// <inheritdoc />
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public bool Add(T item)
        {
            if (hashSet.Add(item))
            {
                listSet.Add(item);
                return true;
            }

            return false;
        }

        public int IndexOf(T item)
        {
            return listSet.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            if (hashSet.Add(item))
            {
                listSet.Insert(index, item);
            }
        }

        public void RemoveAll(Func<T,bool> filter)
        {
            var array = listSet.ToArray();
            foreach (var element in array)
            {
                if (filter(element))
                    Remove(element);
            }
        }

        public void RemoveAt(int index)
        {
            var element = listSet[index];
            hashSet.Remove(element);
            listSet.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                return listSet[index];
            }
            set
            {
                var element = listSet[index];
                hashSet.Remove(element);
                listSet[index] = value;
                if (!hashSet.Add(value))
                {
                    throw new InvalidOperationException("Value is already in the set");
                }
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            hashSet.Clear();
            listSet.Clear();
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return hashSet.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            listSet.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            if (Count != 0)
            {
                if (other == this)
                {
                    Clear();
                }
                else
                {
                    foreach (T local in other)
                    {
                        Remove(local);
                    }
                }
            }
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return listSet.GetEnumerator();
        }

        /// <inheritdoc />
        public void IntersectWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            hashSet.IntersectWith(other);

            // Remove items from ordered list
            for (int i = 0; i < listSet.Count && listSet.Count != hashSet.Count; i++)
            {
                var item = listSet[i];
                if (!hashSet.Contains(item))
                {
                    listSet.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <inheritdoc />
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return hashSet.IsProperSubsetOf(other);
        }

        /// <inheritdoc />
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return hashSet.IsProperSupersetOf(other);
        }

        /// <inheritdoc />
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return hashSet.IsSubsetOf(other);
        }

        /// <inheritdoc />
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return hashSet.IsSupersetOf(other);
        }

        /// <inheritdoc />
        public bool Overlaps(IEnumerable<T> other)
        {
            return hashSet.Overlaps(other);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            if (hashSet.Remove(item))
            {
                listSet.Remove(item);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool SetEquals(IEnumerable<T> other)
        {
            return hashSet.SetEquals(other);
        }

        /// <inheritdoc />
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            var temp = new List<T>(other);
            hashSet.SymmetricExceptWith(temp);

            // Remove items from ordered list
            foreach (var item in temp)
            {
                int indexOf = listSet.IndexOf(item);
                if (indexOf < 0)
                {
                    listSet.Add(item);
                }
            }

            // Remove items from ordered list
            for (int i = 0; i < listSet.Count && i != hashSet.Count; i++)
            {
                var item = listSet[i];
                if (!hashSet.Contains(item))
                {
                    listSet.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <inheritdoc />
        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            foreach (var item in other)
            {
                Add(item);
            }
        }

        #endregion

        #region Explicit Interface Methods

        /// <inheritdoc />
        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

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
            listSet.Sort(index, count, comparer);
        }
    }
}
