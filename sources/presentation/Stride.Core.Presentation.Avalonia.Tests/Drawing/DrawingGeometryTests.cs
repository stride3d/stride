// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Core.Presentation.Drawing;
using Stride.Core.Presentation.Extensions;
using Color = Stride.Core.Mathematics.Color;

namespace Stride.Core.Presentation.Avalonia.Tests.Drawing;

/// <summary>
/// The toolkit-free geometry of the drawing contract (<c>Stride.Core.Presentation.Drawing</c>), which replaces the WPF point,
/// vector, size, rectangle and thickness in the curve editor: the operations it uses behave as the WPF ones did.
/// </summary>
public sealed class DrawingGeometryTests
{
    [Fact]
    public void PointsAndVectorsCombineAsInWpf()
    {
        var p1 = new DrawingPoint(5, 7);
        var p2 = new DrawingPoint(2, 3);

        Assert.Equal(new DrawingVector(3, 4), p1 - p2);
        Assert.Equal(new DrawingPoint(8, 11), p1 + new DrawingVector(3, 4));
        Assert.Equal(new DrawingPoint(2, 3), p1 - new DrawingVector(3, 4));
        Assert.Equal(new DrawingPoint(4, 8), p1.Offset(-1, 1));
        Assert.Equal(5, (p1 - p2).Length);
        Assert.Equal(25, (p1 - p2).LengthSquared);
        Assert.Equal(new DrawingVector(1.5, 2), (p1 - p2) / 2);
        Assert.Equal(new DrawingVector(6, 8), (p1 - p2) * 2);
        Assert.Equal(new DrawingVector(4, 4), new DrawingVector(1, 2) + new DrawingVector(3, 2));
        Assert.Equal(new DrawingVector(-3, -4), -(p1 - p2));
        Assert.Equal(new DrawingVector(5, 7), (DrawingVector)p1);
        Assert.Equal(new DrawingPoint(3, 4), (DrawingPoint)new DrawingVector(3, 4));
    }

    [Fact]
    public void ARectangleHasItsEdgesCenterAndContainsItsBorder()
    {
        var rect = new DrawingRect(10, 20, 30, 40);

        Assert.Equal(10, rect.Left);
        Assert.Equal(20, rect.Top);
        Assert.Equal(40, rect.Right);
        Assert.Equal(60, rect.Bottom);
        Assert.Equal(new DrawingPoint(25, 40), rect.Center);
        Assert.Equal(new DrawingPoint(10, 20), rect.Location);
        Assert.Equal(new DrawingSize(30, 40), rect.Size);
        Assert.True(rect.Contains(new DrawingPoint(10, 20)));
        Assert.True(rect.Contains(new DrawingPoint(40, 60)));
        Assert.True(rect.Contains(new DrawingPoint(25, 40)));
        Assert.False(rect.Contains(new DrawingPoint(9.9, 40)));
        Assert.False(rect.Contains(new DrawingPoint(25, 60.1)));
        Assert.False(rect.IsEmpty);
        Assert.True(new DrawingRect(0, 0, 0, 10).IsEmpty);
    }

    [Fact]
    public void ARectangleBuiltFromTwoPointsSpansThemInAnyOrder()
    {
        Assert.Equal(new DrawingRect(2, 3, 8, 4), new DrawingRect(new DrawingPoint(10, 3), new DrawingPoint(2, 7)));
        Assert.Equal(new DrawingRect(2, 3, 8, 4), new DrawingRect(new DrawingPoint(2, 3), new DrawingSize(8, 4)));
    }

    [Fact]
    public void DeflateAndInflateApplyEachSideOfTheThickness()
    {
        var rect = new DrawingRect(0, 0, 100, 50);
        var margins = new DrawingThickness(1, 2, 3, 4);

        var deflated = rect.Deflate(margins);

        Assert.Equal(new DrawingRect(1, 2, 96, 44), deflated);
        Assert.Equal(rect, deflated.Inflate(margins));
        Assert.Equal(new DrawingThickness(5, 5, 5, 5), new DrawingThickness(5));
        // Larger margins than the rectangle give a negative size, which the curve editor corrects (as with WPF)
        Assert.True(new DrawingRect(0, 0, 4, 4).Deflate(new DrawingThickness(3)).Width < 0);
    }

    [Fact]
    public void PointsAndVectorsConvertToAndFromVector2()
    {
        Assert.Equal(new Vector2(1.5f, -2), new DrawingPoint(1.5, -2).ToVector2());
        Assert.Equal(new Vector2(3, 4), new DrawingVector(3, 4).ToVector2());
        Assert.Equal(new DrawingPoint(1.5, -2), new Vector2(1.5f, -2).ToDrawingPoint());
        Assert.Equal(new DrawingVector(3, 4), new Vector2(3, 4).ToDrawingVector());
    }

    [Fact]
    public void AFontKeepsItsFamilyAndWeightWhenResized()
    {
        var font = new DrawingFont("Arial", 12);

        var larger = font with { Size = 24, Weight = DrawingFontWeight.Bold };

        Assert.Equal(DrawingFontWeight.Normal, font.Weight);
        Assert.Equal(new DrawingFont("Arial", 24, DrawingFontWeight.Bold), larger);
        Assert.Equal(700, (int)DrawingFontWeight.Bold);
        Assert.Equal(400, (int)DrawingFontWeight.Normal);
    }

