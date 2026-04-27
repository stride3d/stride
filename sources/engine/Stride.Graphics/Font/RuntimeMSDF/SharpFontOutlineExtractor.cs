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
            var state = new OutlineDecomposeState();

            FT_Outline_MoveToFunc moveTo = static (to, user) =>
            {
                var handle = GCHandle.FromIntPtr(user);
                var s = (OutlineDecomposeState)handle.Target!;

                var point = ConvertPoint(*to);
                s.CloseCurrentContourIfNeeded();
                s.BeginContour(point);
                s.Include(point);

                return 0;
            };

            FT_Outline_LineToFunc lineTo = static (to, user) =>
            {
                var handle = GCHandle.FromIntPtr(user);
                var s = (OutlineDecomposeState)handle.Target!;

                var end = ConvertPoint(*to);
                s.Include(end);
                s.CurrentContour?.Segments.Add(new LineSegment(s.LastPoint, end));
                s.LastPoint = end;

                return 0;
            };

            FT_Outline_ConicToFunc conicTo = static (control, to, user) =>
            {
                var handle = GCHandle.FromIntPtr(user);
                var s = (OutlineDecomposeState)handle.Target!;

                var cp = ConvertPoint(*control);
                var end = ConvertPoint(*to);

                s.Include(cp);
                s.Include(end);

                s.CurrentContour?.Segments.Add(new QuadraticSegment(s.LastPoint, cp, end));
                s.LastPoint = end;

                return 0;
            };

            FT_Outline_CubicToFunc cubicTo = static (control1, control2, to, user) =>
            {
                var handle = GCHandle.FromIntPtr(user);
                var s = (OutlineDecomposeState)handle.Target!;

                var cp1 = ConvertPoint(*control1);
                var cp2 = ConvertPoint(*control2);
                var end = ConvertPoint(*to);

                s.Include(cp1);
                s.Include(cp2);
                s.Include(end);

                s.CurrentContour?.Segments.Add(new CubicSegment(s.LastPoint, cp1, cp2, end));
                s.LastPoint = end;

                return 0;
            };

            var funcs = new FT_Outline_Funcs
            {
                move_to = Marshal.GetFunctionPointerForDelegate(moveTo),
                line_to = Marshal.GetFunctionPointerForDelegate(lineTo),
                conic_to = Marshal.GetFunctionPointerForDelegate(conicTo),
                cubic_to = Marshal.GetFunctionPointerForDelegate(cubicTo),
                shift = 0,
                delta = new CLong(0)
            };

            var stateHandle = GCHandle.Alloc(state);
            try
            {
                var user = GCHandle.ToIntPtr(stateHandle);
                var localOutline = ft;
                var error = FreeTypeNative.FT_Outline_Decompose(&localOutline, &funcs, user);
                if (error != 0)
                    return new GlyphOutline();

                state.CloseCurrentContourIfNeeded();

                if (state.HasBounds)
                {
                    state.Result.Bounds = new RectangleF(
                        state.MinX,
                        state.MinY,
                        state.MaxX - state.MinX,
                        state.MaxY - state.MinY);
                }

                return state.Result;
            }
            finally
            {
                stateHandle.Free();

                GC.KeepAlive(moveTo);
                GC.KeepAlive(lineTo);
                GC.KeepAlive(conicTo);
                GC.KeepAlive(cubicTo);
            }
        }

        private static Vector2 ConvertPoint(FT_Vector point)
        {
            return new Vector2(
                Fixed26Dot6ToFloat(point.x),
                Fixed26Dot6ToFloat(point.y));
        }

        private sealed class OutlineDecomposeState
        {
            public GlyphOutline Result { get; } = new();
            public GlyphContour? CurrentContour { get; private set; }

            public Vector2 FirstPoint;
            public Vector2 LastPoint;

            public float MinX = float.MaxValue;
            public float MinY = float.MaxValue;
            public float MaxX = float.MinValue;
            public float MaxY = float.MinValue;

            public bool HasBounds => MinX <= MaxX && MinY <= MaxY;

            public void BeginContour(Vector2 start)
            {
                CurrentContour = new GlyphContour();
                Result.Contours.Add(CurrentContour);

                FirstPoint = start;
                LastPoint = start;
            }

            public void Include(Vector2 p)
            {
                MinX = MathF.Min(MinX, p.X);
                MinY = MathF.Min(MinY, p.Y);
                MaxX = MathF.Max(MaxX, p.X);
                MaxY = MathF.Max(MaxY, p.Y);
            }

            public void CloseCurrentContourIfNeeded()
            {
                if (CurrentContour == null)
                    return;

                if (LastPoint != FirstPoint)
                {
                    CurrentContour.Segments.Add(new LineSegment(LastPoint, FirstPoint));
                }

                CurrentContour = null;
            }
        }

        private static float Fixed26Dot6ToFloat(System.Runtime.InteropServices.CLong value) => (int)value.Value / 64f;
    }
}
