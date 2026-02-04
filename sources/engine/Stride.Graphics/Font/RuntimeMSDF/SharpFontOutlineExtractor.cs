using System;
using SharpFont;
using Stride.Core.Mathematics;

namespace Stride.Graphics.Font.RuntimeMsdf
{
    /// <summary>
    /// Using SharpFont to extract an outline from a glyph. 
    /// </summary>
    public static class SharpFontOutlineExtractor
    {
        public static bool TryExtractGlyphOutline(
            Face face,
            uint charCode,
            out GlyphOutline outline,
            out GlyphOutlineMetrics metrics,
            LoadFlags loadFlags = LoadFlags.NoBitmap)
        {
            outline = null;
            metrics = default;

            if (face == null) return false;

            try
            {
                face.LoadChar(charCode, loadFlags, LoadTarget.Normal);
            }
            catch (FreeTypeException)
            {
                return false;
            }

            var slot = face.Glyph;
            if (slot == null) return false;

            // Metrics are standard 26.6 fixed point
            var m = slot.Metrics;
            metrics = new GlyphOutlineMetrics(
                AdvanceX: Fixed26Dot6ToFloat(slot.Advance.X),
                BearingX: Fixed26Dot6ToFloat(m.HorizontalBearingX),
                BearingY: Fixed26Dot6ToFloat(m.HorizontalBearingY),
                Width: Fixed26Dot6ToFloat(m.Width),
                Height: Fixed26Dot6ToFloat(m.Height),
                Baseline: 0f);

            var ftOutline = slot.Outline;
            if (ftOutline == null) return false;

            outline = DecomposeOutline(ftOutline);

            // FreeType bounding box is in 26.6 fixed point format
            var bbox = ftOutline.GetBBox();
            float left = Fixed26Dot6ToFloat(bbox.Left);
            float bottom = Fixed26Dot6ToFloat(bbox.Bottom);
            float right = Fixed26Dot6ToFloat(bbox.Right);
            float top = Fixed26Dot6ToFloat(bbox.Top);

            // FreeType uses Y-up coordinates (bottom < top)
            outline.Bounds = new RectangleF(
                left,           // X position (left edge)
                bottom,         // Y position (bottom edge in Y-up space)
                right - left,   // Width
                top - bottom    // Height (positive because top > bottom)
            );

            return true;
        }

        private static GlyphOutline DecomposeOutline(Outline ft)
        {
            var result = new GlyphOutline();
            GlyphContour currentContour = null;
            Vector2 lastPoint = Vector2.Zero;

            // FreeType automatically closes contours, so we don't need to add closing segments
            var funcs = new OutlineFuncs(
                moveTo: (ref FTVector to, IntPtr user) =>
                {
                    currentContour = new GlyphContour { IsClosed = true };
                    result.Contours.Add(currentContour);
                    lastPoint = ConvertVector(to);
                    return 0;
                },
                lineTo: (ref FTVector to, IntPtr user) =>
                {
                    var endPt = ConvertVector(to);
                    currentContour?.Segments.Add(new LineSegment(lastPoint, endPt));
                    lastPoint = endPt;
                    return 0;
                },
                conicTo: (ref FTVector control, ref FTVector to, IntPtr user) =>
                {
                    var cp = ConvertVector(control);
                    var endPt = ConvertVector(to);
                    currentContour?.Segments.Add(new QuadraticSegment(lastPoint, cp, endPt));
                    lastPoint = endPt;
                    return 0;
                },
                cubicTo: (ref FTVector c1, ref FTVector c2, ref FTVector to, IntPtr user) =>
                {
                    var cp1 = ConvertVector(c1);
                    var cp2 = ConvertVector(c2);
                    var endPt = ConvertVector(to);
                    currentContour?.Segments.Add(new CubicSegment(lastPoint, cp1, cp2, endPt));
                    lastPoint = endPt;
                    return 0;
                },
                shift: 0,
                delta: 0
            );

            ft.Decompose(funcs, IntPtr.Zero);

            return result;
        }

        private static Vector2 ConvertVector(FTVector v)
        {
            // FreeType outline points are in 26.6 fixed point format
            // Even though FTVector uses Fixed16Dot16 type, the actual values are 26.6
            // This is a quirk of the SharpFont wrapper that's included in Stride.
            return new Vector2(v.X.Value / 64f, v.Y.Value / 64f);
        }

        // Helper for 26.6 fixed point (used for Metrics and BBox)
        private static float Fixed26Dot6ToFloat(Fixed26Dot6 v) => v.Value / 64f;
        private static float Fixed26Dot6ToFloat(int v) => v / 64f;
    }
}
