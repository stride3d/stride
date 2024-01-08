// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
#if SUPPORT_RANGE_ACTION
using System.Collections.Specialized;
using System.ComponentModel;
#endif
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Collections
{
    public class ObservableSet<T> : ObservableCollection<T>, IObservableList<T>, IReadOnlyObservableList<T>
    {
        private readonly HashSet<T> hashSet;

        [CollectionAccess(CollectionAccessType.None)]
        public ObservableSet()
            : this(EqualityComparer<T>.Default)
        {
        }

        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        public ObservableSet(IEnumerable<T> collection)
              : this(EqualityComparer<T>.Default, collection)
        {
        }

        [CollectionAccess(CollectionAccessType.None)]
        public ObservableSet(IEqualityComparer<T> comparer)
        {
            hashSet = new HashSet<T>(comparer);
        }

        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        public ObservableSet(IEqualityComparer<T> comparer, IEnumerable<T> collection)
        {
            hashSet = new HashSet<T>(comparer);
            AddRange(collection);
        }

        [CollectionAccess(CollectionAccessType.None)]
        public ObservableSet(int capacity)
            : base(new List<T>(capacity))
        {
            hashSet = [];
        }

        public void AddRange(IEnumerable<T> items)
        {
#if SUPPORT_RANGE_ACTION
            // WPF doesn't support range change from within a ObservableCollection-derived class
            // cf. System.Windows.Data.ListCollectionView vs MS.Internal.Data.EnumerableCollectionView (which is used as a wrapper for other non-derived ObservableCollection)
            // However, we do need to derive from ObservableCollection for Avalonia or some features don't work well (e.g. in tree views)
            var itemList = items.Where(x => hashSet.Add(x)).ToList();
            if (itemList.Count > 0)
            {
                foreach (var item in itemList)
                {
                    Items.Add(item);
                }

                OnCountPropertyChanged();
                OnIndexerPropertyChanged();
                var arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, itemList, Count - itemList.Count);
                OnCollectionChanged(arg);
            }
#else
            foreach (var item in items)
            {
                Add(item);
            }
#endif
        }

        protected override void ClearItems()
        {
            hashSet.Clear();
            base.ClearItems();
        }

        protected override void InsertItem(int index, T item)
        {
            if (hashSet.Add(item))
            {
                base.InsertItem(index, item);
            }
        }

        protected override void SetItem(int index, T item)
        {
            var oldItem = base[index];
            hashSet.Remove(oldItem);
            if (!hashSet.Add(item))
            {
                // restore removed item
                hashSet.Add(oldItem);
                throw new InvalidOperationException("Unable to set this value at the given index because this value is already contained in this ObservableSet.");
            }
            base.SetItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            var item = base[index];
            if (!hashSet.Remove(item))
            {
                // safety check: shouldn't happen. If it does, we have a failed logic somewhere.
                throw new InvalidOperationException("Unable to remove this value at the given index because it wasn't found in the ObservableSet.");
            }
            base.RemoveItem(index);
        }

        /// <inheritdoc/>
        [CollectionAccess(CollectionAccessType.None)]
        public override string ToString()
        {
            return $"{{ObservableSet}} Count = {Count}";
        }

#if SUPPORT_RANGE_ACTION
        /// <summary>
        /// Helper to raise a PropertyChanged event for the Count property
        /// </summary>
        private void OnCountPropertyChanged() => OnPropertyChanged(new PropertyChangedEventArgs("Count"));

        /// <summary>
        /// Helper to raise a PropertyChanged event for the Indexer property
        /// </summary>
        private void OnIndexerPropertyChanged() => OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
#endif
    }
}
