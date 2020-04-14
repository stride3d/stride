// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Collections
{
    /// <summary>
    /// An observable collection that automatically sorts inserted items using either the default comparer for their type, or a custom provider comparer.
    /// Insertion and search are both O(log(n)). The method <see cref="SortedObservableCollection{T}.BinarySearch"/> must be used for O(log(n)).
    /// The items must implement <see cref="INotifyPropertyChanging"/> and <see cref="INotifyPropertyChanged"/>.
    /// The collection watches for property changes in its items and reorders them accordingly if the changes affect the order.
    /// </summary>
    /// <typeparam name="T">The type of item this collection contains. Must be a class that implements <see cref="INotifyPropertyChanging"/> and <see cref="INotifyPropertyChanged"/>.</typeparam>
    /// <seealso cref="SortedObservableCollection{T}"/>
    public class AutoUpdatingSortedObservableCollection<T> : SortedObservableCollection<T> where T : class, INotifyPropertyChanging, INotifyPropertyChanged
    {
        protected T ChangingItem;
        protected int ChangingIndex;
        protected T AddedItem;
        protected int AddedIndex;
        private int changeCount;
        private HashSet<string> propertyNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoUpdatingSortedObservableCollection{T}"/> class with a comparer.
        /// </summary>
        /// <param name="comparer">The comparer to use to compare items. If null, the default comparison for the type T will be used.</param>
        /// <exception cref="InvalidOperationException">No comparer has been provided and the associated type does not implement <see cref="IComparable"/> nor <see cref="IComparable{T}"/>.</exception>
        public AutoUpdatingSortedObservableCollection(IComparer<T> comparer)
            : base(comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoUpdatingSortedObservableCollection{T}"/> class with a comparison delegate.
        /// </summary>
        /// <param name="comparison">The comparison to use to compare items. If null, the default comparison for the type T will be used.</param>
        /// <exception cref="InvalidOperationException">No comparison has been provided and the associated type does not implement <see cref="IComparable"/> nor <see cref="IComparable{T}"/>.</exception>
        public AutoUpdatingSortedObservableCollection(Comparison<T> comparison = null)
            : base(comparison)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoUpdatingSortedObservableCollection{T}"/> class with a comparer.
        /// </summary>
        /// <param name="comparer">The comparer to use to compare items. If null, the default comparison for the type T will be used.</param>
        /// <param name="propertyName">The name of the property that should trigger an update of sorting if it changes.</param>
        /// <param name="otherPropertyNames">The name of additional properties that should trigger an update of sorting if they change.</param>
        /// <exception cref="InvalidOperationException">No comparer has been provided and the associated type does not implement <see cref="IComparable"/> nor <see cref="IComparable{T}"/>.</exception>
        public AutoUpdatingSortedObservableCollection(IComparer<T> comparer, [NotNull] string propertyName, [ItemNotNull] params string[] otherPropertyNames)
            : base(comparer)
        {
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            RegisterPropertyNames(propertyName, otherPropertyNames);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoUpdatingSortedObservableCollection{T}"/> class with a comparison delegate.
        /// </summary>
        /// <param name="comparison">The comparison to use to compare items. If null, the default comparison for the type T will be used.</param>
        /// <param name="propertyName">The name of the property that should trigger an update of sorting if it changes.</param>
        /// <param name="otherPropertyNames">The name of additional properties that should trigger an update of sorting if they change.</param>
        /// <exception cref="InvalidOperationException">No comparison has been provided and the associated type does not implement <see cref="IComparable"/> nor <see cref="IComparable{T}"/>.</exception>
        public AutoUpdatingSortedObservableCollection(Comparison<T> comparison, [NotNull] string propertyName, [ItemNotNull] params string[] otherPropertyNames)
            : base(comparison)
        {
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            RegisterPropertyNames(propertyName, otherPropertyNames);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoUpdatingSortedObservableCollection{T}"/> class with a comparer.
        /// </summary>
        /// <param name="propertyName">The name of the property that should trigger an update of sorting if it changes.</param>
        /// <param name="otherPropertyNames">The name of additional properties that should trigger an update of sorting if they change.</param>
        /// <exception cref="InvalidOperationException">No comparer has been provided and the associated type does not implement <see cref="IComparable"/> nor <see cref="IComparable{T}"/>.</exception>
        public AutoUpdatingSortedObservableCollection([NotNull] string propertyName, [ItemNotNull] params string[] otherPropertyNames)
            : base((IComparer<T>)null)
        {
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            RegisterPropertyNames(propertyName, otherPropertyNames);
        }

        private void RegisterPropertyNames([NotNull] string propertyName, [ItemNotNull] params string[] otherPropertyNames)
        {
            propertyNames = new HashSet<string> { propertyName };
            if (otherPropertyNames != null)
            {
                foreach (var name in otherPropertyNames)
                {
                    propertyNames.Add(name);
                }
            }
        }
        
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{{AutoUpdatingSortedObservableCollection}} Count = {Count}";
        }
        
        protected virtual void ItemPropertyChanging(object sender, [NotNull] PropertyChangingEventArgs e)
        {
            if (propertyNames != null && !propertyNames.Contains(e.PropertyName))
                return;

            var item = (T)sender;
            if (ChangingItem != null && !ReferenceEquals(ChangingItem, item))
                throw new InvalidOperationException("Multiple items in the collection are changing concurrently.");

            ++changeCount;

            ChangingItem = item;
            ChangingIndex = GetIndex(item, false);
            AddedItem = null;
        }

        protected virtual void ItemPropertyChanged(object sender, [NotNull] PropertyChangedEventArgs e)
        {
            if (propertyNames != null && !propertyNames.Contains(e.PropertyName))
                return;

            var item = (T)sender;

            // An object has been added while a property of an existing object has been modified
            if (ChangingItem != null && AddedItem != null)
                throw new InvalidOperationException("PropertyChanged is invoked without PropertyChanging, or multiple items in the collection are changing concurrently.");

            // The object has been added to the collection during the property change, so it was not preregistered by the ItemPropertyChanging event
            if (ChangingItem == null && AddedItem != null)
            {
                ChangingItem = AddedItem;
                ChangingIndex = AddedIndex;
                ++changeCount;
            }

            // No object is changing, or a different object is currently changing
            if (ChangingItem == null || !ReferenceEquals(ChangingItem, item))
            {
                 throw new InvalidOperationException("PropertyChanged is invoked without PropertyChanging, or multiple items in the collection are changing concurrently.");
            }

            --changeCount;
            if (changeCount == 0)
            {
                bool needReorder = (ChangingIndex > 0 && DefaultCompareFunc(Items[ChangingIndex - 1], item) > 0) || (ChangingIndex < Count - 1 && DefaultCompareFunc(item, Items[ChangingIndex + 1]) > 0);
                if (needReorder)
                {
                    int newIndex = GetReorderingIndex(item);
                    if (newIndex != ChangingIndex && newIndex != ChangingIndex + 1)
                    {
                        if (newIndex > ChangingIndex)
                            --newIndex;

                        ObservableCollectionMoveItem(ChangingIndex, newIndex);
                    }
                    else
                    ChangingIndex = GetIndex(item, false);
                }
                ChangingItem = null;
            }
        }

        protected int GetReorderingIndex(T item)
        {
            int imin = 0;
            int imax = Count - 1;
            while (imax >= imin)
            {
                int imid = (imin + imax) / 2;

                int comp = DefaultCompareFunc(this[imid], item);
                if (comp < 0)
                    imin = imid + 1;
                else if (comp > 0)
                    imax = imid - 1;
                else
                {
                    bool found = true;
                    if (imid > 0)
                    {
                        comp = DefaultCompareFunc(this[imid - 1], item);
                        if (comp > 0)
                        {
                            imax = imid - 1;
                            found = false;
                        }
                    }
                    if (imid < Count - 1)
                    {
                        comp = DefaultCompareFunc(this[imid + 1], item);
                        if (comp < 0)
                        {
                            imin = imid + 1;
                            found = false;
                        }
                    }
                    if (found)
                        return imid;
                }
            }

            return imin;
        }

        /// <inheritdoc/>
        protected override void InsertItem(int index, [NotNull] T item)
        {
            item.PropertyChanging += ItemPropertyChanging;
            item.PropertyChanged += ItemPropertyChanged;
            base.InsertItem(index, item);
            AddedItem = item;
            AddedIndex = GetIndex(item, false);
        }

        /// <inheritdoc/>
        protected override void ClearItems()
        {
            foreach (var item in Items)
            {
                item.PropertyChanging -= ItemPropertyChanging;
                item.PropertyChanged -= ItemPropertyChanged;
            }
            base.ClearItems();
        }

        /// <inheritdoc/>
        protected override void RemoveItem(int index)
        {
            var item = Items[index];
            item.PropertyChanging -= ItemPropertyChanging;
            item.PropertyChanged -= ItemPropertyChanged;
            if (ChangingItem == item)
            {
                ChangingItem = null;
                changeCount = 0;
            }
            base.RemoveItem(index);
        }
    }
}
