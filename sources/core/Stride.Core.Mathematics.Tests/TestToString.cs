// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestToString
{
    public TestToString()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [Fact]
    public void TestAngleSingle()
    {
        DoTestToString(AngleSingle.ZeroAngle, ("0°", "0°", "0°", "0°"));
        DoTestToString(AngleSingle.RightAngle, ("90°", "90°", "90°", "90°"));
        DoTestToString(AngleSingle.StraightAngle, ("180°", "180°", "180°", "180°"));
        DoTestToString(AngleSingle.FullRotationAngle, ("360°", "360°", "360°", "360°"));
        DoTestToString(new AngleSingle(123.456f, AngleType.Degree), ("123.46°", "123.46°", "123.46°", "123.46°"));
    }

    [Fact]
    public void TestBoundingBox()
    {
        DoTestToString(
            BoundingBox.Empty,
            (
                "Minimum:X:3.4028235E+38 Y:3.4028235E+38 Z:3.4028235E+38 Maximum:X:-3.4028235E+38 Y:-3.4028235E+38 Z:-3.4028235E+38",
                "Minimum:X:3.4028235E+38 Y:3.4028235E+38 Z:3.4028235E+38 Maximum:X:-3.4028235E+38 Y:-3.4028235E+38 Z:-3.4028235E+38",
                "Minimum:X:3.403E+38 Y:3.403E+38 Z:3.403E+38 Maximum:X:-3.403E+38 Y:-3.403E+38 Z:-3.403E+38",
                "Minimum:X:3.403E+38 Y:3.403E+38 Z:3.403E+38 Maximum:X:-3.403E+38 Y:-3.403E+38 Z:-3.403E+38"
            ), format: "G4"
        );
        DoTestToString(
            new BoundingBox(Vector3.UnitX, Vector3.UnitY),
            (
                "Minimum:X:1 Y:0 Z:0 Maximum:X:0 Y:1 Z:0",
                "Minimum:X:1 Y:0 Z:0 Maximum:X:0 Y:1 Z:0",
                "Minimum:X:1.00 Y:0.00 Z:0.00 Maximum:X:0.00 Y:1.00 Z:0.00",
                "Minimum:X:1.00 Y:0.00 Z:0.00 Maximum:X:0.00 Y:1.00 Z:0.00"
            ), format: "F2"
        );
    }

    [Fact]
    public void TestBoundingBoxExt()
    {
        DoTestToString(
            BoundingBoxExt.Empty,
            (
                "Center:X:0 Y:0 Z:0 Extent:X:-∞ Y:-∞ Z:-∞",
                "Center:X:0 Y:0 Z:0 Extent:X:-Infinity Y:-Infinity Z:-Infinity",
                "Center:X:0.00 Y:0.00 Z:0.00 Extent:X:-∞ Y:-∞ Z:-∞",
                "Center:X:0.00 Y:0.00 Z:0.00 Extent:X:-Infinity Y:-Infinity Z:-Infinity"
            ), format: "F2"
        );
        DoTestToString(
            new BoundingBoxExt(Vector3.UnitX, Vector3.UnitY),
            (
                "Center:X:0.5 Y:0.5 Z:0 Extent:X:-0.5 Y:0.5 Z:0",
                "Center:X:0.5 Y:0.5 Z:0 Extent:X:-0.5 Y:0.5 Z:0",
                "Center:X:0.500 Y:0.500 Z:0.000 Extent:X:-0.500 Y:0.500 Z:0.000",
                "Center:X:0.500 Y:0.500 Z:0.000 Extent:X:-0.500 Y:0.500 Z:0.000"
            ), format: "F3"
        );
    }

    [Fact]
    public void TestBoundingSphere()
    {
        DoTestToString(
            BoundingSphere.Empty,
            (
                "Center:X:0 Y:0 Z:0 Radius:0",
                "Center:X:0 Y:0 Z:0 Radius:0",
                "Center:X:0.00 Y:0.00 Z:0.00 Radius:0.00",
                "Center:X:0.00 Y:0.00 Z:0.00 Radius:0.00"
            ), format: "F2"
        );
        DoTestToString(
            new BoundingSphere(Vector3.UnitX, 1.23456f),
            (
                "Center:X:1 Y:0 Z:0 Radius:1.23456",
                "Center:X:1 Y:0 Z:0 Radius:1.23456",
                "Center:X:1.000 Y:0.000 Z:0.000 Radius:1.235",
                "Center:X:1.000 Y:0.000 Z:0.000 Radius:1.235"
            ), format: "F3"
        );
    }

    [Fact]
    public void TestColor3()
    {
        DoTestToString(Color.AliceBlue.ToColor3(),
            (
                "R:0.9411765 G:0.972549 B:1",
                "R:0.9411765 G:0.972549 B:1",
                "R:0.941 G:0.973 B:1.000",
                "R:0.941 G:0.973 B:1.000"
            ), format: "F3"
        );
        DoTestToString(Color.YellowGreen.ToColor3(),
            (
                "R:0.6039216 G:0.8039216 B:0.19607843",
                "R:0.6039216 G:0.8039216 B:0.19607843",
                "R:0.604 G:0.804 B:0.196",
                "R:0.604 G:0.804 B:0.196"
            ), format: "F3"
        );
    }

    [Fact]
    public void TestColor4()
    {
        DoTestToString(Color.AntiqueWhite.ToColor4(),
            (
                "A:1 R:0.98039216 G:0.92156863 B:0.84313726",
                "A:1 R:0.98039216 G:0.92156863 B:0.84313726",
                "A:1.000 R:0.980 G:0.922 B:0.843",
                "A:1.000 R:0.980 G:0.922 B:0.843"
            ), format: "F3"
        );
        DoTestToString(Color.Yellow.ToColor4(),
            (
                "A:1 R:1 G:1 B:0",
                "A:1 R:1 G:1 B:0",
                "A:1.00 R:1.00 G:1.00 B:0.00",
                "A:1.00 R:1.00 G:1.00 B:0.00"
            ), format: "F2"
        );
    }

    [Fact]
    public void TestColorBGRA()
    {
        DoTestToString(ColorBGRA.FromBgra(Color.Beige.ToBgra()),
            (
                "A:255 R:245 G:245 B:220",
                "A:255 R:245 G:245 B:220",
                "A:255 R:245 G:245 B:220",
                "A:255 R:245 G:245 B:220"
            )
        );
        DoTestToString(ColorBGRA.FromBgra(Color.DarkGoldenrod.ToBgra()),
            (
                "A:255 R:184 G:134 B:11",
                "A:255 R:184 G:134 B:11",
                "A:255 R:184 G:134 B:11",
                "A:255 R:184 G:134 B:11"
            )
        );
    }

    [Fact]
    public void TestColorHSV()
    {
        DoTestToString(ColorHSV.FromColor(Color.Fuchsia),
            (
                "Hue:300 Saturation:1 Value:1 Alpha:1",
                "Hue:300 Saturation:1 Value:1 Alpha:1",
                "Hue:300.0 Saturation:1.0 Value:1.0 Alpha:1.0",
                "Hue:300.0 Saturation:1.0 Value:1.0 Alpha:1.0"
            ), format: "F1"
        );
        DoTestToString(ColorHSV.FromColor(Color.PowderBlue),
            (
                "Hue:186.66667 Saturation:0.23478259 Value:0.9019608 Alpha:1",
                "Hue:186.66667 Saturation:0.23478259 Value:0.9019608 Alpha:1",
                "Hue:186.67 Saturation:0.23 Value:0.90 Alpha:1.00",
                "Hue:186.67 Saturation:0.23 Value:0.90 Alpha:1.00"
            ), format: "F2"
        );
    }

    [Fact]
    public void TestDouble2()
    {
        DoTestToString(Double2.UnitX, ("X:1 Y:0", "X:1 Y:0", "X:1 Y:0", "X:1 Y:0"));
        DoTestToString(Double2.UnitY, ("X:0 Y:1", "X:0 Y:1", "X:0 Y:1", "X:0 Y:1"));
    }

    [Fact]
    public void TestDouble3()
    {
        DoTestToString(Double3.UnitX, ("X:1 Y:0 Z:0", "X:1 Y:0 Z:0", "X:1 Y:0 Z:0", "X:1 Y:0 Z:0"));
        DoTestToString(Double3.UnitY, ("X:0 Y:1 Z:0", "X:0 Y:1 Z:0", "X:0 Y:1 Z:0", "X:0 Y:1 Z:0"));
        DoTestToString(Double3.UnitZ, ("X:0 Y:0 Z:1", "X:0 Y:0 Z:1", "X:0 Y:0 Z:1", "X:0 Y:0 Z:1"));
    }

    [Fact]
    public void TestDouble4()
    {
        DoTestToString(Double4.UnitX, ("X:1 Y:0 Z:0 W:0", "X:1 Y:0 Z:0 W:0", "X:1 Y:0 Z:0 W:0", "X:1 Y:0 Z:0 W:0"));
        DoTestToString(Double4.UnitY, ("X:0 Y:1 Z:0 W:0", "X:0 Y:1 Z:0 W:0", "X:0 Y:1 Z:0 W:0", "X:0 Y:1 Z:0 W:0"));
        DoTestToString(Double4.UnitZ, ("X:0 Y:0 Z:1 W:0", "X:0 Y:0 Z:1 W:0", "X:0 Y:0 Z:1 W:0", "X:0 Y:0 Z:1 W:0"));
        DoTestToString(Double4.UnitW, ("X:0 Y:0 Z:0 W:1", "X:0 Y:0 Z:0 W:1", "X:0 Y:0 Z:0 W:1", "X:0 Y:0 Z:0 W:1"));
    }

    [Fact]
    public void TestHalf()
    {
        DoTestToString(Half.Zero, ("0", "0", "0", "0"));
        DoTestToString(Half.One, ("1", "1", "1", "1"));
    }

    [Fact]
    public void TestHalf2()
    {
        DoTestToString(Half2.Zero, ("0, 0", "0, 0", "0, 0", "0, 0"));
        DoTestToString(Half2.One, ("1, 1", "1, 1", "1, 1", "1, 1"));
        DoTestToString(Half2.UnitX, ("1, 0", "1, 0", "1, 0", "1, 0"));
        DoTestToString(Half2.UnitY, ("0, 1", "0, 1", "0, 1", "0, 1"));
        DoTestToString(
            new Half2(-0.1f, 0.3f),
            (
                "-0.099975586, 0.2998047",
                "-0.099975586, 0.2998047",
                "-0.1, 0.3",
                "-0.1, 0.3"
            )
        );
        DoTestToString(
            new Half2(-0.1f, 0.3f),
            (
                "-0.099975586, 0.2998047",
                "-0,099975586; 0,2998047",
                "-0.1, 0.3",
                "-0,1; 0,3"
            ), culture: CultureInfo.GetCultureInfo("fr-FR")
        );
    }

    [Fact]
    public void TestHalf3()
    {
        DoTestToString(Half3.Zero, ("0, 0, 0", "0, 0, 0", "0, 0, 0", "0, 0, 0"));
        DoTestToString(Half3.One, ("1, 1, 1", "1, 1, 1", "1, 1, 1", "1, 1, 1"));
        DoTestToString(Half3.UnitX, ("1, 0, 0", "1, 0, 0", "1, 0, 0", "1, 0, 0"));
        DoTestToString(Half3.UnitY, ("0, 1, 0", "0, 1, 0", "0, 1, 0", "0, 1, 0"));
        DoTestToString(Half3.UnitZ, ("0, 0, 1", "0, 0, 1", "0, 0, 1", "0, 0, 1"));
        DoTestToString(
            new Half3(-0.1f, 0.3f, -0.2f),
            (
                "-0.099975586, 0.2998047, -0.19995117",
                "-0.099975586, 0.2998047, -0.19995117",
                "-0.1, 0.3, -0.2",
                "-0.1, 0.3, -0.2"
            )
        );
        DoTestToString(
            new Half3(-0.1f, 0.3f, -0.2f),
            (
                "-0.099975586, 0.2998047, -0.19995117",
                "-0,099975586; 0,2998047; -0,19995117",
                "-0.1, 0.3, -0.2",
                "-0,1; 0,3; -0,2"
            ), culture: CultureInfo.GetCultureInfo("fr-FR")
        );
    }

    [Fact]
    public void TestHalf4()
    {
        DoTestToString(Half4.Zero, ("0, 0, 0, 0", "0, 0, 0, 0", "0, 0, 0, 0", "0, 0, 0, 0"));
        DoTestToString(Half4.One, ("1, 1, 1, 1", "1, 1, 1, 1", "1, 1, 1, 1", "1, 1, 1, 1"));
        DoTestToString(Half4.UnitX, ("1, 0, 0, 0", "1, 0, 0, 0", "1, 0, 0, 0", "1, 0, 0, 0"));
        DoTestToString(Half4.UnitY, ("0, 1, 0, 0", "0, 1, 0, 0", "0, 1, 0, 0", "0, 1, 0, 0"));
        DoTestToString(Half4.UnitZ, ("0, 0, 1, 0", "0, 0, 1, 0", "0, 0, 1, 0", "0, 0, 1, 0"));
        DoTestToString(Half4.UnitW, ("0, 0, 0, 1", "0, 0, 0, 1", "0, 0, 0, 1", "0, 0, 0, 1"));
        DoTestToString(
            new Half4(-0.1f, 0.3f, -0.2f, -0.4f),
            (
                "-0.099975586, 0.2998047, -0.19995117, -0.39990234",
                "-0.099975586, 0.2998047, -0.19995117, -0.39990234",
                "-0.1, 0.3, -0.2, -0.4",
                "-0.1, 0.3, -0.2, -0.4"
            )
        );
        DoTestToString(
            new Half4(-0.1f, 0.3f, -0.2f, -0.4f),
            (
                "-0.099975586, 0.2998047, -0.19995117, -0.39990234",
                "-0,099975586; 0,2998047; -0,19995117; -0,39990234",
                "-0.1, 0.3, -0.2, -0.4",
                "-0,1; 0,3; -0,2; -0,4"
            ), culture: CultureInfo.GetCultureInfo("fr-FR")
        );
    }

    [Fact]
    public void TestInt2()
    {
        DoTestToString(Int2.Zero, ("X:0 Y:0", "X:0 Y:0", "X:0 Y:0", "X:0 Y:0"));
        DoTestToString(Int2.One, ("X:1 Y:1", "X:1 Y:1", "X:1 Y:1", "X:1 Y:1"));
        DoTestToString(Int2.UnitX, ("X:1 Y:0", "X:1 Y:0", "X:1 Y:0", "X:1 Y:0"));
        DoTestToString(Int2.UnitY, ("X:0 Y:1", "X:0 Y:1", "X:0 Y:1", "X:0 Y:1"));
    }

    [Fact]
    public void TestInt3()
    {
        DoTestToString(Int3.Zero, ("X:0 Y:0 Z:0", "X:0 Y:0 Z:0", "X:0 Y:0 Z:0", "X:0 Y:0 Z:0"));
        DoTestToString(Int3.One, ("X:1 Y:1 Z:1", "X:1 Y:1 Z:1", "X:1 Y:1 Z:1", "X:1 Y:1 Z:1"));
        DoTestToString(Int3.UnitX, ("X:1 Y:0 Z:0", "X:1 Y:0 Z:0", "X:1 Y:0 Z:0", "X:1 Y:0 Z:0"));
        DoTestToString(Int3.UnitY, ("X:0 Y:1 Z:0", "X:0 Y:1 Z:0", "X:0 Y:1 Z:0", "X:0 Y:1 Z:0"));
        DoTestToString(Int3.UnitZ, ("X:0 Y:0 Z:1", "X:0 Y:0 Z:1", "X:0 Y:0 Z:1", "X:0 Y:0 Z:1"));
    }

    [Fact]
    public void TestInt4()
    {
        DoTestToString(Int4.Zero, ("X:0 Y:0 Z:0 W:0", "X:0 Y:0 Z:0 W:0", "X:0 Y:0 Z:0 W:0", "X:0 Y:0 Z:0 W:0"));
        DoTestToString(Int4.One, ("X:1 Y:1 Z:1 W:1", "X:1 Y:1 Z:1 W:1", "X:1 Y:1 Z:1 W:1", "X:1 Y:1 Z:1 W:1"));
        DoTestToString(Int4.UnitX, ("X:1 Y:0 Z:0 W:0", "X:1 Y:0 Z:0 W:0", "X:1 Y:0 Z:0 W:0", "X:1 Y:0 Z:0 W:0"));
        DoTestToString(Int4.UnitY, ("X:0 Y:1 Z:0 W:0", "X:0 Y:1 Z:0 W:0", "X:0 Y:1 Z:0 W:0", "X:0 Y:1 Z:0 W:0"));
        DoTestToString(Int4.UnitZ, ("X:0 Y:0 Z:1 W:0", "X:0 Y:0 Z:1 W:0", "X:0 Y:0 Z:1 W:0", "X:0 Y:0 Z:1 W:0"));
        DoTestToString(Int4.UnitW, ("X:0 Y:0 Z:0 W:1", "X:0 Y:0 Z:0 W:1", "X:0 Y:0 Z:0 W:1", "X:0 Y:0 Z:0 W:1"));
    }

    [Fact]
    public void TestMatrix()
    {
        DoTestToString(
            Matrix.Zero,
            (
                "[M11:0 M12:0 M13:0 M14:0] [M21:0 M22:0 M23:0 M24:0] [M31:0 M32:0 M33:0 M34:0] [M41:0 M42:0 M43:0 M44:0]",
                "[M11:0 M12:0 M13:0 M14:0] [M21:0 M22:0 M23:0 M24:0] [M31:0 M32:0 M33:0 M34:0] [M41:0 M42:0 M43:0 M44:0]",
                "[M11:0 M12:0 M13:0 M14:0] [M21:0 M22:0 M23:0 M24:0] [M31:0 M32:0 M33:0 M34:0] [M41:0 M42:0 M43:0 M44:0]",
                "[M11:0 M12:0 M13:0 M14:0] [M21:0 M22:0 M23:0 M24:0] [M31:0 M32:0 M33:0 M34:0] [M41:0 M42:0 M43:0 M44:0]"
            )
        );
        DoTestToString(
            Matrix.Identity,
            (
                "[M11:1 M12:0 M13:0 M14:0] [M21:0 M22:1 M23:0 M24:0] [M31:0 M32:0 M33:1 M34:0] [M41:0 M42:0 M43:0 M44:1]",
                "[M11:1 M12:0 M13:0 M14:0] [M21:0 M22:1 M23:0 M24:0] [M31:0 M32:0 M33:1 M34:0] [M41:0 M42:0 M43:0 M44:1]",
                "[M11:1 M12:0 M13:0 M14:0] [M21:0 M22:1 M23:0 M24:0] [M31:0 M32:0 M33:1 M34:0] [M41:0 M42:0 M43:0 M44:1]",
                "[M11:1 M12:0 M13:0 M14:0] [M21:0 M22:1 M23:0 M24:0] [M31:0 M32:0 M33:1 M34:0] [M41:0 M42:0 M43:0 M44:1]"
            )
        );
    }

    [Fact]
    public void TestPlane()
    {
        DoTestToString(new Plane(0.0f), ("A:0 B:0 C:0 D:0", "A:0 B:0 C:0 D:0", "A:0 B:0 C:0 D:0", "A:0 B:0 C:0 D:0"));
        DoTestToString(new Plane(1.0f), ("A:1 B:1 C:1 D:1", "A:1 B:1 C:1 D:1", "A:1 B:1 C:1 D:1", "A:1 B:1 C:1 D:1"));
        DoTestToString(new Plane(Vector3.Zero, Vector3.UnitY), ("A:0 B:1 C:0 D:0", "A:0 B:1 C:0 D:0", "A:0 B:1 C:0 D:0", "A:0 B:1 C:0 D:0"));
        DoTestToString(new Plane(Vector3.One, Vector3.UnitY + Vector3.UnitZ), ("A:0 B:1 C:1 D:2", "A:0 B:1 C:1 D:2", "A:0 B:1 C:1 D:2", "A:0 B:1 C:1 D:2"));
    }

    [Fact]
    public void TestPoint()
    {
        DoTestToString(Point.Zero, ("(0,0)", "(0,0)", "(0,0)", "(0,0)"));
        DoTestToString(
            new Point(int.MinValue, int.MaxValue),
            (
                "(-2147483648,2147483647)",
                "(-2147483648,2147483647)",
                "(-2147483648,2147483647)",
                "(-2147483648,2147483647)"
            )
        );
        DoTestToString(
            new Point(int.MinValue, int.MaxValue),
            (
                "(-2147483648,2147483647)",
                "(-2147483648,2147483647)",
                "(-2.1E+09,2.1E+09)",
                "(-2.1E+09,2.1E+09)"
            ), format: "G2"
        );
    }

    [Fact]
    public void TestQuaternion()
    {
        DoTestToString(Quaternion.Zero, ("X:0 Y:0 Z:0 W:0", "X:0 Y:0 Z:0 W:0", "X:0 Y:0 Z:0 W:0", "X:0 Y:0 Z:0 W:0"));
        DoTestToString(Quaternion.Identity, ("X:0 Y:0 Z:0 W:1", "X:0 Y:0 Z:0 W:1", "X:0 Y:0 Z:0 W:1", "X:0 Y:0 Z:0 W:1"));
        DoTestToString(Quaternion.One, ("X:1 Y:1 Z:1 W:1", "X:1 Y:1 Z:1 W:1", "X:1 Y:1 Z:1 W:1", "X:1 Y:1 Z:1 W:1"));
    }

    [Fact]
    public void TestRay()
    {
        DoTestToString(
            new Ray(Vector3.Zero, Vector3.UnitZ),
            (
                "Position:X:0 Y:0 Z:0 Direction:X:0 Y:0 Z:1",
                "Position:X:0 Y:0 Z:0 Direction:X:0 Y:0 Z:1",
                "Position:X:0 Y:0 Z:0 Direction:X:0 Y:0 Z:1",
                "Position:X:0 Y:0 Z:0 Direction:X:0 Y:0 Z:1"
            )
        );
    }

    [Fact]
    public void TestRectangle()
    {
        DoTestToString(
            Rectangle.Empty,
            (
                "X:0 Y:0 Width:0 Height:0",
                "X:0 Y:0 Width:0 Height:0",
                "X:0 Y:0 Width:0 Height:0",
                "X:0 Y:0 Width:0 Height:0"
            )
        );
        DoTestToString(
            new Rectangle(-1, 1, 123, 456),
            (
                "X:-1 Y:1 Width:123 Height:456",
                "X:-1 Y:1 Width:123 Height:456",
                "X:-1 Y:1 Width:123 Height:456",
                "X:-1 Y:1 Width:123 Height:456"
            )
        );
    }

    [Fact]
    public void TestRectangleF()
    {
        DoTestToString(
            RectangleF.Empty,
            (
                "X:0 Y:0 Width:0 Height:0",
                "X:0 Y:0 Width:0 Height:0",
                "X:0 Y:0 Width:0 Height:0",
                "X:0 Y:0 Width:0 Height:0"
            )
        );
        DoTestToString(
            new RectangleF(-1, 1, 12.3f, 4.56f),
            (
                "X:-1 Y:1 Width:12.3 Height:4.56",
                "X:-1 Y:1 Width:12.3 Height:4.56",
                "X:-1 Y:1 Width:12.3 Height:4.56",
                "X:-1 Y:1 Width:12.3 Height:4.56"
            )
        );
    }

    [Fact]
    public void TestSize2()
    {
        DoTestToString(Size2.Zero, ("(0,0)", "(0,0)", "(0,0)", "(0,0)"));
        DoTestToString(Size2.Empty, ("(0,0)", "(0,0)", "(0,0)", "(0,0)"));
        DoTestToString(new Size2(800, 600), ("(800,600)", "(800,600)", "(800,600)", "(800,600)"));
    }

    [Fact]
    public void TestSize2F()
    {
        DoTestToString(Size2F.Zero, ("(0,0)", "(0,0)", "(0,0)", "(0,0)"));
        DoTestToString(Size2F.Empty, ("(0,0)", "(0,0)", "(0,0)", "(0,0)"));
        DoTestToString(new Size2F(800, 600), ("(800,600)", "(800,600)", "(800,600)", "(800,600)"));
    }

    [Fact]
    public void TestSize3()
    {
        DoTestToString(Size3.Zero, ("(0,0,0)", "(0,0,0)", "(0,0,0)", "(0,0,0)"));
        DoTestToString(Size3.Empty, ("(0,0,0)", "(0,0,0)", "(0,0,0)", "(0,0,0)"));
        DoTestToString(new Size3(800, 600, 32), ("(800,600,32)", "(800,600,32)", "(800,600,32)", "(800,600,32)"));
    }

    [Fact]
    public void TestUInt4()
    {
        DoTestToString(UInt4.Zero, ("X:0 Y:0 Z:0 W:0", "X:0 Y:0 Z:0 W:0", "X:0 Y:0 Z:0 W:0", "X:0 Y:0 Z:0 W:0"));
        DoTestToString(UInt4.One, ("X:1 Y:1 Z:1 W:1", "X:1 Y:1 Z:1 W:1", "X:1 Y:1 Z:1 W:1", "X:1 Y:1 Z:1 W:1"));
        DoTestToString(UInt4.UnitX, ("X:1 Y:0 Z:0 W:0", "X:1 Y:0 Z:0 W:0", "X:1 Y:0 Z:0 W:0", "X:1 Y:0 Z:0 W:0"));
        DoTestToString(UInt4.UnitY, ("X:0 Y:1 Z:0 W:0", "X:0 Y:1 Z:0 W:0", "X:0 Y:1 Z:0 W:0", "X:0 Y:1 Z:0 W:0"));
        DoTestToString(UInt4.UnitZ, ("X:0 Y:0 Z:1 W:0", "X:0 Y:0 Z:1 W:0", "X:0 Y:0 Z:1 W:0", "X:0 Y:0 Z:1 W:0"));
        DoTestToString(UInt4.UnitW, ("X:0 Y:0 Z:0 W:1", "X:0 Y:0 Z:0 W:1", "X:0 Y:0 Z:0 W:1", "X:0 Y:0 Z:0 W:1"));
    }

    [Fact]
    public void TestVector2()
    {
        DoTestToString(Vector2.Zero, ("X:0 Y:0", "X:0 Y:0", "X:0 Y:0", "X:0 Y:0"));
        DoTestToString(Vector2.One, ("X:1 Y:1", "X:1 Y:1", "X:1 Y:1", "X:1 Y:1"));
        DoTestToString(Vector2.UnitX, ("X:1 Y:0", "X:1 Y:0", "X:1 Y:0", "X:1 Y:0"));
        DoTestToString(Vector2.UnitY, ("X:0 Y:1", "X:0 Y:1", "X:0 Y:1", "X:0 Y:1"));
    }

    [Fact]
    public void TestVector3()
    {
        DoTestToString(Vector3.Zero, ("X:0 Y:0 Z:0", "X:0 Y:0 Z:0", "X:0 Y:0 Z:0", "X:0 Y:0 Z:0"));
        DoTestToString(Vector3.One, ("X:1 Y:1 Z:1", "X:1 Y:1 Z:1", "X:1 Y:1 Z:1", "X:1 Y:1 Z:1"));
        DoTestToString(Vector3.UnitX, ("X:1 Y:0 Z:0", "X:1 Y:0 Z:0", "X:1 Y:0 Z:0", "X:1 Y:0 Z:0"));
        DoTestToString(Vector3.UnitY, ("X:0 Y:1 Z:0", "X:0 Y:1 Z:0", "X:0 Y:1 Z:0", "X:0 Y:1 Z:0"));
        DoTestToString(Vector3.UnitZ, ("X:0 Y:0 Z:1", "X:0 Y:0 Z:1", "X:0 Y:0 Z:1", "X:0 Y:0 Z:1"));
    }

    [Fact]
    public void TestVector4()
    {
        DoTestToString(Vector4.Zero, ("X:0 Y:0 Z:0 W:0", "X:0 Y:0 Z:0 W:0", "X:0 Y:0 Z:0 W:0", "X:0 Y:0 Z:0 W:0"));
        DoTestToString(Vector4.One, ("X:1 Y:1 Z:1 W:1", "X:1 Y:1 Z:1 W:1", "X:1 Y:1 Z:1 W:1", "X:1 Y:1 Z:1 W:1"));
        DoTestToString(Vector4.UnitX, ("X:1 Y:0 Z:0 W:0", "X:1 Y:0 Z:0 W:0", "X:1 Y:0 Z:0 W:0", "X:1 Y:0 Z:0 W:0"));
        DoTestToString(Vector4.UnitY, ("X:0 Y:1 Z:0 W:0", "X:0 Y:1 Z:0 W:0", "X:0 Y:1 Z:0 W:0", "X:0 Y:1 Z:0 W:0"));
        DoTestToString(Vector4.UnitZ, ("X:0 Y:0 Z:1 W:0", "X:0 Y:0 Z:1 W:0", "X:0 Y:0 Z:1 W:0", "X:0 Y:0 Z:1 W:0"));
        DoTestToString(Vector4.UnitW, ("X:0 Y:0 Z:0 W:1", "X:0 Y:0 Z:0 W:1", "X:0 Y:0 Z:0 W:1", "X:0 Y:0 Z:0 W:1"));
    }

    private static void DoTestToString<T>(T value, (string none, string culture, string format, string formatAndCulture) args, CultureInfo? culture = null, string? format = null)
        where T : IFormattable
    {
        culture ??= CultureInfo.InvariantCulture;
        format ??= "0.##";
        Assert.Equal(args.none, value.ToString());
        Assert.Equal(args.culture, value.ToString(null, culture));
        Assert.Equal(args.format, value.ToString(format, null));
        Assert.Equal(args.formatAndCulture, value.ToString(format, culture));
    }
}
