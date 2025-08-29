// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable
namespace Stride.Graphics.Semantic;
using System;
using Core.Mathematics;

public interface ISemantic
{
    public static abstract string Name { get; }
}


public interface ISemantic<T> : 
    ISemantic,
    // Default source types we support for conversion
    IConversion<Vector2, T>,
    IConversion<Vector3, T>,
    IConversion<Vector4, T>,
    IConversion<Half2, T>,
    IConversion<Half4, T>,
    IConversion<UShort4, T>,
    IConversion<Byte4, T>;

public interface IFloat2Semantic : ISemantic<Vector2>
{
    static void IConversion<Vector2, Vector2>.Convert(in Vector2 source, out Vector2 dest) => dest = source;
    static void IConversion<Vector3, Vector2>.Convert(in Vector3 source, out Vector2 dest) => dest = (Vector2)source;
    static void IConversion<Vector4, Vector2>.Convert(in Vector4 source, out Vector2 dest) => dest = (Vector2)source;
    static void IConversion<Half2, Vector2>.Convert(in Half2 source, out Vector2 dest) => dest = (Vector2)source;
    static void IConversion<Half4, Vector2>.Convert(in Half4 source, out Vector2 dest) => dest = new(source.X, source.Y);
    static void IConversion<UShort4, Vector2>.Convert(in UShort4 source, out Vector2 dest) => dest = new(source.X, source.Y);
    static void IConversion<Byte4, Vector2>.Convert(in Byte4 source, out Vector2 dest) => dest = new(source.X, source.Y);
}

public interface IFloat3Semantic : ISemantic<Vector3>
{
    static void IConversion<Vector2, Vector3>.Convert(in Vector2 source, out Vector3 dest) => dest = (Vector3)source;
    static void IConversion<Vector3, Vector3>.Convert(in Vector3 source, out Vector3 dest) => dest = source;
    static void IConversion<Vector4, Vector3>.Convert(in Vector4 source, out Vector3 dest) => dest = (Vector3)source;
    static void IConversion<Half2, Vector3>.Convert(in Half2 source, out Vector3 dest) => dest = new(source.X, source.Y, 0f);
    static void IConversion<Half4, Vector3>.Convert(in Half4 source, out Vector3 dest) => dest = new(source.X, source.Y, source.Z);
    static void IConversion<UShort4, Vector3>.Convert(in UShort4 source, out Vector3 dest) => dest = new(source.X, source.Y, source.Z);
    static void IConversion<Byte4, Vector3>.Convert(in Byte4 source, out Vector3 dest) => dest = new(source.X, source.Y, source.Z);
}

public interface IFloat4Semantic : ISemantic<Vector4>
{
    static void IConversion<Vector2, Vector4>.Convert(in Vector2 source, out Vector4 dest) => dest = (Vector4)source;
    static void IConversion<Vector3, Vector4>.Convert(in Vector3 source, out Vector4 dest) => dest = (Vector4)source;
    static void IConversion<Vector4, Vector4>.Convert(in Vector4 source, out Vector4 dest) => dest = source;
    static void IConversion<Half2, Vector4>.Convert(in Half2 source, out Vector4 dest) => dest = new(source.X, source.Y, 0f, 0f);
    static void IConversion<Half4, Vector4>.Convert(in Half4 source, out Vector4 dest) => dest = (Vector4)source;
    static void IConversion<UShort4, Vector4>.Convert(in UShort4 source, out Vector4 dest) => dest = new(source.X, source.Y, source.Z, source.W);
    static void IConversion<Byte4, Vector4>.Convert(in Byte4 source, out Vector4 dest) => dest = new(source.X, source.Y, source.Z, source.W);
}

public interface IHalf2Semantic : ISemantic<Half2>
{
    static void IConversion<Vector2, Half2>.Convert(in Vector2 source, out Half2 dest) => dest = (Half2)source;
    static void IConversion<Vector3, Half2>.Convert(in Vector3 source, out Half2 dest) => dest = new(source.X, source.Y);
    static void IConversion<Vector4, Half2>.Convert(in Vector4 source, out Half2 dest) => dest = new(source.X, source.Y);
    static void IConversion<Half2, Half2>.Convert(in Half2 source, out Half2 dest) => dest = source;
    static void IConversion<Half4, Half2>.Convert(in Half4 source, out Half2 dest) => dest = new(source.X, source.Y);
    static void IConversion<UShort4, Half2>.Convert(in UShort4 source, out Half2 dest) => dest = new(source.X, source.Y);
    static void IConversion<Byte4, Half2>.Convert(in Byte4 source, out Half2 dest) => dest = new(source.X, source.Y);
}

