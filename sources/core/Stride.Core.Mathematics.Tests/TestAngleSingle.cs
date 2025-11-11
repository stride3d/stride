// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestAngleSingle
{
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

    [Fact]
    public void TestAngleSingleStaticAngles()
    {
        Assert.Equal(0.0f, AngleSingle.ZeroAngle.Radians);
        Assert.Equal(MathUtil.PiOverTwo, AngleSingle.RightAngle.Radians, 3);
        Assert.Equal(MathUtil.Pi, AngleSingle.StraightAngle.Radians, 3);
        Assert.Equal(MathUtil.TwoPi, AngleSingle.FullRotationAngle.Radians, 3);
    }

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

    [Fact]
    public void TestAngleSingleWrapStatic()
    {
        var angle = AngleSingle.Wrap(new AngleSingle(450, AngleType.Degree));
        Assert.Equal(90.0f, angle.Degrees, 3);

        var angle2 = AngleSingle.WrapPositive(new AngleSingle(-90, AngleType.Degree));
        Assert.Equal(270.0f, angle2.Degrees, 3);
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
    public void TestAngleSingleIsOblique()
    {
        var angle1 = new AngleSingle(45, AngleType.Degree);
        Assert.True(angle1.IsOblique);

        var angle2 = new AngleSingle(90, AngleType.Degree);
        Assert.False(angle2.IsOblique);
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

    [Fact]
    public void TestAngleSingleNegation()
    {
        var angle = new AngleSingle(45, AngleType.Degree);
        var result = -angle;
        Assert.Equal(-45.0f, result.Degrees, 3);
    }

    [Fact]
    public void TestAngleSingleUnaryPlus()
    {
        var angle = new AngleSingle(45, AngleType.Degree);
        var result = +angle;
        Assert.Equal(45.0f, result.Degrees, 3);
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
    public void TestAngleSingleCompareTo()
    {
        var angle1 = new AngleSingle(30, AngleType.Degree);
        var angle2 = new AngleSingle(60, AngleType.Degree);
        var angle3 = new AngleSingle(30, AngleType.Degree);

        Assert.True(angle1.CompareTo(angle2) < 0);
        Assert.True(angle2.CompareTo(angle1) > 0);
        Assert.Equal(0, angle1.CompareTo(angle3));
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
    public void TestAngleSingleToString()
    {
        var angle = new AngleSingle(45, AngleType.Degree);
        var str = angle.ToString();
        Assert.NotEmpty(str);
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
}
