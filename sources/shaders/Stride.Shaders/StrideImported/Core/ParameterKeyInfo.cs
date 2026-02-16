// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;

namespace Stride.Rendering;

[DataContract]
public record struct ParameterKeyInfo(ParameterKey Key, int Offset, int Count, int BindingSlot) : IEquatable<ParameterKeyInfo>
{
    public const int Invalid = -1;
    #region Convenience properties

    public readonly bool IsValueParameter => Offset != Invalid;

    public readonly bool IsResourceParameter => BindingSlot != Invalid;

    #endregion


    public ParameterKeyInfo(ParameterKey key, int offset, int count) : this(key, offset, count, Invalid)
    {
    }

    public ParameterKeyInfo(ParameterKey key, int bindingSlot) : this(key, Invalid, 1, bindingSlot)
    {
        Offset = Invalid;
        Count = 1;
    }


    // internal readonly ParameterAccessor GetObjectAccessor()
    // {
    //     return new ParameterAccessor(BindingSlot, Count);
    // }

    // internal readonly ParameterAccessor GetValueAccessor()
    // {
    //     return new ParameterAccessor(Offset, Count);
    // }


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
}