// <copyright file="EquatableList.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stride.Shaders.Spirv.Generators;



public class EquatableListJsonConverter<T> : JsonConverter<EquatableList<T>>
    where T : IEquatable<T>
{
    public override EquatableList<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = JsonSerializer.Deserialize<T[]>(ref reader, options) ?? [];
        return new EquatableList<T>([..list]);
    }

    public override void Write(Utf8JsonWriter writer, EquatableList<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(value.AsList(), options);
    }
}

/// <summary>
/// An immutable, equatable list. This is equivalent to <see cref="List"/> but with value equality support.
/// </summary>
/// <typeparam name="T">The type of values in the list.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="EquatableList{T}"/> struct.
/// </remarks>
/// <param name="list">The input list to wrap.</param>
public readonly struct EquatableList<T>(List<T> list) : IEquatable<EquatableList<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    /// <summary>
    /// The underlying <typeparamref name="T"/> list.
    /// </summary>
    private readonly List<T> _list = list;

    /// <summary>
    /// Gets the length of the list, or 0 if the list is null
    /// </summary>
    public int Count => _list.Count;

    /// <summary>
    /// Checks whether two <see cref="EquatableList{T}"/> values are the same.
    /// </summary>
    /// <param name="left">The first <see cref="EquatableList{T}"/> value.</param>
    /// <param name="right">The second <see cref="EquatableList{T}"/> value.</param>
    /// <returns>Whether <paramref name="left"/> and <paramref name="right"/> are equal.</returns>
    public static bool operator ==(EquatableList<T> left, EquatableList<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Checks whether two <see cref="EquatableList{T}"/> values are not the same.
    /// </summary>
    /// <param name="left">The first <see cref="EquatableList{T}"/> value.</param>
    /// <param name="right">The second <see cref="EquatableList{T}"/> value.</param>
    /// <returns>Whether <paramref name="left"/> and <paramref name="right"/> are not equal.</returns>
    public static bool operator !=(EquatableList<T> left, EquatableList<T> right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(EquatableList<T> list)
    {
        if (Count != list.Count)
            return false;
        for (int i = 0; i < Count; i++)
        {
            if (!_list![i].Equals(list._list![i]))
                return false;
        }
        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is EquatableList<T> list && Equals(this, list);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        if (_list is not List<T> list)
        {
            return 0;
        }

        HashCode hashCode = default;

        foreach (T item in list)
        {
            hashCode.Add(item);
        }

        return hashCode.ToHashCode();
    }


    /// <summary>
    /// Returns the underlying wrapped list.
    /// </summary>
    /// <returns>Returns the underlying list.</returns>
    public List<T> AsList()
    {
        return _list;
    }

    /// <inheritdoc/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _list.GetEnumerator();
    }
    
    public static implicit operator EquatableList<T>(List<T> list) => new([.. list]);
    public static implicit operator EquatableList<T>(ImmutableList<T> list) => new([.. list]);
}