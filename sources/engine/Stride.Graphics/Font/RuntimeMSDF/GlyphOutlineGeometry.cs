using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Stride.Graphics.Font.RuntimeMsdf
{
    /// <summary>
    /// A minimal, engine-friendly outline representation for a single glyph.
    ///
    /// This is intentionally NOT tied to any particular MSDF library.
    /// The goal is: SharpFont -> GlyphOutline -> (any MSDF generator) -> CharacterBitmapRgba.
    /// </summary>
    public sealed class GlyphOutline
    {
        public readonly List<GlyphContour> Contours = [];

        /// <summary>
        /// Outline bounds in the same coordinate space as the points.
        /// </summary>
        public RectangleF Bounds;

        /// <summary>
        /// TrueType / FreeType winding can be CW/CCW depending on font.
        /// Keep it explicit so downstream generators can choose to normalize.
        /// </summary>
        public GlyphWinding Winding = GlyphWinding.Unknown;
    }

    public enum GlyphWinding
    {
        Unknown = 0,
        Clockwise,
        CounterClockwise,
    }

    public sealed class GlyphContour
    {
        public readonly List<GlyphSegment> Segments = [];
        public bool IsClosed = true;
    }

    public abstract record GlyphSegment(Vector2 P0, Vector2 P1);

    /// <summary>Line segment (P0 -> P1).</summary>
    public sealed record LineSegment(Vector2 P0, Vector2 P1) : GlyphSegment(P0, P1);

    /// <summary>Quadratic Bezier (P0 -> C0 -> P1).</summary>
    public sealed record QuadraticSegment(Vector2 P0, Vector2 C0, Vector2 P1) : GlyphSegment(P0, P1);

    /// <summary>Cubic Bezier (P0 -> C0 -> C1 -> P1).</summary>
    public sealed record CubicSegment(Vector2 P0, Vector2 C0, Vector2 C1, Vector2 P1) : GlyphSegment(P0, P1);

    /// <summary>
    /// Metrics that matter for layout. Values are in the same coordinate space as the outline.
    /// </summary>
    public readonly record struct GlyphOutlineMetrics(
        float AdvanceX,
        float BearingX,
        float BearingY,
        float Width,
        float Height,
        float Baseline);
}
