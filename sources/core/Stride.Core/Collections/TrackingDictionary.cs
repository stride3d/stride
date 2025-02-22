// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;

namespace Stride.Core.Collections;

/// <summary>
/// Represents a dictionary of key/value pairs that generates events when items get added or removed.
/// </summary>
/// <remarks>
/// Underlying storage is done with a <see cref="Dictionary{TKey,TValue}"/>.
/// </remarks>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
[DataSerializer(typeof(DictionaryAllSerializer<,,>), Mode = DataSerializerGenericMode.TypeAndGenericArguments)]
public class TrackingDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, ITrackingCollectionChanged
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> innerDictionary;

    private EventHandler<TrackingCollectionChangedEventArgs>? itemAdded;
    private EventHandler<TrackingCollectionChangedEventArgs>? itemRemoved;

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
            itemAdded = (EventHandler<TrackingCollectionChangedEventArgs>?)Delegate.Remove(itemAdded, value);
            itemRemoved = (EventHandler<TrackingCollectionChangedEventArgs>?)Delegate.Remove(itemRemoved, value);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackingDictionary{TKey, TValue}"/> class.
    /// </summary>
    public TrackingDictionary()
    {
        innerDictionary = [];
    }

    /// <inheritdoc/>
    public void Add(TKey key, TValue value)
    {
        innerDictionary.Add(key, value);
        itemAdded?.Invoke(this, new TrackingCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, key, value, null, true));
    }

    /// <inheritdoc/>
    public bool ContainsKey(TKey key)
    {
        return innerDictionary.ContainsKey(key);
    }

    /// <inheritdoc/>
    public ICollection<TKey> Keys
    {
        get { return innerDictionary.Keys; }
    }

    /// <inheritdoc/>
    public bool Remove(TKey key)
    {
        var collectionChanged = itemRemoved;
        if (collectionChanged != null && innerDictionary.TryGetValue(key, out var dictValue))
            collectionChanged(this, new TrackingCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, key, dictValue, null, true));

        return innerDictionary.Remove(key);
    }

    /// <inheritdoc/>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return innerDictionary.TryGetValue(key, out value);
    }

    /// <inheritdoc/>
    public ICollection<TValue> Values
    {
        get { return innerDictionary.Values; }
    }

    /// <inheritdoc/>
    public TValue this[TKey key]
    {
        get
        {
            return innerDictionary[key];
        }
        set
        {
            var collectionChangedRemoved = itemRemoved;
            if (collectionChangedRemoved != null)
            {
                var alreadyExisting = innerDictionary.TryGetValue(key, out var oldValue);
                if (alreadyExisting)
                    collectionChangedRemoved(this, new TrackingCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, key, oldValue, null, false));

                innerDictionary[key] = value;

                // Note: CollectionChanged is considered not thread-safe, so no need to skip if null here, shouldn't happen
                itemAdded?.Invoke(this, new TrackingCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, key, innerDictionary[key], oldValue, !alreadyExisting));
            }
            else
            {
                innerDictionary[key] = value;
            }
        }
    }

    /// <inheritdoc/>
    public void Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        var collectionChanged = itemRemoved;
        if (collectionChanged != null)
        {
            foreach (var key in innerDictionary.Keys.ToArray())
            {
                Remove(key);
            }
        }
        else
        {
            innerDictionary.Clear();
        }
    }

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return innerDictionary.Contains(item);
    }

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ((IDictionary<TKey, TValue>)innerDictionary).CopyTo(array, arrayIndex);
    }

    /// <inheritdoc/>
    public int Count
    {
        get { return innerDictionary.Count; }
    }

    /// <inheritdoc/>
    public bool IsReadOnly
    {
        get { return ((IDictionary<TKey, TValue>)innerDictionary).IsReadOnly; }
    }

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        var collectionChanged = itemRemoved;
        if (collectionChanged != null && innerDictionary.Contains(item))
            return innerDictionary.Remove(item.Key);

        return ((IDictionary<TKey, TValue>)innerDictionary).Remove(item);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return innerDictionary.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return innerDictionary.GetEnumerator();
    }

    /// <inheritdoc/>
    void IDictionary.Add(object key, object value)
    {
        Add((TKey)key, (TValue)value);
    }

    /// <inheritdoc/>
    bool IDictionary.Contains(object key)
    {
        return ((IDictionary)innerDictionary).Contains(key);
    }

    /// <inheritdoc/>
    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return ((IDictionary)innerDictionary).GetEnumerator();
    }

    /// <inheritdoc/>
    bool IDictionary.IsFixedSize
    {
        get { return ((IDictionary)innerDictionary).IsFixedSize; }
    }

    /// <inheritdoc/>
    ICollection IDictionary.Keys
    {
        get { return ((IDictionary)innerDictionary).Keys; }
    }

    /// <inheritdoc/>
    void IDictionary.Remove(object key)
    {
        Remove((TKey)key);
    }

    /// <inheritdoc/>
    ICollection IDictionary.Values
    {
        get { return ((IDictionary)innerDictionary).Values; }
    }

    /// <inheritdoc/>
    object IDictionary.this[object key]
    {
        get
        {
            return this[(TKey)key];
        }
        set
        {
            this[(TKey)key] = (TValue)value;
        }
    }

    /// <inheritdoc/>
    void ICollection.CopyTo(Array array, int index)
    {
        ((IDictionary)innerDictionary).CopyTo(array, index);
    }

    /// <inheritdoc/>
    bool ICollection.IsSynchronized
    {
        get { return ((IDictionary)innerDictionary).IsSynchronized; }
    }

    /// <inheritdoc/>
    object ICollection.SyncRoot
    {
        get { return ((IDictionary)innerDictionary).SyncRoot; }
    }
}
