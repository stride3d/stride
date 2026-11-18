// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;
using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestSerialization
{
    // Compiled content reads the same when a member changes between a Stride type and its System.Numerics type
    [Fact]
    public void TestSameBytesAsSystemNumerics()
    {
        Assert.Equal(Bytes(new System.Numerics.Vector2(1, 2)), Bytes(new Vector2(1, 2)));
        Assert.Equal(Bytes(new System.Numerics.Vector3(1, 2, 3)), Bytes(new Vector3(1, 2, 3)));
        Assert.Equal(Bytes(new System.Numerics.Vector4(1, 2, 3, 4)), Bytes(new Vector4(1, 2, 3, 4)));
        Assert.Equal(Bytes(new System.Numerics.Quaternion(1, 2, 3, 4)), Bytes(new Quaternion(1, 2, 3, 4)));
        Assert.Equal(Bytes(new System.Numerics.Plane(1, 2, 3, 4)), Bytes(new Plane(1, 2, 3, 4)));
        Assert.Equal(
            Bytes(new System.Numerics.Matrix4x4(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16)),
            Bytes(new Matrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16)));
    }

    private static byte[] Bytes<T>(T value)
    {
        using var memory = new MemoryStream();
        new BinarySerializationWriter(memory).Write(value);
        Assert.NotEmpty(memory.ToArray());
        return memory.ToArray();
    }
}
