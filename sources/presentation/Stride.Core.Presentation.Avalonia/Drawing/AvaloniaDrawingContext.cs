// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Stride.Core.Presentation.Avalonia.Extensions;
using Stride.Core.Presentation.Drawing;
using Color = Stride.Core.Mathematics.Color;

namespace Stride.Core.Presentation.Avalonia.Drawing;

/// <summary>
/// An <see cref="IDrawingContext"/> over an Avalonia <see cref="DrawingContext"/>: the drawing of an <see cref="IDrawingModel"/>
/// in a <see cref="Controls.DrawingCanvas"/>. Each frame draws the whole model again, so nothing is kept between frames.
/// </summary>
public sealed class AvaloniaDrawingContext : IDrawingContext, IDisposable
{
    private readonly DrawingContext context;
    private DrawingContext.PushedState? clip;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaDrawingContext"/> class.
    /// </summary>
    /// <param name="context">The Avalonia drawing context of the frame.</param>
    public AvaloniaDrawingContext(DrawingContext context)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public void Clear()
    {
        // Avalonia draws each frame from nothing: there is nothing to clear
    }

    /// <inheritdoc/>
    public void DrawEllipse(DrawingPoint point, DrawingSize size, Color fillColor, Color strokeColor,
        double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool isHitTestVisible = false)
    {
        // The point is the center of the ellipse, as for DrawEllipses
        var width = Math.Max(0, size.Width);
        var height = Math.Max(0, size.Height);
        context.DrawEllipse(GetBrush(fillColor), GetPen(strokeColor, thickness, lineJoin, dashArray, dashOffset), new Rect(point.X - width / 2, point.Y - height / 2, width, height));
    }

    /// <inheritdoc/>
    public void DrawEllipses(IList<DrawingPoint> points, double radiusX, double radiusY, Color fillColor, Color strokeColor,
        double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool isHitTestVisible = false)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
            return;

