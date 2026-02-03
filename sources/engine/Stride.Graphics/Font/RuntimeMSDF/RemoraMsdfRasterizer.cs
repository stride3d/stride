// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using NVector2 = System.Numerics.Vector2;
using Remora.MSDFGen;
using Remora.MSDFGen.Graphics;
using Stride.Core.Mathematics;
using Stride.Graphics.Font;

namespace Stride.Graphics.Font.RuntimeMsdf
{
    /// <summary>
    /// Remora.MSDFGen-backed implementation of <see cref="IGlyphMsdfRasterizer"/>.
    /// 
    /// This class is intentionally isolated from the rest of the runtime font pipeline so that
    /// swapping MSDF backends later is just a matter of replacing this file.
    /// </summary>
    public sealed class RemoraMsdfRasterizer : IGlyphMsdfRasterizer
    {
        // The upstream msdfgen sample code uses ~3.0 radians as a common default.
        private const double DefaultAngleThresholdRadians = 3.0;

        // We flip Y when converting from FreeType/Stride outline space (Y up) into pixel space (Y down).
        // If your outline extractor already flips Y, set this to false.
        private const bool FlipOutlineYAxis = true;

        CharacterBitmapRgba IGlyphMsdfRasterizer.RasterizeMsdf(GlyphOutline outline, DistanceFieldSettings df, MsdfEncodeSettings encode)
        {
            ArgumentNullException.ThrowIfNull(outline);

            var totalWidth = df.TotalWidth;
            var totalHeight = df.TotalHeight;

            if (totalWidth <= 0 || totalHeight <= 0)
                return new CharacterBitmapRgba();

            // 1) Convert neutral outline -> Remora shape
            var shape = BuildShape(outline, FlipOutlineYAxis, out var minX, out var minY, out var maxX, out var maxY);

            if (shape.Contours.Count == 0)
                return new CharacterBitmapRgba(totalWidth, totalHeight);

            // 2) Edge coloring (required for correct MSDF)
            MSDF.EdgeColoringSimple(shape, DefaultAngleThresholdRadians);

            // 3) Generate float MSDF into a pixmap
            var pix = new Pixmap<Remora.MSDFGen.Graphics.Color3>(totalWidth, totalHeight);

            // We treat outline units as pixel units (after scaling in FreeType).
            // Place the shape so its min corner starts at (Padding, Padding).
            // Note: MSDF.GenerateMSDF computes p = (pixel - translate) / scale.
            // So translate is in pixel space.
            var translate = new NVector2((float)(df.Padding - minX), (float)(df.Padding - minY));
            var scale = new NVector2(1f, 1f);

            MSDF.GenerateMSDF(pix, shape, df.PixelRange, scale, translate);

            // 4) Pack float RGB -> RGBA8 in a Stride bitmap
            var bmp = new CharacterBitmapRgba(totalWidth, totalHeight);
            PackPixmapToRgba8(pix, bmp, encode);

            return bmp;
        }

