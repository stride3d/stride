// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Drawing;
using Color = Stride.Core.Mathematics.Color;

namespace Stride.Assets.Editor.Tests.Helpers;

/// <summary>
/// A drawing context that records what is drawn, in order, and measures each character as half the font size wide.
/// </summary>
public sealed class RecordingDrawingContext : IDrawingContext
{
    /// <summary>
    /// One drawing call: its kind (the method name without "Draw"), its points, its color, and its texts.
    /// </summary>
    public sealed record Call(string Kind, IReadOnlyList<DrawingPoint> Points, Color Color, IReadOnlyList<string> Texts, DrawingRect Rect = default);

    public List<Call> Calls { get; } = [];

    public IEnumerable<Call> OfKind(string kind) => Calls.Where(x => x.Kind == kind);

    public void Clear() => Calls.Clear();

    public void DrawEllipse(DrawingPoint point, DrawingSize size, Color fillColor, Color strokeColor, double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool isHitTestVisible = false)
        => Calls.Add(new Call("Ellipse", [point], fillColor, []));

    public void DrawEllipses(IList<DrawingPoint> points, double radiusX, double radiusY, Color fillColor, Color strokeColor, double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool isHitTestVisible = false)
        => Calls.Add(new Call("Ellipses", [.. points], fillColor, []));

    public void DrawLine(DrawingPoint p1, DrawingPoint p2, Color strokeColor, double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool aliased = false, bool isHitTestVisible = false)
        => Calls.Add(new Call("Line", [p1, p2], strokeColor, []));

    public void DrawLineSegments(IList<DrawingPoint> points, Color strokeColor, double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool aliased = false, bool isHitTestVisible = false)
        => Calls.Add(new Call("LineSegments", [.. points], strokeColor, []));

    public void DrawPolygon(IList<DrawingPoint> points, Color fillColor, Color strokeColor, double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool aliased = false, bool isHitTestVisible = false)
        => Calls.Add(new Call("Polygon", [.. points], fillColor, []));

    public void DrawPolyline(IList<DrawingPoint> points, Color strokeColor, double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool aliased = false, bool isHitTestVisible = false)
        => Calls.Add(new Call("Polyline", [.. points], strokeColor, []));

    public void DrawRectangle(DrawingRect rect, Color fillColor, Color strokeColor, double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool isHitTestVisible = false)
        => Calls.Add(new Call("Rectangle", [rect.Location], strokeColor, [], rect));

    public void DrawText(DrawingPoint point, Color color, string text, DrawingFont font, DrawingHorizontalAlignment hAlign = DrawingHorizontalAlignment.Left, DrawingVerticalAlignment vAlign = DrawingVerticalAlignment.Top, bool isHitTestVisible = false)
        => Calls.Add(new Call("Text", [point], color, [text]));

    public void DrawTexts(IList<DrawingPoint> points, Color color, IList<string> texts, DrawingFont font, DrawingHorizontalAlignment hAlign = DrawingHorizontalAlignment.Left, DrawingVerticalAlignment vAlign = DrawingVerticalAlignment.Top, bool isHitTestVisible = false)
        => Calls.Add(new Call("Texts", [.. points], color, [.. texts]));

    public DrawingSize MeasureText(string text, DrawingFont font, TextMeasurementMethod measurementMethod = TextMeasurementMethod.GlyphTypeface)
        => new(text.Length * font.Size / 2, font.Size);

    public DrawingSize MeasureTexts(IList<string> texts, DrawingFont font, TextMeasurementMethod measurementMethod = TextMeasurementMethod.GlyphTypeface)
        => texts.Count == 0 ? DrawingSize.Empty : new(texts.Max(x => x.Length) * font.Size / 2, font.Size);

    public void ResetClip() => Calls.Add(new Call("ResetClip", [], default, []));

    public void SetClip(DrawingRect clippingRect) => Calls.Add(new Call("SetClip", [clippingRect.Location], default, [], clippingRect));
}
