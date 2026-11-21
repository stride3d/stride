// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Numerics;
using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestNormalizeSafe
{
    // Shorter than MathUtil.ZeroTolerance, and a power of two: the scaling is exact
    private const float Tiny = 1f / (1 << 21);

    [Fact]
    public void WithoutAToleranceAVectorShorterThanZeroToleranceIsReturnedAsItIs()
    {
        var tiny = new Vector3(Tiny, 0, 0);

        Assert.Equal(tiny, Vector3.NormalizeSafe(tiny));
        Assert.Equal(Vector3.UnitX, Vector3.NormalizeSafe(new Vector3(0.25f, 0, 0)));
        // Unlike Normalize
        Assert.Equal(Vector3.UnitX, Vector3.Normalize(tiny));
    }

    [Fact]
    public void AValueAsLongAsTheToleranceOrShorterIsReturnedAsItIs()
    {
        Assert.Equal(new Vector2(0.25f, 0), Vector2.NormalizeSafe(new Vector2(0.25f, 0), 0.5f));
        Assert.Equal(new Vector2(0.25f, 0), Vector2.NormalizeSafe(new Vector2(0.25f, 0), 0.25f));
        Assert.Equal(Vector2.UnitX, Vector2.NormalizeSafe(new Vector2(0.25f, 0), 0.125f));

        Assert.Equal(new Vector3(0, 0.25f, 0), Vector3.NormalizeSafe(new Vector3(0, 0.25f, 0), 0.5f));
        Assert.Equal(new Vector3(0, 0.25f, 0), Vector3.NormalizeSafe(new Vector3(0, 0.25f, 0), 0.25f));
        Assert.Equal(Vector3.UnitY, Vector3.NormalizeSafe(new Vector3(0, 0.25f, 0), 0.125f));

        Assert.Equal(new Vector4(0, 0, 0.25f, 0), Vector4.NormalizeSafe(new Vector4(0, 0, 0.25f, 0), 0.5f));
        Assert.Equal(new Vector4(0, 0, 0.25f, 0), Vector4.NormalizeSafe(new Vector4(0, 0, 0.25f, 0), 0.25f));
        Assert.Equal(Vector4.UnitZ, Vector4.NormalizeSafe(new Vector4(0, 0, 0.25f, 0), 0.125f));

        Assert.Equal(new Quaternion(0, 0, 0, 0.25f), Quaternion.NormalizeSafe(new Quaternion(0, 0, 0, 0.25f), 0.5f));
        Assert.Equal(new Quaternion(0, 0, 0, 0.25f), Quaternion.NormalizeSafe(new Quaternion(0, 0, 0, 0.25f), 0.25f));
        Assert.Equal(Quaternion.Identity, Quaternion.NormalizeSafe(new Quaternion(0, 0, 0, 0.25f), 0.125f));
    }

    [Fact]
    public void WithAToleranceOfZeroOnlyZeroIsReturnedAsItIs()
    {
        Assert.Equal(Vector3.UnitX, Vector3.NormalizeSafe(new Vector3(Tiny, 0, 0), 0));
        Assert.Equal(Vector3.Zero, Vector3.NormalizeSafe(Vector3.Zero, 0));
    }
}