        private static Shape BuildShape(GlyphOutline outline, bool flipY, out double minX, out double minY, out double maxX, out double maxY)
        {
            var shape = new Shape
            {
                // Only affects output row order in Remora's generator; we handle Y by flipping coordinates.
                InverseYAxis = false
            };

            minX = double.PositiveInfinity;
            minY = double.PositiveInfinity;
            maxX = double.NegativeInfinity;
            maxY = double.NegativeInfinity;

            foreach (var srcContour in outline.Contours)
            {
                if (srcContour?.Segments == null || srcContour.Segments.Count == 0)
                    continue;

                var contour = new Contour();

                foreach (var seg in srcContour.Segments)
                {
                    if (seg == null)
                        continue;

                    switch (seg)
                    {
                        case LineSegment line:
                            {
                                var a = ToRemora(line.P0, flipY);
                                var b = ToRemora(line.P1, flipY);
                                UpdateBounds(a, ref minX, ref minY, ref maxX, ref maxY);
                                UpdateBounds(b, ref minX, ref minY, ref maxX, ref maxY);
                                contour.Edges.Add(new LinearSegment(a, b, EdgeColor.White));
                                break;
                            }
                        case QuadraticSegment quad:
                            {
                                var a = ToRemora(quad.P0, flipY);
                                var c = ToRemora(quad.C0, flipY);
                                var b = ToRemora(quad.P1, flipY);
                                UpdateBounds(a, ref minX, ref minY, ref maxX, ref maxY);
                                UpdateBounds(c, ref minX, ref minY, ref maxX, ref maxY);
                                UpdateBounds(b, ref minX, ref minY, ref maxX, ref maxY);
                                contour.Edges.Add(new Remora.MSDFGen.QuadraticSegment(a, c, b, EdgeColor.White));
                                break;
                            }
                        case CubicSegment cubic:
                            {
                                var a = ToRemora(cubic.P0, flipY);
                                var c0 = ToRemora(cubic.C0, flipY);
                                var c1 = ToRemora(cubic.C1, flipY);
                                var b = ToRemora(cubic.P1, flipY);
                                UpdateBounds(a, ref minX, ref minY, ref maxX, ref maxY);
                                UpdateBounds(c0, ref minX, ref minY, ref maxX, ref maxY);
                                UpdateBounds(c1, ref minX, ref minY, ref maxX, ref maxY);
                                UpdateBounds(b, ref minX, ref minY, ref maxX, ref maxY);
                                contour.Edges.Add(new Remora.MSDFGen.CubicSegment(a, c0, c1, b, EdgeColor.White));
                                break;
                            }
                        default:
                            {
                                // Unknown segment type - ignore rather than crash.
                                break;
                            }
                    }
                }

                if (contour.Edges.Count > 0)
                    shape.Contours.Add(contour);
            }

            if (double.IsInfinity(minX) || double.IsInfinity(minY))
            {
                minX = minY = 0;
                maxX = maxY = 0;
            }

            return shape;
        }

        private static NVector2 ToRemora(Vector2 v, bool flipY)
            => new(v.X, flipY ? -v.Y : v.Y);

        private static void UpdateBounds(NVector2 p, ref double minX, ref double minY, ref double maxX, ref double maxY)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }

        private static unsafe void PackPixmapToRgba8(Pixmap<Remora.MSDFGen.Graphics.Color3> pix, CharacterBitmapRgba dst, MsdfEncodeSettings encode)
        {
            // Encode settings apply around the 0.5 midpoint.
            // encode.Scale is defined so that 0.5 means "identity".
            var scaleFactor = encode.Scale * 2f;

            byte* basePtr = (byte*)dst.Buffer;
            int pitch = dst.Pitch;

            for (int y = 0; y < pix.Height; y++)
            {
                byte* row = basePtr + y * pitch;

                for (int x = 0; x < pix.Width; x++)
                {
                    var c = pix[x, y];

                    float r = ApplyEncode(c.R, encode.Bias, scaleFactor);
                    float g = ApplyEncode(c.G, encode.Bias, scaleFactor);
                    float b = ApplyEncode(c.B, encode.Bias, scaleFactor);

                    int o = x * 4;
                    row[o + 0] = FloatToByte(r);
                    row[o + 1] = FloatToByte(g);
                    row[o + 2] = FloatToByte(b);
                    row[o + 3] = 255;
                }
            }
        }

        private static float ApplyEncode(float v, float bias, float scaleFactor)
        {
            // v is expected to be in [0,1], centered around 0.5.
            // v' = bias + (v-0.5)*scaleFactor
            var e = bias + (v - 0.5f) * scaleFactor;
            if (e < 0f) return 0f;
            if (e > 1f) return 1f;
            return e;
        }

        private static byte FloatToByte(float v)
        {
            // Clamp and round.
            if (v <= 0f) return 0;
            if (v >= 1f) return 255;
            return (byte)(v * 255f + 0.5f);
        }
    }
}