public interface IHalf3Semantic : ISemantic<Half3>
{
    static void IConversion<Vector2, Half3>.Convert(in Vector2 source, out Half3 dest) => dest = new(source.X, source.Y, 0f);
    static void IConversion<Vector3, Half3>.Convert(in Vector3 source, out Half3 dest) => dest = (Half3)source;
    static void IConversion<Vector4, Half3>.Convert(in Vector4 source, out Half3 dest) => dest = new(source.X, source.Y, source.Z);
    static void IConversion<Half2, Half3>.Convert(in Half2 source, out Half3 dest) => dest = new(source.X, source.Y, 0f);
    static void IConversion<Half4, Half3>.Convert(in Half4 source, out Half3 dest) => dest = new(source.X, source.Y, source.Z);
    static void IConversion<UShort4, Half3>.Convert(in UShort4 source, out Half3 dest) => dest = new(source.X, source.Y, source.Z);
    static void IConversion<Byte4, Half3>.Convert(in Byte4 source, out Half3 dest) => dest = new(source.X, source.Y, source.Z);
}

public interface IHalf4Semantic : ISemantic<Half4>
{
    static void IConversion<Vector2, Half4>.Convert(in Vector2 source, out Half4 dest) => dest = new(source.X, source.Y, 0f, 0f);
    static void IConversion<Vector3, Half4>.Convert(in Vector3 source, out Half4 dest) => dest = new(source.X, source.Y, source.Z, 0f);
    static void IConversion<Vector4, Half4>.Convert(in Vector4 source, out Half4 dest) => dest = (Half4)source;
    static void IConversion<Half2, Half4>.Convert(in Half2 source, out Half4 dest) => dest = new(source.X, source.Y, 0f, 0f);
    static void IConversion<Half4, Half4>.Convert(in Half4 source, out Half4 dest) => dest = source;
    static void IConversion<UShort4, Half4>.Convert(in UShort4 source, out Half4 dest) => dest = new(source.X, source.Y, source.Z, source.W);
    static void IConversion<Byte4, Half4>.Convert(in Byte4 source, out Half4 dest) => dest = new(source.X, source.Y, source.Z, source.W);
}

public interface IUShort4Semantic : ISemantic<UShort4>
{
    static void IConversion<Vector2, UShort4>.Convert(in Vector2 source, out UShort4 dest) => dest = new((ushort)source.X, (ushort)source.Y, 0, 0);
    static void IConversion<Vector3, UShort4>.Convert(in Vector3 source, out UShort4 dest) => dest = new((ushort)source.X, (ushort)source.Y, (ushort)source.Z, 0);
    static void IConversion<Vector4, UShort4>.Convert(in Vector4 source, out UShort4 dest) => dest = new((ushort)source.X, (ushort)source.Y, (ushort)source.Z, (ushort)source.W);
    static void IConversion<Half2, UShort4>.Convert(in Half2 source, out UShort4 dest) => dest = new((ushort)source.X, (ushort)source.Y, 0, 0);
    static void IConversion<Half4, UShort4>.Convert(in Half4 source, out UShort4 dest) => dest = new((ushort)source.X, (ushort)source.Y, (ushort)source.Z, (ushort)source.W);
    static void IConversion<UShort4, UShort4>.Convert(in UShort4 source, out UShort4 dest) => dest = source;
    static void IConversion<Byte4, UShort4>.Convert(in Byte4 source, out UShort4 dest) => dest = new(source.X, source.Y, source.Z, source.W);
}

public interface IByte4Semantic : ISemantic<Byte4>
{
    static void IConversion<Vector2, Byte4>.Convert(in Vector2 source, out Byte4 dest) => dest = new((byte)source.X, (byte)source.Y, 0, 0);
    static void IConversion<Vector3, Byte4>.Convert(in Vector3 source, out Byte4 dest) => dest = new((byte)source.X, (byte)source.Y, (byte)source.Z, 0);
    static void IConversion<Vector4, Byte4>.Convert(in Vector4 source, out Byte4 dest) => dest = new((byte)source.X, (byte)source.Y, (byte)source.Z, (byte)source.W);
    static void IConversion<Half2, Byte4>.Convert(in Half2 source, out Byte4 dest) => dest = new((byte)source.X, (byte)source.Y, 0, 0);
    static void IConversion<Half4, Byte4>.Convert(in Half4 source, out Byte4 dest) => dest = new((byte)source.X, (byte)source.Y, (byte)source.Z, (byte)source.W);
    static void IConversion<UShort4, Byte4>.Convert(in UShort4 source, out Byte4 dest) => dest = new((byte)source.X, (byte)source.Y, (byte)source.Z, (byte)source.W);
    static void IConversion<Byte4, Byte4>.Convert(in Byte4 source, out Byte4 dest) => dest = source;
}

public struct UShort4(ushort x, ushort y, ushort z, ushort w)
{
    public ushort X = x, Y = y, Z = z, W = w;
}

public struct Byte4(byte x, byte y, byte z, byte w)
{
    public byte X = x, Y = y, Z = z, W = w;
}
