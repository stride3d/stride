// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Specialized;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;

namespace Stride.Core.Collections
{
    /// <summary>
    /// Represents a collection that generates events when items get added or removed.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    [DataSerializer(typeof(ListAllSerializer<,>), Mode = DataSerializerGenericMode.TypeAndGenericArguments)]
    public class TrackingCollection<T> : FastCollection<T>, ITrackingCollectionChanged
    {
        private EventHandler<TrackingCollectionChangedEventArgs> itemAdded;
        private EventHandler<TrackingCollectionChangedEventArgs> itemRemoved;

        /// <inheritdoc/>
        public event EventHandler<TrackingCollectionChangedEventArgs> CollectionChanged
        {
            add
            {
                // We keep a list in reverse order for removal, so that we can easily have multiple handlers depending on each others
                itemAdded = (EventHandler<TrackingCollectionChangedEventArgs>)Delegate.Combine(itemAdded, value);
                itemRemoved = (EventHandler<TrackingCollectionChangedEventArgs>)Delegate.Combine(value, itemRemoved);
            }
            remove
            {
                itemAdded = (EventHandler<TrackingCollectionChangedEventArgs>)Delegate.Remove(itemAdded, value);
                itemRemoved = (EventHandler<TrackingCollectionChangedEventArgs>)Delegate.Remove(itemRemoved, value);
            }
        }

        /// <inheritdoc/>
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            itemAdded?.Invoke(this, new TrackingCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, null, index, true));
        }

        /// <inheritdoc/>
        protected override void RemoveItem(int index)
        {
            itemRemoved?.Invoke(this, new TrackingCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, this[index], null, index, true));
            base.RemoveItem(index);
        }

        /// <inheritdoc/>
        protected override void ClearItems()
        {
            ClearItemsEvents();
            base.ClearItems();
        }

        protected void ClearItemsEvents()
        {
            // Note: Changing CollectionChanged is not thread-safe
            var collectionChanged = itemRemoved;
            if (collectionChanged != null)
            {
                for (var i = Count - 1; i >= 0; --i)
                    collectionChanged(this, new TrackingCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, this[i], null, i, true));
            }
        }

        /// <inheritdoc/>
        protected override void SetItem(int index, T item)
        {
            // Note: Changing CollectionChanged is not thread-safe
            var collectionChangedRemoved = itemRemoved;

            var oldItem = collectionChangedRemoved != null ? (object)this[index] : null;
            collectionChangedRemoved?.Invoke(this, new TrackingCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItem, null, index, false));

            base.SetItem(index, item);

            itemAdded?.Invoke(this, new TrackingCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, oldItem, index, false));
        }
    }
}
