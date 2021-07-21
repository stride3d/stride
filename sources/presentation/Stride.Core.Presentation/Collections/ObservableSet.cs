// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Collections
{
    public class ObservableSet<T> : IObservableList<T>, IReadOnlyObservableList<T>
    {
        private readonly HashSet<T> hashSet;
        private readonly List<T> list;

        [CollectionAccess(CollectionAccessType.None)]
        public ObservableSet()
            : this(EqualityComparer<T>.Default)
        {
        }

        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        public ObservableSet([NotNull] IEnumerable<T> collection)
              : this(EqualityComparer<T>.Default, collection)
        {
        }

        [CollectionAccess(CollectionAccessType.None)]
        public ObservableSet(IEqualityComparer<T> comparer)
        {
            hashSet = new HashSet<T>(comparer);
            list = new List<T>();
        }

        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        public ObservableSet(IEqualityComparer<T> comparer, [NotNull] IEnumerable<T> collection)
        {
            list = new List<T>();
            hashSet = new HashSet<T>(comparer);
            foreach (var item in collection)
            {
                if (hashSet.Add(item))
                    list.Add(item);
            }
        }

        [CollectionAccess(CollectionAccessType.None)]
        public ObservableSet(int capacity)
        {
            hashSet = new HashSet<T>();
            list = new List<T>(capacity);
        }

        public T this[int index]
        {
            [CollectionAccess(CollectionAccessType.Read)]
            get { return list[index]; }
            [CollectionAccess(CollectionAccessType.ModifyExistingContent)]
            set
            {
                var oldItem = list[index];
                hashSet.Remove(oldItem);
                if (!hashSet.Add(value)) throw new InvalidOperationException("Unable to set this value at the given index because this value is already contained in this ObservableSet.");
                list[index] = value;
                var arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldItem, index);
                OnCollectionChanged(arg);
            }
        }

        [CollectionAccess(CollectionAccessType.None)]
        public bool IsReadOnly => false;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        [CollectionAccess(CollectionAccessType.None)]
        public int Count => list.Count;

        [Pure]
        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        [Pure]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        [NotNull, Pure]
        public IList ToIList()
        {
            return new NonGenericObservableListWrapper<T>(this);
        }

        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        public void Add(T item)
        {
            if (hashSet.Add(item))
            {
                list.Add(item);
                var arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, list.Count - 1);
                OnCollectionChanged(arg);
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            var itemList = items.Where(x => hashSet.Add(x)).ToList();
            if (itemList.Count > 0)
            {
                list.AddRange(itemList);
                var arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, itemList, Count - itemList.Count);
                OnCollectionChanged(arg);
            }
        }

        [CollectionAccess(CollectionAccessType.ModifyExistingContent)]
        public void Clear()
        {
            var raiseEvent = list.Count > 0;
            hashSet.Clear();
            list.Clear();
            if (raiseEvent)
            {
                var arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                OnCollectionChanged(arg);
            }
        }

        [CollectionAccess(CollectionAccessType.Read)]
        public bool Contains(T item)
        {
            return hashSet.Contains(item);
        }

        [CollectionAccess(CollectionAccessType.Read)]
        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        [CollectionAccess(CollectionAccessType.ModifyExistingContent)]
        public bool Remove(T item)
        {
            if (!hashSet.Contains(item))
                return false;
            int index = list.IndexOf(item);
            if (index != -1)
            {
                RemoveAt(index);
            }
            return index != -1;
        }

        [CollectionAccess(CollectionAccessType.Read)]
        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }

        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        public void Insert(int index, T item)
        {
            if (hashSet.Add(item))
            {
                list.Insert(index, item);
                var arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);
                OnCollectionChanged(arg);
            }
        }

        [CollectionAccess(CollectionAccessType.ModifyExistingContent)]
        public void RemoveAt(int index)
        {
            var item = list[index];
            list.RemoveAt(index);
            hashSet.Remove(item);

            var arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index);
            OnCollectionChanged(arg);
        }

        /// <inheritdoc/>
        [CollectionAccess(CollectionAccessType.None)]
        public override string ToString()
        {
            return $"{{ObservableSet}} Count = {Count}";
        }

        protected void OnCollectionChanged([NotNull] NotifyCollectionChangedEventArgs arg)
        {
            CollectionChanged?.Invoke(this, arg);

            switch (arg.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Reset:
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
                    break;
            }
        }

        protected void OnPropertyChanged([NotNull] PropertyChangedEventArgs arg)
        {
            PropertyChanged?.Invoke(this, arg);
        }
    }
}
