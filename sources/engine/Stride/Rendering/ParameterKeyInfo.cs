// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;

namespace Stride.Rendering;

/// <summary>
///   Represents information about a <see cref="ParameterKey"/>, including the key that identifies it,
///   and details specific to value or resource parameters such as offset, count, or binding slot.
/// </summary>
/// <remarks>
///   This structure is used to describe both value parameters and resource parameters.
///   <list type="bullet">
///     <item>For value parameters, it includes an <see cref="Offset"/> and a <see cref="Count"/> of elements.</item>
///     <item>For resource parameters, it includes a <see cref="BindingSlot"/>.</item>
///   </list>
///   Fields that are not applicable for the type of parameter being described
///   will be set to the <see cref="Invalid"/> constant.
/// </remarks>
[DataContract]
public struct ParameterKeyInfo : IEquatable<ParameterKeyInfo>
{
    /// <summary>
    ///   A constant value representing an invalid field in a parameter key info.
    /// </summary>
    public const int Invalid = -1;


    // Common to both value and resource parameters

    /// <summary>
    ///   The key that identifies the parameter.
    /// </summary>
    public ParameterKey Key;

    // For Value parameters

    /// <summary>
    ///   If the parameter is a value, this is the offset where the value can be accessed in its containing layout.
    ///   Otherwise, this is <see cref="Invalid"/>.
    /// </summary>
    public int Offset;
    /// <summary>
    ///   If the parameter is a value, this is the number of elements the value is composed of.
    ///   Otherwise, this is <see cref="Invalid"/>.
    /// </summary>
    public int Count;

    // For Resources (Object) parameters

    /// <summary>
    ///   If the parameter is a resource (like a <c>Texture</c>, a <c>SamplerState</c>, etc.),
    ///   this is the binding slot where that resource is bound.
    ///   Otherwise, this is <see cref="Invalid"/>.
    /// </summary>
    public int BindingSlot;

    #region Convenience properties

    /// <summary>
    ///   Gets a value indicating whether the parameter is a value parameter.
    /// </summary>
    public readonly bool IsValueParameter => Offset != Invalid;

    /// <summary>
    ///   Gets a value indicating whether the parameter is an object (like a <c>Texture</c> or <c>SamplerState</c>)
    ///   parameter, or a permutation parameter.
    /// </summary>
    public readonly bool IsResourceParameter => BindingSlot != Invalid;

    #endregion


    /// <summary>
    ///   Initializes a new instance of the <see cref="ParameterKeyInfo"/> structure
    ///   describing a value parameter with its offset, and number of elements.
    /// </summary>
    /// <param name="key">The parameter key that identifies the value parameter.</param>
    /// <param name="offset">The offset where the value can be accessed in its containing layout.</param>
    /// <param name="count">The number of elements the value parameter is composed of.</param>
    public ParameterKeyInfo(ParameterKey key, int offset, int count)
    {
        Key = key;
        Offset = offset;
        Count = count;
        BindingSlot = Invalid;
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="ParameterKeyInfo"/> structure
    ///   describing a resource parameter with its binding slot.
    /// </summary>
    /// <param name="key">The parameter key that identifies the value parameter.</param>
    /// <param name="bindingSlot">The binding slot where the resource can be found.</param>
    public ParameterKeyInfo(ParameterKey key, int bindingSlot)
    {
        Key = key;
        BindingSlot = bindingSlot;
        Offset = Invalid;
        Count = 1;
    }


    /// <summary>
    ///   Returns an accessor for accessing the parameter as a resource.
    /// </summary>
    /// <returns>A <see cref="ParameterAccessor"/> for accessing the resource.</returns>
    internal readonly ParameterAccessor GetObjectAccessor()
    {
        return new ParameterAccessor(BindingSlot, Count);
    }

    /// <summary>
    ///   Returns an accessor for accessing the parameter as a value.
    /// </summary>
    /// <returns>A <see cref="ParameterAccessor"/> for accessing the value.</returns>
    internal readonly ParameterAccessor GetValueAccessor()
    {
        return new ParameterAccessor(Offset, Count);
    }


    /// <inheritdoc/>
    public override readonly string ToString()
    {
        if (Key is null)
            return "Invalid Parameter Key";

        return IsResourceParameter
            ? $"Object \"{Key}\" at Binding Slot {BindingSlot}"
            : $"Value \"{Key}\" at Offset {Offset}" + (Count > 1
                ? $" (Count {Count})"
                : string.Empty);
    }

    /// <inheritdoc/>
    public readonly bool Equals(ParameterKeyInfo other)
    {
        return Key.Equals(other.Key)
            && Offset == other.Offset
            && Count == other.Count
            && BindingSlot == other.BindingSlot;
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object obj)
    {
        return obj is ParameterKeyInfo parameterKeyInfo && Equals(parameterKeyInfo);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Key, Offset, Count, BindingSlot);
    }

    public static bool operator ==(ParameterKeyInfo left, ParameterKeyInfo right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ParameterKeyInfo left, ParameterKeyInfo right)
    {
        return !left.Equals(right);
    }
}