    [Fact]
    public void ACircleIsAnEllipseOfTwiceItsRadius()
    {
        var context = new RecordingDrawingContext();

        context.DrawCircle(new DrawingPoint(3, 4), 5, Color.Red, Color.Blue);

        var (point, size) = Assert.Single(context.Ellipses);
        Assert.Equal(new DrawingPoint(3, 4), point);
        Assert.Equal(new DrawingSize(10, 10), size);
    }

    [Fact]
    public void MathTextDrawsSuperscriptsSmallerAndSideBySide()
    {
        var context = new RecordingDrawingContext();
        var font = new DrawingFont("Arial", 10);

        context.DrawMathText(new DrawingPoint(0, 0), Color.Black, "E=mc^{2}", font);

        Assert.Equal(["E=mc", "2"], context.Texts.Select(x => x.Text));
        Assert.Equal(10, context.Texts[0].Font.Size);
        Assert.Equal(6.2, context.Texts[1].Font.Size, 6);
        Assert.Equal("Arial", context.Texts[1].Font.Family);
        Assert.True(context.Texts[1].Point.X > context.Texts[0].Point.X);
    }

    [Fact]
    public void PlainTextIsDrawnOnceWithItsAlignment()
    {
        var context = new RecordingDrawingContext();

        context.DrawMathText(new DrawingPoint(1, 2), Color.Black, "12.5", new DrawingFont("Arial", 12), DrawingHorizontalAlignment.Right, DrawingVerticalAlignment.Center);

        var text = Assert.Single(context.Texts);
        Assert.Equal(("12.5", new DrawingPoint(1, 2), DrawingHorizontalAlignment.Right, DrawingVerticalAlignment.Center), (text.Text, text.Point, text.HAlign, text.VAlign));
    }

    /// <summary>
    /// A drawing context that records the ellipses and texts drawn, and measures each character as half the font size wide.
    /// </summary>
    private sealed class RecordingDrawingContext : IDrawingContext
    {
        public List<(DrawingPoint Point, DrawingSize Size)> Ellipses { get; } = [];

        public List<(DrawingPoint Point, string Text, DrawingFont Font, DrawingHorizontalAlignment HAlign, DrawingVerticalAlignment VAlign)> Texts { get; } = [];

        public void Clear() { }

        public void DrawEllipse(DrawingPoint point, DrawingSize size, Color fillColor, Color strokeColor, double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool isHitTestVisible = false)
            => Ellipses.Add((point, size));

        public void DrawEllipses(IList<DrawingPoint> points, double radiusX, double radiusY, Color fillColor, Color strokeColor, double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool isHitTestVisible = false)
        {
            foreach (var point in points)
                Ellipses.Add((point, new DrawingSize(radiusX * 2, radiusY * 2)));
        }

        public void DrawLine(DrawingPoint p1, DrawingPoint p2, Color strokeColor, double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool aliased = false, bool isHitTestVisible = false) { }

        public void DrawLineSegments(IList<DrawingPoint> points, Color strokeColor, double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool aliased = false, bool isHitTestVisible = false) { }

        public void DrawPolygon(IList<DrawingPoint> points, Color fillColor, Color strokeColor, double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool aliased = false, bool isHitTestVisible = false) { }

        public void DrawPolyline(IList<DrawingPoint> points, Color strokeColor, double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool aliased = false, bool isHitTestVisible = false) { }

        public void DrawRectangle(DrawingRect rect, Color fillColor, Color strokeColor, double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool isHitTestVisible = false) { }

        public void DrawText(DrawingPoint point, Color color, string text, DrawingFont font, DrawingHorizontalAlignment hAlign = DrawingHorizontalAlignment.Left, DrawingVerticalAlignment vAlign = DrawingVerticalAlignment.Top, bool isHitTestVisible = false)
            => Texts.Add((point, text, font, hAlign, vAlign));

        public void DrawTexts(IList<DrawingPoint> points, Color color, IList<string> texts, DrawingFont font, DrawingHorizontalAlignment hAlign = DrawingHorizontalAlignment.Left, DrawingVerticalAlignment vAlign = DrawingVerticalAlignment.Top, bool isHitTestVisible = false)
        {
            for (var i = 0; i < points.Count; ++i)
                DrawText(points[i], color, texts[i], font, hAlign, vAlign, isHitTestVisible);
        }

        public DrawingSize MeasureText(string text, DrawingFont font, TextMeasurementMethod measurementMethod = TextMeasurementMethod.GlyphTypeface)
            => new(text.Length * font.Size / 2, font.Size);

        public DrawingSize MeasureTexts(IList<string> texts, DrawingFont font, TextMeasurementMethod measurementMethod = TextMeasurementMethod.GlyphTypeface)
            => new(texts.Max(x => x.Length) * font.Size / 2, font.Size);

        public void ResetClip() { }

        public void SetClip(DrawingRect clippingRect) { }
    }
}
