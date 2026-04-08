// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;

namespace Stride.Rendering;

[DataContract]
public struct ParameterKeyInfo : IEquatable<ParameterKeyInfo>
{
    public const int Invalid = -1;


    // Common to both value and resource parameters

    public ParameterKey Key;

    // For Value parameters

    public int Offset;
    public int Count;

    // For Resources (Object) parameters

    public int BindingSlot;

    #region Convenience properties

    public readonly bool IsValueParameter => Offset != Invalid;

    public readonly bool IsResourceParameter => BindingSlot != Invalid;

    #endregion


    public ParameterKeyInfo(ParameterKey key, int offset, int count)
    {
        Key = key;
        Offset = offset;
        Count = count;
        BindingSlot = Invalid;
    }

    public ParameterKeyInfo(ParameterKey key, int bindingSlot)
    {
        Key = key;
        BindingSlot = bindingSlot;
        Offset = Invalid;
        Count = 1;
    }


    internal readonly ParameterAccessor GetObjectAccessor()
    {
        return new ParameterAccessor(BindingSlot, Count);
    }

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
