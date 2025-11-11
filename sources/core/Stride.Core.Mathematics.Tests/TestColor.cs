// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestColor
{
    #region Color (RGBA byte) Tests

    [Fact]
    public void TestColorConstruction()
    {
        // Note: Color(128) matches Color(float) constructor, which clamps 128.0f to byte range
        // For byte constructor, need explicit cast or use component constructor
        var c1 = new Color(128, 128, 128, 128);
        Assert.Equal((byte)128, c1.R);
        Assert.Equal((byte)128, c1.G);
        Assert.Equal((byte)128, c1.B);
        Assert.Equal((byte)128, c1.A);

        var c2 = new Color(255, 128, 64, 32);
        Assert.Equal((byte)255, c2.R);
        Assert.Equal((byte)128, c2.G);
        Assert.Equal((byte)64, c2.B);
        Assert.Equal((byte)32, c2.A);

        var c3 = new Color(255, 128, 64);
        Assert.Equal((byte)255, c3.R);
        Assert.Equal((byte)128, c3.G);
        Assert.Equal((byte)64, c3.B);
        Assert.Equal((byte)255, c3.A);

        var c4 = new Color(1.0f, 0.5f, 0.25f, 0.125f);
        Assert.Equal((byte)255, c4.R);
        Assert.Equal((byte)127, c4.G);
        Assert.Equal((byte)63, c4.B);
        Assert.Equal((byte)31, c4.A);

        var c5 = new Color(new Vector4(1.0f, 0.5f, 0.25f, 0.125f));
        Assert.Equal((byte)255, c5.R);
        Assert.Equal((byte)127, c5.G);
        Assert.Equal((byte)63, c5.B);
        Assert.Equal((byte)31, c5.A);

        var c6 = new Color(new Vector3(1.0f, 0.5f, 0.25f), 0.125f);
        Assert.Equal((byte)255, c6.R);
        Assert.Equal((byte)127, c6.G);
        Assert.Equal((byte)63, c6.B);
        Assert.Equal((byte)31, c6.A);
    }

    [Fact]
    public void TestColorFromRgba()
    {
        // RGBA format: R in low byte, G next, B next, A in high byte
        var c1 = Color.FromRgba(0x204080FF);
        Assert.Equal((byte)0xFF, c1.R);
        Assert.Equal((byte)0x80, c1.G);
        Assert.Equal((byte)0x40, c1.B);
        Assert.Equal((byte)0x20, c1.A);
    }

    [Fact]
    public void TestColorFromBgra()
    {
        // BGRA format: B in bits 0-7, G in bits 8-15, R in bits 16-23, A in bits 24-31
        var c1 = Color.FromBgra(0x20804040);
        Assert.Equal((byte)0x80, c1.R);
        Assert.Equal((byte)0x40, c1.G);
        Assert.Equal((byte)0x40, c1.B);
        Assert.Equal((byte)0x20, c1.A);
    }

    [Fact]
    public void TestColorToRgba()
    {
        var c = new Color(255, 128, 64, 32);
        int rgba = c.ToRgba();
        // RGBA: R=255 (low), G=128, B=64, A=32 (high)
        // = 0x20408FF
        Assert.Equal(unchecked((int)0x204080FF), rgba);
    }

    [Fact]
    public void TestColorToVector()
    {
        var c = new Color(255, 128, 64, 32);
        var v3 = c.ToVector3();
        Assert.Equal(1.0f, v3.X, 3);
        Assert.Equal(128f / 255f, v3.Y, 3);
        Assert.Equal(64f / 255f, v3.Z, 3);

        var v4 = c.ToVector4();
        Assert.Equal(1.0f, v4.X, 3);
        Assert.Equal(128f / 255f, v4.Y, 3);
        Assert.Equal(64f / 255f, v4.Z, 3);
        Assert.Equal(32f / 255f, v4.W, 3);
    }

    [Fact]
    public void TestColorAddition()
    {
        var c1 = new Color(100, 50, 25, 10);
        var c2 = new Color(50, 100, 150, 200);
        var result = Color.Add(c1, c2);
        Assert.Equal((byte)150, result.R);
        Assert.Equal((byte)150, result.G);
        Assert.Equal((byte)175, result.B);
        Assert.Equal((byte)210, result.A);
    }

    [Fact]
    public void TestColorSubtraction()
    {
        var c1 = new Color(200, 150, 100, 50);
        var c2 = new Color(50, 50, 50, 25);
        var result = Color.Subtract(c1, c2);
        Assert.Equal((byte)150, result.R);
        Assert.Equal((byte)100, result.G);
        Assert.Equal((byte)50, result.B);
        Assert.Equal((byte)25, result.A);
    }

    [Fact]
    public void TestColorModulate()
    {
        var c1 = new Color(255, 128, 64, 32);
        var c2 = new Color(128, 128, 128, 128);
        var result = Color.Modulate(c1, c2);
        // Modulate uses: (byte)(left * right / 255)
        Assert.Equal((byte)128, result.R); // 255 * 128 / 255 = 128
        Assert.Equal((byte)64, result.G);  // 128 * 128 / 255 = 64
        Assert.Equal((byte)32, result.B);  // 64 * 128 / 255 = 32
        Assert.Equal((byte)16, result.A);  // 32 * 128 / 255 = 16
    }

    [Fact]
    public void TestColorScale()
    {
        var c = new Color(100, 80, 60, 40);
        var result = Color.Scale(c, 2.0f);
        Assert.Equal((byte)200, result.R);
        Assert.Equal((byte)160, result.G);
        Assert.Equal((byte)120, result.B);
        Assert.Equal((byte)80, result.A);
    }

    [Fact]
    public void TestColorNegate()
    {
        var c = new Color(200, 150, 100, 50);
        var result = Color.Negate(c);
        Assert.Equal((byte)55, result.R);
        Assert.Equal((byte)105, result.G);
        Assert.Equal((byte)155, result.B);
        Assert.Equal((byte)205, result.A);
    }

    [Fact]
    public void TestColorClamp()
    {
        var c = new Color(50, 150, 200, 250);
        var min = new Color(100, 100, 100, 100);
        var max = new Color(180, 180, 180, 180);
        var result = Color.Clamp(c, min, max);
        Assert.Equal((byte)100, result.R);
        Assert.Equal((byte)150, result.G);
        Assert.Equal((byte)180, result.B);
        Assert.Equal((byte)180, result.A);
    }

    [Fact]
    public void TestColorEquality()
    {
        var c1 = new Color(255, 128, 64, 32);
        var c2 = new Color(255, 128, 64, 32);
        var c3 = new Color(255, 128, 64, 31);

        Assert.True(c1 == c2);
        Assert.False(c1 == c3);
        Assert.True(c1.Equals(c2));
        Assert.False(c1.Equals(c3));
    }

    [Fact]
    public void TestColorArrayConstructor()
    {
        var floatArray = new float[] { 1.0f, 0.5f, 0.25f, 0.125f };
        var c1 = new Color(floatArray);
        Assert.Equal((byte)255, c1.R);
        Assert.Equal((byte)127, c1.G);
        Assert.Equal((byte)63, c1.B);
        Assert.Equal((byte)31, c1.A);

        var byteArray = new byte[] { 255, 128, 64, 32 };
        var c2 = new Color(byteArray);
        Assert.Equal((byte)255, c2.R);
        Assert.Equal((byte)128, c2.G);
        Assert.Equal((byte)64, c2.B);
        Assert.Equal((byte)32, c2.A);
    }

    [Fact]
    public void TestColorIndexer()
    {
        var c = new Color(255, 128, 64, 32);
        Assert.Equal((byte)255, c[0]);
        Assert.Equal((byte)128, c[1]);
        Assert.Equal((byte)64, c[2]);
        Assert.Equal((byte)32, c[3]);

        c[0] = 100;
        c[1] = 150;
        c[2] = 200;
        c[3] = 250;
        Assert.Equal((byte)100, c.R);
        Assert.Equal((byte)150, c.G);
        Assert.Equal((byte)200, c.B);
        Assert.Equal((byte)250, c.A);
    }

    [Fact]
    public void TestColorToBgra()
    {
        var c = new Color(255, 128, 64, 32);
        int bgra = c.ToBgra();
        // BGRA: B=64 (low), G=128, R=255, A=32 (high)
        Assert.Equal(unchecked((int)0x20FF8040), bgra);
    }

    [Fact]
    public void TestColorToArgb()
    {
        var c = new Color(255, 128, 64, 32);
        int argb = c.ToArgb();
        // ARGB: A | (R << 8) | (G << 16) | (B << 24)
        Assert.Equal(1082195744, argb);
    }

    [Fact]
    public void TestColorToAbgr()
    {
        var c = new Color(255, 128, 64, 32);
        int abgr = c.ToAbgr();
        // ABGR: A | (B << 8) | (G << 16) | (R << 24)
        // 32 | (64 << 8) | (128 << 16) | (255 << 24) = 0xFF804020
        Assert.Equal(unchecked((int)0xFF804020), abgr);
    }

    [Fact]
    public void TestColorToArray()
    {
        var c = new Color(255, 128, 64, 32);
        var arr = c.ToArray();
        Assert.Equal(4, arr.Length);
        Assert.Equal((byte)255, arr[0]);
        Assert.Equal((byte)128, arr[1]);
        Assert.Equal((byte)64, arr[2]);
        Assert.Equal((byte)32, arr[3]);
    }

    [Fact]
    public void TestColorGetBrightness()
    {
        var white = Color.White;
        Assert.Equal(1.0f, white.GetBrightness(), 3);

        var black = Color.Black;
        Assert.Equal(0.0f, black.GetBrightness(), 3);

        var gray = new Color(128, 128, 128, 255);
        Assert.Equal(0.5019608f, gray.GetBrightness(), 3);
    }

    [Fact]
    public void TestColorGetHue()
    {
        var red = Color.Red;
        Assert.Equal(0.0f, red.GetHue(), 3);

        var green = Color.Lime;
        Assert.Equal(120.0f, green.GetHue(), 3);

        var blue = Color.Blue;
        Assert.Equal(240.0f, blue.GetHue(), 3);

        var gray = new Color(128, 128, 128, 255);
        Assert.Equal(0.0f, gray.GetHue(), 3); // No hue for grayscale
    }

    [Fact]
    public void TestColorGetSaturation()
    {
        var red = Color.Red;
        Assert.Equal(1.0f, red.GetSaturation(), 3);

        var gray = new Color(128, 128, 128, 255);
        Assert.Equal(0.0f, gray.GetSaturation(), 3);

        var pink = new Color(255, 192, 203, 255);
        float saturation = pink.GetSaturation();
        Assert.True(saturation >= 0 && saturation <= 1);
    }

    [Fact]
    public void TestColorLerp()
    {
        var start = new Color(0, 0, 0, 0);
        var end = new Color(200, 100, 50, 255);
        
        var mid = Color.Lerp(start, end, 0.5f);
        Assert.Equal((byte)100, mid.R);
        Assert.Equal((byte)50, mid.G);
        Assert.Equal((byte)25, mid.B);
        Assert.Equal((byte)127, mid.A);

        var atStart = Color.Lerp(start, end, 0.0f);
        Assert.Equal(start, atStart);

        var atEnd = Color.Lerp(start, end, 1.0f);
        Assert.Equal(end, atEnd);
    }

    [Fact]
    public void TestColorSmoothStep()
    {
        var start = new Color(0, 0, 0, 0);
        var end = new Color(200, 100, 50, 255);
        
        var mid = Color.SmoothStep(start, end, 0.5f);
        // SmoothStep should give smoother interpolation than Lerp
        Assert.True(mid.R > 0 && mid.R < 200);
        Assert.True(mid.G > 0 && mid.G < 100);
    }

    [Fact]
    public void TestColorMin()
    {
        var c1 = new Color(200, 50, 150, 100);
        var c2 = new Color(100, 150, 75, 200);
        var result = Color.Min(c1, c2);
        Assert.Equal((byte)100, result.R);
        Assert.Equal((byte)50, result.G);
        Assert.Equal((byte)75, result.B);
        Assert.Equal((byte)100, result.A);
    }

    [Fact]
    public void TestColorMax()
    {
        var c1 = new Color(200, 50, 150, 100);
        var c2 = new Color(100, 150, 75, 200);
        var result = Color.Max(c1, c2);
        Assert.Equal((byte)200, result.R);
        Assert.Equal((byte)150, result.G);
        Assert.Equal((byte)150, result.B);
        Assert.Equal((byte)200, result.A);
    }

    [Fact]
    public void TestColorAdjustContrast()
    {
        var c = new Color(128, 128, 128, 128);
        var result = Color.AdjustContrast(c, 2.0f);
        // Adjusting contrast of gray should still produce gray-ish results
        Assert.True(result.R < 255);
        Assert.True(result.G < 255);
        Assert.True(result.B < 255);
    }

    [Fact]
    public void TestColorAdjustSaturation()
    {
        var c = new Color(255, 128, 64, 255);
        var desaturated = Color.AdjustSaturation(c, 0.0f);
        // Zero saturation should give grayscale
        Assert.Equal(desaturated.R, desaturated.G);
        Assert.Equal(desaturated.G, desaturated.B);

        var saturated = Color.AdjustSaturation(c, 2.0f);
        // Increased saturation should enhance color differences
        Assert.NotEqual(c, saturated);
    }

    [Fact]
    public void TestColorOperatorAddition()
    {
        var c1 = new Color(100, 50, 25, 10);
        var c2 = new Color(50, 100, 150, 200);
        var result = c1 + c2;
        Assert.Equal((byte)150, result.R);
        Assert.Equal((byte)150, result.G);
        Assert.Equal((byte)175, result.B);
        Assert.Equal((byte)210, result.A);
    }

    [Fact]
    public void TestColorOperatorSubtraction()
    {
        var c1 = new Color(200, 150, 100, 50);
        var c2 = new Color(50, 50, 50, 25);
        var result = c1 - c2;
        Assert.Equal((byte)150, result.R);
        Assert.Equal((byte)100, result.G);
        Assert.Equal((byte)50, result.B);
        Assert.Equal((byte)25, result.A);
    }

    [Fact]
    public void TestColorOperatorMultiply()
    {
        var c = new Color(100, 80, 60, 40);
        var result = c * 2.0f;
        Assert.Equal((byte)200, result.R);
        Assert.Equal((byte)160, result.G);
        Assert.Equal((byte)120, result.B);
        Assert.Equal((byte)80, result.A);

        var result2 = 2.0f * c;
        Assert.Equal((byte)200, result2.R);
        Assert.Equal((byte)160, result2.G);
        Assert.Equal((byte)120, result2.B);
        Assert.Equal((byte)80, result2.A);
    }

    [Fact]
    public void TestColorOperatorModulate()
    {
        var c1 = new Color(255, 128, 64, 32);
        var c2 = new Color(128, 128, 128, 128);
        var result = c1 * c2;
        Assert.Equal((byte)128, result.R);
        Assert.Equal((byte)64, result.G);
        Assert.Equal((byte)32, result.B);
        Assert.Equal((byte)16, result.A);
    }

    [Fact]
    public void TestColorUnaryOperators()
    {
        var c = new Color(100, 50, 25, 10);
        var positive = +c;
        Assert.Equal(c, positive);

        var negative = -c;
        // Negate uses: new Color(-value.R, -value.G, -value.B, -value.A)
        // which passes int to float constructor, ToByte clamps negative values to 0
        Assert.Equal((byte)0, negative.R);
        Assert.Equal((byte)0, negative.G);
        Assert.Equal((byte)0, negative.B);
        Assert.Equal((byte)0, negative.A);
    }

    [Fact]
    public void TestColorConversions()
    {
        var c = new Color(255, 128, 64, 32);

        // To Color3
        var c3 = (Color3)c;
        Assert.Equal(1.0f, c3.R, 3);
        Assert.Equal(128f / 255f, c3.G, 3);
        Assert.Equal(64f / 255f, c3.B, 3);

        // To Color4
        var c4 = (Color4)c;
        Assert.Equal(1.0f, c4.R, 3);
        Assert.Equal(128f / 255f, c4.G, 3);
        Assert.Equal(64f / 255f, c4.B, 3);
        Assert.Equal(32f / 255f, c4.A, 3);

        // To Vector3
        var v3 = (Vector3)c;
        Assert.Equal(1.0f, v3.X, 3);
        Assert.Equal(128f / 255f, v3.Y, 3);
        Assert.Equal(64f / 255f, v3.Z, 3);

        // To Vector4
        var v4 = (Vector4)c;
        Assert.Equal(1.0f, v4.X, 3);
        Assert.Equal(128f / 255f, v4.Y, 3);
        Assert.Equal(64f / 255f, v4.Z, 3);
        Assert.Equal(32f / 255f, v4.W, 3);

        // To int
        int intValue = (int)c;
        Assert.Equal(c.ToRgba(), intValue);

        // From int
        var fromInt = (Color)0x204080FF;
        Assert.Equal((byte)0xFF, fromInt.R);
        Assert.Equal((byte)0x80, fromInt.G);
        Assert.Equal((byte)0x40, fromInt.B);
        Assert.Equal((byte)0x20, fromInt.A);
    }

    [Fact]
    public void TestColorFromAbgr()
    {
        // ABGR format: A in bits 0-7, B in bits 8-15, G in bits 16-23, R in bits 24-31
        var c1 = Color.FromAbgr(0xFF804020);
        Assert.Equal((byte)0xFF, c1.R);
        Assert.Equal((byte)0x80, c1.G);
        Assert.Equal((byte)0x40, c1.B);
        Assert.Equal((byte)0x20, c1.A);

        var c2 = Color.FromAbgr(0xFF0000FFu);
        Assert.Equal((byte)0xFF, c2.R);
        Assert.Equal((byte)0x00, c2.G);
        Assert.Equal((byte)0x00, c2.B);
        Assert.Equal((byte)0xFF, c2.A);
    }

    [Fact]
    public void TestColorOutParameters()
    {
        var c1 = new Color(100, 50, 25, 10);
        var c2 = new Color(50, 100, 150, 200);

        Color.Add(ref c1, ref c2, out Color addResult);
        Assert.Equal((byte)150, addResult.R);
        Assert.Equal((byte)150, addResult.G);
        Assert.Equal((byte)175, addResult.B);
        Assert.Equal((byte)210, addResult.A);

        Color.Subtract(ref c1, ref c2, out Color subResult);
        Assert.Equal((byte)50, subResult.R);
        Assert.Equal((byte)206, subResult.G);  // Underflow wraps
        Assert.Equal((byte)131, subResult.B);  // Underflow wraps
        Assert.Equal((byte)66, subResult.A);   // Underflow wraps

        Color.Modulate(ref c1, ref c2, out Color modResult);
        Assert.True(modResult.R < c1.R);
        Assert.True(modResult.G < c1.G);

        Color.Scale(ref c1, 2.0f, out Color scaleResult);
        Assert.Equal((byte)200, scaleResult.R);
        Assert.Equal((byte)100, scaleResult.G);

        Color.Negate(ref c1, out Color negResult);
        Assert.Equal((byte)155, negResult.R);
        Assert.Equal((byte)205, negResult.G);

        var min = new Color(75, 75, 75, 75);
        var max = new Color(125, 125, 125, 125);
        Color.Clamp(ref c1, ref min, ref max, out Color clampResult);
        Assert.Equal((byte)100, clampResult.R);
        Assert.Equal((byte)75, clampResult.G);
        Assert.Equal((byte)75, clampResult.B);
        Assert.Equal((byte)75, clampResult.A);

        var start = new Color(0, 0, 0, 0);
        var end = new Color(200, 100, 50, 255);
        Color.Lerp(ref start, ref end, 0.5f, out Color lerpResult);
        Assert.Equal((byte)100, lerpResult.R);
        Assert.Equal((byte)50, lerpResult.G);

        Color.SmoothStep(ref start, ref end, 0.5f, out Color smoothResult);
        Assert.True(smoothResult.R > 0 && smoothResult.R < 200);

        Color.Min(ref c1, ref c2, out Color minResult);
        Assert.Equal((byte)50, minResult.R);
        Assert.Equal((byte)50, minResult.G);

        Color.Max(ref c1, ref c2, out Color maxResult);
        Assert.Equal((byte)100, maxResult.R);
        Assert.Equal((byte)100, maxResult.G);

        Color.AdjustContrast(ref c1, 1.5f, out Color contrastResult);
        Assert.NotEqual(c1, contrastResult);

        Color.AdjustSaturation(ref c1, 0.5f, out Color satResult);
        Assert.NotEqual(c1, satResult);
    }

    [Fact]
    public void TestColorGetHashCode()
    {
        var c1 = new Color(255, 128, 64, 32);
        var c2 = new Color(255, 128, 64, 32);
        var c3 = new Color(255, 128, 64, 31);

        Assert.Equal(c1.GetHashCode(), c2.GetHashCode());
        Assert.NotEqual(c1.GetHashCode(), c3.GetHashCode());
    }

    [Fact]
    public void TestColorToString()
    {
        var c = new Color(255, 128, 64, 32);
        var str = c.ToString();
        Assert.NotNull(str);
        Assert.NotEmpty(str);
    }

    #endregion

    #region HSV Conversion Tests

    [Fact]
    public void TestRGB2HSVConversion()
    {
        Assert.Equal(new ColorHSV(312, 1, 1, 1), ColorHSV.FromColor(new Color4(1, 0, 0.8f, 1)));
        Assert.Equal(new ColorHSV(0, 0, 0, 1), ColorHSV.FromColor(Color.Black));
        Assert.Equal(new ColorHSV(0, 0, 1, 1), ColorHSV.FromColor(Color.White));
        Assert.Equal(new ColorHSV(0, 1, 1, 1), ColorHSV.FromColor(Color.Red));
        Assert.Equal(new ColorHSV(120, 1, 1, 1), ColorHSV.FromColor(Color.Lime));
        Assert.Equal(new ColorHSV(240, 1, 1, 1), ColorHSV.FromColor(Color.Blue));
        Assert.Equal(new ColorHSV(60, 1, 1, 1), ColorHSV.FromColor(Color.Yellow));
        Assert.Equal(new ColorHSV(180, 1, 1, 1), ColorHSV.FromColor(Color.Cyan));
        Assert.Equal(new ColorHSV(300, 1, 1, 1), ColorHSV.FromColor(Color.Magenta));
        Assert.Equal(new ColorHSV(0, 0, 0.7529412f, 1), ColorHSV.FromColor(Color.Silver));
        Assert.Equal(new ColorHSV(0, 0, 0.5019608f, 1), ColorHSV.FromColor(Color.Gray));
        Assert.Equal(new ColorHSV(0, 1, 0.5019608f, 1), ColorHSV.FromColor(Color.Maroon));
    }

    [Fact]
    public void TestHSV2RGBConversion()
    {
        Assert.Equal(Color.Black.ToColor4(), ColorHSV.FromColor(Color.Black).ToColor());
        Assert.Equal(Color.White.ToColor4(), ColorHSV.FromColor(Color.White).ToColor());
        Assert.Equal(Color.Red.ToColor4(), ColorHSV.FromColor(Color.Red).ToColor());
        Assert.Equal(Color.Lime.ToColor4(), ColorHSV.FromColor(Color.Lime).ToColor());
        Assert.Equal(Color.Blue.ToColor4(), ColorHSV.FromColor(Color.Blue).ToColor());
        Assert.Equal(Color.Silver.ToColor4(), ColorHSV.FromColor(Color.Silver).ToColor());
        Assert.Equal(Color.Maroon.ToColor4(), ColorHSV.FromColor(Color.Maroon).ToColor());
        Assert.Equal(new Color(184, 209, 219, 255).ToRgba(), ColorHSV.FromColor(new Color(184, 209, 219, 255)).ToColor().ToRgba());
    }

    #endregion
}
