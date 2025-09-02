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

public interface ISemantic<T> : ISemantic;

public interface V2V2 : IConversion<Vector2, Vector2> { static void IConversion<Vector2, Vector2>.Convert(in Vector2 source, out Vector2 dest) => dest = source; }
public interface V3V2 : IConversion<Vector3, Vector2> { static void IConversion<Vector3, Vector2>.Convert(in Vector3 source, out Vector2 dest) => dest = (Vector2)source; }
public interface V4V2 : IConversion<Vector4, Vector2> { static void IConversion<Vector4, Vector2>.Convert(in Vector4 source, out Vector2 dest) => dest = (Vector2)source; }
public interface H2V2 : IConversion<Half2, Vector2> { static void IConversion<Half2, Vector2>.Convert(in Half2 source, out Vector2 dest) => dest = (Vector2)source; }
public interface H3V2 : IConversion<Half3, Vector2> { static void IConversion<Half3, Vector2>.Convert(in Half3 source, out Vector2 dest) => dest = new(source.X, source.Y); }
public interface H4V2 : IConversion<Half4, Vector2> { static void IConversion<Half4, Vector2>.Convert(in Half4 source, out Vector2 dest) => dest = new(source.X, source.Y); }
public interface U4V2 : IConversion<UShort4, Vector2> { static void IConversion<UShort4, Vector2>.Convert(in UShort4 source, out Vector2 dest) => dest = new(source.X, source.Y); }
public interface B4V2 : IConversion<Byte4, Vector2> { static void IConversion<Byte4, Vector2>.Convert(in Byte4 source, out Vector2 dest) => dest = new(source.X, source.Y); }

public interface V2V3 : IConversion<Vector2, Vector3> { static void IConversion<Vector2, Vector3>.Convert(in Vector2 source, out Vector3 dest) => dest = (Vector3)source; }
public interface V3V3 : IConversion<Vector3, Vector3> { static void IConversion<Vector3, Vector3>.Convert(in Vector3 source, out Vector3 dest) => dest = source; }
public interface V4V3 : IConversion<Vector4, Vector3> { static void IConversion<Vector4, Vector3>.Convert(in Vector4 source, out Vector3 dest) => dest = (Vector3)source; }
public interface H2V3 : IConversion<Half2, Vector3> { static void IConversion<Half2, Vector3>.Convert(in Half2 source, out Vector3 dest) => dest = new(source.X, source.Y, 0f); }
public interface H3V3 : IConversion<Half3, Vector3> { static void IConversion<Half3, Vector3>.Convert(in Half3 source, out Vector3 dest) => dest = new(source.X, source.Y, source.Z); }
public interface H4V3 : IConversion<Half4, Vector3> { static void IConversion<Half4, Vector3>.Convert(in Half4 source, out Vector3 dest) => dest = new(source.X, source.Y, source.Z); }
public interface U4V3 : IConversion<UShort4, Vector3> { static void IConversion<UShort4, Vector3>.Convert(in UShort4 source, out Vector3 dest) => dest = new(source.X, source.Y, source.Z); }
public interface B4V3 : IConversion<Byte4, Vector3> { static void IConversion<Byte4, Vector3>.Convert(in Byte4 source, out Vector3 dest) => dest = new(source.X, source.Y, source.Z); }

