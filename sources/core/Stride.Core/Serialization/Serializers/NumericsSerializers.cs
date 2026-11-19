// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Numerics;
using System.Runtime.InteropServices;

namespace Stride.Core.Serialization.Serializers;

/// <summary>
/// Data serializer for a System.Numerics type made of <see cref="float"/> fields only: it writes them in memory order.
/// </summary>
/// <typeparam name="T">The type to serialize.</typeparam>
public abstract class NumericsSerializer<T> : DataSerializer<T> where T : unmanaged
{
    /// <inheritdoc/>
    public override bool IsBlittable => true;

    /// <inheritdoc/>
    public override void Serialize(ref T obj, ArchiveMode mode, SerializationStream stream)
    {
        // One float at a time: a stream may give each value another form than its bytes
        foreach (ref var component in MemoryMarshal.Cast<T, float>(new Span<T>(ref obj)))
            stream.Serialize(ref component);
    }
}

/// <summary>
/// Data serializer for <see cref="Vector2"/>.
/// </summary>
[DataSerializerGlobal(typeof(Vector2Serializer))]
public sealed class Vector2Serializer : NumericsSerializer<Vector2>;

/// <summary>
/// Data serializer for <see cref="Vector3"/>.
/// </summary>
[DataSerializerGlobal(typeof(Vector3Serializer))]
public sealed class Vector3Serializer : NumericsSerializer<Vector3>;

/// <summary>
/// Data serializer for <see cref="Vector4"/>.
/// </summary>
[DataSerializerGlobal(typeof(Vector4Serializer))]
public sealed class Vector4Serializer : NumericsSerializer<Vector4>;

/// <summary>
/// Data serializer for <see cref="Quaternion"/>.
/// </summary>
[DataSerializerGlobal(typeof(QuaternionSerializer))]
public sealed class QuaternionSerializer : NumericsSerializer<Quaternion>;

/// <summary>
/// Data serializer for <see cref="Plane"/>.
/// </summary>
[DataSerializerGlobal(typeof(PlaneSerializer))]
public sealed class PlaneSerializer : NumericsSerializer<Plane>;

/// <summary>
/// Data serializer for <see cref="Matrix4x4"/>.
/// </summary>
[DataSerializerGlobal(typeof(Matrix4x4Serializer))]
public sealed class Matrix4x4Serializer : NumericsSerializer<Matrix4x4>;

/// <summary>
/// Data serializer for <see cref="Half"/>: it writes its 16 bits.
/// </summary>
[DataSerializerGlobal(typeof(HalfSerializer))]
public sealed class HalfSerializer : DataSerializer<Half>
{
    /// <inheritdoc/>
    public override bool IsBlittable => true;

    /// <inheritdoc/>
    public override void Serialize(ref Half obj, ArchiveMode mode, SerializationStream stream)
    {
        var bits = BitConverter.HalfToUInt16Bits(obj);
        stream.Serialize(ref bits);
        obj = BitConverter.UInt16BitsToHalf(bits);
    }
}
