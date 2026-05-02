// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Msdfgen;
using static Msdfgen.ErrorCorrectionConfig;
using MsdfVector2 = Msdfgen.Vector2;

namespace Stride.Graphics.Font.RuntimeMsdf
{
    /// <summary>
    /// MSDFGen-Sharp (Msdfgen.Core) implementation for generating MSDF textures from font outlines.
    /// For fonts with self intersecting contours, it's best to preprocess it with FontForge first.
    /// </summary>
    public sealed class MsdfGenCoreRasterizer : IGlyphMsdfRasterizer
    {
        CharacterBitmapRgba IGlyphMsdfRasterizer.RasterizeMsdf(
            GlyphOutline outline,
            DistanceFieldSettings df,
            MsdfEncodeSettings encode)
        {
            ArgumentNullException.ThrowIfNull(outline);

            int width = df.TotalWidth;
            int height = df.TotalHeight;

            if (width <= 0 || height <= 0)
                return new CharacterBitmapRgba();

            // Build MsdfGen shape from outline (NO Y-flip - MsdfGen uses Y-up like FreeType)
            var shape = BuildMsdfGenShape(outline);

            if (shape.Contours.Count == 0)
                return new CharacterBitmapRgba(width, height);

            // Normalize BEFORE orienting for self-intersecting shapes
            shape.Normalize();
            
            // Orient contours consistently
            shape.OrientContours();

            // Use ink trap aware edge coloring for better self-intersection handling
            EdgeColoringInkTrap(shape, 3.0);

            // Calculate shape bounds
            var bounds = shape.GetBounds();
            double shapeWidth = bounds.R - bounds.L;
            double shapeHeight = bounds.T - bounds.B;

            if (shapeWidth <= 0 || shapeHeight <= 0)
                return new CharacterBitmapRgba(width, height);

            // Calculate scale to fit shape into target size (excluding padding)
            double scaleX = df.Width / shapeWidth;
            double scaleY = df.Height / shapeHeight;
            double scale = Math.Min(scaleX, scaleY);

            // Calculate translation to center the shape with padding
            double translateX = df.Padding - bounds.L * scale;
            double translateY = df.Padding - bounds.B * scale;

            // Create projection and range for MSDF generation
            var projection = new Projection(
                new MsdfVector2(scale, scale),
                new MsdfVector2(translateX, translateY)
            );

            var range = new Msdfgen.Range(df.PixelRange);

            // Create output bitmap (3 channels for RGB MSDF)
            var msdfBitmap = new Bitmap<float>(width, height, 3);

            // Overlap support on by default.
            var config = new MSDFGeneratorConfig
            {
            };

            MsdfGenerator.GenerateMSDF(msdfBitmap, shape, projection, range, config);

            // Pack float RGB to CharacterBitmapRgba (flip Y here for Stride's Y-down pixels)
            var result = new CharacterBitmapRgba(width, height);
            PackToRgba8(msdfBitmap, result, encode, flipY: true);

            return result;
        }

        private static Shape BuildMsdfGenShape(GlyphOutline outline)
        {
            var shape = new Shape();

            foreach (var srcContour in outline.Contours)
            {
                if (srcContour?.Segments == null || srcContour.Segments.Count == 0)
                    continue;

                var contour = new Contour();

                foreach (var segment in srcContour.Segments)
                {
                    if (segment == null) continue;

                    switch (segment)
                    {
                        case Stride.Graphics.Font.RuntimeMsdf.LineSegment line:
                            contour.Edges.Add(new Msdfgen.LinearSegment(
                                ToMsdfGen(line.P0),
                                ToMsdfGen(line.P1),
                                EdgeColor.WHITE
                            ));
                            break;

                        case Stride.Graphics.Font.RuntimeMsdf.QuadraticSegment quad:
                            contour.Edges.Add(new Msdfgen.QuadraticSegment(
                                ToMsdfGen(quad.P0),
                                ToMsdfGen(quad.C0),
                                ToMsdfGen(quad.P1),
                                EdgeColor.WHITE
                            ));
                            break;

                        case Stride.Graphics.Font.RuntimeMsdf.CubicSegment cubic:
                            contour.Edges.Add(new Msdfgen.CubicSegment(
                                ToMsdfGen(cubic.P0),
                                ToMsdfGen(cubic.C0),
                                ToMsdfGen(cubic.C1),
                                ToMsdfGen(cubic.P1),
                                EdgeColor.WHITE
                            ));
                            break;
                    }
                }

                if (contour.Edges.Count > 0)
                    shape.AddContour(contour);
            }

            return shape;
        }

        private static MsdfVector2 ToMsdfGen(Stride.Core.Mathematics.Vector2 v)
        {
            // No Y-flip needed - both FreeType and MsdfGen use Y-up
            return new MsdfVector2(v.X, v.Y);
        }

