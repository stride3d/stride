// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestColor4
{
    [Fact]
    public void TestColor4ConstructorSingleValue()
    {
        var color = new Color4(0.5f);
        Assert.Equal(0.5f, color.R, 5);
        Assert.Equal(0.5f, color.G, 5);
        Assert.Equal(0.5f, color.B, 5);
        Assert.Equal(0.5f, color.A, 5);
    }

    [Fact]
    public void TestColor4ConstructorRGBA()
    {
        var color = new Color4(0.2f, 0.4f, 0.6f, 0.8f);
        Assert.Equal(0.2f, color.R, 5);
        Assert.Equal(0.4f, color.G, 5);
        Assert.Equal(0.6f, color.B, 5);
        Assert.Equal(0.8f, color.A, 5);
    }

    [Fact]
    public void TestColor4ConstructorRGBADefaultAlpha()
    {
        var color = new Color4(0.2f, 0.4f, 0.6f);
        Assert.Equal(0.2f, color.R, 5);
        Assert.Equal(0.4f, color.G, 5);
        Assert.Equal(0.6f, color.B, 5);
        Assert.Equal(1.0f, color.A, 5); // Default alpha
    }

    [Fact]
    public void TestColor4ConstructorVector4()
    {
        var vec = new Vector4(0.25f, 0.5f, 0.75f, 1.0f);
        var color = new Color4(vec);
        Assert.Equal(0.25f, color.R, 5);
        Assert.Equal(0.5f, color.G, 5);
        Assert.Equal(0.75f, color.B, 5);
        Assert.Equal(1.0f, color.A, 5);
    }

    [Fact]
    public void TestColor4ConstructorVector3()
    {
        var vec = new Vector3(0.25f, 0.5f, 0.75f);
        var color = new Color4(vec, 0.5f);
        Assert.Equal(0.25f, color.R, 5);
        Assert.Equal(0.5f, color.G, 5);
        Assert.Equal(0.75f, color.B, 5);
        Assert.Equal(0.5f, color.A, 5);
    }

    [Fact]
    public void TestColor4ConstructorVector3DefaultAlpha()
    {
        var vec = new Vector3(0.25f, 0.5f, 0.75f);
        var color = new Color4(vec);
        Assert.Equal(0.25f, color.R, 5);
        Assert.Equal(0.5f, color.G, 5);
        Assert.Equal(0.75f, color.B, 5);
        Assert.Equal(1.0f, color.A, 5); // Default alpha
    }

    [Fact]
    public void TestColor4ConstructorInt()
    {
        // int constructor extracts bytes: bits 0-7=R, 8-15=G, 16-23=B, 24-31=A, all divided by 255
        var color = new Color4(0xFF804020); // 0xAABBGGRR -> A=255, B=128, G=64, R=32
        Assert.Equal(32 / 255.0f, color.R, 5);   // R = bits 0-7 = 0x20 = 32
        Assert.Equal(64 / 255.0f, color.G, 5);   // G = bits 8-15 = 0x40 = 64
        Assert.Equal(128 / 255.0f, color.B, 5);  // B = bits 16-23 = 0x80 = 128
        Assert.Equal(255 / 255.0f, color.A, 5);  // A = bits 24-31 = 0xFF = 255
    }

    [Fact]
    public void TestColor4ConstructorUInt()
    {
        var color = new Color4(0xFF804020u);
        Assert.Equal(32 / 255.0f, color.R, 5);
        Assert.Equal(64 / 255.0f, color.G, 5);
        Assert.Equal(128 / 255.0f, color.B, 5);
        Assert.Equal(255 / 255.0f, color.A, 5);
    }

    [Fact]
    public void TestColor4ConstructorFloatArray()
    {
        var values = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };
        var color = new Color4(values);
        Assert.Equal(0.1f, color.R, 5);
        Assert.Equal(0.2f, color.G, 5);
        Assert.Equal(0.3f, color.B, 5);
        Assert.Equal(0.4f, color.A, 5);
    }

    [Fact]
    public void TestColor4ConstructorFloatArrayInvalid()
    {
        // Color4 accepts 3 or 4 elements, anything else throws
        var values = new float[] { 1.0f, 0.5f };
        Assert.Throws<ArgumentOutOfRangeException>(() => new Color4(values));
    }

    [Fact]
    public void TestColor4ConstructorColor3()
    {
        var color3 = new Color3(0.2f, 0.4f, 0.6f);
        var color = new Color4(color3);
        Assert.Equal(0.2f, color.R, 5);
        Assert.Equal(0.4f, color.G, 5);
        Assert.Equal(0.6f, color.B, 5);
        Assert.Equal(1.0f, color.A, 5); // Default alpha
    }

    [Fact]
    public void TestColor4ConstructorColor3WithAlpha()
    {
        var color3 = new Color3(0.2f, 0.4f, 0.6f);
        var color = new Color4(color3, 0.5f);
        Assert.Equal(0.2f, color.R, 5);
        Assert.Equal(0.4f, color.G, 5);
        Assert.Equal(0.6f, color.B, 5);
        Assert.Equal(0.5f, color.A, 5);
    }

    [Fact]
    public void TestColor4IndexerGet()
    {
        var color = new Color4(0.1f, 0.2f, 0.3f, 0.4f);
        Assert.Equal(0.1f, color[0], 5);
        Assert.Equal(0.2f, color[1], 5);
        Assert.Equal(0.3f, color[2], 5);
        Assert.Equal(0.4f, color[3], 5);
    }

    [Fact]
    public void TestColor4IndexerSet()
    {
        var color = new Color4();
        color[0] = 0.5f;
        color[1] = 0.6f;
        color[2] = 0.7f;
        color[3] = 0.8f;
        Assert.Equal(0.5f, color.R, 5);
        Assert.Equal(0.6f, color.G, 5);
        Assert.Equal(0.7f, color.B, 5);
        Assert.Equal(0.8f, color.A, 5);
    }

    [Fact]
    public void TestColor4IndexerOutOfRange()
    {
        var color = new Color4();
        Assert.Throws<ArgumentOutOfRangeException>(() => color[4]);
        Assert.Throws<ArgumentOutOfRangeException>(() => color[-1]);
    }

    [Fact]
    public void TestColor4ToBgra()
    {
        var color = new Color4(1.0f, 0.5f, 0.25f, 0.75f);
        var bgra = color.ToBgra();
        // ToBgra packs as: B + (G << 8) + (R << 16) + (A << 24)
        // B=63, G=127, R=255, A=191 -> 0xBFFF7F3F as uint, cast to int is negative
        Assert.Equal(unchecked((int)0xBFFF7F3F), bgra);
    }

    [Fact]
    public void TestColor4ToBgraOut()
    {
        var color = new Color4(1.0f, 0.5f, 0.25f, 0.75f);
        color.ToBgra(out byte r, out byte g, out byte b, out byte a);
        Assert.Equal((byte)255, r);
        Assert.Equal((byte)127, g);
        Assert.Equal((byte)63, b);
        Assert.Equal((byte)191, a);
    }

    [Fact]
    public void TestColor4ToRgba()
    {
        var color = new Color4(1.0f, 0.5f, 0.25f, 0.75f);
        var rgba = color.ToRgba();
        // ToRgba packs differently - check actual implementation
        Assert.Equal(unchecked((int)0xBF3F7FFF), rgba);
    }

    [Fact]
    public void TestColor4ToVector3()
    {
        var color = new Color4(0.2f, 0.4f, 0.6f, 0.8f);
        var vec = color.ToVector3();
        Assert.Equal(0.2f, vec.X, 5);
        Assert.Equal(0.4f, vec.Y, 5);
        Assert.Equal(0.6f, vec.Z, 5);
    }

    [Fact]
    public void TestColor4ToVector4()
    {
        var color = new Color4(0.2f, 0.4f, 0.6f, 0.8f);
        var vec = color.ToVector4();
        Assert.Equal(0.2f, vec.X, 5);
        Assert.Equal(0.4f, vec.Y, 5);
        Assert.Equal(0.6f, vec.Z, 5);
        Assert.Equal(0.8f, vec.W, 5);
    }

    [Fact]
    public void TestColor4ToArray()
    {
        var color = new Color4(0.1f, 0.2f, 0.3f, 0.4f);
        var array = color.ToArray();
        Assert.Equal(4, array.Length);
        Assert.Equal(0.1f, array[0], 5);
        Assert.Equal(0.2f, array[1], 5);
        Assert.Equal(0.3f, array[2], 5);
        Assert.Equal(0.4f, array[3], 5);
    }

    [Fact]
    public void TestColor4ToSRgb()
    {
        var color = new Color4(0.5f, 0.5f, 0.5f, 1.0f);
        var srgb = color.ToSRgb();
        // ToSRgb applies gamma correction, result should be different
        Assert.NotEqual(0.5f, srgb.R, 5);
        Assert.NotEqual(0.5f, srgb.G, 5);
        Assert.NotEqual(0.5f, srgb.B, 5);
        Assert.Equal(1.0f, srgb.A, 5); // Alpha unchanged
    }

    [Fact]
    public void TestColor4ToLinear()
    {
        var color = new Color4(0.5f, 0.5f, 0.5f, 1.0f);
        var linear = color.ToLinear();
        // ToLinear applies inverse gamma correction
        Assert.NotEqual(0.5f, linear.R, 5);
        Assert.NotEqual(0.5f, linear.G, 5);
        Assert.NotEqual(0.5f, linear.B, 5);
        Assert.Equal(1.0f, linear.A, 5); // Alpha unchanged
    }

    [Fact]
    public void TestColor4Add()
    {
        var left = new Color4(0.2f, 0.3f, 0.4f, 0.5f);
        var right = new Color4(0.1f, 0.2f, 0.3f, 0.4f);
        var result = Color4.Add(left, right);
        Assert.Equal(0.3f, result.R, 5);
        Assert.Equal(0.5f, result.G, 5);
        Assert.Equal(0.7f, result.B, 5);
        Assert.Equal(0.9f, result.A, 5);
    }

    [Fact]
    public void TestColor4Subtract()
    {
        var left = new Color4(0.5f, 0.6f, 0.7f, 0.8f);
        var right = new Color4(0.1f, 0.2f, 0.3f, 0.4f);
        var result = Color4.Subtract(left, right);
        Assert.Equal(0.4f, result.R, 5);
        Assert.Equal(0.4f, result.G, 5);
        Assert.Equal(0.4f, result.B, 5);
        Assert.Equal(0.4f, result.A, 5);
    }

    [Fact]
    public void TestColor4Modulate()
    {
        var left = new Color4(0.5f, 0.6f, 0.8f, 1.0f);
        var right = new Color4(0.5f, 0.5f, 0.5f, 0.5f);
        var result = Color4.Modulate(left, right);
        Assert.Equal(0.25f, result.R, 5);
        Assert.Equal(0.3f, result.G, 5);
        Assert.Equal(0.4f, result.B, 5);
        Assert.Equal(0.5f, result.A, 5);
    }

    [Fact]
    public void TestColor4Scale()
    {
        var color = new Color4(0.2f, 0.4f, 0.6f, 0.8f);
        var result = Color4.Scale(color, 2.0f);
        Assert.Equal(0.4f, result.R, 5);
        Assert.Equal(0.8f, result.G, 5);
        Assert.Equal(1.2f, result.B, 5);
        Assert.Equal(1.6f, result.A, 5);
    }

    [Fact]
    public void TestColor4Negate()
    {
        var color = new Color4(0.3f, 0.5f, 0.7f, 0.9f);
        var result = -color;
        Assert.Equal(-0.3f, result.R, 5);
        Assert.Equal(-0.5f, result.G, 5);
        Assert.Equal(-0.7f, result.B, 5);
        Assert.Equal(-0.9f, result.A, 5);
    }

    [Fact]
    public void TestColor4NegateStatic()
    {
        var color = new Color4(0.3f, 0.5f, 0.7f, 0.9f);
        var result = Color4.Negate(color);
        // Note: Color4.Negate computes (1 - x), not (-x)
        Assert.Equal(1.0f - 0.3f, result.R, 5);
        Assert.Equal(1.0f - 0.5f, result.G, 5);
        Assert.Equal(1.0f - 0.7f, result.B, 5);
        Assert.Equal(1.0f - 0.9f, result.A, 5);
    }

    [Fact]
    public void TestColor4Clamp()
    {
        var value = new Color4(0.5f, 1.5f, -0.5f, 2.0f);
        var min = new Color4(0.0f, 0.0f, 0.0f, 0.0f);
        var max = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
        var result = Color4.Clamp(value, min, max);
        Assert.Equal(0.5f, result.R, 5);
        Assert.Equal(1.0f, result.G, 5);
        Assert.Equal(0.0f, result.B, 5);
        Assert.Equal(1.0f, result.A, 5);
    }

    [Fact]
    public void TestColor4Lerp()
    {
        var start = new Color4(0.0f, 0.0f, 0.0f, 0.0f);
        var end = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
        var result = Color4.Lerp(start, end, 0.5f);
        Assert.Equal(0.5f, result.R, 5);
        Assert.Equal(0.5f, result.G, 5);
        Assert.Equal(0.5f, result.B, 5);
        Assert.Equal(0.5f, result.A, 5);
    }

    [Fact]
    public void TestColor4SmoothStep()
    {
        var start = new Color4(0.0f, 0.0f, 0.0f, 0.0f);
        var end = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
        var result = Color4.SmoothStep(start, end, 0.5f);
        // SmoothStep uses hermite interpolation
        Assert.Equal(0.5f, result.R, 5);
        Assert.Equal(0.5f, result.G, 5);
        Assert.Equal(0.5f, result.B, 5);
        Assert.Equal(0.5f, result.A, 5);
    }

    [Fact]
    public void TestColor4Min()
    {
        var left = new Color4(0.2f, 0.6f, 0.3f, 0.8f);
        var right = new Color4(0.4f, 0.5f, 0.7f, 0.1f);
        var result = Color4.Min(left, right);
        Assert.Equal(0.2f, result.R, 5);
        Assert.Equal(0.5f, result.G, 5);
        Assert.Equal(0.3f, result.B, 5);
        Assert.Equal(0.1f, result.A, 5);
    }

    [Fact]
    public void TestColor4Max()
    {
        var left = new Color4(0.2f, 0.6f, 0.3f, 0.8f);
        var right = new Color4(0.4f, 0.5f, 0.7f, 0.1f);
        var result = Color4.Max(left, right);
        Assert.Equal(0.4f, result.R, 5);
        Assert.Equal(0.6f, result.G, 5);
        Assert.Equal(0.7f, result.B, 5);
        Assert.Equal(0.8f, result.A, 5);
    }

    [Fact]
    public void TestColor4AdjustContrast()
    {
        var color = new Color4(0.75f, 0.75f, 0.75f, 1.0f);
        var result = Color4.AdjustContrast(color, 2.0f);
        // Formula: 0.5 + contrast * (value - 0.5) = 0.5 + 2.0 * (0.75 - 0.5) = 0.5 + 0.5 = 1.0
        Assert.Equal(1.0f, result.R, 5);
        Assert.Equal(1.0f, result.G, 5);
        Assert.Equal(1.0f, result.B, 5);
        Assert.Equal(1.0f, result.A, 5); // Alpha unchanged
    }

    [Fact]
    public void TestColor4AdjustSaturation()
    {
        var color = new Color4(1.0f, 0.5f, 0.0f, 1.0f);
        var result = Color4.AdjustSaturation(color, 0.5f);
        // Reduced saturation moves colors toward gray
        Assert.NotEqual(1.0f, result.R, 5);
        Assert.NotEqual(0.0f, result.B, 5);
        Assert.Equal(1.0f, result.A, 5); // Alpha unchanged
    }

    [Fact]
    public void TestColor4PremultiplyAlpha()
    {
        var color = new Color4(1.0f, 0.8f, 0.6f, 0.5f);
        var result = Color4.PremultiplyAlpha(color);
        Assert.Equal(0.5f, result.R, 5);  // 1.0 * 0.5
        Assert.Equal(0.4f, result.G, 5);  // 0.8 * 0.5
        Assert.Equal(0.3f, result.B, 5);  // 0.6 * 0.5
        Assert.Equal(0.5f, result.A, 5);  // Alpha unchanged
    }

    [Fact]
    public void TestColor4OperatorAdd()
    {
        var left = new Color4(0.2f, 0.3f, 0.4f, 0.5f);
        var right = new Color4(0.1f, 0.2f, 0.3f, 0.4f);
        var result = left + right;
        Assert.Equal(0.3f, result.R, 5);
        Assert.Equal(0.5f, result.G, 5);
        Assert.Equal(0.7f, result.B, 5);
        Assert.Equal(0.9f, result.A, 5);
    }

    [Fact]
    public void TestColor4OperatorSubtract()
    {
        var left = new Color4(0.5f, 0.6f, 0.7f, 0.8f);
        var right = new Color4(0.1f, 0.2f, 0.3f, 0.4f);
        var result = left - right;
        Assert.Equal(0.4f, result.R, 5);
        Assert.Equal(0.4f, result.G, 5);
        Assert.Equal(0.4f, result.B, 5);
        Assert.Equal(0.4f, result.A, 5);
    }

    [Fact]
    public void TestColor4OperatorMultiplyScaleLeft()
    {
        var color = new Color4(0.2f, 0.4f, 0.6f, 0.8f);
        var result = 2.0f * color;
        Assert.Equal(0.4f, result.R, 5);
        Assert.Equal(0.8f, result.G, 5);
        Assert.Equal(1.2f, result.B, 5);
        Assert.Equal(1.6f, result.A, 5);
    }

    [Fact]
    public void TestColor4OperatorMultiplyScaleRight()
    {
        var color = new Color4(0.2f, 0.4f, 0.6f, 0.8f);
        var result = color * 2.0f;
        Assert.Equal(0.4f, result.R, 5);
        Assert.Equal(0.8f, result.G, 5);
        Assert.Equal(1.2f, result.B, 5);
        Assert.Equal(1.6f, result.A, 5);
    }

    [Fact]
    public void TestColor4OperatorMultiplyModulate()
    {
        var left = new Color4(0.5f, 0.6f, 0.8f, 1.0f);
        var right = new Color4(0.5f, 0.5f, 0.5f, 0.5f);
        var result = left * right;
        Assert.Equal(0.25f, result.R, 5);
        Assert.Equal(0.3f, result.G, 5);
        Assert.Equal(0.4f, result.B, 5);
        Assert.Equal(0.5f, result.A, 5);
    }

    [Fact]
    public void TestColor4Equality()
    {
        var color1 = new Color4(0.5f, 0.6f, 0.7f, 0.8f);
        var color2 = new Color4(0.5f, 0.6f, 0.7f, 0.8f);
        var color3 = new Color4(0.5f, 0.6f, 0.7f, 0.9f);

        Assert.True(color1 == color2);
        Assert.False(color1 == color3);
        Assert.False(color1 != color2);
        Assert.True(color1 != color3);
    }

    [Fact]
    public void TestColor4Equals()
    {
        var color1 = new Color4(0.5f, 0.6f, 0.7f, 0.8f);
        var color2 = new Color4(0.5f, 0.6f, 0.7f, 0.8f);
        var color3 = new Color4(0.5f, 0.6f, 0.7f, 0.9f);

        Assert.True(color1.Equals(color2));
        Assert.False(color1.Equals(color3));
    }

    [Fact]
    public void TestColor4GetHashCode()
    {
        var color1 = new Color4(0.5f, 0.6f, 0.7f, 0.8f);
        var color2 = new Color4(0.5f, 0.6f, 0.7f, 0.8f);

        Assert.Equal(color1.GetHashCode(), color2.GetHashCode());
    }

    [Fact]
    public void TestColor4ToString()
    {
        var color = new Color4(0.5f, 0.6f, 0.7f, 0.8f);
        var str = color.ToString();
        Assert.NotNull(str);
        // ToString uses culture, accept decimal comma or point
        Assert.True(str.Contains("0.5") || str.Contains("0,5"));
    }

    [Fact]
    public void TestColor4Deconstruct()
    {
        var color = new Color4(0.1f, 0.2f, 0.3f, 0.4f);
        var (r, g, b, a) = color;
        Assert.Equal(0.1f, r, 5);
        Assert.Equal(0.2f, g, 5);
        Assert.Equal(0.3f, b, 5);
        Assert.Equal(0.4f, a, 5);
    }

    // Out parameter versions
    [Fact]
    public void TestColor4AddWithOutParameter()
    {
        var left = new Color4(0.2f, 0.3f, 0.4f, 0.5f);
        var right = new Color4(0.1f, 0.2f, 0.3f, 0.4f);
        Color4.Add(ref left, ref right, out var result);
        Assert.Equal(0.3f, result.R, 5);
        Assert.Equal(0.5f, result.G, 5);
        Assert.Equal(0.7f, result.B, 5);
        Assert.Equal(0.9f, result.A, 5);
    }

    [Fact]
    public void TestColor4SubtractWithOutParameter()
    {
        var left = new Color4(0.5f, 0.6f, 0.7f, 0.8f);
        var right = new Color4(0.1f, 0.2f, 0.3f, 0.4f);
        Color4.Subtract(ref left, ref right, out var result);
        Assert.Equal(0.4f, result.R, 5);
        Assert.Equal(0.4f, result.G, 5);
        Assert.Equal(0.4f, result.B, 5);
        Assert.Equal(0.4f, result.A, 5);
    }

    [Fact]
    public void TestColor4ModulateWithOutParameter()
    {
        var left = new Color4(0.5f, 0.6f, 0.8f, 1.0f);
        var right = new Color4(0.5f, 0.5f, 0.5f, 0.5f);
        Color4.Modulate(ref left, ref right, out var result);
        Assert.Equal(0.25f, result.R, 5);
        Assert.Equal(0.3f, result.G, 5);
        Assert.Equal(0.4f, result.B, 5);
        Assert.Equal(0.5f, result.A, 5);
    }

    [Fact]
    public void TestColor4ScaleWithOutParameter()
    {
        var color = new Color4(0.2f, 0.4f, 0.6f, 0.8f);
        Color4.Scale(ref color, 2.0f, out var result);
        Assert.Equal(0.4f, result.R, 5);
        Assert.Equal(0.8f, result.G, 5);
        Assert.Equal(1.2f, result.B, 5);
        Assert.Equal(1.6f, result.A, 5);
    }

    [Fact]
    public void TestColor4NegateWithOutParameter()
    {
        var color = new Color4(0.3f, 0.5f, 0.7f, 0.9f);
        Color4.Negate(ref color, out var result);
        // Note: Color4.Negate computes (1 - x), not (-x)
        Assert.Equal(1.0f - 0.3f, result.R, 5);
        Assert.Equal(1.0f - 0.5f, result.G, 5);
        Assert.Equal(1.0f - 0.7f, result.B, 5);
        Assert.Equal(1.0f - 0.9f, result.A, 5);
    }

    [Fact]
    public void TestColor4ClampWithOutParameter()
    {
        var value = new Color4(0.5f, 1.5f, -0.5f, 2.0f);
        var min = new Color4(0.0f, 0.0f, 0.0f, 0.0f);
        var max = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
        Color4.Clamp(ref value, ref min, ref max, out var result);
        Assert.Equal(0.5f, result.R, 5);
        Assert.Equal(1.0f, result.G, 5);
        Assert.Equal(0.0f, result.B, 5);
        Assert.Equal(1.0f, result.A, 5);
    }

    [Fact]
    public void TestColor4LerpWithOutParameter()
    {
        var start = new Color4(0.0f, 0.0f, 0.0f, 0.0f);
        var end = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
        Color4.Lerp(ref start, ref end, 0.5f, out var result);
        Assert.Equal(0.5f, result.R, 5);
        Assert.Equal(0.5f, result.G, 5);
        Assert.Equal(0.5f, result.B, 5);
        Assert.Equal(0.5f, result.A, 5);
    }

    [Fact]
    public void TestColor4SmoothStepWithOutParameter()
    {
        var start = new Color4(0.0f, 0.0f, 0.0f, 0.0f);
        var end = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
        Color4.SmoothStep(ref start, ref end, 0.5f, out var result);
        Assert.Equal(0.5f, result.R, 5);
        Assert.Equal(0.5f, result.G, 5);
        Assert.Equal(0.5f, result.B, 5);
        Assert.Equal(0.5f, result.A, 5);
    }

    [Fact]
    public void TestColor4MinWithOutParameter()
    {
        var left = new Color4(0.2f, 0.6f, 0.3f, 0.8f);
        var right = new Color4(0.4f, 0.5f, 0.7f, 0.1f);
        Color4.Min(ref left, ref right, out var result);
        Assert.Equal(0.2f, result.R, 5);
        Assert.Equal(0.5f, result.G, 5);
        Assert.Equal(0.3f, result.B, 5);
        Assert.Equal(0.1f, result.A, 5);
    }

    [Fact]
    public void TestColor4MaxWithOutParameter()
    {
        var left = new Color4(0.2f, 0.6f, 0.3f, 0.8f);
        var right = new Color4(0.4f, 0.5f, 0.7f, 0.1f);
        Color4.Max(ref left, ref right, out var result);
        Assert.Equal(0.4f, result.R, 5);
        Assert.Equal(0.6f, result.G, 5);
        Assert.Equal(0.7f, result.B, 5);
        Assert.Equal(0.8f, result.A, 5);
    }

    [Fact]
    public void TestColor4AdjustContrastWithOutParameter()
    {
        var color = new Color4(0.75f, 0.75f, 0.75f, 1.0f);
        Color4.AdjustContrast(ref color, 2.0f, out var result);
        Assert.Equal(1.0f, result.R, 5);
        Assert.Equal(1.0f, result.G, 5);
        Assert.Equal(1.0f, result.B, 5);
        Assert.Equal(1.0f, result.A, 5);
    }

    [Fact]
    public void TestColor4AdjustSaturationWithOutParameter()
    {
        var color = new Color4(1.0f, 0.5f, 0.0f, 1.0f);
        Color4.AdjustSaturation(ref color, 0.5f, out var result);
        Assert.NotEqual(1.0f, result.R, 5);
        Assert.NotEqual(0.0f, result.B, 5);
        Assert.Equal(1.0f, result.A, 5);
    }
}
