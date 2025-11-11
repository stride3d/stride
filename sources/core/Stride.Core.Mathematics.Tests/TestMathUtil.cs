// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestMathUtil
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, MathUtil.PiOverTwo)]
    [InlineData(180, MathUtil.Pi)]
    [InlineData(360, MathUtil.TwoPi)]
    [InlineData(45, MathUtil.PiOverFour)]
    public void TestDegreesToRadians(float degrees, float expectedRadians)
    {
        var radians = MathUtil.DegreesToRadians(degrees);
        Assert.Equal(expectedRadians, radians, 5);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(MathUtil.PiOverTwo, 90)]
    [InlineData(MathUtil.Pi, 180)]
    [InlineData(MathUtil.TwoPi, 360)]
    [InlineData(MathUtil.PiOverFour, 45)]
    public void TestRadiansToDegrees(float radians, float expectedDegrees)
    {
        var degrees = MathUtil.RadiansToDegrees(radians);
        Assert.Equal(expectedDegrees, degrees, 5);
    }

    [Theory]
    [InlineData(0, -1, 1, 0)]
    [InlineData(0.5f, 0, 1, 0.5f)]
    [InlineData(-0.5f, 0, 1, 0)]
    [InlineData(1.5f, 0, 1, 1)]
    [InlineData(0.5f, 0, 0, 0)]
    public void TestClamp(float value, float min, float max, float expected)
    {
        Assert.Equal(expected, MathUtil.Clamp(value, min, max));
    }

    [Theory]
    [InlineData(0, 1, 0, 0)]
    [InlineData(0, 1, 0.5f, 0.5f)]
    [InlineData(0, 1, 1, 1)]
    [InlineData(10, 20, 0.5f, 15)]
    public void TestLerp(float start, float end, float amount, float expected)
    {
        Assert.Equal(expected, MathUtil.Lerp(start, end, amount), 5);
    }

    [Theory]
    [InlineData(5, 0, 10, 0.5f)]
    [InlineData(0, 0, 10, 0)]
    [InlineData(10, 0, 10, 1)]
    [InlineData(15, 0, 10, 1.5f)]
    public void TestInverseLerp(float value, float start, float end, float expected)
    {
        Assert.Equal(expected, MathUtil.InverseLerp(start, end, value), 5);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(MathUtil.ZeroTolerance * 0.5f, true)]
    [InlineData(MathUtil.ZeroTolerance * 2, false)]
    [InlineData(-MathUtil.ZeroTolerance * 0.5f, true)]
    public void TestIsZero(float value, bool expected)
    {
        Assert.Equal(expected, MathUtil.IsZero(value));
    }

    [Theory]
    [InlineData(1.0f, true)]
    [InlineData(1.0f + MathUtil.ZeroTolerance * 0.5f, true)]
    [InlineData(1.0f - MathUtil.ZeroTolerance * 0.5f, true)]
    [InlineData(1.1f, false)]
    public void TestIsOne(float value, bool expected)
    {
        Assert.Equal(expected, MathUtil.IsOne(value));
    }

    [Theory]
    [InlineData(0, 0, 0, true)]
    [InlineData(1, 1, 0.0001f, true)]
    [InlineData(1, 1.1f, 0.0001f, false)]
    [InlineData(1, 1.00001f, 0.0001f, true)]
    public void TestWithinEpsilon(float a, float b, float epsilon, bool expected)
    {
        Assert.Equal(expected, MathUtil.WithinEpsilon(a, b, epsilon));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(1, 1)]
    [InlineData(0.25f, 0.15625f)]
    public void TestSmoothStep(float amount, float expected)
    {
        var result = MathUtil.SmoothStep(amount);
        Assert.Equal(expected, result, 5);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(1, 1)]
    public void TestSmootherStep(float amount, float expected)
    {
        var result = MathUtil.SmootherStep(amount);
        Assert.Equal(expected, result, 5);
    }

    [Theory]
    [InlineData(1.0f, 1.0f, true)]
    [InlineData(1.0f, 1.0000001f, true)]
    [InlineData(1.0f, 2.0f, false)]
    [InlineData(0.0f, 0.0f, true)]
    public void TestNearEqual(float a, float b, bool expected)
    {
        Assert.Equal(expected, MathUtil.NearEqual(a, b));
    }

    [Theory]
    [InlineData(45, 0.125f)]
    [InlineData(90, 0.25f)]
    [InlineData(180, 0.5f)]
    [InlineData(360, 1.0f)]
    public void TestDegreesToRevolutions(float degrees, float expected)
    {
        Assert.Equal(expected, MathUtil.DegreesToRevolutions(degrees), 5);
    }

    [Theory]
    [InlineData(0.125f, 45)]
    [InlineData(0.25f, 90)]
    [InlineData(0.5f, 180)]
    [InlineData(1.0f, 360)]
    public void TestRevolutionsToDegrees(float revolutions, float expected)
    {
        Assert.Equal(expected, MathUtil.RevolutionsToDegrees(revolutions), 5);
    }

    [Theory]
    [InlineData(MathUtil.PiOverFour, 50)]
    [InlineData(MathUtil.PiOverTwo, 100)]
    [InlineData(MathUtil.Pi, 200)]
    public void TestRadiansToGradians(float radians, float expectedGradians)
    {
        Assert.Equal(expectedGradians, MathUtil.RadiansToGradians(radians), 5);
    }

    [Theory]
    [InlineData(50, MathUtil.PiOverFour)]
    [InlineData(100, MathUtil.PiOverTwo)]
    [InlineData(200, MathUtil.Pi)]
    public void TestGradiansToRadians(float gradians, float expectedRadians)
    {
        Assert.Equal(expectedRadians, MathUtil.GradiansToRadians(gradians), 5);
    }

    [Fact]
    public void TestArrayCreation()
    {
        var array = MathUtil.Array(5, 10);
        Assert.Equal(10, array.Length);
        Assert.All(array, x => Assert.Equal(5, x));
    }

    [Theory]
    [InlineData(5, -10, 10, 5)]
    [InlineData(-15, -10, 10, -10)]
    [InlineData(15, -10, 10, 10)]
    public void TestClampInt(int value, int min, int max, int expected)
    {
        Assert.Equal(expected, MathUtil.Clamp(value, min, max));
    }

    [Theory]
    [InlineData(5.5, -10.0, 10.0, 5.5)]
    [InlineData(-15.5, -10.0, 10.0, -10.0)]
    [InlineData(15.5, -10.0, 10.0, 10.0)]
    public void TestClampDouble(double value, double min, double max, double expected)
    {
        Assert.Equal(expected, MathUtil.Clamp(value, min, max));
    }

    [Theory]
    [InlineData(0.0, 10.0, 0.5, 5.0)]
    [InlineData(0.0, 10.0, 0.0, 0.0)]
    [InlineData(0.0, 10.0, 1.0, 10.0)]
    public void TestLerpDouble(double start, double end, double amount, double expected)
    {
        Assert.Equal(expected, MathUtil.Lerp(start, end, amount), 5);
    }

    [Theory]
    [InlineData(5.0, 0.0, 10.0, 0.5)]
    [InlineData(0.0, 0.0, 10.0, 0.0)]
    [InlineData(10.0, 0.0, 10.0, 1.0)]
    public void TestInverseLerpDouble(double value, double start, double end, double expected)
    {
        Assert.Equal(expected, MathUtil.InverseLerp(start, end, value), 5);
    }

    [Fact]
    public void TestLerpByte()
    {
        byte result = MathUtil.Lerp((byte)0, (byte)255, 0.5f);
        Assert.InRange(result, (byte)126, (byte)128);
    }

    [Theory]
    [InlineData(0.125f, MathUtil.PiOverFour)]
    [InlineData(0.25f, MathUtil.PiOverTwo)]
    [InlineData(0.5f, MathUtil.Pi)]
    [InlineData(1.0f, MathUtil.TwoPi)]
    public void TestRevolutionsToRadians(float revolutions, float expectedRadians)
    {
        Assert.Equal(expectedRadians, MathUtil.RevolutionsToRadians(revolutions), 5);
    }

    [Theory]
    [InlineData(0.125f, 50)]
    [InlineData(0.25f, 100)]
    [InlineData(0.5f, 200)]
    [InlineData(1.0f, 400)]
    public void TestRevolutionsToGradians(float revolutions, float expectedGradians)
    {
        Assert.Equal(expectedGradians, MathUtil.RevolutionsToGradians(revolutions), 5);
    }

    [Theory]
    [InlineData(MathUtil.PiOverFour, 0.125f)]
    [InlineData(MathUtil.PiOverTwo, 0.25f)]
    [InlineData(MathUtil.Pi, 0.5f)]
    [InlineData(MathUtil.TwoPi, 1.0f)]
    public void TestRadiansToRevolutions(float radians, float expectedRevolutions)
    {
        Assert.Equal(expectedRevolutions, MathUtil.RadiansToRevolutions(radians), 5);
    }

    [Theory]
    [InlineData(50, 0.125f)]
    [InlineData(100, 0.25f)]
    [InlineData(200, 0.5f)]
    [InlineData(400, 1.0f)]
    public void TestGradiansToRevolutions(float gradians, float expectedRevolutions)
    {
        Assert.Equal(expectedRevolutions, MathUtil.GradiansToRevolutions(gradians), 5);
    }

    [Theory]
    [InlineData(50, 45)]
    [InlineData(100, 90)]
    [InlineData(200, 180)]
    [InlineData(400, 360)]
    public void TestGradiansToDegrees(float gradians, float expectedDegrees)
    {
        Assert.Equal(expectedDegrees, MathUtil.GradiansToDegrees(gradians), 5);
    }

    [Theory]
    [InlineData(5, 0, 10, true)]
    [InlineData(0, 0, 10, true)]
    [InlineData(10, 0, 10, true)]
    [InlineData(-1, 0, 10, false)]
    [InlineData(11, 0, 10, false)]
    public void TestIsInRangeFloat(float value, float min, float max, bool expected)
    {
        Assert.Equal(expected, MathUtil.IsInRange(value, min, max));
    }

    [Theory]
    [InlineData(5, 0, 10, true)]
    [InlineData(0, 0, 10, true)]
    [InlineData(10, 0, 10, true)]
    [InlineData(-1, 0, 10, false)]
    [InlineData(11, 0, 10, false)]
    public void TestIsInRangeInt(int value, int min, int max, bool expected)
    {
        Assert.Equal(expected, MathUtil.IsInRange(value, min, max));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(4, true)]
    [InlineData(8, true)]
    [InlineData(16, true)]
    [InlineData(1024, true)]
    [InlineData(0, false)]
    [InlineData(3, false)]
    [InlineData(5, false)]
    [InlineData(15, false)]
    public void TestIsPow2(int value, bool expected)
    {
        Assert.Equal(expected, MathUtil.IsPow2(value));
    }

    [Theory]
    [InlineData(0.0f, 0.0f)]
    [InlineData(1.0f, 1.0f)]
    [InlineData(0.5f, 0.21404f)] // Approximate conversion
    public void TestSRgbToLinear(float sRgbValue, float expectedLinear)
    {
        Assert.Equal(expectedLinear, MathUtil.SRgbToLinear(sRgbValue), 3);
    }

    [Theory]
    [InlineData(0.0f, 0.0f)]
    [InlineData(1.0f, 1.0f)]
    [InlineData(0.21404f, 0.5f)] // Approximate conversion
    public void TestLinearToSRgb(float linearValue, float expectedSRgb)
    {
        Assert.Equal(expectedSRgb, MathUtil.LinearToSRgb(linearValue), 3);
    }

    [Theory]
    [InlineData(1.0f, 0.0f)]
    [InlineData(2.0f, 1.0f)]
    [InlineData(4.0f, 2.0f)]
    [InlineData(8.0f, 3.0f)]
    [InlineData(16.0f, 4.0f)]
    public void TestLog2Float(float value, float expected)
    {
        Assert.Equal(expected, MathUtil.Log2(value), 5);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(4, 2)]
    [InlineData(8, 3)]
    [InlineData(16, 4)]
    [InlineData(5, 2)] // Floor of log2
    public void TestLog2Int(int value, int expected)
    {
        Assert.Equal(expected, MathUtil.Log2(value));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(5, 8)]
    [InlineData(9, 16)]
    [InlineData(17, 32)]
    public void TestNextPowerOfTwoInt(int value, int expected)
    {
        Assert.Equal(expected, MathUtil.NextPowerOfTwo(value));
    }

    [Theory]
    [InlineData(1.0f, 1.0f)]
    [InlineData(2.0f, 2.0f)]
    [InlineData(3.0f, 4.0f)]
    [InlineData(5.0f, 8.0f)]
    [InlineData(9.0f, 16.0f)]
    public void TestNextPowerOfTwoFloat(float value, float expected)
    {
        Assert.Equal(expected, MathUtil.NextPowerOfTwo(value), 5);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [InlineData(5, 4)]
    [InlineData(9, 8)]
    [InlineData(17, 16)]
    public void TestPreviousPowerOfTwoInt(int value, int expected)
    {
        Assert.Equal(expected, MathUtil.PreviousPowerOfTwo(value));
    }

    [Theory]
    [InlineData(1.0f, 1.0f)]
    [InlineData(2.0f, 2.0f)]
    [InlineData(3.0f, 2.0f)]
    [InlineData(5.0f, 4.0f)]
    [InlineData(9.0f, 8.0f)]
    public void TestPreviousPowerOfTwoFloat(float value, float expected)
    {
        Assert.Equal(expected, MathUtil.PreviousPowerOfTwo(value), 5);
    }

    [Theory]
    [InlineData(5, 4, 8)]
    [InlineData(8, 4, 8)]
    [InlineData(9, 4, 12)]
    [InlineData(0, 4, 0)]
    [InlineData(1, 4, 4)]
    public void TestAlignUp(int value, int alignment, int expected)
    {
        Assert.Equal(expected, MathUtil.AlignUp(value, alignment));
    }

    [Theory]
    [InlineData(5, 4, 4)]
    [InlineData(8, 4, 8)]
    [InlineData(9, 4, 8)]
    [InlineData(0, 4, 0)]
    [InlineData(1, 4, 0)]
    public void TestAlignDown(int value, int alignment, int expected)
    {
        Assert.Equal(expected, MathUtil.AlignDown(value, alignment));
    }

    [Theory]
    [InlineData(0, 4, true)]
    [InlineData(4, 4, true)]
    [InlineData(8, 4, true)]
    [InlineData(1, 4, false)]
    [InlineData(5, 4, false)]
    public void TestIsAligned(int value, int alignment, bool expected)
    {
        Assert.Equal(expected, MathUtil.IsAligned(value, alignment));
    }

    [Theory]
    [InlineData(5.3f, 1.0f, 5.0f)]
    [InlineData(5.7f, 1.0f, 6.0f)]
    [InlineData(5.5f, 1.0f, 6.0f)]
    [InlineData(13.2f, 5.0f, 15.0f)]
    public void TestSnapFloat(float value, float gap, float expected)
    {
        Assert.Equal(expected, MathUtil.Snap(value, gap), 5);
    }

    [Theory]
    [InlineData(5.3, 1.0, 5.0)]
    [InlineData(5.7, 1.0, 6.0)]
    [InlineData(13.2, 5.0, 15.0)]
    public void TestSnapDouble(double value, double gap, double expected)
    {
        Assert.Equal(expected, MathUtil.Snap(value, gap), 5);
    }

    [Fact]
    public void TestSnapVector2()
    {
        var value = new Vector2(5.3f, 7.6f);
        var result = MathUtil.Snap(value, 1.0f);
        Assert.Equal(5.0f, result.X, 5);
        Assert.Equal(8.0f, result.Y, 5);
    }

    [Fact]
    public void TestSnapVector3()
    {
        var value = new Vector3(5.3f, 7.6f, 2.4f);
        var result = MathUtil.Snap(value, 1.0f);
        Assert.Equal(5.0f, result.X, 5);
        Assert.Equal(8.0f, result.Y, 5);
        Assert.Equal(2.0f, result.Z, 5);
    }

    [Fact]
    public void TestSnapVector4()
    {
        var value = new Vector4(5.3f, 7.6f, 2.4f, 9.8f);
        var result = MathUtil.Snap(value, 1.0f);
        Assert.Equal(5.0f, result.X, 5);
        Assert.Equal(8.0f, result.Y, 5);
        Assert.Equal(2.0f, result.Z, 5);
        Assert.Equal(10.0f, result.W, 5);
    }

    [Theory]
    [InlineData(5.0f, 3.0f, 2.0f)]
    [InlineData(7.0f, 3.0f, 1.0f)]
    [InlineData(-1.0f, 3.0f, 2.0f)]
    [InlineData(3.5f, 2.0f, 1.5f)]
    public void TestMod(float value, float divisor, float expected)
    {
        Assert.Equal(expected, MathUtil.Mod(value, divisor), 5);
    }

    [Fact]
    public void TestExpDecayFloat()
    {
        float result = MathUtil.ExpDecay(0.0f, 10.0f, 2.0f, 0.5f);
        // Expected: 10 * (1 - exp(-2 * 0.5)) = 10 * (1 - exp(-1)) ≈ 6.32
        Assert.InRange(result, 6.0f, 7.0f);
    }

    [Fact]
    public void TestExpDecayDouble()
    {
        double result = MathUtil.ExpDecay(0.0, 10.0, 2.0, 0.5);
        // Expected: 10 * (1 - exp(-2 * 0.5)) = 10 * (1 - exp(-1)) ≈ 6.32
        Assert.InRange(result, 6.0, 7.0);
    }

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(MathUtil.ZeroToleranceDouble * 0.5, true)]
    [InlineData(MathUtil.ZeroToleranceDouble * 2, false)]
    [InlineData(-MathUtil.ZeroToleranceDouble * 0.5, true)]
    public void TestIsZeroDouble(double value, bool expected)
    {
        Assert.Equal(expected, MathUtil.IsZero(value));
    }
}
