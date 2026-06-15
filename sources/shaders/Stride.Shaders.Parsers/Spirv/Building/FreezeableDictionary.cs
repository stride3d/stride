using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Stride.Shaders.Spirv.Building;

/// <summary>
/// A dictionary wrapper that can be frozen to prevent mutations.
/// Read operations (indexer get, TryGetValue, ContainsKey, Count, enumeration) always work.
/// Write operations (Add, Remove, indexer set, Clear) throw after freezing.
/// </summary>
public class FreezeableDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> inner;

    public bool Frozen { get; set; }

    public FreezeableDictionary() => inner = new();
    public FreezeableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source) => inner = new(source);
    public FreezeableDictionary(FreezeableDictionary<TKey, TValue> other) => inner = new(other.inner);

    private void ThrowIfFrozen()
    {
        if (Frozen)
            throw new InvalidOperationException("Attempted to mutate a frozen dictionary. Cached shader contexts must not be modified.");
    }

    // Read operations — always allowed
    public TValue this[TKey key]
    {
        get => inner[key];
        set { ThrowIfFrozen(); inner[key] = value; }
    }
    public int Count => inner.Count;
    public bool ContainsKey(TKey key) => inner.ContainsKey(key);
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => inner.TryGetValue(key, out value);
    public IEnumerable<TKey> Keys => inner.Keys;
    public IEnumerable<TValue> Values => inner.Values;
    /// <summary>Returns the struct enumerator directly to avoid boxing in foreach.</summary>
    public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => inner.GetEnumerator();
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => inner.GetEnumerator();

    // Write operations — throw when frozen
    public void Add(TKey key, TValue value) { ThrowIfFrozen(); inner.Add(key, value); }
    public bool TryAdd(TKey key, TValue value) { ThrowIfFrozen(); return inner.TryAdd(key, value); }
    public bool Remove(TKey key) { ThrowIfFrozen(); return inner.Remove(key); }
    public void Clear() { ThrowIfFrozen(); inner.Clear(); }
}
