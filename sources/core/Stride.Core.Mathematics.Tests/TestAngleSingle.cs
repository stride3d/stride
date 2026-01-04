// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestAngleSingle
{
    // ========================================
    // Constructors
    // ========================================

    [Fact]
    public void TestAngleSingleConstruction()
    {
        var angle1 = new AngleSingle(90, AngleType.Degree);
        Assert.Equal(MathUtil.PiOverTwo, angle1.Radians, 3);

        var angle2 = new AngleSingle(MathUtil.Pi, AngleType.Radian);
        Assert.Equal(MathUtil.Pi, angle2.Radians, 3);

        var angle3 = new AngleSingle(0.25f, AngleType.Revolution);
        Assert.Equal(MathUtil.PiOverTwo, angle3.Radians, 3);

        var angle4 = new AngleSingle(100, AngleType.Gradian);
        Assert.Equal(MathUtil.PiOverTwo, angle4.Radians, 3);
    }

    [Fact]
    public void TestAngleSingleArcLengthConstruction()
    {
        // Angle = arc length / radius
        var angle = new AngleSingle(MathUtil.Pi, 1.0f); // π radians
        Assert.Equal(MathUtil.Pi, angle.Radians, 3);
    }

    // ========================================
    // Instance methods
    // ========================================

    [Fact]
    public void TestAngleSingleWrap()
    {
        var angle = new AngleSingle(450, AngleType.Degree);
        angle.Wrap();
        Assert.Equal(90.0f, angle.Degrees, 3);

        var angle2 = new AngleSingle(-90, AngleType.Degree);
        angle2.Wrap();
        Assert.Equal(-90.0f, angle2.Degrees, 3);
    }

    [Fact]
    public void TestAngleSingleWrapPositive()
    {
        var angle = new AngleSingle(450, AngleType.Degree);
        angle.WrapPositive();
        Assert.Equal(90.0f, angle.Degrees, 3);

        var angle2 = new AngleSingle(-90, AngleType.Degree);
        angle2.WrapPositive();
        Assert.Equal(270.0f, angle2.Degrees, 3);
    }

    // ========================================
    // Properties
    // ========================================

    [Fact]
    public void TestAngleSingleProperties()
    {
        var angle = new AngleSingle(90, AngleType.Degree);

        Assert.Equal(90.0f, angle.Degrees, 3);
        Assert.Equal(0.25f, angle.Revolutions, 3);
        Assert.InRange(angle.Gradians, 99.9f, 100.1f);
        Assert.Equal(MathUtil.PiOverTwo, angle.Radians, 3);
    }

    [Fact]
    public void TestAngleSingleRevolutionsConversion()
    {
        var angle = new AngleSingle(0.5f, AngleType.Revolution);
        Assert.InRange(angle.Degrees, 179.9f, 180.1f);

        // Test revolutions setter
        angle.Revolutions = 0.25f;
        Assert.InRange(angle.Degrees, 89.9f, 90.1f);
    }

    [Fact]
    public void TestAngleSingleGradiansConversion()
    {
        // Test that gradians conversion works properly
        var angle = new AngleSingle(100, AngleType.Gradian);
        Assert.InRange(angle.Degrees, 89.9f, 90.1f);

        // Test gradians property
        var angle2 = new AngleSingle(90, AngleType.Degree);
        Assert.InRange(angle2.Gradians, 99.9f, 100.1f);
    }

    [Fact]
    public void TestAngleSingleMinutesProperty()
    {
        // Test getter
        var angle = new AngleSingle(30.5f, AngleType.Degree);
        Assert.InRange(angle.Minutes, 29.5f, 30.5f);

        // Test setter with value in range (-60, 60)
        var angle2 = new AngleSingle(45, AngleType.Degree);
        angle2.Minutes = 30;
        Assert.InRange(angle2.Degrees, 45.4f, 45.6f);

        // Test negative angle minutes
        var angle3 = new AngleSingle(-30.5f, AngleType.Degree);
        var minutes = angle3.Minutes;
        Assert.InRange(minutes, -31f, -29f);
    }

    [Fact]
    public void TestAngleSingleSecondsProperty()
    {
        // Test getter for positive angle
        var angle = new AngleSingle(30.50833f, AngleType.Degree); // ~30° 30' 30"
        var seconds = angle.Seconds;
        Assert.InRange(seconds, 25f, 35f);

        // Test setter
        var angle2 = new AngleSingle(45, AngleType.Degree);
        angle2.Seconds = 30;
        Assert.InRange(angle2.Degrees, 44.99f, 45.01f);

        // Test negative angle seconds
        var angle3 = new AngleSingle(-30.50833f, AngleType.Degree);
        var negSeconds = angle3.Seconds;
        Assert.InRange(negSeconds, -35f, -25f);
    }

    [Fact]
    public void TestAngleSingleMilliradiansProperty()
    {
        // Test getter
        var angle = new AngleSingle(MathUtil.Pi, AngleType.Radian);
        var milliradians = angle.Milliradians;
        Assert.NotEqual(0, milliradians);

        // Test setter
        var angle2 = new AngleSingle(0, AngleType.Radian);
        angle2.Milliradians = 1000;
        Assert.NotEqual(0, angle2.Radians);
    }

    [Fact]
    public void TestAngleSingleIsRight()
    {
        var angle1 = new AngleSingle(90, AngleType.Degree);
        Assert.True(angle1.IsRight);

        var angle2 = new AngleSingle(45, AngleType.Degree);
        Assert.False(angle2.IsRight);
    }

    [Fact]
    public void TestAngleSingleIsStraight()
    {
        var angle1 = new AngleSingle(180, AngleType.Degree);
        Assert.True(angle1.IsStraight);

        var angle2 = new AngleSingle(90, AngleType.Degree);
        Assert.False(angle2.IsStraight);
    }

    [Fact]
    public void TestAngleSingleIsFullRotation()
    {
        var angle1 = new AngleSingle(360, AngleType.Degree);
        Assert.True(angle1.IsFullRotation);

        var angle2 = new AngleSingle(180, AngleType.Degree);
        Assert.False(angle2.IsFullRotation);
    }

    [Fact]
    public void TestAngleSingleIsOblique()
    {
        var angle1 = new AngleSingle(45, AngleType.Degree);
        Assert.True(angle1.IsOblique);

        var angle2 = new AngleSingle(90, AngleType.Degree);
        Assert.False(angle2.IsOblique);
    }

    [Fact]
    public void TestAngleSingleIsObliqueWithMultiplesOf90()
    {
        // IsOblique checks if the angle is not a right angle (90 degrees)
        // when wrapped to positive range
        var angle1 = new AngleSingle(90, AngleType.Degree);
        Assert.False(angle1.IsOblique);

        // 180, 270, etc. when wrapped are oblique (not 90 degrees)
        var angle2 = new AngleSingle(45, AngleType.Degree);
        Assert.True(angle2.IsOblique);

        var angle3 = new AngleSingle(135, AngleType.Degree);
        Assert.True(angle3.IsOblique);
    }

    [Fact]
    public void TestAngleSingleIsAcute()
    {
        var angle1 = new AngleSingle(45, AngleType.Degree);
        Assert.True(angle1.IsAcute);

        var angle2 = new AngleSingle(90, AngleType.Degree);
        Assert.False(angle2.IsAcute);
    }

    [Fact]
    public void TestAngleSingleIsObtuse()
    {
        var angle1 = new AngleSingle(120, AngleType.Degree);
        Assert.True(angle1.IsObtuse);

        var angle2 = new AngleSingle(90, AngleType.Degree);
        Assert.False(angle2.IsObtuse);
    }

    [Fact]
    public void TestAngleSingleIsReflex()
    {
        var angle1 = new AngleSingle(270, AngleType.Degree);
        Assert.True(angle1.IsReflex);

        var angle2 = new AngleSingle(90, AngleType.Degree);
        Assert.False(angle2.IsReflex);
    }

    [Fact]
    public void TestAngleSingleComplement()
    {
        var angle = new AngleSingle(30, AngleType.Degree);
        var complement = angle.Complement;
        Assert.Equal(60.0f, complement.Degrees, 3);
    }

    [Fact]
    public void TestAngleSingleSupplement()
    {
        var angle = new AngleSingle(30, AngleType.Degree);
        var supplement = angle.Supplement;
        Assert.Equal(150.0f, supplement.Degrees, 3);
    }

    // ========================================
    // Static methods
    // ========================================

    [Fact]
    public void TestAngleSingleStaticAngles()
    {
        Assert.Equal(0.0f, AngleSingle.ZeroAngle.Radians);
        Assert.Equal(MathUtil.PiOverTwo, AngleSingle.RightAngle.Radians, 3);
        Assert.Equal(MathUtil.Pi, AngleSingle.StraightAngle.Radians, 3);
        Assert.Equal(MathUtil.TwoPi, AngleSingle.FullRotationAngle.Radians, 3);
    }

    [Fact]
    public void TestAngleSingleWrapStatic()
    {
        var angle = AngleSingle.Wrap(new AngleSingle(450, AngleType.Degree));
        Assert.Equal(90.0f, angle.Degrees, 3);

        var angle2 = AngleSingle.WrapPositive(new AngleSingle(-90, AngleType.Degree));
        Assert.Equal(270.0f, angle2.Degrees, 3);
    }

    [Fact]
    public void TestAngleSingleMinMax()
    {
        var angle1 = new AngleSingle(30, AngleType.Degree);
        var angle2 = new AngleSingle(60, AngleType.Degree);

        var min = AngleSingle.Min(angle1, angle2);
        Assert.Equal(30.0f, min.Degrees, 3);

        var max = AngleSingle.Max(angle1, angle2);
        Assert.Equal(60.0f, max.Degrees, 3);
    }

    [Fact]
    public void TestAngleSingleStaticAdd()
    {
        var angle1 = new AngleSingle(30, AngleType.Degree);
        var angle2 = new AngleSingle(45, AngleType.Degree);
        var result = AngleSingle.Add(angle1, angle2);
        Assert.Equal(75.0f, result.Degrees, 3);
    }

    [Fact]
    public void TestAngleSingleStaticSubtract()
    {
        var angle1 = new AngleSingle(90, AngleType.Degree);
        var angle2 = new AngleSingle(30, AngleType.Degree);
        var result = AngleSingle.Subtract(angle1, angle2);
        Assert.Equal(60.0f, result.Degrees, 3);
    }

    [Fact]
    public void TestAngleSingleStaticMultiply()
    {
        var angle1 = new AngleSingle(30, AngleType.Degree);
        var angle2 = new AngleSingle(2, AngleType.Radian);
        var result = AngleSingle.Multiply(angle1, angle2);
        Assert.NotEqual(0, result.Radians);
    }

    [Fact]
    public void TestAngleSingleStaticDivide()
    {
        var angle1 = new AngleSingle(90, AngleType.Degree);
        var angle2 = new AngleSingle(2, AngleType.Radian);
        var result = AngleSingle.Divide(angle1, angle2);
        Assert.NotEqual(0, result.Radians);
    }

    // ========================================
    // Operators
    // ========================================

    [Fact]
    public void TestAngleSingleEquality()
    {
        var angle1 = new AngleSingle(45, AngleType.Degree);
        var angle2 = new AngleSingle(45, AngleType.Degree);
        var angle3 = new AngleSingle(90, AngleType.Degree);

        Assert.True(angle1 == angle2);
        Assert.False(angle1 == angle3);
        Assert.False(angle1 != angle2);
        Assert.True(angle1 != angle3);

        Assert.True(angle1.Equals(angle2));
        Assert.False(angle1.Equals(angle3));
    }

    [Fact]
    public void TestAngleSingleComparison()
    {
        var angle1 = new AngleSingle(30, AngleType.Degree);
        var angle2 = new AngleSingle(60, AngleType.Degree);

        Assert.True(angle1 < angle2);
        Assert.True(angle2 > angle1);
        Assert.True(angle1 <= angle2);
        Assert.True(angle2 >= angle1);
        Assert.True(angle1 <= angle1);
        Assert.True(angle1 >= angle1);
    }

    [Fact]
    public void TestAngleSingleUnaryPlus()
    {
        var angle = new AngleSingle(45, AngleType.Degree);
        var result = +angle;
        Assert.Equal(45.0f, result.Degrees, 3);
    }

    [Fact]
    public void TestAngleSingleNegation()
    {
        var angle = new AngleSingle(45, AngleType.Degree);
        var result = -angle;
        Assert.Equal(-45.0f, result.Degrees, 3);
    }

    [Fact]
    public void TestAngleSingleAddition()
    {
        var angle1 = new AngleSingle(45, AngleType.Degree);
        var angle2 = new AngleSingle(45, AngleType.Degree);
        var result = angle1 + angle2;
        Assert.Equal(90.0f, result.Degrees, 3);

        var result2 = AngleSingle.Add(angle1, angle2);
        Assert.Equal(result.Radians, result2.Radians, 3);
    }

    [Fact]
    public void TestAngleSingleSubtraction()
    {
        var angle1 = new AngleSingle(90, AngleType.Degree);
        var angle2 = new AngleSingle(45, AngleType.Degree);
        var result = angle1 - angle2;
        Assert.Equal(45.0f, result.Degrees, 3);

        var result2 = AngleSingle.Subtract(angle1, angle2);
        Assert.Equal(result.Radians, result2.Radians, 3);
    }

    [Fact]
    public void TestAngleSingleMultiplication()
    {
        var angle1 = new AngleSingle(45, AngleType.Degree);
        var angle2 = new AngleSingle(2, AngleType.Degree);
        var result = angle1 * angle2;

        var result2 = AngleSingle.Multiply(angle1, angle2);
        Assert.Equal(result.Radians, result2.Radians, 3);
    }

    [Fact]
    public void TestAngleSingleDivision()
    {
        var angle1 = new AngleSingle(90, AngleType.Degree);
        var angle2 = new AngleSingle(2, AngleType.Degree);
        var result = angle1 / angle2;

        var result2 = AngleSingle.Divide(angle1, angle2);
        Assert.Equal(result.Radians, result2.Radians, 3);
    }

    // ========================================
    // IComparable
    // ========================================

    [Fact]
    public void TestAngleSingleCompareToObject()
    {
        var angle = new AngleSingle(45, AngleType.Degree);

        // Test with null
        Assert.Equal(1, angle.CompareTo(null));

        // Test with same type
        var other = new AngleSingle(30, AngleType.Degree);
        Assert.True(angle.CompareTo((object)other) > 0);

        // Test with invalid type
        Assert.Throws<ArgumentException>(() => angle.CompareTo(45));
    }

    [Fact]
    public void TestAngleSingleCompareTo()
    {
        var angle1 = new AngleSingle(30, AngleType.Degree);
        var angle2 = new AngleSingle(60, AngleType.Degree);
        var angle3 = new AngleSingle(30, AngleType.Degree);

        Assert.True(angle1.CompareTo(angle2) < 0);
        Assert.True(angle2.CompareTo(angle1) > 0);
        Assert.Equal(0, angle1.CompareTo(angle3));
    }

    // ========================================
    // Object overrides
    // ========================================

    [Fact]
    public void TestAngleSingleToString()
    {
        var angle = new AngleSingle(45, AngleType.Degree);
        var str = angle.ToString();
        Assert.NotEmpty(str);
    }

    [Fact]
    public void TestAngleSingleToStringWithFormat()
    {
        var angle = new AngleSingle(45.123f, AngleType.Degree);

        var str1 = angle.ToString("0.0", null);
        Assert.Contains("°", str1);
        Assert.Contains("45", str1);

        var str2 = angle.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains("°", str2);
    }

    [Fact]
    public void TestAngleSingleHashCode()
    {
        var angle1 = new AngleSingle(45, AngleType.Degree);
        var angle2 = new AngleSingle(45, AngleType.Degree);
        var angle3 = new AngleSingle(90, AngleType.Degree);

        Assert.Equal(angle1.GetHashCode(), angle2.GetHashCode());
        Assert.NotEqual(angle1.GetHashCode(), angle3.GetHashCode());
    }

    [Fact]
    public void TestAngleSingleEqualsObject()
    {
        var angle1 = new AngleSingle(45, AngleType.Degree);
        var angle2 = new AngleSingle(45, AngleType.Degree);
        var angle3 = new AngleSingle(90, AngleType.Degree);

        // Test with AngleSingle
        Assert.True(angle1.Equals((object)angle2));
        Assert.False(angle1.Equals((object)angle3));

        // Test with null
        Assert.False(angle1.Equals(null));

        // Test with different type
        Assert.False(angle1.Equals(45));
    }

    // ========================================
    // Boundary conditions and edge cases
    // ========================================

    [Fact]
    public void TestAngleSingleBoundaryConditions()
    {
        // Test zero angle edge cases
        var zero = AngleSingle.ZeroAngle;
        Assert.False(zero.IsAcute);
        Assert.False(zero.IsObtuse);
        Assert.False(zero.IsReflex);

        // Test wrap with exact multiples
        var exactMultiple = new AngleSingle(720, AngleType.Degree);
        exactMultiple.Wrap();
        Assert.InRange(exactMultiple.Degrees, -1f, 1f);

        // Test complement and supplement at boundaries
        var rightAngle = AngleSingle.RightAngle;
        var complement = rightAngle.Complement;
        Assert.InRange(complement.Degrees, -1f, 1f);
    }
}
