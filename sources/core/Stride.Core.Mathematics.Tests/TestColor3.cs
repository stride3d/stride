// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestColor3
{
    [Fact]
    public void TestColor3ConstructorFloat()
    {
        var color = new Color3(0.5f);
        Assert.Equal(0.5f, color.R);
        Assert.Equal(0.5f, color.G);
        Assert.Equal(0.5f, color.B);
    }

    [Fact]
    public void TestColor3ConstructorRGB()
    {
        var color = new Color3(1.0f, 0.5f, 0.25f);
        Assert.Equal(1.0f, color.R);
        Assert.Equal(0.5f, color.G);
        Assert.Equal(0.25f, color.B);
    }

    [Fact]
    public void TestColor3ConstructorVector3()
    {
        var vector = new Vector3(1.0f, 0.5f, 0.25f);
        var color = new Color3(vector);
        Assert.Equal(1.0f, color.R);
        Assert.Equal(0.5f, color.G);
        Assert.Equal(0.25f, color.B);
    }

    [Fact]
    public void TestColor3ConstructorInt()
    {
        // int constructor extracts bytes: bits 0-7=R, 8-15=G, 16-23=B, all divided by 255
        var color = new Color3(0x00FF8040); // 0x00BBGGRR -> B=255, G=128, R=64
        Assert.Equal(64 / 255.0f, color.R, 5);   // R = bits 0-7 = 0x40 = 64
        Assert.Equal(128 / 255.0f, color.G, 5);  // G = bits 8-15 = 0x80 = 128
        Assert.Equal(255 / 255.0f, color.B, 5);  // B = bits 16-23 = 0xFF = 255
    }

    [Fact]
    public void TestColor3ConstructorUInt()
    {
        var color = new Color3(0x00FF8040u);
        Assert.Equal(64 / 255.0f, color.R, 5);
        Assert.Equal(128 / 255.0f, color.G, 5);
        Assert.Equal(255 / 255.0f, color.B, 5);
    }

    [Fact]
    public void TestColor3ConstructorFloatArray()
    {
        var values = new float[] { 1.0f, 0.5f, 0.25f };
        var color = new Color3(values);
        Assert.Equal(1.0f, color.R);
        Assert.Equal(0.5f, color.G);
        Assert.Equal(0.25f, color.B);
    }

    [Fact]
    public void TestColor3ConstructorFloatArrayInvalid()
    {
        var values = new float[] { 1.0f, 0.5f };
        Assert.Throws<ArgumentOutOfRangeException>(() => new Color3(values));
    }

    [Fact]
    public void TestColor3Indexer()
    {
        var color = new Color3(1.0f, 0.5f, 0.25f);
        Assert.Equal(1.0f, color[0]);
        Assert.Equal(0.5f, color[1]);
        Assert.Equal(0.25f, color[2]);
        
        color[0] = 0.1f;
        color[1] = 0.2f;
        color[2] = 0.3f;
        Assert.Equal(0.1f, color.R);
        Assert.Equal(0.2f, color.G);
        Assert.Equal(0.3f, color.B);
    }

    [Fact]
    public void TestColor3IndexerOutOfRange()
    {
        var color = new Color3(1.0f, 0.5f, 0.25f);
        Assert.Throws<ArgumentOutOfRangeException>(() => color[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => color[3]);
        Assert.Throws<ArgumentOutOfRangeException>(() => color[-1] = 0.0f);
        Assert.Throws<ArgumentOutOfRangeException>(() => color[3] = 0.0f);
    }

    [Fact]
    public void TestColor3ToRgb()
    {
        var color = new Color3(1.0f, 0.5f, 0.25f);
        var rgb = color.ToRgb();
        // ToRgb packs as: R + (G << 8) + (B << 16) + (255 << 24)
        // R=255, G=127, B=63, A=255 -> 0xFF3F7FFF as uint, but cast to int is negative
        Assert.Equal(unchecked((int)0xFF3F7FFF), rgb);
    }

    [Fact]
    public void TestColor3ToArray()
    {
        var color = new Color3(1.0f, 0.5f, 0.25f);
        var array = color.ToArray();
        Assert.Equal(3, array.Length);
        Assert.Equal(1.0f, array[0]);
        Assert.Equal(0.5f, array[1]);
        Assert.Equal(0.25f, array[2]);
    }

    [Fact]
    public void TestColor3Pow()
    {
        var color = new Color3(0.5f, 0.25f, 0.125f);
        color.Pow(2.0f);
        Assert.Equal(0.25f, color.R, 5);
        Assert.Equal(0.0625f, color.G, 5);
        Assert.Equal(0.015625f, color.B, 5);
    }

    [Fact]
    public void TestColor3Add()
    {
        var c1 = new Color3(0.1f, 0.2f, 0.3f);
        var c2 = new Color3(0.4f, 0.5f, 0.6f);
        var result = c1 + c2;
        Assert.Equal(0.5f, result.R, 5);
        Assert.Equal(0.7f, result.G, 5);
        Assert.Equal(0.9f, result.B, 5);
    }

    [Fact]
    public void TestColor3AddStatic()
    {
        var c1 = new Color3(0.1f, 0.2f, 0.3f);
        var c2 = new Color3(0.4f, 0.5f, 0.6f);
        var result = Color3.Add(c1, c2);
        Assert.Equal(0.5f, result.R, 5);
        Assert.Equal(0.7f, result.G, 5);
        Assert.Equal(0.9f, result.B, 5);
    }

    [Fact]
    public void TestColor3Subtract()
    {
        var c1 = new Color3(0.9f, 0.7f, 0.5f);
        var c2 = new Color3(0.1f, 0.2f, 0.3f);
        var result = c1 - c2;
        Assert.Equal(0.8f, result.R, 5);
        Assert.Equal(0.5f, result.G, 5);
        Assert.Equal(0.2f, result.B, 5);
    }

    [Fact]
    public void TestColor3Modulate()
    {
        var c1 = new Color3(0.5f, 0.4f, 0.2f);
        var c2 = new Color3(2.0f, 0.5f, 3.0f);
        var result = Color3.Modulate(c1, c2);
        Assert.Equal(1.0f, result.R, 5);
        Assert.Equal(0.2f, result.G, 5);
        Assert.Equal(0.6f, result.B, 5);
    }

    [Fact]
    public void TestColor3ModulateOperator()
    {
        var c1 = new Color3(0.5f, 0.4f, 0.2f);
        var c2 = new Color3(2.0f, 0.5f, 3.0f);
        var result = c1 * c2;
        Assert.Equal(1.0f, result.R, 5);
        Assert.Equal(0.2f, result.G, 5);
        Assert.Equal(0.6f, result.B, 5);
    }

    [Fact]
    public void TestColor3Scale()
    {
        var color = new Color3(0.2f, 0.4f, 0.6f);
        var result = Color3.Scale(color, 2.0f);
        Assert.Equal(0.4f, result.R, 5);
        Assert.Equal(0.8f, result.G, 5);
        Assert.Equal(1.2f, result.B, 5);
    }

    [Fact]
    public void TestColor3ScaleOperator()
    {
        var color = new Color3(0.2f, 0.4f, 0.6f);
        var result = color * 2.0f;
        Assert.Equal(0.4f, result.R, 5);
        Assert.Equal(0.8f, result.G, 5);
        Assert.Equal(1.2f, result.B, 5);
    }

    [Fact]
    public void TestColor3Negate()
    {
        var color = new Color3(0.3f, 0.5f, 0.7f);
        var result = -color;
        Assert.Equal(-0.3f, result.R, 5);
        Assert.Equal(-0.5f, result.G, 5);
        Assert.Equal(-0.7f, result.B, 5);
    }

    [Fact]
    public void TestColor3Clamp()
    {
        var color = new Color3(1.5f, -0.5f, 0.5f);
        var min = new Color3(0.0f, 0.0f, 0.0f);
        var max = new Color3(1.0f, 1.0f, 1.0f);
        var result = Color3.Clamp(color, min, max);
        Assert.Equal(1.0f, result.R);
        Assert.Equal(0.0f, result.G);
        Assert.Equal(0.5f, result.B);
    }

    [Fact]
    public void TestColor3Lerp()
    {
        var start = new Color3(0.0f, 0.0f, 0.0f);
        var end = new Color3(1.0f, 1.0f, 1.0f);
        var result = Color3.Lerp(start, end, 0.5f);
        Assert.Equal(0.5f, result.R);
        Assert.Equal(0.5f, result.G);
        Assert.Equal(0.5f, result.B);
    }

    [Fact]
    public void TestColor3SmoothStep()
    {
        var start = new Color3(0.0f, 0.0f, 0.0f);
        var end = new Color3(1.0f, 1.0f, 1.0f);
        var result = Color3.SmoothStep(start, end, 0.5f);
        Assert.Equal(0.5f, result.R);
        Assert.Equal(0.5f, result.G);
        Assert.Equal(0.5f, result.B);
    }

    [Fact]
    public void TestColor3Min()
    {
        var c1 = new Color3(0.3f, 0.7f, 0.5f);
        var c2 = new Color3(0.5f, 0.4f, 0.6f);
        var result = Color3.Min(c1, c2);
        Assert.Equal(0.3f, result.R);
        Assert.Equal(0.4f, result.G);
        Assert.Equal(0.5f, result.B);
    }

    [Fact]
    public void TestColor3Max()
    {
        var c1 = new Color3(0.3f, 0.7f, 0.5f);
        var c2 = new Color3(0.5f, 0.4f, 0.6f);
        var result = Color3.Max(c1, c2);
        Assert.Equal(0.5f, result.R);
        Assert.Equal(0.7f, result.G);
        Assert.Equal(0.6f, result.B);
    }

    [Fact]
    public void TestColor3AdjustContrast()
    {
        var color = new Color3(0.5f, 0.5f, 0.5f);
        var result = Color3.AdjustContrast(color, 2.0f);
        // Contrast adjustment formula: (color - 0.5) * contrast + 0.5
        Assert.Equal(0.5f, result.R, 5);
        Assert.Equal(0.5f, result.G, 5);
        Assert.Equal(0.5f, result.B, 5);
    }

    [Fact]
    public void TestColor3AdjustSaturation()
    {
        var color = new Color3(1.0f, 0.5f, 0.0f);
        var result = Color3.AdjustSaturation(color, 0.5f);
        // Should reduce saturation
        Assert.True(result.R < 1.0f && result.R > 0.5f);
        Assert.True(result.G > 0.0f);
    }

    [Fact]
    public void TestColor3ToSRgb()
    {
        var color = new Color3(0.5f, 0.25f, 0.75f);
        var srgb = color.ToSRgb();
        // sRGB conversion is non-linear
        Assert.NotEqual(color.R, srgb.R);
        Assert.NotEqual(color.G, srgb.G);
        Assert.NotEqual(color.B, srgb.B);
    }

    [Fact]
    public void TestColor3ToLinear()
    {
        var color = new Color3(0.5f, 0.25f, 0.75f);
        var linear = color.ToLinear();
        // Linear conversion is non-linear
        Assert.NotEqual(color.R, linear.R);
        Assert.NotEqual(color.G, linear.G);
        Assert.NotEqual(color.B, linear.B);
    }

    [Fact]
    public void TestColor3Equality()
    {
        var c1 = new Color3(1.0f, 0.5f, 0.25f);
        var c2 = new Color3(1.0f, 0.5f, 0.25f);
        var c3 = new Color3(0.5f, 0.5f, 0.25f);

        Assert.True(c1 == c2);
        Assert.False(c1 == c3);
        Assert.False(c1 != c2);
        Assert.True(c1 != c3);
        Assert.True(c1.Equals(c2));
        Assert.False(c1.Equals(c3));
    }

    [Fact]
    public void TestColor3GetHashCode()
    {
        var c1 = new Color3(1.0f, 0.5f, 0.25f);
        var c2 = new Color3(1.0f, 0.5f, 0.25f);
        Assert.Equal(c1.GetHashCode(), c2.GetHashCode());
    }

    [Fact]
    public void TestColor3ToString()
    {
        var color = new Color3(1.0f, 0.5f, 0.25f);
        var str = color.ToString();
        Assert.Contains("1", str);
        Assert.Contains("5", str);
        Assert.Contains("25", str);
    }

    [Fact]
    public void TestColor3ConversionToVector3()
    {
        var color = new Color3(1.0f, 0.5f, 0.25f);
        Vector3 vector = (Vector3)color;
        Assert.Equal(1.0f, vector.X);
        Assert.Equal(0.5f, vector.Y);
        Assert.Equal(0.25f, vector.Z);
    }

    [Fact]
    public void TestColor3ConversionFromVector3()
    {
        var vector = new Vector3(1.0f, 0.5f, 0.25f);
        Color3 color = (Color3)vector;
        Assert.Equal(1.0f, color.R);
        Assert.Equal(0.5f, color.G);
        Assert.Equal(0.25f, color.B);
    }

    [Fact]
    public void TestColor3AddWithOutParameter()
    {
        var c1 = new Color3(0.1f, 0.2f, 0.3f);
        var c2 = new Color3(0.4f, 0.5f, 0.6f);
        Color3.Add(ref c1, ref c2, out var result);
        Assert.Equal(0.5f, result.R, 5);
        Assert.Equal(0.7f, result.G, 5);
        Assert.Equal(0.9f, result.B, 5);
    }

    [Fact]
    public void TestColor3SubtractWithOutParameter()
    {
        var c1 = new Color3(0.9f, 0.7f, 0.5f);
        var c2 = new Color3(0.1f, 0.2f, 0.3f);
        Color3.Subtract(ref c1, ref c2, out var result);
        Assert.Equal(0.8f, result.R, 5);
        Assert.Equal(0.5f, result.G, 5);
        Assert.Equal(0.2f, result.B, 5);
    }

    [Fact]
    public void TestColor3ModulateWithOutParameter()
    {
        var c1 = new Color3(0.5f, 0.4f, 0.2f);
        var c2 = new Color3(2.0f, 0.5f, 3.0f);
        Color3.Modulate(ref c1, ref c2, out var result);
        Assert.Equal(1.0f, result.R, 5);
        Assert.Equal(0.2f, result.G, 5);
        Assert.Equal(0.6f, result.B, 5);
    }

    [Fact]
    public void TestColor3ScaleWithOutParameter()
    {
        var color = new Color3(0.2f, 0.4f, 0.6f);
        Color3.Scale(ref color, 2.0f, out var result);
        Assert.Equal(0.4f, result.R, 5);
        Assert.Equal(0.8f, result.G, 5);
        Assert.Equal(1.2f, result.B, 5);
    }

    [Fact]
    public void TestColor3NegateWithOutParameter()
    {
        var color = new Color3(0.3f, 0.5f, 0.7f);
        Color3.Negate(ref color, out var result);
        // Note: Color3.Negate computes (1 - x), not (-x)
        Assert.Equal(1.0f - 0.3f, result.R, 5);
        Assert.Equal(1.0f - 0.5f, result.G, 5);
        Assert.Equal(1.0f - 0.7f, result.B, 5);
    }

    [Fact]
    public void TestColor3ClampWithOutParameter()
    {
        var color = new Color3(1.5f, -0.5f, 0.5f);
        var min = new Color3(0.0f, 0.0f, 0.0f);
        var max = new Color3(1.0f, 1.0f, 1.0f);
        Color3.Clamp(ref color, ref min, ref max, out var result);
        Assert.Equal(1.0f, result.R);
        Assert.Equal(0.0f, result.G);
        Assert.Equal(0.5f, result.B);
    }

    [Fact]
    public void TestColor3LerpWithOutParameter()
    {
        var start = new Color3(0.0f, 0.0f, 0.0f);
        var end = new Color3(1.0f, 1.0f, 1.0f);
        Color3.Lerp(ref start, ref end, 0.5f, out var result);
        Assert.Equal(0.5f, result.R);
        Assert.Equal(0.5f, result.G);
        Assert.Equal(0.5f, result.B);
    }

    [Fact]
    public void TestColor3SmoothStepWithOutParameter()
    {
        var start = new Color3(0.0f, 0.0f, 0.0f);
        var end = new Color3(1.0f, 1.0f, 1.0f);
        Color3.SmoothStep(ref start, ref end, 0.5f, out var result);
        Assert.Equal(0.5f, result.R);
        Assert.Equal(0.5f, result.G);
        Assert.Equal(0.5f, result.B);
    }

    [Fact]
    public void TestColor3MinWithOutParameter()
    {
        var c1 = new Color3(0.3f, 0.7f, 0.5f);
        var c2 = new Color3(0.5f, 0.4f, 0.6f);
        Color3.Min(ref c1, ref c2, out var result);
        Assert.Equal(0.3f, result.R);
        Assert.Equal(0.4f, result.G);
        Assert.Equal(0.5f, result.B);
    }

    [Fact]
    public void TestColor3MaxWithOutParameter()
    {
        var c1 = new Color3(0.3f, 0.7f, 0.5f);
        var c2 = new Color3(0.5f, 0.4f, 0.6f);
        Color3.Max(ref c1, ref c2, out var result);
        Assert.Equal(0.5f, result.R);
        Assert.Equal(0.7f, result.G);
        Assert.Equal(0.6f, result.B);
    }

    [Fact]
    public void TestColor3AdjustContrastWithOutParameter()
    {
        var color = new Color3(0.5f, 0.5f, 0.5f);
        Color3.AdjustContrast(ref color, 2.0f, out var result);
        Assert.Equal(0.5f, result.R, 5);
        Assert.Equal(0.5f, result.G, 5);
        Assert.Equal(0.5f, result.B, 5);
    }

    [Fact]
    public void TestColor3AdjustSaturationWithOutParameter()
    {
        var color = new Color3(1.0f, 0.5f, 0.0f);
        Color3.AdjustSaturation(ref color, 0.5f, out var result);
        Assert.True(result.R < 1.0f && result.R > 0.5f);
        Assert.True(result.G > 0.0f);
    }

    [Fact]
    public void TestColor3Deconstruct()
    {
        var color = new Color3(1.0f, 0.5f, 0.25f);
        var (r, g, b) = color;
        Assert.Equal(1.0f, r);
        Assert.Equal(0.5f, g);
        Assert.Equal(0.25f, b);
    }
}
