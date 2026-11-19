// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Numerics;
using System.Runtime.InteropServices;
using Xunit;
using Stride.Core.Serialization;

namespace Stride.Core.Tests.Serialization;

public class NumericsSerializersTests
{
    [Fact]
    public void SystemNumericsTypesAreSerializedAsTheirFloatsInMemoryOrder()
    {
        AssertFloats(new Vector2(1, 2), 1, 2);
        AssertFloats(new Vector3(1, 2, 3), 1, 2, 3);
        AssertFloats(new Vector4(1, 2, 3, 4), 1, 2, 3, 4);
        AssertFloats(new Quaternion(1, 2, 3, 4), 1, 2, 3, 4);
        AssertFloats(new Plane(1, 2, 3, 4), 1, 2, 3, 4);
        AssertFloats(
            new Matrix4x4(
                1, 2, 3, 4,
                5, 6, 7, 8,
                9, 10, 11, 12,
                13, 14, 15, 16),
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
    }

    [Fact]
    public void HalfIsSerializedAsItsBits()
    {
        using var memory = new MemoryStream();
        new BinarySerializationWriter(memory).Write((Half)1.5f);
        Assert.Equal(new byte[] { 0x00, 0x3E }, memory.ToArray());

        memory.Position = 0;
        Assert.Equal((Half)1.5f, new BinarySerializationReader(memory).Read<Half>());
    }

    // Through the serializer that the selector finds for the type
    private static void AssertFloats<T>(T value, params float[] expected) where T : unmanaged
    {
        using var memory = new MemoryStream();
        new BinarySerializationWriter(memory).Write(value);
        Assert.Equal(expected, MemoryMarshal.Cast<byte, float>(memory.ToArray()).ToArray());

        memory.Position = 0;
        Assert.Equal(value, new BinarySerializationReader(memory).Read<T>());
    }
}
