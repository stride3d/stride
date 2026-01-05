// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestRectangleF
{
    [Fact]
    public void TestRectangleFConstruction()
    {
        var rect = new RectangleF(10.5f, 20.5f, 100.5f, 200.5f);
        Assert.Equal(10.5f, rect.X);
        Assert.Equal(20.5f, rect.Y);
        Assert.Equal(100.5f, rect.Width);
        Assert.Equal(200.5f, rect.Height);
    }

    [Fact]
    public void TestRectangleFEmpty()
    {
        var empty = RectangleF.Empty;
        Assert.Equal(0f, empty.X);
        Assert.Equal(0f, empty.Y);
        Assert.Equal(0f, empty.Width);
        Assert.Equal(0f, empty.Height);
        Assert.True(empty.IsEmpty);
    }

    [Fact]
    public void TestRectangleFIsEmptyFalse()
    {
        var rect = new RectangleF(10f, 20f, 100f, 200f);
        Assert.False(rect.IsEmpty);
    }

    [Fact]
    public void TestRectangleFProperties()
    {
        var rect = new RectangleF(10f, 20f, 100f, 200f);
        Assert.Equal(10f, rect.Left);
        Assert.Equal(110f, rect.Right);
        Assert.Equal(20f, rect.Top);
        Assert.Equal(220f, rect.Bottom);
        Assert.Equal(new Vector2(10f, 20f), rect.TopLeft);
        Assert.Equal(new Vector2(60f, 120f), rect.Center);
    }

    [Fact]
    public void TestRectangleFLocationSetter()
    {
        var rect = new RectangleF(10f, 20f, 100f, 200f);
        rect.Location = new Vector2(30f, 40f);
        Assert.Equal(30f, rect.X);
        Assert.Equal(40f, rect.Y);
        Assert.Equal(100f, rect.Width);
        Assert.Equal(200f, rect.Height);
    }

    [Fact]
    public void TestRectangleFSize()
    {
        var rect = new RectangleF(10f, 20f, 100f, 200f);
        Assert.Equal(new Size2F(100f, 200f), rect.Size);

        rect.Size = new Size2F(150f, 250f);
        Assert.Equal(150f, rect.Width);
        Assert.Equal(250f, rect.Height);
    }

    [Fact]
    public void TestRectangleFCorners()
    {
        var rect = new RectangleF(10f, 20f, 100f, 200f);
        Assert.Equal(new Vector2(10f, 20f), rect.TopLeft);
        Assert.Equal(new Vector2(110f, 20f), rect.TopRight);
        Assert.Equal(new Vector2(10f, 220f), rect.BottomLeft);
        Assert.Equal(new Vector2(110f, 220f), rect.BottomRight);
    }

    [Fact]
    public void TestRectangleFOffsetPoint()
    {
        var rect = new RectangleF(10f, 20f, 100f, 200f);
        rect.Offset(new Point(5, 10));
        Assert.Equal(15f, rect.X);
        Assert.Equal(30f, rect.Y);
        Assert.Equal(100f, rect.Width);
        Assert.Equal(200f, rect.Height);
    }

    [Fact]
    public void TestRectangleFOffsetVector2()
    {
        var rect = new RectangleF(10f, 20f, 100f, 200f);
        rect.Offset(new Vector2(5.5f, 10.5f));
        Assert.Equal(15.5f, rect.X);
        Assert.Equal(30.5f, rect.Y);
        Assert.Equal(100f, rect.Width);
        Assert.Equal(200f, rect.Height);
    }

    [Fact]
    public void TestRectangleFOffsetXY()
    {
        var rect = new RectangleF(10f, 20f, 100f, 200f);
        rect.Offset(5.5f, 10.5f);
        Assert.Equal(15.5f, rect.X);
        Assert.Equal(30.5f, rect.Y);
        Assert.Equal(100f, rect.Width);
        Assert.Equal(200f, rect.Height);
    }

    [Fact]
    public void TestRectangleFInflate()
    {
        var rect = new RectangleF(10f, 20f, 100f, 200f);
        rect.Inflate(5f, 10f);
        Assert.Equal(5f, rect.X);
        Assert.Equal(10f, rect.Y);
        Assert.Equal(110f, rect.Width);
        Assert.Equal(220f, rect.Height);
    }

    [Theory]
    [InlineData(50f, 100f, true)]
    [InlineData(10f, 20f, true)]     // Top-left corner (inclusive)
    [InlineData(110f, 220f, true)]   // Bottom-right corner (inclusive based on Contains implementation)
    [InlineData(111f, 221f, false)]  // Outside bottom-right
    [InlineData(0f, 0f, false)]
    public void TestRectangleFContainsXY(float x, float y, bool expected)
    {
        var rect = new RectangleF(10f, 20f, 100f, 200f);
        Assert.Equal(expected, rect.Contains(x, y));
    }

    [Fact]
    public void TestRectangleFContainsVector2()
    {
        var rect = new RectangleF(10f, 20f, 100f, 200f);
        Assert.True(rect.Contains(new Vector2(50f, 100f)));
        Assert.False(rect.Contains(new Vector2(0f, 0f)));
    }

    [Fact]
    public void TestRectangleFContainsVector2WithOutParameter()
    {
        var rect = new RectangleF(10f, 20f, 100f, 200f);
        var point = new Vector2(50f, 100f);
        rect.Contains(ref point, out bool result);
        Assert.True(result);
    }

    [Fact]
    public void TestRectangleFContainsRectangle()
    {
        var rect = new RectangleF(10f, 20f, 100f, 200f);
        var inner = new Rectangle(20, 30, 50, 100);
        var outer = new Rectangle(0, 0, 200, 300);

        Assert.True(rect.Contains(inner));
        Assert.False(rect.Contains(outer));
    }

    [Fact]
    public void TestRectangleFContainsRectangleFWithOutParameter()
    {
        var rect = new RectangleF(10f, 20f, 100f, 200f);
        var inner = new RectangleF(20f, 30f, 50f, 100f);
        rect.Contains(ref inner, out bool result);
        Assert.True(result);
    }

    [Fact]
    public void TestRectangleFIntersects()
    {
        var rect1 = new RectangleF(10f, 20f, 100f, 200f);
        var rect2 = new RectangleF(50f, 100f, 100f, 200f);
        var rect3 = new RectangleF(200f, 300f, 100f, 200f);

        Assert.True(rect1.Intersects(rect2));
        Assert.False(rect1.Intersects(rect3));
    }

    [Fact]
    public void TestRectangleFIntersectsWithOutParameter()
    {
        var rect1 = new RectangleF(10f, 20f, 100f, 200f);
        var rect2 = new RectangleF(50f, 100f, 100f, 200f);
        rect1.Intersects(ref rect2, out bool result);
        Assert.True(result);
    }

    [Fact]
    public void TestRectangleFIntersect()
    {
        var rect1 = new RectangleF(10f, 20f, 100f, 200f);
        var rect2 = new RectangleF(50f, 100f, 100f, 200f);
        var result = RectangleF.Intersect(rect1, rect2);

        Assert.Equal(50f, result.X);
        Assert.Equal(100f, result.Y);
        Assert.Equal(60f, result.Width);
        Assert.Equal(120f, result.Height);
    }

    [Fact]
    public void TestRectangleFIntersectWithOutParameter()
    {
        var rect1 = new RectangleF(10f, 20f, 100f, 200f);
        var rect2 = new RectangleF(50f, 100f, 100f, 200f);
        RectangleF.Intersect(ref rect1, ref rect2, out RectangleF result);

        Assert.Equal(50f, result.X);
        Assert.Equal(100f, result.Y);
        Assert.Equal(60f, result.Width);
        Assert.Equal(120f, result.Height);
    }

    [Fact]
    public void TestRectangleFUnion()
    {
        var rect1 = new RectangleF(10f, 20f, 100f, 200f);
        var rect2 = new RectangleF(50f, 100f, 150f, 250f);
        var result = RectangleF.Union(rect1, rect2);

        Assert.Equal(10f, result.X);
        Assert.Equal(20f, result.Y);
        Assert.Equal(190f, result.Width);
        Assert.Equal(330f, result.Height);
    }

    [Fact]
    public void TestRectangleFUnionWithOutParameter()
    {
        var rect1 = new RectangleF(10f, 20f, 100f, 200f);
        var rect2 = new RectangleF(50f, 100f, 150f, 250f);
        RectangleF.Union(ref rect1, ref rect2, out RectangleF result);

        Assert.Equal(10f, result.X);
        Assert.Equal(20f, result.Y);
        Assert.Equal(190f, result.Width);
        Assert.Equal(330f, result.Height);
    }

    [Fact]
    public void TestRectangleFEquality()
    {
        var rect1 = new RectangleF(10f, 20f, 100f, 200f);
        var rect2 = new RectangleF(10f, 20f, 100f, 200f);
        var rect3 = new RectangleF(5f, 10f, 50f, 100f);

        Assert.Equal(rect1, rect2);
        Assert.NotEqual(rect1, rect3);
        Assert.True(rect1 == rect2);
        Assert.True(rect1 != rect3);
        Assert.True(rect1.Equals(rect2));
        Assert.False(rect1.Equals(rect3));
    }

    [Fact]
    public void TestRectangleFGetHashCode()
    {
        var rect1 = new RectangleF(10f, 20f, 100f, 200f);
        var rect2 = new RectangleF(10f, 20f, 100f, 200f);
        Assert.Equal(rect1.GetHashCode(), rect2.GetHashCode());
    }

    [Fact]
    public void TestRectangleFToString()
    {
        var rect = new RectangleF(10.5f, 20.5f, 100.5f, 200.5f);
        var str = rect.ToString();
        Assert.NotNull(str);
        Assert.Contains("10", str);
        Assert.Contains("20", str);
        Assert.Contains("100", str);
        Assert.Contains("200", str);
    }

    [Fact]
    public void TestRectangleFExplicitToRectangle()
    {
        var rectF = new RectangleF(10.7f, 20.7f, 100.7f, 200.7f);
        Rectangle rect = (Rectangle)rectF;
        Assert.Equal(10, rect.X);
        Assert.Equal(20, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(200, rect.Height);
    }
}
