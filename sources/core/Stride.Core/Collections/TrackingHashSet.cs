// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;

namespace Stride.Core.Collections;

/// <summary>
/// Represents a hash set that generates events when items get added or removed.
/// </summary>
/// <remarks>
/// Underlying storage is done with a <see cref="HashSet{T}"/>.
/// </remarks>
/// <typeparam name="T">The type of elements in the hash set.</typeparam>
public class TrackingHashSet<T> : ISet<T>, IReadOnlySet<T>, ITrackingCollectionChanged<T, T>
{
    private readonly HashSet<T> innerHashSet = [];

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
    public bool Add(T item)
    {
        if (innerHashSet.Add(item))
        {
            _itemAdded?.Invoke(this, new TrackingCollectionChangedEventArgs<T, T>(NotifyCollectionChangedAction.Add, item, default, -1, true));
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void ExceptWith(IEnumerable<T> other)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void IntersectWith(IEnumerable<T> other)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        return innerHashSet.IsProperSubsetOf(other);
    }

    /// <inheritdoc/>
    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        return innerHashSet.IsProperSupersetOf(other);
    }

    /// <inheritdoc/>
    public bool IsSubsetOf(IEnumerable<T> other)
    {
        return innerHashSet.IsSubsetOf(other);
    }

    /// <inheritdoc/>
    public bool IsSupersetOf(IEnumerable<T> other)
    {
        return innerHashSet.IsSupersetOf(other);
    }

    /// <inheritdoc/>
    public bool Overlaps(IEnumerable<T> other)
    {
        return innerHashSet.Overlaps(other);
    }

    /// <inheritdoc/>
    public bool SetEquals(IEnumerable<T> other)
    {
        return innerHashSet.SetEquals(other);
    }

    /// <inheritdoc/>
    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void UnionWith(IEnumerable<T> other)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    void ICollection<T>.Add(T item)
    {
        innerHashSet.Add(item);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        if (_itemRemoved != null)
        {
            foreach (var item in innerHashSet.ToArray())
            {
                Remove(item);
            }
        }
        else
        {
            innerHashSet.Clear();
        }
    }

    /// <inheritdoc/>
    public bool Contains(T item)
    {
        return innerHashSet.Contains(item);
    }

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex)
    {
        innerHashSet.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc/>
    public int Count => innerHashSet.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => ((ICollection<T>)innerHashSet).IsReadOnly;

    /// <inheritdoc/>
    public bool Remove(T item)
    {
        if (!innerHashSet.Remove(item))
            return false;

        _itemRemoved?.Invoke(this, new TrackingCollectionChangedEventArgs<T, T>(NotifyCollectionChangedAction.Remove, item, default, -1, true));
        return true;
    }

    /// <inheritdoc/>
    public HashSet<T>.Enumerator GetEnumerator()
    {
        return innerHashSet.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return innerHashSet.GetEnumerator();
    }

    /// <inheritdoc/>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return innerHashSet.GetEnumerator();
    }
}
