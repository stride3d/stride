using System;
using Stride.Core.Mathematics;
using System.Runtime.InteropServices;

namespace Stride.Graphics.Font.RuntimeMsdf
{
    /// <summary>
    /// Extracts glyph outlines from FreeType glyph data for runtime MSDF generation.
    /// </summary>
    internal static unsafe class SharpFontOutlineExtractor
    {
        private const byte FT_CURVE_TAG_ON = 1;
        private const byte FT_CURVE_TAG_CUBIC = 2;
        private const byte FT_CURVE_TAG_MASK = 3;

        public static bool TryExtractGlyphOutline(
            FT_FaceRec* face,
            uint charCode,
            out GlyphOutline outline,
            out GlyphOutlineMetrics metrics,
            FreeTypeLoadFlags loadFlags = FreeTypeLoadFlags.NoBitmap)
        {
            outline = null;
            metrics = default;

            if (face == null)
                return false;

            var glyphIndex = FreeTypeNative.FT_Get_Char_Index(face, charCode);
            if (glyphIndex == 0)
                return false;

            var err = FreeTypeNative.FT_Load_Glyph(face, glyphIndex, (int)loadFlags | (int)FreeTypeLoadTarget.Normal);
            if (err != 0)
                return false;

            var slot = face->glyph;
            if (slot == null)
                return false;

            var m = slot->metrics;
            metrics = new GlyphOutlineMetrics(
                AdvanceX: Fixed26Dot6ToFloat(slot->advance.x),
                BearingX: Fixed26Dot6ToFloat(m.horiBearingX),
                BearingY: Fixed26Dot6ToFloat(m.horiBearingY),
                Width: Fixed26Dot6ToFloat(m.width),
                Height: Fixed26Dot6ToFloat(m.height),
                Baseline: 0f);

            if (slot->outline.n_contours == 0 || slot->outline.n_points == 0)
                return false;

            outline = DecomposeOutline(ref slot->outline);
            return true;
        }

        private static GlyphOutline DecomposeOutline(ref FT_Outline ft)
        {
            var result = new GlyphOutline();

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            var contourStart = 0;
            for (var contourIndex = 0; contourIndex < ft.n_contours; contourIndex++)
            {
                var contourEnd = ft.contours[contourIndex];
                if (contourEnd < contourStart)
                    continue;

                var contour = new GlyphContour();
                result.Contours.Add(contour);

                Vector2 last = ConvertPoint(ft.points[contourStart], ref minX, ref minY, ref maxX, ref maxY);
                for (var i = contourStart + 1; i <= contourEnd; i++)
                {
                    var point = ConvertPoint(ft.points[i], ref minX, ref minY, ref maxX, ref maxY);
                    var tag = ft.tags[i] & FT_CURVE_TAG_MASK;

                    if (tag == FT_CURVE_TAG_CUBIC && i + 2 <= contourEnd)
                    {
                        var cp1 = point;
                        var cp2 = ConvertPoint(ft.points[i + 1], ref minX, ref minY, ref maxX, ref maxY);
                        var end = ConvertPoint(ft.points[i + 2], ref minX, ref minY, ref maxX, ref maxY);
                        contour.Segments.Add(new CubicSegment(last, cp1, cp2, end));
                        last = end;
                        i += 2;
                    }
                    else if (tag != FT_CURVE_TAG_ON && i + 1 <= contourEnd)
                    {
                        var control = point;
                        var end = ConvertPoint(ft.points[i + 1], ref minX, ref minY, ref maxX, ref maxY);
                        contour.Segments.Add(new QuadraticSegment(last, control, end));
                        last = end;
                        i += 1;
                    }
                    else
                    {
                        contour.Segments.Add(new LineSegment(last, point));
                        last = point;
                    }
                }

                var firstPoint = ConvertPoint(ft.points[contourStart], ref minX, ref minY, ref maxX, ref maxY);
                if (last != firstPoint)
                    contour.Segments.Add(new LineSegment(last, firstPoint));

                contourStart = contourEnd + 1;
            }

            if (minX <= maxX && minY <= maxY)
                result.Bounds = new RectangleF(minX, minY, maxX - minX, maxY - minY);

            return result;
        }

        private static Vector2 ConvertPoint(FT_Vector point, ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            var v = new Vector2(Fixed26Dot6ToFloat(point.x), Fixed26Dot6ToFloat(point.y));
            minX = MathF.Min(minX, v.X);
            minY = MathF.Min(minY, v.Y);
            maxX = MathF.Max(maxX, v.X);
            maxY = MathF.Max(maxY, v.Y);
            return v;
        }

        private static float Fixed26Dot6ToFloat(System.Runtime.InteropServices.CLong value) => (int)value.Value / 64f;
    }
}
