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
    public class FastTrackingCollection<T> : FastCollection<T>
    {
        public delegate void FastEventHandler<TEventArgs>(object sender, ref TEventArgs e);

        private FastEventHandler<FastTrackingCollectionChangedEventArgs> itemAdded;
        private FastEventHandler<FastTrackingCollectionChangedEventArgs> itemRemoved;

        public event FastEventHandler<FastTrackingCollectionChangedEventArgs> CollectionChanged
        {
            add
            {
                // We keep a list in reverse order for removal, so that we can easily have multiple handlers depending on each others
                itemAdded = (FastEventHandler<FastTrackingCollectionChangedEventArgs>)Delegate.Combine(itemAdded, value);
                itemRemoved = (FastEventHandler<FastTrackingCollectionChangedEventArgs>)Delegate.Combine(value, itemRemoved);
            }
            remove
            {
                itemAdded = (FastEventHandler<FastTrackingCollectionChangedEventArgs>)Delegate.Remove(itemAdded, value);
                itemRemoved = (FastEventHandler<FastTrackingCollectionChangedEventArgs>)Delegate.Remove(itemRemoved, value);
            }
        }

        /// <inheritdoc/>
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            var collectionChanged = itemAdded;
            if (collectionChanged == null) return;
            var e = new FastTrackingCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, null, index, true);
            collectionChanged(this, ref e);
        }

        /// <inheritdoc/>
        protected override void RemoveItem(int index)
        {
            var collectionChanged = itemRemoved;
            if (collectionChanged != null)
            {
                var e = new FastTrackingCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, this[index], null, index, true);
                collectionChanged(this, ref e);
            }
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
            var collectionChanged = itemRemoved;
            if (collectionChanged == null) return;
            for (var i = Count - 1; i >= 0; --i)
            {
                var e = new FastTrackingCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, this[i], null, i, true);
                collectionChanged(this, ref e);
            }
        }

        /// <inheritdoc/>
        protected override void SetItem(int index, T item)
        {
            // Note: Changing CollectionChanged is not thread-safe
            var collectionChangedRemoved = itemRemoved;

            var oldItem = (collectionChangedRemoved != null) ? (object)this[index] : null;
            if (collectionChangedRemoved != null)
            {
                var e = new FastTrackingCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItem, null, index);
                collectionChangedRemoved(this, ref e);
            }

            base.SetItem(index, item);

            var collectionChangedAdded = itemAdded;
            if (collectionChangedAdded != null)
            {
                var e = new FastTrackingCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, oldItem, index);
                collectionChangedAdded(this, ref e);
            }
        }
    }
}
