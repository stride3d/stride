// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Numerics;
using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestNearEquals
{
    [Fact]
    public void WithoutAToleranceTheDifferenceMustBeBelowZeroTolerance()
    {
        var value = new Vector3(1, 2, 3);

        Assert.True(value.NearEquals(new Vector3(1, 2, 3 + 5e-7f)));
        Assert.False(value.NearEquals(new Vector3(1, 2, 3 + 1e-5f)));
        // Unlike ==
        Assert.False(value == new Vector3(1, 2, 3 + 5e-7f));
    }

    [Fact]
    public void TheToleranceAppliesToEveryComponent()
    {
        Assert.True(new Vector2(1, 2).NearEquals(new Vector2(1.05f, 1.95f), 0.1f));
        Assert.False(new Vector2(1, 2).NearEquals(new Vector2(1, 2.2f), 0.1f));

        Assert.True(new Vector3(1, 2, 3).NearEquals(new Vector3(1, 2, 3.05f), 0.1f));
        Assert.False(new Vector3(1, 2, 3).NearEquals(new Vector3(1, 2, 3.2f), 0.1f));

        Assert.True(new Vector4(1, 2, 3, 4).NearEquals(new Vector4(1, 2, 3, 4.05f), 0.1f));
        Assert.False(new Vector4(1, 2, 3, 4).NearEquals(new Vector4(1, 2, 3, 4.2f), 0.1f));

        Assert.True(Quaternion.Identity.NearEquals(new Quaternion(0, 0, 0.05f, 1), 0.1f));
        Assert.False(Quaternion.Identity.NearEquals(new Quaternion(0, 0, 0.2f, 1), 0.1f));

        var matrix = Matrix4x4.Identity;
        matrix.M43 = 0.05f;
        Assert.True(Matrix4x4.Identity.NearEquals(matrix, 0.1f));
        matrix.M43 = 0.2f;
        Assert.False(Matrix4x4.Identity.NearEquals(matrix, 0.1f));

        Assert.True(new Plane(0, 1, 0, 2).NearEquals(new Plane(0, 1, 0, 2.05f), 0.1f));
        Assert.False(new Plane(0, 1, 0, 2).NearEquals(new Plane(0, 1, 0, 2.2f), 0.1f));
    }

    [Fact]
    public void DoubleVectorsCompareExactlyAndHaveNearEquals()
    {
        Assert.False(new Double2(1, 2) == new Double2(1, 2 + 1e-9));
        Assert.True(new Double2(1, 2).NearEquals(new Double2(1, 2 + 1e-9)));
        Assert.False(new Double2(1, 2).NearEquals(new Double2(1, 2 + 1e-9), 1e-12));

        Assert.True(new Double3(1, 2, 3) != new Double3(1, 2, 3 + 1e-9));
        Assert.True(new Double3(1, 2, 3).NearEquals(new Double3(1, 2, 3 + 1e-9)));
        Assert.False(new Double3(1, 2, 3).NearEquals(new Double3(1, 2, 3.2), 0.1));

        Assert.False(new Double4(1, 2, 3, 4).Equals(new Double4(1, 2, 3, 4 + 1e-9)));
        Assert.True(new Double4(1, 2, 3, 4).NearEquals(new Double4(1, 2, 3, 4 + 1e-9)));
        Assert.False(new Double4(1, 2, 3, 4).NearEquals(new Double4(1, 2, 3, 4.2), 0.1));
    }
}
