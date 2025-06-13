// <copyright file="EquatableArray.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stride.Shaders.Spirv.Generators;



public class EquatableDictionaryJsonConverter<TKey, TValue> : JsonConverter<EquatableDictionary<TKey, TValue>>
    where TKey : IEquatable<TKey>
    where TValue : IEquatable<TValue>
{
    public override EquatableDictionary<TKey,TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<TKey,TValue>>(ref reader, options) ?? [];
        return new EquatableDictionary<TKey, TValue>(dict);
    }

    public override void Write(Utf8JsonWriter writer, EquatableDictionary<TKey,TValue> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(value.AsDictionary(), options);
    }
}

/// <summary>
/// An immutable, equatable array. This is equivalent to <see cref="Array"/> but with value equality support.
/// </summary>
/// <typeparam name="T">The type of values in the array.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="EquatableArray{T}"/> struct.
/// </remarks>
/// <param name="dict">The input array to wrap.</param>
public readonly struct EquatableDictionary<TKey, TValue>(Dictionary<TKey, TValue>? dict) : IEquatable<EquatableDictionary<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : IEquatable<TKey>
    where TValue : IEquatable<TValue>
{
    /// <summary>
    /// The underlying <typeparamref name="T"/> array.
    /// </summary>
    private readonly Dictionary<TKey,TValue>? _dict = dict;

    /// <summary>
    /// Gets the length of the array, or 0 if the array is null
    /// </summary>
    public int Count => _dict?.Count ?? 0;

    /// <summary>
    /// Checks whether two <see cref="EquatableArray{T}"/> values are the same.
    /// </summary>
    /// <param name="left">The first <see cref="EquatableArray{T}"/> value.</param>
    /// <param name="right">The second <see cref="EquatableArray{T}"/> value.</param>
    /// <returns>Whether <paramref name="left"/> and <paramref name="right"/> are equal.</returns>
    public static bool operator ==(EquatableDictionary<TKey, TValue> left, EquatableDictionary<TKey, TValue> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Checks whether two <see cref="EquatableArray{T}"/> values are not the same.
    /// </summary>
    /// <param name="left">The first <see cref="EquatableArray{T}"/> value.</param>
    /// <param name="right">The second <see cref="EquatableArray{T}"/> value.</param>
    /// <returns>Whether <paramref name="left"/> and <paramref name="right"/> are not equal.</returns>
    public static bool operator !=(EquatableDictionary<TKey, TValue> left, EquatableDictionary<TKey, TValue> right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(EquatableDictionary<TKey, TValue> other)
    {
        return _dict?.Count == other._dict?.Count &&
               _dict?.All(kv => other._dict?.TryGetValue(kv.Key, out var value) == true && kv.Value.Equals(value)) == true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is EquatableDictionary<TKey, TValue> dict && Equals(this, dict);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        if (_dict is not Dictionary<TKey,TValue> dict)
        {
            return 0;
        }

        HashCode hashCode = default;

        foreach (var kv in dict)
        {
            hashCode.Add(kv);
        }

        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Returns the underlying wrapped array.
    /// </summary>
    /// <returns>Returns the underlying array.</returns>
    public Dictionary<TKey, TValue>? AsDictionary()
    {
        return _dict;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return _dict?.GetEnumerator() ?? Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static implicit operator EquatableDictionary<TKey,TValue>(Dictionary<TKey,TValue> dict) => new(dict);
}