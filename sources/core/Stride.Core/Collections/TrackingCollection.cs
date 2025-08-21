// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;

namespace Stride.Core.Collections;

/// <summary>
/// Represents a collection that generates events when items get added or removed.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
[DataSerializer(typeof(ListAllSerializer<,>), Mode = DataSerializerGenericMode.TypeAndGenericArguments)]
public class TrackingCollection<T> : FastCollection<T>, ITrackingCollectionChanged<T, T>
{
    private EventHandler<TrackingCollectionChangedEventArgs<T, T>>? _itemAdded;
    private EventHandler<TrackingCollectionChangedEventArgs<T, T>>? _itemRemoved;

    /// <inheritdoc/>
    public event EventHandler<TrackingCollectionChangedEventArgs<T, T>> CollectionChanged
    {
        add
        {
            // We keep a list in reverse order for removal, so that we can easily have multiple handlers depending on each others
            _itemAdded = (EventHandler<TrackingCollectionChangedEventArgs<T, T>>)Delegate.Combine(_itemAdded, value);
            _itemRemoved = (EventHandler<TrackingCollectionChangedEventArgs<T, T>>)Delegate.Combine(value, _itemRemoved);
        }
        remove
        {
            _itemAdded = (EventHandler<TrackingCollectionChangedEventArgs<T, T>>?)Delegate.Remove(_itemAdded, value);
            _itemRemoved = (EventHandler<TrackingCollectionChangedEventArgs<T, T>>?)Delegate.Remove(_itemRemoved, value);
        }
    }

    /// <inheritdoc/>
    protected override void InsertItem(int index, T item)
    {
        base.InsertItem(index, item);
        _itemAdded?.Invoke(this, new TrackingCollectionChangedEventArgs<T, T>(NotifyCollectionChangedAction.Add, item, default, index, true));
    }

    /// <inheritdoc/>
    protected override void RemoveItem(int index)
    {
        _itemRemoved?.Invoke(this, new TrackingCollectionChangedEventArgs<T, T>(NotifyCollectionChangedAction.Remove, this[index], default, index, true));
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
        var collectionChanged = _itemRemoved;
        if (collectionChanged != null)
        {
            for (var i = Count - 1; i >= 0; --i)
                collectionChanged(this, new TrackingCollectionChangedEventArgs<T, T>(NotifyCollectionChangedAction.Remove, this[i], default, i, true));
        }
    }

    /// <inheritdoc/>
    protected override void SetItem(int index, T item)
    {
        // Note: Changing CollectionChanged is not thread-safe
        var collectionChangedRemoved = _itemRemoved;

        T? oldItem = collectionChangedRemoved != null ? this[index] : default;
        collectionChangedRemoved?.Invoke(this, new TrackingCollectionChangedEventArgs<T, T>(NotifyCollectionChangedAction.Remove, oldItem, default, index, false));

        base.SetItem(index, item);

        _itemAdded?.Invoke(this, new TrackingCollectionChangedEventArgs<T, T>(NotifyCollectionChangedAction.Add, item, oldItem, index, false));
    }
}
