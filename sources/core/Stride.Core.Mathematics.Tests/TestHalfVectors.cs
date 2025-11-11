// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestHalfVectors
{
    #region Half Tests

    [Fact]
    public void TestHalfConstruction()
    {
        var h1 = new Half(1.5f);
        var h2 = (Half)2.5f;

        // Convert back to float for comparison
        float f1 = (float)h1;
        float f2 = (float)h2;

        Assert.Equal(1.5f, f1, 2);
        Assert.Equal(2.5f, f2, 2);
    }

    [Fact]
    public void TestHalfStaticFields()
    {
        Assert.Equal(0.0f, (float)Half.Zero, 2);
        Assert.Equal(1.0f, (float)Half.One, 2);
    }

    [Fact]
    public void TestHalfConversion()
    {
        // Test float to half and back
        float original = 3.14159f;
        Half h = (Half)original;
        float result = (float)h;

        // Half precision loses some precision, so use tolerance
        Assert.Equal(original, result, 2);
    }

    [Fact]
    public void TestHalfArrayConversion()
    {
        float[] floats = [1.0f, 2.0f, 3.0f, 4.0f];
        Half[] halves = Half.ConvertToHalf(floats);
        float[] backToFloats = Half.ConvertToFloat(halves);

        Assert.Equal(floats.Length, halves.Length);
        Assert.Equal(floats.Length, backToFloats.Length);

        for (int i = 0; i < floats.Length; i++)
        {
            Assert.Equal(floats[i], backToFloats[i], 2);
        }
    }

    [Fact]
    public void TestHalfRawValue()
    {
        var h = new Half(1.0f);
        ushort raw = h.RawValue;

        var h2 = new Half { RawValue = raw };
        Assert.Equal((float)h, (float)h2, 2);
    }

    [Fact]
    public void TestHalfToString()
    {
        var h = new Half(1.5f);
        string str = h.ToString();
        Assert.NotNull(str);
        Assert.NotEmpty(str);
    }

    #endregion

    #region Half2 Tests

    [Fact]
    public void TestHalf2Construction()
    {
        var h1 = new Half2(1.0f, 2.0f);
        Assert.Equal(1.0f, (float)h1.X, 2);
        Assert.Equal(2.0f, (float)h1.Y, 2);

        var h2 = new Half2(3.0f);
        Assert.Equal(3.0f, (float)h2.X, 2);
        Assert.Equal(3.0f, (float)h2.Y, 2);

        var h3 = new Half2((Half)1.5f, (Half)2.5f);
        Assert.Equal(1.5f, (float)h3.X, 2);
        Assert.Equal(2.5f, (float)h3.Y, 2);

        var h4 = new Half2((Half)4.0f);
        Assert.Equal(4.0f, (float)h4.X, 2);
        Assert.Equal(4.0f, (float)h4.Y, 2);

        var h5 = new Half2([new Half(5.0f), new Half(6.0f)]);
        Assert.Equal(5.0f, (float)h5.X, 2);
        Assert.Equal(6.0f, (float)h5.Y, 2);
    }

    [Fact]
    public void TestHalf2StaticFields()
    {
        Assert.Equal(0.0f, (float)Half2.Zero.X, 2);
        Assert.Equal(0.0f, (float)Half2.Zero.Y, 2);

        Assert.Equal(1.0f, (float)Half2.One.X, 2);
        Assert.Equal(1.0f, (float)Half2.One.Y, 2);

        Assert.Equal(1.0f, (float)Half2.UnitX.X, 2);
        Assert.Equal(0.0f, (float)Half2.UnitX.Y, 2);

        Assert.Equal(0.0f, (float)Half2.UnitY.X, 2);
        Assert.Equal(1.0f, (float)Half2.UnitY.Y, 2);
    }

    [Fact]
    public void TestHalf2Equality()
    {
        var h1 = new Half2(1.0f, 2.0f);
        var h2 = new Half2(1.0f, 2.0f);
        var h3 = new Half2(1.0f, 3.0f);

        Assert.True(h1 == h2);
        Assert.False(h1 == h3);
        Assert.True(h1.Equals(h2));
        Assert.False(h1.Equals(h3));
    }

    [Fact]
    public void TestHalf2GetHashCode()
    {
        var h1 = new Half2(1.0f, 2.0f);
        var h2 = new Half2(1.0f, 2.0f);

        Assert.Equal(h1.GetHashCode(), h2.GetHashCode());
    }

    [Fact]
    public void TestHalf2ToString()
    {
        var h = new Half2(1.5f, 2.5f);
        string str = h.ToString();
        Assert.NotNull(str);
        Assert.NotEmpty(str);
    }

    #endregion

    #region Half3 Tests

    [Fact]
    public void TestHalf3Construction()
    {
        var h1 = new Half3(1.0f, 2.0f, 3.0f);
        Assert.Equal(1.0f, (float)h1.X, 2);
        Assert.Equal(2.0f, (float)h1.Y, 2);
        Assert.Equal(3.0f, (float)h1.Z, 2);

        var h2 = new Half3(4.0f);
        Assert.Equal(4.0f, (float)h2.X, 2);
        Assert.Equal(4.0f, (float)h2.Y, 2);
        Assert.Equal(4.0f, (float)h2.Z, 2);

        var h3 = new Half3((Half)1.5f, (Half)2.5f, (Half)3.5f);
        Assert.Equal(1.5f, (float)h3.X, 2);
        Assert.Equal(2.5f, (float)h3.Y, 2);
        Assert.Equal(3.5f, (float)h3.Z, 2);

        var h4 = new Half3((Half)5.0f);
        Assert.Equal(5.0f, (float)h4.X, 2);
        Assert.Equal(5.0f, (float)h4.Y, 2);
        Assert.Equal(5.0f, (float)h4.Z, 2);

        var h5 = new Half3([new Half(6.0f), new Half(7.0f), new Half(8.0f)]);
        Assert.Equal(6.0f, (float)h5.X, 2);
        Assert.Equal(7.0f, (float)h5.Y, 2);
        Assert.Equal(8.0f, (float)h5.Z, 2);
    }

    [Fact]
    public void TestHalf3StaticFields()
    {
        Assert.Equal(0.0f, (float)Half3.Zero.X, 2);
        Assert.Equal(0.0f, (float)Half3.Zero.Y, 2);
        Assert.Equal(0.0f, (float)Half3.Zero.Z, 2);

        Assert.Equal(1.0f, (float)Half3.One.X, 2);
        Assert.Equal(1.0f, (float)Half3.One.Y, 2);
        Assert.Equal(1.0f, (float)Half3.One.Z, 2);

        Assert.Equal(1.0f, (float)Half3.UnitX.X, 2);
        Assert.Equal(0.0f, (float)Half3.UnitX.Y, 2);
        Assert.Equal(0.0f, (float)Half3.UnitX.Z, 2);

        Assert.Equal(0.0f, (float)Half3.UnitY.X, 2);
        Assert.Equal(1.0f, (float)Half3.UnitY.Y, 2);
        Assert.Equal(0.0f, (float)Half3.UnitY.Z, 2);

        Assert.Equal(0.0f, (float)Half3.UnitZ.X, 2);
        Assert.Equal(0.0f, (float)Half3.UnitZ.Y, 2);
        Assert.Equal(1.0f, (float)Half3.UnitZ.Z, 2);
    }

    [Fact]
    public void TestHalf3Equality()
    {
        var h1 = new Half3(1.0f, 2.0f, 3.0f);
        var h2 = new Half3(1.0f, 2.0f, 3.0f);
        var h3 = new Half3(1.0f, 2.0f, 4.0f);

        Assert.True(h1 == h2);
        Assert.False(h1 == h3);
        Assert.True(h1.Equals(h2));
        Assert.False(h1.Equals(h3));
    }

    [Fact]
    public void TestHalf3GetHashCode()
    {
        var h1 = new Half3(1.0f, 2.0f, 3.0f);
        var h2 = new Half3(1.0f, 2.0f, 3.0f);

        Assert.Equal(h1.GetHashCode(), h2.GetHashCode());
    }

    [Fact]
    public void TestHalf3ToString()
    {
        var h = new Half3(1.5f, 2.5f, 3.5f);
        string str = h.ToString();
        Assert.NotNull(str);
        Assert.NotEmpty(str);
    }

    #endregion

    #region Half4 Tests

    [Fact]
    public void TestHalf4Construction()
    {
        var h1 = new Half4(1.0f, 2.0f, 3.0f, 4.0f);
        Assert.Equal(1.0f, (float)h1.X, 2);
        Assert.Equal(2.0f, (float)h1.Y, 2);
        Assert.Equal(3.0f, (float)h1.Z, 2);
        Assert.Equal(4.0f, (float)h1.W, 2);

        var h2 = new Half4(5.0f);
        Assert.Equal(5.0f, (float)h2.X, 2);
        Assert.Equal(5.0f, (float)h2.Y, 2);
        Assert.Equal(5.0f, (float)h2.Z, 2);
        Assert.Equal(5.0f, (float)h2.W, 2);

        var h3 = new Half4((Half)1.5f, (Half)2.5f, (Half)3.5f, (Half)4.5f);
        Assert.Equal(1.5f, (float)h3.X, 2);
        Assert.Equal(2.5f, (float)h3.Y, 2);
        Assert.Equal(3.5f, (float)h3.Z, 2);
        Assert.Equal(4.5f, (float)h3.W, 2);

        var h4 = new Half4((Half)6.0f);
        Assert.Equal(6.0f, (float)h4.X, 2);
        Assert.Equal(6.0f, (float)h4.Y, 2);
        Assert.Equal(6.0f, (float)h4.Z, 2);
        Assert.Equal(6.0f, (float)h4.W, 2);

        var h5 = new Half4([new Half(7.0f), new Half(8.0f), new Half(9.0f), new Half(10.0f)]);
        Assert.Equal(7.0f, (float)h5.X, 2);
        Assert.Equal(8.0f, (float)h5.Y, 2);
        Assert.Equal(9.0f, (float)h5.Z, 2);
        Assert.Equal(10.0f, (float)h5.W, 2);
    }

    [Fact]
    public void TestHalf4StaticFields()
    {
        Assert.Equal(0.0f, (float)Half4.Zero.X, 2);
        Assert.Equal(0.0f, (float)Half4.Zero.Y, 2);
        Assert.Equal(0.0f, (float)Half4.Zero.Z, 2);
        Assert.Equal(0.0f, (float)Half4.Zero.W, 2);

        Assert.Equal(1.0f, (float)Half4.One.X, 2);
        Assert.Equal(1.0f, (float)Half4.One.Y, 2);
        Assert.Equal(1.0f, (float)Half4.One.Z, 2);
        Assert.Equal(1.0f, (float)Half4.One.W, 2);

        Assert.Equal(1.0f, (float)Half4.UnitX.X, 2);
        Assert.Equal(0.0f, (float)Half4.UnitX.Y, 2);
        Assert.Equal(0.0f, (float)Half4.UnitX.Z, 2);
        Assert.Equal(0.0f, (float)Half4.UnitX.W, 2);

        Assert.Equal(0.0f, (float)Half4.UnitY.X, 2);
        Assert.Equal(1.0f, (float)Half4.UnitY.Y, 2);
        Assert.Equal(0.0f, (float)Half4.UnitY.Z, 2);
        Assert.Equal(0.0f, (float)Half4.UnitY.W, 2);

        Assert.Equal(0.0f, (float)Half4.UnitZ.X, 2);
        Assert.Equal(0.0f, (float)Half4.UnitZ.Y, 2);
        Assert.Equal(1.0f, (float)Half4.UnitZ.Z, 2);
        Assert.Equal(0.0f, (float)Half4.UnitZ.W, 2);

        Assert.Equal(0.0f, (float)Half4.UnitW.X, 2);
        Assert.Equal(0.0f, (float)Half4.UnitW.Y, 2);
        Assert.Equal(0.0f, (float)Half4.UnitW.Z, 2);
        Assert.Equal(1.0f, (float)Half4.UnitW.W, 2);
    }

    [Fact]
    public void TestHalf4Equality()
    {
        var h1 = new Half4(1.0f, 2.0f, 3.0f, 4.0f);
        var h2 = new Half4(1.0f, 2.0f, 3.0f, 4.0f);
        var h3 = new Half4(1.0f, 2.0f, 3.0f, 5.0f);

        Assert.True(h1 == h2);
        Assert.False(h1 == h3);
        Assert.True(h1.Equals(h2));
        Assert.False(h1.Equals(h3));
    }

    [Fact]
    public void TestHalf4GetHashCode()
    {
        var h1 = new Half4(1.0f, 2.0f, 3.0f, 4.0f);
        var h2 = new Half4(1.0f, 2.0f, 3.0f, 4.0f);

        Assert.Equal(h1.GetHashCode(), h2.GetHashCode());
    }

    [Fact]
    public void TestHalf4ToString()
    {
        var h = new Half4(1.5f, 2.5f, 3.5f, 4.5f);
        string str = h.ToString();
        Assert.NotNull(str);
        Assert.NotEmpty(str);
    }

    #endregion
}