        /// <summary>
        /// Improved edge coloring that handles self-intersecting contours and ink traps better.
        /// </summary>
        private static void EdgeColoringInkTrap(Shape shape, double angleThreshold)
        {
            const double crossThreshold = 0.05; // sin(~3 degrees) for detecting corners
            
            foreach (var contour in shape.Contours)
            {
                if (contour.Edges.Count == 0) continue;

                EdgeColor[] colors = { EdgeColor.CYAN, EdgeColor.MAGENTA, EdgeColor.YELLOW };

                // Initialize all edges to white
                foreach (var edge in contour.Edges)
                {
                    edge.Color = EdgeColor.WHITE;
                }

                // Multi-pass coloring
                // Pass 1: Assign initial colors avoiding neighbor conflicts
                for (int i = 0; i < contour.Edges.Count; i++)
                {
                    int prevIndex = (i - 1 + contour.Edges.Count) % contour.Edges.Count;
                    int nextIndex = (i + 1) % contour.Edges.Count;
                    
                    var prevColor = contour.Edges[prevIndex].Color;
                    var nextColor = contour.Edges[nextIndex].Color;

                    // Find a color that doesn't conflict with neighbors
                    EdgeColor chosen = colors[0];
                    foreach (var c in colors)
                    {
                        if (c != prevColor && c != nextColor)
                        {
                            chosen = c;
                            break;
                        }
                    }

                    contour.Edges[i].Color = chosen;
                }

                // Pass 2: Adjust colors at corners for better MSDF quality
                for (int i = 0; i < contour.Edges.Count; i++)
                {
                    int prevIndex = (i - 1 + contour.Edges.Count) % contour.Edges.Count;
                    
                    var prevEdge = contour.Edges[prevIndex];
                    var edge = contour.Edges[i];

                    var prevDir = prevEdge.Direction(1).Normalize();
                    var curDir = edge.Direction(0).Normalize();
                    
                    double dot = MsdfVector2.DotProduct(prevDir, curDir);
                    double cross = Math.Abs(MsdfVector2.CrossProduct(prevDir, curDir));

                    // Detect sharp corners (angle > ~90 degrees or high curvature)
                    bool isCorner = dot < 0 || cross > crossThreshold;

                    if (isCorner && edge.Color == prevEdge.Color)
                    {
                        // At corners, use different colors to prevent artifacts
                        foreach (var c in colors)
                        {
                            if (c != prevEdge.Color && c != edge.Color)
                            {
                                edge.Color = c;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static unsafe void PackToRgba8(Bitmap<float> source, CharacterBitmapRgba dest, MsdfEncodeSettings encode, bool flipY)
        {
            // MsdfGen outputs float values in [0,1] where 0.5 is the edge
            // Inside shape: > 0.5, Outside shape: < 0.5
            
            // For self-intersecting shapes, we may need median filtering
            // to reduce artifacts at overlap points
            
            bool invertDistance = false; // MsdfGen convention matches Stride's SDF shader
            float scaleFactor = encode.Scale * 2f;

            byte* buffer = (byte*)dest.Buffer;
            int pitch = dest.Pitch;

            for (int y = 0; y < source.Height; y++)
            {
                // Flip Y when writing to output (MsdfGen is Y-up, Stride pixels are Y-down)
                int destY = flipY ? (source.Height - 1 - y) : y;
                byte* row = buffer + destY * pitch;

                for (int x = 0; x < source.Width; x++)
                {
                    float r = source[x, y, 0];
                    float g = source[x, y, 1];
                    float b = source[x, y, 2];

                    if (invertDistance)
                    {
                        r = 1f - r;
                        g = 1f - g;
                        b = 1f - b;
                    }

                    // Apply encoding (scale around 0.5 midpoint)
                    r = Encode(r, encode.Bias, scaleFactor);
                    g = Encode(g, encode.Bias, scaleFactor);
                    b = Encode(b, encode.Bias, scaleFactor);

                    int offset = x * 4;
                    row[offset + 0] = FloatToByte(r);
                    row[offset + 1] = FloatToByte(g);
                    row[offset + 2] = FloatToByte(b);
                    row[offset + 3] = 255;
                }
            }
        }

        private static float Encode(float value, float bias, float scaleFactor)
        {
            // Transform: output = bias + (value - 0.5) * scaleFactor
            float result = bias + (value - 0.5f) * scaleFactor;
            return Math.Clamp(result, 0f, 1f);
        }

        private static byte FloatToByte(float value)
        {
            if (value <= 0f) return 0;
            if (value >= 1f) return 255;
            return (byte)(value * 255f + 0.5f);
        }
    }
}