public interface V2V4 : IConversion<Vector2, Vector4> { static void IConversion<Vector2, Vector4>.Convert(in Vector2 source, out Vector4 dest) => dest = (Vector4)source; }
public interface V3V4 : IConversion<Vector3, Vector4> { static void IConversion<Vector3, Vector4>.Convert(in Vector3 source, out Vector4 dest) => dest = (Vector4)source; }
public interface V4V4 : IConversion<Vector4, Vector4> { static void IConversion<Vector4, Vector4>.Convert(in Vector4 source, out Vector4 dest) => dest = source; }
public interface H2V4 : IConversion<Half2, Vector4> { static void IConversion<Half2, Vector4>.Convert(in Half2 source, out Vector4 dest) => dest = new(source.X, source.Y, 0f, 0f); }
public interface H3V4 : IConversion<Half3, Vector4> { static void IConversion<Half3, Vector4>.Convert(in Half3 source, out Vector4 dest) => dest = new(source.X, source.Y, source.Z, 0f); }
public interface H4V4 : IConversion<Half4, Vector4> { static void IConversion<Half4, Vector4>.Convert(in Half4 source, out Vector4 dest) => dest = (Vector4)source; }
public interface U4V4 : IConversion<UShort4, Vector4> { static void IConversion<UShort4, Vector4>.Convert(in UShort4 source, out Vector4 dest) => dest = new(source.X, source.Y, source.Z, source.W); }
public interface B4V4 : IConversion<Byte4, Vector4> { static void IConversion<Byte4, Vector4>.Convert(in Byte4 source, out Vector4 dest) => dest = new(source.X, source.Y, source.Z, source.W); }

public interface V2H2 : IConversion<Vector2, Half2> { static void IConversion<Vector2, Half2>.Convert(in Vector2 source, out Half2 dest) => dest = (Half2)source; }
public interface V3H2 : IConversion<Vector3, Half2> { static void IConversion<Vector3, Half2>.Convert(in Vector3 source, out Half2 dest) => dest = new(source.X, source.Y); }
public interface V4H2 : IConversion<Vector4, Half2> { static void IConversion<Vector4, Half2>.Convert(in Vector4 source, out Half2 dest) => dest = new(source.X, source.Y); }
public interface H2H2 : IConversion<Half2, Half2> { static void IConversion<Half2, Half2>.Convert(in Half2 source, out Half2 dest) => dest = source; }
public interface H3H2 : IConversion<Half3, Half2> { static void IConversion<Half3, Half2>.Convert(in Half3 source, out Half2 dest) => dest = new(source.X, source.Y); }
public interface H4H2 : IConversion<Half4, Half2> { static void IConversion<Half4, Half2>.Convert(in Half4 source, out Half2 dest) => dest = new(source.X, source.Y); }
public interface U4H2 : IConversion<UShort4, Half2> { static void IConversion<UShort4, Half2>.Convert(in UShort4 source, out Half2 dest) => dest = new(source.X, source.Y); }
public interface B4H2 : IConversion<Byte4, Half2> { static void IConversion<Byte4, Half2>.Convert(in Byte4 source, out Half2 dest) => dest = new(source.X, source.Y); }

public interface V2H3 : IConversion<Vector2, Half3> { static void IConversion<Vector2, Half3>.Convert(in Vector2 source, out Half3 dest) => dest = new(source.X, source.Y, 0f); }
public interface V3H3 : IConversion<Vector3, Half3> { static void IConversion<Vector3, Half3>.Convert(in Vector3 source, out Half3 dest) => dest = (Half3)source; }
public interface V4H3 : IConversion<Vector4, Half3> { static void IConversion<Vector4, Half3>.Convert(in Vector4 source, out Half3 dest) => dest = new(source.X, source.Y, source.Z); }
public interface H2H3 : IConversion<Half2, Half3> { static void IConversion<Half2, Half3>.Convert(in Half2 source, out Half3 dest) => dest = new(source.X, source.Y, 0f); }
public interface H3H3 : IConversion<Half3, Half3> { static void IConversion<Half3, Half3>.Convert(in Half3 source, out Half3 dest) => dest = source; }
public interface H4H3 : IConversion<Half4, Half3> { static void IConversion<Half4, Half3>.Convert(in Half4 source, out Half3 dest) => dest = new(source.X, source.Y, source.Z); }
public interface U4H3 : IConversion<UShort4, Half3> { static void IConversion<UShort4, Half3>.Convert(in UShort4 source, out Half3 dest) => dest = new(source.X, source.Y, source.Z); }
public interface B4H3 : IConversion<Byte4, Half3> { static void IConversion<Byte4, Half3>.Convert(in Byte4 source, out Half3 dest) => dest = new(source.X, source.Y, source.Z); }

