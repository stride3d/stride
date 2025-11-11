// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestRectangle
{
    [Fact]
    public void TestRectangleConstruction()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        Assert.Equal(10, rect.X);
        Assert.Equal(20, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(200, rect.Height);
    }

    [Fact]
    public void TestRectangleEmpty()
    {
        var empty = Rectangle.Empty;
        Assert.Equal(0, empty.X);
        Assert.Equal(0, empty.Y);
        Assert.Equal(0, empty.Width);
        Assert.Equal(0, empty.Height);
        Assert.True(empty.IsEmpty);
    }

    [Fact]
    public void TestRectangleIsEmptyFalse()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        Assert.False(rect.IsEmpty);
    }

    [Fact]
    public void TestRectangleProperties()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        Assert.Equal(10, rect.Left);
        Assert.Equal(110, rect.Right);
        Assert.Equal(20, rect.Top);
        Assert.Equal(220, rect.Bottom);
        Assert.Equal(new Point(10, 20), rect.Location);
        Assert.Equal(new Point(60, 120), rect.Center);
    }

    [Fact]
    public void TestRectangleLocationSetter()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        rect.Location = new Point(30, 40);
        Assert.Equal(30, rect.X);
        Assert.Equal(40, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(200, rect.Height);
    }

    [Fact]
    public void TestRectangleSize()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        Assert.Equal(new Size2(100, 200), rect.Size);

        rect.Size = new Size2(150, 250);
        Assert.Equal(150, rect.Width);
        Assert.Equal(250, rect.Height);
    }

    [Fact]
    public void TestRectangleCorners()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        Assert.Equal(new Point(10, 20), rect.TopLeft);
        Assert.Equal(new Point(110, 20), rect.TopRight);
        Assert.Equal(new Point(10, 220), rect.BottomLeft);
        Assert.Equal(new Point(110, 220), rect.BottomRight);
    }

    [Fact]
    public void TestRectangleOffset()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        rect.Offset(new Point(5, 10));
        Assert.Equal(15, rect.X);
        Assert.Equal(30, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(200, rect.Height);
    }

    [Fact]
    public void TestRectangleOffsetXY()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        rect.Offset(5, 10);
        Assert.Equal(15, rect.X);
        Assert.Equal(30, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(200, rect.Height);
    }

    [Fact]
    public void TestRectangleInflate()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        rect.Inflate(5, 10);
        Assert.Equal(5, rect.X);
        Assert.Equal(10, rect.Y);
        Assert.Equal(110, rect.Width);
        Assert.Equal(220, rect.Height);
    }

    [Theory]
    [InlineData(50, 100, true)]
    [InlineData(10, 20, true)]  // Top-left corner (inclusive)
    [InlineData(109, 219, true)]  // Just inside bottom-right
    [InlineData(110, 220, false)]  // Bottom-right corner (exclusive)
    [InlineData(0, 0, false)]
    public void TestRectangleContainsXY(int x, int y, bool expected)
    {
        var rect = new Rectangle(10, 20, 100, 200);
        Assert.Equal(expected, rect.Contains(x, y));
    }

    [Fact]
    public void TestRectangleContainsPoint()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        Assert.True(rect.Contains(new Point(50, 100)));
        Assert.False(rect.Contains(new Point(0, 0)));
    }

    [Fact]
    public void TestRectangleContainsPointWithOutParameter()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        var point = new Point(50, 100);
        rect.Contains(ref point, out bool result);
        Assert.True(result);
    }

    [Fact]
    public void TestRectangleContainsRectangle()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        var inner = new Rectangle(20, 30, 50, 100);
        var outer = new Rectangle(0, 0, 200, 300);

        Assert.True(rect.Contains(inner));
        Assert.False(rect.Contains(outer));
    }

    [Fact]
    public void TestRectangleContainsRectangleWithOutParameter()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        var inner = new Rectangle(20, 30, 50, 100);
        rect.Contains(ref inner, out bool result);
        Assert.True(result);
    }

    [Fact]
    public void TestRectangleContainsFloatXY()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        Assert.True(rect.Contains(50.5f, 100.5f));
        Assert.False(rect.Contains(0.5f, 0.5f));
    }

    [Fact]
    public void TestRectangleContainsVector2()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        Assert.True(rect.Contains(new Vector2(50.5f, 100.5f)));
        Assert.False(rect.Contains(new Vector2(0.5f, 0.5f)));
    }

    [Fact]
    public void TestRectangleIntersects()
    {
        var rect1 = new Rectangle(10, 20, 100, 200);
        var rect2 = new Rectangle(50, 100, 100, 200);
        var rect3 = new Rectangle(200, 300, 100, 200);

        Assert.True(rect1.Intersects(rect2));
        Assert.False(rect1.Intersects(rect3));
    }

    [Fact]
    public void TestRectangleIntersectsWithOutParameter()
    {
        var rect1 = new Rectangle(10, 20, 100, 200);
        var rect2 = new Rectangle(50, 100, 100, 200);
        rect1.Intersects(ref rect2, out bool result);
        Assert.True(result);
    }

    [Fact]
    public void TestRectangleIntersect()
    {
        var rect1 = new Rectangle(10, 20, 100, 200);
        var rect2 = new Rectangle(50, 100, 100, 200);
        var result = Rectangle.Intersect(rect1, rect2);

        Assert.Equal(50, result.X);
        Assert.Equal(100, result.Y);
        Assert.Equal(60, result.Width);
        Assert.Equal(120, result.Height);
    }

    [Fact]
    public void TestRectangleIntersectWithOutParameter()
    {
        var rect1 = new Rectangle(10, 20, 100, 200);
        var rect2 = new Rectangle(50, 100, 100, 200);
        Rectangle.Intersect(ref rect1, ref rect2, out Rectangle result);

        Assert.Equal(50, result.X);
        Assert.Equal(100, result.Y);
        Assert.Equal(60, result.Width);
        Assert.Equal(120, result.Height);
    }

    [Fact]
    public void TestRectangleUnion()
    {
        var rect1 = new Rectangle(10, 20, 100, 200);
        var rect2 = new Rectangle(50, 100, 150, 250);
        var result = Rectangle.Union(rect1, rect2);

        Assert.Equal(10, result.X);
        Assert.Equal(20, result.Y);
        Assert.Equal(190, result.Width);
        Assert.Equal(330, result.Height);
    }

    [Fact]
    public void TestRectangleUnionWithOutParameter()
    {
        var rect1 = new Rectangle(10, 20, 100, 200);
        var rect2 = new Rectangle(50, 100, 150, 250);
        Rectangle.Union(ref rect1, ref rect2, out Rectangle result);

        Assert.Equal(10, result.X);
        Assert.Equal(20, result.Y);
        Assert.Equal(190, result.Width);
        Assert.Equal(330, result.Height);
    }

    [Fact]
    public void TestRectangleEquality()
    {
        var rect1 = new Rectangle(10, 20, 100, 200);
        var rect2 = new Rectangle(10, 20, 100, 200);
        var rect3 = new Rectangle(5, 10, 50, 100);

        Assert.Equal(rect1, rect2);
        Assert.NotEqual(rect1, rect3);
        Assert.True(rect1 == rect2);
        Assert.True(rect1 != rect3);
        Assert.True(rect1.Equals(rect2));
        Assert.False(rect1.Equals(rect3));
    }

    [Fact]
    public void TestRectangleGetHashCode()
    {
        var rect1 = new Rectangle(10, 20, 100, 200);
        var rect2 = new Rectangle(10, 20, 100, 200);
        Assert.Equal(rect1.GetHashCode(), rect2.GetHashCode());
    }

    [Fact]
    public void TestRectangleToString()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        var str = rect.ToString();
        Assert.NotNull(str);
        Assert.Contains("10", str);
        Assert.Contains("20", str);
        Assert.Contains("100", str);
        Assert.Contains("200", str);
    }

    [Fact]
    public void TestRectangleImplicitToRectangleF()
    {
        var rect = new Rectangle(10, 20, 100, 200);
        RectangleF rectF = rect;
        Assert.Equal(10f, rectF.X);
        Assert.Equal(20f, rectF.Y);
        Assert.Equal(100f, rectF.Width);
        Assert.Equal(200f, rectF.Height);
    }
}
