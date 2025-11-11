// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestVectorExtensions
{
    [Fact]
    public void TestVector2YX()
    {
        var vector = new Vector2(1.0f, 2.0f);
        var result = vector.YX();

        Assert.Equal(2.0f, result.X);
        Assert.Equal(1.0f, result.Y);
    }

    [Fact]
    public void TestVector3XY()
    {
        var vector = new Vector3(1.0f, 2.0f, 3.0f);
        var result = vector.XY();

        Assert.Equal(1.0f, result.X);
        Assert.Equal(2.0f, result.Y);
    }

    [Fact]
    public void TestVector3XZ()
    {
        var vector = new Vector3(1.0f, 2.0f, 3.0f);
        var result = vector.XZ();

        Assert.Equal(1.0f, result.X);
        Assert.Equal(3.0f, result.Y);
    }

    [Fact]
    public void TestVector3YZ()
    {
        var vector = new Vector3(1.0f, 2.0f, 3.0f);
        var result = vector.YZ();

        Assert.Equal(2.0f, result.X);
        Assert.Equal(3.0f, result.Y);
    }

    [Fact]
    public void TestVector4XY()
    {
        var vector = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        var result = vector.XY();

        Assert.Equal(1.0f, result.X);
        Assert.Equal(2.0f, result.Y);
    }

    [Fact]
    public void TestVector4XYZ()
    {
        var vector = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        var result = vector.XYZ();

        Assert.Equal(1.0f, result.X);
        Assert.Equal(2.0f, result.Y);
        Assert.Equal(3.0f, result.Z);
    }
}
