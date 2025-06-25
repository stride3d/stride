// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

public struct InputElementDescription : IEquatable<InputElementDescription>
{
    public string SemanticName;

    public int SemanticIndex;

    public PixelFormat Format;

    public int InputSlot;

    public int AlignedByteOffset;

    public InputClassification InputSlotClass;

    public int InstanceDataStepRate;


    public readonly bool Equals(InputElementDescription other)
    {
        return string.Equals(SemanticName, other.SemanticName)
               && SemanticIndex == other.SemanticIndex
               && Format == other.Format
               && InputSlot == other.InputSlot
               && AlignedByteOffset == other.AlignedByteOffset
               && InputSlotClass == other.InputSlotClass
               && InstanceDataStepRate == other.InstanceDataStepRate;
    }

    public override readonly bool Equals(object obj)
    {
        return obj is InputElementDescription iedesc && Equals(iedesc);
    }

    public static bool operator ==(InputElementDescription left, InputElementDescription right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(InputElementDescription left, InputElementDescription right)
    {
        return !left.Equals(right);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(SemanticName, SemanticIndex, Format, InputSlot, AlignedByteOffset, InputSlotClass, InstanceDataStepRate);
    }
}
