// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Rendering;

/// <summary>
/// The deltas of one morph target at one vertex of a <see cref="MeshMorphData"/>: position, normal and tangent.
/// </summary>
/// <remarks>
/// Packed for the GPU, with the same bytes on the CPU: the vertex index, then the target index and the nine delta
/// components as 16-bit floats. Build entries with <see cref="Create"/> and read them through <see cref="ShapeIndex"/>,
/// <see cref="PositionDelta"/>, <see cref="NormalDelta"/> and <see cref="TangentDelta"/>. The packed words are internal,
/// so the packing can change without breaking user code.
/// </remarks>
[DataContract]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct MeshMorphEntry
{
    /// <summary>Six 32-bit words: the vertex, then the shape and nine binary16 components packed two per word.</summary>
    public const int SizeInBytes = 6 * sizeof(uint);

    // binary16 layout: sign bit, five exponent bits, ten mantissa bits.
    private const int HalfBits = 16;
    private const uint HalfMagnitudeMask = 0x7FFF;
    private const uint HalfExponentMask = 0x7C00;
    private const uint HighHalfMagnitudeMask = HalfMagnitudeMask << HalfBits;
    private const uint PairMagnitudeMask = HalfMagnitudeMask | HighHalfMagnitudeMask;

    /// <summary>The vertex the deltas apply to.</summary>
    [DataMember(0)]
    public uint VertexIndex;

    /// <summary>Packed: the target index (low 16 bits) and the position delta X (high 16 bits).</summary>
    [DataMember(1)]
    internal uint ShapeAndPositionX;

    /// <summary>Packed: the position delta Y and Z.</summary>
    [DataMember(2)]
    internal uint PositionYZ;

    /// <summary>Packed: the normal delta X and Y.</summary>
    [DataMember(3)]
    internal uint NormalXY;

    /// <summary>Packed: the normal delta Z and the tangent delta X.</summary>
    [DataMember(4)]
    internal uint NormalZTangentX;

    /// <summary>Packed: the tangent delta Y and Z.</summary>
    [DataMember(5)]
    internal uint TangentYZ;

    /// <summary>The index of the target in <see cref="MeshMorphData.TargetNames"/>.</summary>
    [DataMemberIgnore] public readonly ushort ShapeIndex => (ushort)ShapeAndPositionX;
    /// <summary>The position delta at full weight.</summary>
    [DataMemberIgnore] public readonly Vector3 PositionDelta => new(Unpack(ShapeAndPositionX >> HalfBits), Unpack(PositionYZ), Unpack(PositionYZ >> HalfBits));
    /// <summary>The normal delta at full weight, added to the base normal before normalizing.</summary>
    [DataMemberIgnore] public readonly Vector3 NormalDelta => new(Unpack(NormalXY), Unpack(NormalXY >> HalfBits), Unpack(NormalZTangentX));
    /// <summary>The tangent delta at full weight, added to the base tangent before normalizing.</summary>
    [DataMemberIgnore] public readonly Vector3 TangentDelta => new(Unpack(NormalZTangentX >> HalfBits), Unpack(TangentYZ), Unpack(TangentYZ >> HalfBits));
    // Bit tests on the packed binary16 components: magnitude bits zero means ±0, an all-ones exponent means NaN/Inf.
    /// <summary>Whether every delta is zero once packed; such entries are left out of <see cref="MeshMorphData"/>.</summary>
    [DataMemberIgnore] public readonly bool IsZero => ((ShapeAndPositionX >> HalfBits & HalfMagnitudeMask) | (PositionYZ & PairMagnitudeMask) | (NormalXY & PairMagnitudeMask) | (NormalZTangentX & PairMagnitudeMask) | (TangentYZ & PairMagnitudeMask)) == 0;
    internal readonly bool HasNormal => ((NormalXY & PairMagnitudeMask) | (NormalZTangentX & HalfMagnitudeMask)) != 0;
    internal readonly bool HasTangent => ((NormalZTangentX & HighHalfMagnitudeMask) | (TangentYZ & PairMagnitudeMask)) != 0;
    internal readonly bool IsFinite => Finite(ShapeAndPositionX >> HalfBits) && Finite(PositionYZ) && Finite(PositionYZ >> HalfBits) && Finite(NormalXY) && Finite(NormalXY >> HalfBits)
        && Finite(NormalZTangentX) && Finite(NormalZTangentX >> HalfBits) && Finite(TangentYZ) && Finite(TangentYZ >> HalfBits);

    private static bool Finite(uint half)
    {
        return (half & HalfExponentMask) != HalfExponentMask;
    }

    /// <summary>Packs the deltas of a target at a vertex, rounding each component to a 16-bit float.</summary>
    /// <param name="vertex">The vertex index.</param>
    /// <param name="shape">The target index.</param>
    /// <param name="position">The position delta.</param>
    /// <param name="normal">The normal delta, or zero.</param>
    /// <param name="tangent">The tangent delta, or zero.</param>
    /// <returns>The packed entry.</returns>
    /// <exception cref="ArgumentException">A component is not finite or is beyond the 16-bit float range (65504).</exception>
    public static MeshMorphEntry Create(uint vertex, ushort shape, Vector3 position, Vector3 normal, Vector3 tangent)
    {
        return new MeshMorphEntry
        {
            VertexIndex = vertex,
            ShapeAndPositionX = shape | (uint)Pack(position.X) << HalfBits,
            PositionYZ = Pair(position.Y, position.Z),
            NormalXY = Pair(normal.X, normal.Y),
            NormalZTangentX = Pair(normal.Z, tangent.X),
            TangentYZ = Pair(tangent.Y, tangent.Z),
        };
    }

    private static uint Pair(float low, float high)
    {
        return Pack(low) | (uint)Pack(high) << HalfBits;
    }

    private static ushort Pack(float value)
    {
        if (!float.IsFinite(value) || Math.Abs(value) > (float)System.Half.MaxValue)
            throw new ArgumentException("Morph delta components must be finite and within binary16 range.");
        return BitConverter.HalfToUInt16Bits((System.Half)value);
    }

    private static float Unpack(uint value)
    {
        return (float)BitConverter.UInt16BitsToHalf((ushort)value);
    }
}
