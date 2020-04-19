// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Collections
{
    /// <summary>
    /// A class that wraps an instance of the <see cref="IList{T}"/> class and implement the <see cref="IList"/> interface.
    /// The wrapped object must also implement some <see cref="INotifyPropertyChanged"/> and <see cref="INotifyCollectionChanged"/>.
    /// </summary>
    /// <remarks>
    /// In some scenarii, <see cref="IList"/> does not support range changes on the collection (Especially when bound to a ListCollectionView).
    /// This is why the <see cref="ObservableList{T}"/> and the <see cref="ObservableSet{T}"/> class does not implement this interface directly.
    /// However this wrapper class can be used when the <see cref="IList"/> interface is required.
    /// </remarks>
    /// <typeparam name="T">The type of item contained in the <see cref="ObservableList{T}"/>.</typeparam>
    public abstract class NonGenericObservableCollectionWrapper<T> : IList, IList<T>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        [NotNull] protected readonly IList<T> List;

        /// <summary>
        /// Initializes a new instance of the <see cref="NonGenericObservableListWrapper{T}"/> class.
        /// </summary>
        /// <param name="list">The <see cref="ObservableList{T}"/> to wrap.</param>
        protected NonGenericObservableCollectionWrapper([NotNull] IList<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (!(list is INotifyPropertyChanged)) throw new ArgumentException(@"The list must implements INotifyPropertyChanged", nameof(list));
            if (!(list is INotifyCollectionChanged)) throw new ArgumentException(@"The list must implements INotifyCollectionChanged", nameof(list));

            List = list;
            ((INotifyPropertyChanged)List).PropertyChanged += (sender, e) => PropertyChanged?.Invoke(this, e);
            ((INotifyCollectionChanged)List).CollectionChanged += (sender, e) => CollectionChanged?.Invoke(this, e);
        }

        /// <inheritdoc/>
        public object this[int index] { get { return List[index]; } set { List[index] = (T)value; } }

        /// <inheritdoc/>
        T IList<T>.this[int index] { get { return List[index]; } set { List[index] = value; } }

        /// <inheritdoc/>
        public bool IsReadOnly => List.IsReadOnly;

        /// <inheritdoc/>
        public bool IsFixedSize => false;

        /// <inheritdoc/>
        public int Count => List.Count;

        /// <inheritdoc/>
        public object SyncRoot { get; } = new object();

        /// <inheritdoc/>
        public bool IsSynchronized => false;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;
        //public event PropertyChangedEventHandler PropertyChanged { add { NotifyPropertyChanged.PropertyChanged += value; } remove { NotifyPropertyChanged.PropertyChanged -= value; } }

        /// <inheritdoc/>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        //public event NotifyCollectionChangedEventHandler CollectionChanged { add { NotifyCollectionChanged.CollectionChanged += value; } remove { NotifyCollectionChanged.CollectionChanged -= value; } }

        /// <inheritdoc/>
        public IEnumerator GetEnumerator()
        {
            return List.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return List.GetEnumerator();
        }

        /// <inheritdoc/>
        public void CopyTo(Array array, int index)
        {
            List.CopyTo((T[])array, index);
        }

        /// <inheritdoc/>
        public void CopyTo(T[] array, int arrayIndex)
        {
            List.CopyTo(array, arrayIndex);
        }
        
        /// <inheritdoc/>
        public int Add(object value)
        {
            List.Add((T)value);
            return List.Count - 1;
        }

        /// <inheritdoc/>
        public void Add(T item)
        {
            List.Add(item);
        }

        /// <inheritdoc/>
        public bool Contains(object value)
        {
            return List.Contains((T)value);
        }

        /// <inheritdoc/>
        public bool Contains(T item)
        {
            return List.Contains(item);
        }
        
        /// <inheritdoc/>
        public void Clear()
        {
            List.Clear();
        }

        /// <inheritdoc/>
        public int IndexOf(object value)
        {
            return List.IndexOf((T)value);
        }

        /// <inheritdoc/>
        public int IndexOf(T item)
        {
            return List.IndexOf(item);
        }
        
        /// <inheritdoc/>
        public void Insert(int index, object value)
        {
            List.Insert(index, (T)value);
        }

        /// <inheritdoc/>
        public void Insert(int index, T item)
        {
            List.Insert(index, item);
        }
        
        /// <inheritdoc/>
        public void Remove(object value)
        {
            List.Remove((T)value);
        }

        /// <inheritdoc/>
        public bool Remove(T item)
        {
            return List.Remove(item);
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            List.RemoveAt(index);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{{{GetType().Name}}} Count = {Count}";
        }
    }
}