        var brush = GetBrush(fillColor);
        var pen = GetPen(strokeColor, thickness, lineJoin, dashArray, dashOffset);
        foreach (var point in points)
        {
            context.DrawEllipse(brush, pen, point.ToAvalonia(), radiusX, radiusY);
        }
    }

    /// <inheritdoc/>
    public void DrawLine(DrawingPoint p1, DrawingPoint p2, Color strokeColor,
        double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool aliased = false, bool isHitTestVisible = false)
    {
        if (GetPen(strokeColor, thickness, lineJoin, dashArray, dashOffset) is { } pen)
            context.DrawLine(pen, p1.ToAvalonia(), p2.ToAvalonia());
    }

    /// <inheritdoc/>
    public void DrawLineSegments(IList<DrawingPoint> points, Color strokeColor,
        double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool aliased = false, bool isHitTestVisible = false)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 2 || GetPen(strokeColor, thickness, lineJoin, dashArray, dashOffset) is not { } pen)
            return;

        // One geometry for all the segments (the grid lines and ticks of the axes): each pair of points is a segment
        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            for (var i = 0; i + 1 < points.Count; i += 2)
            {
                stream.BeginFigure(points[i].ToAvalonia(), false);
                stream.LineTo(points[i + 1].ToAvalonia());
                stream.EndFigure(false);
            }
        }
        context.DrawGeometry(null, pen, geometry);
    }

    /// <inheritdoc/>
    public void DrawPolygon(IList<DrawingPoint> points, Color fillColor, Color strokeColor,
        double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool aliased = false, bool isHitTestVisible = false)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 2)
            return;

        context.DrawGeometry(GetBrush(fillColor), GetPen(strokeColor, thickness, lineJoin, dashArray, dashOffset), CreatePolyline(points, true));
    }

    /// <inheritdoc/>
    public void DrawPolyline(IList<DrawingPoint> points, Color strokeColor,
        double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool aliased = false, bool isHitTestVisible = false)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 2 || GetPen(strokeColor, thickness, lineJoin, dashArray, dashOffset) is not { } pen)
            return;

        context.DrawGeometry(null, pen, CreatePolyline(points, false));
    }

    /// <inheritdoc/>
    public void DrawRectangle(DrawingRect rect, Color fillColor, Color strokeColor,
        double thickness = 1, DrawingLineJoin lineJoin = DrawingLineJoin.Miter, ICollection<double>? dashArray = null, double dashOffset = 0, bool isHitTestVisible = false)
    {
        context.DrawRectangle(GetBrush(fillColor), GetPen(strokeColor, thickness, lineJoin, dashArray, dashOffset), rect.ToAvalonia());
    }

    /// <inheritdoc/>
    public void DrawText(DrawingPoint point, Color color, string text, DrawingFont font,
        DrawingHorizontalAlignment hAlign = DrawingHorizontalAlignment.Left, DrawingVerticalAlignment vAlign = DrawingVerticalAlignment.Top, bool isHitTestVisible = false)
    {
        ArgumentNullException.ThrowIfNull(text);
        DrawFormattedText(point, CreateText(text, font, GetBrush(color)), hAlign, vAlign);
    }

    /// <inheritdoc/>
    public void DrawTexts(IList<DrawingPoint> points, Color color, IList<string> texts, DrawingFont font,
        DrawingHorizontalAlignment hAlign = DrawingHorizontalAlignment.Left, DrawingVerticalAlignment vAlign = DrawingVerticalAlignment.Top, bool isHitTestVisible = false)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(texts);
        if (points.Count != texts.Count)
            throw new ArgumentException($"{nameof(points)} and {nameof(texts)} must have the same number of elements.");

        var brush = GetBrush(color);
        var typeface = GetTypeface(font);
        for (var i = 0; i < points.Count; ++i)
        {
            DrawFormattedText(points[i], CreateText(texts[i], typeface, font.Size, brush), hAlign, vAlign);
        }
    }

    /// <inheritdoc/>
    public DrawingSize MeasureText(string text, DrawingFont font, TextMeasurementMethod measurementMethod = TextMeasurementMethod.GlyphTypeface)
    {
        if (string.IsNullOrEmpty(text))
            return DrawingSize.Empty;

        var formattedText = CreateText(text, font, null);
        return new DrawingSize(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height);
    }

    /// <inheritdoc/>
    public DrawingSize MeasureTexts(IList<string> texts, DrawingFont font, TextMeasurementMethod measurementMethod = TextMeasurementMethod.GlyphTypeface)
    {
        ArgumentNullException.ThrowIfNull(texts);

        var typeface = GetTypeface(font);
        double width = 0, height = 0;
        foreach (var text in texts)
        {
            if (string.IsNullOrEmpty(text))
                continue;

            var formattedText = CreateText(text, typeface, font.Size, null);
            width = Math.Max(width, formattedText.WidthIncludingTrailingWhitespace);
            height = Math.Max(height, formattedText.Height);
        }
        return new DrawingSize(width, height);
    }

    /// <inheritdoc/>
    public void ResetClip()
    {
        clip?.Dispose();
        clip = null;
    }

    /// <inheritdoc/>
    public void SetClip(DrawingRect clippingRect)
    {
        // One clip at a time, as in WPF: a new clip replaces the current one
        ResetClip();
        clip = context.PushClip(clippingRect.ToAvalonia());
    }

    /// <summary>
    /// Pops the clip that the model left set, if any.
    /// </summary>
    public void Dispose()
    {
        ResetClip();
    }

    private void DrawFormattedText(DrawingPoint point, FormattedText text, DrawingHorizontalAlignment hAlign, DrawingVerticalAlignment vAlign)
    {
        var x = hAlign switch
        {
            DrawingHorizontalAlignment.Center => point.X - text.WidthIncludingTrailingWhitespace / 2,
            DrawingHorizontalAlignment.Right => point.X - text.WidthIncludingTrailingWhitespace,
            _ => point.X,
        };
        var y = vAlign switch
        {
            DrawingVerticalAlignment.Center => point.Y - text.Height / 2,
            DrawingVerticalAlignment.Bottom => point.Y - text.Height,
            _ => point.Y,
        };
        context.DrawText(text, new Point(x, y));
    }

    private static StreamGeometry CreatePolyline(IList<DrawingPoint> points, bool isClosed)
    {
        var geometry = new StreamGeometry();
        using var stream = geometry.Open();
        stream.BeginFigure(points[0].ToAvalonia(), isClosed);
        for (var i = 1; i < points.Count; ++i)
        {
            stream.LineTo(points[i].ToAvalonia());
        }
        stream.EndFigure(isClosed);
        return geometry;
    }

    private static FormattedText CreateText(string text, DrawingFont font, IBrush? brush)
        => CreateText(text, GetTypeface(font), font.Size, brush);

    private static FormattedText CreateText(string text, Typeface typeface, double size, IBrush? brush)
        => new(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, Math.Max(size, 1), brush);

    private static Typeface GetTypeface(DrawingFont font)
    {
        // An unknown family falls back to the default font of the platform
        var family = string.IsNullOrWhiteSpace(font.Family) ? FontFamily.Default : new FontFamily(font.Family);
        return new Typeface(family, FontStyle.Normal, font.Weight == DrawingFontWeight.Bold ? FontWeight.Bold : FontWeight.Normal);
    }

    private static IBrush? GetBrush(Color color)
        => color.A == 0 ? null : new ImmutableSolidColorBrush(color.ToAvaloniaColor());

    private static IPen? GetPen(Color color, double thickness, DrawingLineJoin lineJoin, ICollection<double>? dashArray, double dashOffset)
    {
        if (color.A == 0 || thickness <= 0)
            return null;

        var dashStyle = dashArray is { Count: > 0 } ? new ImmutableDashStyle(dashArray, dashOffset) : null;
        var join = lineJoin switch
        {
            DrawingLineJoin.Bevel => PenLineJoin.Bevel,
            DrawingLineJoin.Round => PenLineJoin.Round,
            _ => PenLineJoin.Miter,
        };
        return new ImmutablePen(new ImmutableSolidColorBrush(color.ToAvaloniaColor()), thickness, dashStyle, PenLineCap.Flat, join);
    }
}
