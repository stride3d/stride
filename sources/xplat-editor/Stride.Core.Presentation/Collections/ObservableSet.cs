// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Collections;

public class ObservableSet<T> : ObservableCollection<T>, IObservableList<T>, IReadOnlyObservableList<T>
{
    private readonly HashSet<T> hashSet;

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
    }

    [CollectionAccess(CollectionAccessType.UpdatedContent)]
    public ObservableSet(IEqualityComparer<T> comparer, [NotNull] IEnumerable<T> collection)
    {
        hashSet = new HashSet<T>(comparer);
        foreach (var item in collection)
        {
            if (hashSet.Add(item))
                Add(item);
        }
    }

    [CollectionAccess(CollectionAccessType.UpdatedContent)]
    public void AddRange(IEnumerable<T> items)
    {
        var itemList = items.Where(x => hashSet.Add(x)).ToList();
        if (itemList.Count > 0)
        {
            var index = Count;
            foreach (var item in itemList)
            {
                base.InsertItem(index++, item);
            }
        }
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
        hashSet.Remove(item);
        base.RemoveItem(index);
    }

    /// <inheritdoc/>
    [CollectionAccess(CollectionAccessType.None)]
    public override string ToString()
    {
        return $"{{ObservableSet}} Count = {Count}";
    }
}
