// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Stride.BepuPhysics.Definitions;

/// <summary>
/// Every function in this class has a 01 complexity, adding allocates one small class.
/// </summary>
/// <remarks>
/// You should do a reverse for loop if you intend to iterate over this list while removing because of how the <see cref="SwapRemoveAt"/> work.
/// </remarks>
internal class UnsortedO1List<T, T2> where T : notnull
{
    private readonly Dictionary<T, Indexer> _dict = new();
    private readonly List<SequentialData> _list = new();

    internal class Indexer
    {
        internal int Value;
    }

    public struct SequentialData
    {
        public T Key;
        public T2 Value;
        internal Indexer Indexer { get; init; }
    }

    public int Count => _list.Count;
    public bool IsReadOnly => false;

    public T2 this[int index]
    {
        get => _list[index].Value;
        set
        {
            var v = _list[index];
            v.Value = value;
            _list[index] = v;
        }
    }

    /// <summary>
    /// You should not add to the collection while using this span, you must do a reverse for loop when removing while iterating over this span
    /// </summary>
    public Span<SequentialData> UnsafeGetSpan() => CollectionsMarshal.AsSpan(_list);

    public void Add(T key, T2 value)
    {
        var index = new Indexer { Value = _list.Count };
        _dict.Add(key, index);
        _list.Add(new()
        {
            Key = key,
            Value = value,
            Indexer = index
        });
    }

    public bool Remove(T item)
    {
        if (_dict.Remove(item, out var indexer))
        {
            // RemoveAt for non-last element is costly, instead we ...
            // Replace the content of the list at the position this element occupied with the last element of the array, then remove the last element
            // This means that the list is not sorted

            if (_list.Count > 0)
            {
                var last = _list[^1];
                last.Indexer.Value = indexer.Value; // Notifies the dictionary that this key changed index
                _list[indexer.Value] = last;
            }

            _list.RemoveAt(_list.Count - 1);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Replace the element at this index with the last element and remove the last element.
    /// </summary>
    /// <remarks>
    /// Because of how this method work, you should do a reverse for loop if you intend to iterate while removing.
    /// </remarks>
    public void SwapRemoveAt(int index)
    {
        _dict.Remove(_list[index].Key);
        if (_list.Count > 0)
        {
            var last = _list[^1];
            last.Indexer.Value = index; // Notifies the dictionary that this key changed index
            _list[index] = last;
        }
        _list.RemoveAt(_list.Count - 1);
    }

    public bool Contains(T item) => _dict.ContainsKey(item);

    public bool TryGet(T key, [MaybeNullWhen(false)] out T2 value)
    {
        if (_dict.TryGetValue(key, out Indexer? indexer))
        {
            value = _list[indexer.Value].Value;
            return true;
        }

        value = default;
        return false;
    }

    public void Clear()
    {
        _dict.Clear();
        _list.Clear();
    }
}