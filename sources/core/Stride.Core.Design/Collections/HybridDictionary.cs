// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Stride.Core.Extensions;

namespace Stride.Core.Collections;

public class HybridDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    where TKey : notnull
{
    private const int CutoverPoint = 9;
    private const int InitialDictionarySize = 13;
    private const int ConstructorCutoverPoint = 6;
    private readonly IEqualityComparer<TKey> keyComparer;
    private List<KeyValuePair<TKey, TValue>>? list;
    private Dictionary<TKey, TValue>? dictionary;
    private ICollection<TKey>? keyCollection;
    private ICollection<TValue>? valueCollection;

    [Conditional("DEBUG")]
    private void CheckInvariant()
    {
        // invariant: dictionary is not null when list is null
        Debug.Assert(list is not null || dictionary is not null);
    }

    public HybridDictionary() : this(0, null)
    {
    }
    public HybridDictionary(int capacity) : this(capacity, null)
    {
    }

    public HybridDictionary(IEqualityComparer<TKey> comparer) : this(0, comparer)
    {
    }

    public HybridDictionary(IDictionary<TKey, TValue> dictionary) : this(dictionary, null)
    {
    }

    public HybridDictionary(int capacity, IEqualityComparer<TKey>? comparer)
    {
#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
#else
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
#endif
        keyComparer = comparer ?? EqualityComparer<TKey>.Default;
        if (capacity >= ConstructorCutoverPoint)
        {
            dictionary = new Dictionary<TKey, TValue>(capacity, keyComparer);
        }
        else
        {
            list = new List<KeyValuePair<TKey, TValue>>(capacity);
        }
    }
    public HybridDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer)
        : this(dictionary.SafeArgument().Count, comparer)
    {
        foreach (var keyValuePair in dictionary)
            InternalAdd(keyValuePair);
    }

    public TValue this[TKey key]
    {
        get => Fetch(key);
        set => Update(key, value);
    }

    public ICollection<TKey> Keys
    {
        get
        {
            CheckInvariant();
            if (keyCollection != null)
                return keyCollection;

            if (list != null)
            {
                keyCollection = new List<TKey>(list.Count);
                foreach (var item in list)
                    keyCollection.Add(item.Key);
                return keyCollection;
            }
            return keyCollection = dictionary!.Keys;
        }
    }

    public ICollection<TValue> Values
    {
        get
        {
            CheckInvariant();
            if (valueCollection != null)
                return valueCollection;

            if (list != null)
            {
                valueCollection = new List<TValue>(list.Count);
                foreach (var item in list)
                    valueCollection.Add(item.Value);
                return valueCollection;
            }
            return valueCollection = dictionary!.Values;
        }
    }

    public int Count => list?.Count ?? dictionary!.Count;

    public bool IsReadOnly => false;

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        CheckInvariant();
        if (list != null)
        {
            foreach (var kvp in list)
            {
                if (keyComparer.Equals(kvp.Key, item.Key))
                    throw new ArgumentException("An item with the same key has already been added.");
            }
        }
        InternalAdd(item);
    }

    public void Add(TKey key, TValue value)
    {
        CheckInvariant();
        if (list != null)
        {
            foreach (var kvp in list)
            {
                if (keyComparer.Equals(kvp.Key, key))
                    throw new ArgumentException("An item with the same key has already been added.");
            }
        }
        InternalAdd(new KeyValuePair<TKey, TValue>(key, value));
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        CheckInvariant();
        keyCollection = null;
        valueCollection = null;
        if (list != null)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var kvp = list[i];
                if (keyComparer.Equals(kvp.Key, item.Key) && Equals(kvp.Value, item.Value))
                {
                    list.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary!).Remove(item);
    }

    public bool Remove(TKey key)
    {
        CheckInvariant();
        keyCollection = null;
        valueCollection = null;
        if (list != null)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var kvp = list[i];
                if (keyComparer.Equals(kvp.Key, key))
                {
                    list.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        return dictionary!.Remove(key);
    }

    public void Clear()
    {
        list?.Clear();
        dictionary?.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        CheckInvariant();
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(item.Key);
#else
        if (item.Key is null) throw new ArgumentNullException(nameof(item.Key));
#endif
        if (list != null)
        {
            foreach (var kvp in list)
            {
                if (keyComparer.Equals(kvp.Key, item.Key))
                    return Equals(kvp.Value, item.Value);
            }
            return false;
        }
        return dictionary!.Contains(item);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        CheckInvariant();
        if (list != null)
        {
            list.CopyTo(array, arrayIndex);
        }
        else
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)dictionary!).CopyTo(array, arrayIndex);
        }
    }

    public bool ContainsKey(TKey key)
    {
        CheckInvariant();
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(key);
#else
        if (key is null) throw new ArgumentNullException(nameof(key));
#endif
        if (list != null)
        {
            foreach (var kvp in list)
            {
                if (keyComparer.Equals(kvp.Key, key))
                    return true;
            }
            return false;
        }
        return dictionary!.ContainsKey(key);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        CheckInvariant();
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(key);
#else
        if (key is null) throw new ArgumentNullException(nameof(key));
#endif
        if (list != null)
        {
            foreach (var kvp in list)
            {
                if (keyComparer.Equals(kvp.Key, key))
                {
                    value = kvp.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }
        return dictionary!.TryGetValue(key, out value);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        CheckInvariant();
        return (IEnumerator<KeyValuePair<TKey, TValue>>?)list?.GetEnumerator() ?? dictionary!.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private TValue Fetch(TKey key)
    {
        if (list != null)
        {
            foreach (var kvp in list)
            {
                if (keyComparer.Equals(kvp.Key, key))
                    return kvp.Value;
            }
            throw new KeyNotFoundException("The given key was not present in the dictionary.");
        }
        return dictionary![key];
    }

    private void Update(TKey key, TValue value)
    {
        valueCollection = null;
        if (list != null)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var kvp = list[i];
                if (keyComparer.Equals(kvp.Key, key))
                {
                    list[i] = new KeyValuePair<TKey, TValue>(key, value);
                    return;
                }
            }
            keyCollection = null;
            InternalAdd(new KeyValuePair<TKey, TValue>(key, value));
            return;
        }
        keyCollection = null;
        dictionary![key] = value;
    }

    private void InternalAdd(KeyValuePair<TKey, TValue> item)
    {
        keyCollection = null;
        valueCollection = null;
        if (list != null)
        {
            if (list.Count + 1 < CutoverPoint)
            {
                list.Add(item);
                return;
            }
            else
            {
                ChangeOver();
            }
        }
        dictionary!.Add(item.Key, item.Value);
    }

    private void ChangeOver()
    {
        if (list == null)
            throw new InvalidOperationException("Internal error, the collection has already muted to a dictionary.");

        dictionary = new Dictionary<TKey, TValue>(InitialDictionarySize, keyComparer);
        foreach (var item in list)
        {
            dictionary.Add(item.Key, item.Value);
        }
        list = null;
    }
}