public interface V2H4 : IConversion<Vector2, Half4> { static void IConversion<Vector2, Half4>.Convert(in Vector2 source, out Half4 dest) => dest = new(source.X, source.Y, 0f, 0f); }
public interface V3H4 : IConversion<Vector3, Half4> { static void IConversion<Vector3, Half4>.Convert(in Vector3 source, out Half4 dest) => dest = new(source.X, source.Y, source.Z, 0f); }
public interface V4H4 : IConversion<Vector4, Half4> { static void IConversion<Vector4, Half4>.Convert(in Vector4 source, out Half4 dest) => dest = (Half4)source; }
public interface H2H4 : IConversion<Half2, Half4> { static void IConversion<Half2, Half4>.Convert(in Half2 source, out Half4 dest) => dest = new(source.X, source.Y, 0f, 0f); }
public interface H3H4 : IConversion<Half3, Half4> { static void IConversion<Half3, Half4>.Convert(in Half3 source, out Half4 dest) => dest = new(source.X, source.Y, source.Z, 0f); }
public interface H4H4 : IConversion<Half4, Half4> { static void IConversion<Half4, Half4>.Convert(in Half4 source, out Half4 dest) => dest = source; }
public interface U4H4 : IConversion<UShort4, Half4> { static void IConversion<UShort4, Half4>.Convert(in UShort4 source, out Half4 dest) => dest = new(source.X, source.Y, source.Z, source.W); }
public interface B4H4 : IConversion<Byte4, Half4> { static void IConversion<Byte4, Half4>.Convert(in Byte4 source, out Half4 dest) => dest = new(source.X, source.Y, source.Z, source.W); }

public interface V2U4 : IConversion<Vector2, UShort4> { static void IConversion<Vector2, UShort4>.Convert(in Vector2 source, out UShort4 dest) => dest = new((ushort)source.X, (ushort)source.Y, 0, 0); }
public interface V3U4 : IConversion<Vector3, UShort4> { static void IConversion<Vector3, UShort4>.Convert(in Vector3 source, out UShort4 dest) => dest = new((ushort)source.X, (ushort)source.Y, (ushort)source.Z, 0); }
public interface V4U4 : IConversion<Vector4, UShort4> { static void IConversion<Vector4, UShort4>.Convert(in Vector4 source, out UShort4 dest) => dest = new((ushort)source.X, (ushort)source.Y, (ushort)source.Z, (ushort)source.W); }
public interface H2U4 : IConversion<Half2, UShort4> { static void IConversion<Half2, UShort4>.Convert(in Half2 source, out UShort4 dest) => dest = new((ushort)source.X, (ushort)source.Y, 0, 0); }
public interface H3U4 : IConversion<Half3, UShort4> { static void IConversion<Half3, UShort4>.Convert(in Half3 source, out UShort4 dest) => dest = new((ushort)source.X, (ushort)source.Y, (ushort)source.Z, 0); }
public interface H4U4 : IConversion<Half4, UShort4> { static void IConversion<Half4, UShort4>.Convert(in Half4 source, out UShort4 dest) => dest = new((ushort)source.X, (ushort)source.Y, (ushort)source.Z, (ushort)source.W); }
public interface U4U4 : IConversion<UShort4, UShort4> { static void IConversion<UShort4, UShort4>.Convert(in UShort4 source, out UShort4 dest) => dest = source; }
public interface B4U4 : IConversion<Byte4, UShort4> { static void IConversion<Byte4, UShort4>.Convert(in Byte4 source, out UShort4 dest) => dest = new(source.X, source.Y, source.Z, source.W); }

