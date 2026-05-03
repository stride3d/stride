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
    public class ObservableList<T> : ObservableCollection<T>, IObservableList<T>, IReadOnlyObservableList<T>
    {
        [CollectionAccess(CollectionAccessType.None)]
        public ObservableList()
            : base()
        {
        }

        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        public ObservableList(IEnumerable<T> collection)
            : base(collection)
        {
        }

        [CollectionAccess(CollectionAccessType.None)]
        public ObservableList(int capacity)
            : base(new List<T>(capacity))
        {
        }

        public void AddRange(IEnumerable<T> items)
        {
#if SUPPORT_RANGE_ACTION
            // WPF doesn't support range change from within a ObservableCollection-derived class
            // cf. System.Windows.Data.ListCollectionView vs MS.Internal.Data.EnumerableCollectionView (which is used as a wrapper for other non-derived ObservableCollection)
            // However, we do need to derive from ObservableCollection for Avalonia or some features don't work well (e.g. in tree views)
            var itemList = items.ToList();
            if (Items is List<T> list)
            {
                list.AddRange(itemList);
            }
            else
            {
                foreach (var item in itemList)
                {
                    Items.Add(item);
                }
            }

            if (itemList.Count > 0)
            {
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

        [CollectionAccess(CollectionAccessType.Read)]
        public int FindIndex(Predicate<T> match)
        {
            if (Items is List<T> list)
            {
                return list.FindIndex(match);
            }

            for (int i = 0; i < Count; i++)
            {
                if (match(Items[i])) return i;
            }
            return -1;
        }

        [CollectionAccess(CollectionAccessType.ModifyExistingContent)]
        public void RemoveRange(int index, int count)
        {
#if SUPPORT_RANGE_ACTION
            // WPF doesn't support range change from within a ObservableCollection-derived class
            // cf. System.Windows.Data.ListCollectionView vs MS.Internal.Data.EnumerableCollectionView (which is used as a wrapper for other non-derived ObservableCollection)
            // However, we do need to derive from ObservableCollection for Avalonia or some features don't work well (e.g. in tree views)
            var oldItems = Items.Skip(index).Take(count).ToList();
            if (Items is List<T> list)
            {
                list.RemoveRange(index, count);
            }
            else
            {
                // slow algorithm, optimized from collection's end
                for (int i = 1; i <= count; ++i)
                {
                    Items.RemoveAt(index + count - i);
                }
            }

            if (oldItems.Count > 0)
            {
                OnCountPropertyChanged();
                OnIndexerPropertyChanged();
                var arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, index);
                OnCollectionChanged(arg);
            }
#else
            for (int i = 1; i <= count; ++i)
            {
                RemoveAt(index + count - i);
            }
#endif
        }

        /// <inheritdoc/>
        [CollectionAccess(CollectionAccessType.None)]
        public override string ToString()
        {
            return $"{{ObservableList}} Count = {Count}";
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