public interface V2B4 : IConversion<Vector2, Byte4> { static void IConversion<Vector2, Byte4>.Convert(in Vector2 source, out Byte4 dest) => dest = new((byte)source.X, (byte)source.Y, 0, 0); }
public interface V3B4 : IConversion<Vector3, Byte4> { static void IConversion<Vector3, Byte4>.Convert(in Vector3 source, out Byte4 dest) => dest = new((byte)source.X, (byte)source.Y, (byte)source.Z, 0); }
public interface V4B4 : IConversion<Vector4, Byte4> { static void IConversion<Vector4, Byte4>.Convert(in Vector4 source, out Byte4 dest) => dest = new((byte)source.X, (byte)source.Y, (byte)source.Z, (byte)source.W); }
public interface H2B4 : IConversion<Half2, Byte4> { static void IConversion<Half2, Byte4>.Convert(in Half2 source, out Byte4 dest) => dest = new((byte)source.X, (byte)source.Y, 0, 0); }
public interface H3B4 : IConversion<Half3, Byte4> { static void IConversion<Half3, Byte4>.Convert(in Half3 source, out Byte4 dest) => dest = new((byte)source.X, (byte)source.Y, (byte)source.Z, 0); }
public interface H4B4 : IConversion<Half4, Byte4> { static void IConversion<Half4, Byte4>.Convert(in Half4 source, out Byte4 dest) => dest = new((byte)source.X, (byte)source.Y, (byte)source.Z, (byte)source.W); }
public interface U4B4 : IConversion<UShort4, Byte4> { static void IConversion<UShort4, Byte4>.Convert(in UShort4 source, out Byte4 dest) => dest = new((byte)source.X, (byte)source.Y, (byte)source.Z, (byte)source.W); }
public interface B4B4 : IConversion<Byte4, Byte4> { static void IConversion<Byte4, Byte4>.Convert(in Byte4 source, out Byte4 dest) => dest = source; }

public interface IFloat2Semantic : ISemantic<Vector2>,
    V2V2,
    V2V3, V3V2,
    V2V4, V4V2,
    V2B4, B4V2,
    V2U4, U4V2,
    V2H2, H2V2,
    V2H3,
    V2H4, H4V2;

public interface IFloat3Semantic : ISemantic<Vector3>,
    V3V2, V2V3,
    V3V3,
    V3V4, V4V3,
    V3B4, B4V3,
    V3U4, U4V3,
    V3H2, H2V3,
    V3H3,
    V3H4, H4V3;

public interface IFloat4Semantic : ISemantic<Vector4>,
    V4V2, V2V4,
    V4V3, V3V4,
    V4V4,
    V4B4, B4V4,
    V4U4, U4V4,
    V4H2, H2V4,
    V4H3,
    V4H4, H4V4;

public interface IHalf2Semantic : ISemantic<Half2>,
    H2V2, V2H2,
    H2V3, V3H2,
    H2V4, V4H2,
    H2B4, B4H2,
    H2U4, U4H2,
    H2H2,
    H2H3,
    H2H4, H4H2;

public interface IHalf3Semantic : ISemantic<Half3>,
    H3V2, V2H3,
    H3V3, V3H3,
    H3V4, V4H3,
    H3B4, B4H3,
    H3U4, U4H3,
    H3H2, H2H3,
    H3H3,
    H3H4;

public interface IHalf4Semantic : ISemantic<Half4>,
    H4V2, V2H4,
    H4V3, V3H4,
    H4V4, V4H4,
    H4B4, B4H4,
    H4U4, U4H4,
    H4H2, H2H4,
    H4H3,
    H4H4;

public interface IUShort4Semantic : ISemantic<UShort4>,
    U4V2, V2U4,
    U4V3, V3U4,
    U4V4, V4U4,
    U4B4, B4U4,
    U4U4,
    U4H2, H2U4,
    U4H3,
    U4H4, H4U4;

public interface IByte4Semantic : ISemantic<Byte4>,
    B4V2, V2B4,
    B4V3, V3B4,
    B4V4, V4B4,
    B4B4,
    B4U4, U4B4,
    B4H2, H2B4,
    B4H3,
    B4H4, H4B4;

public struct UShort4(ushort x, ushort y, ushort z, ushort w)
{
    public ushort X = x, Y = y, Z = z, W = w;
}

public struct Byte4(byte x, byte y, byte z, byte w)
{
    public byte X = x, Y = y, Z = z, W = w;
}
