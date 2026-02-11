using System;
using System.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Graphics.Font;

namespace Stride.Graphics.Font.RuntimeMsdf
{
    /// <summary>
    /// Diagnostic rasterizer that visualizes the outline data to identify extraction issues.
    /// Draws the outline directly without MSDF to see if the geometry is correct.
    /// </summary>
    public sealed class OutlineDiagnosticRasterizer : IGlyphMsdfRasterizer
    {
        CharacterBitmapRgba IGlyphMsdfRasterizer.RasterizeMsdf(
            GlyphOutline outline,
            DistanceFieldSettings df,
            MsdfEncodeSettings encode)
        {
            var totalWidth = df.TotalWidth;
            var totalHeight = df.TotalHeight;

            if (totalWidth <= 0 || totalHeight <= 0)
                return new CharacterBitmapRgba();

            var bmp = new CharacterBitmapRgba(totalWidth, totalHeight);

            // Log outline information
            Debug.WriteLine($"=== Outline Diagnostic ===");
            Debug.WriteLine($"Contours: {outline?.Contours?.Count ?? 0}");
            Debug.WriteLine($"Bounds: {outline?.Bounds}");
            Debug.WriteLine($"Target size: {df.Width}x{df.Height} (padded: {totalWidth}x{totalHeight})");

            if (outline == null || outline.Contours == null || outline.Contours.Count == 0)
            {
                Debug.WriteLine("ERROR: No outline data!");
                // Return red bitmap to indicate error
                FillSolid(bmp, 255, 0, 0);
                return bmp;
            }

            // Calculate bounds from actual segments
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            int totalSegments = 0;

            foreach (var contour in outline.Contours)
            {
                if (contour?.Segments == null) continue;
                
                foreach (var seg in contour.Segments)
                {
                    if (seg == null) continue;
                    totalSegments++;
                    
                    UpdateBounds(seg.P0, ref minX, ref minY, ref maxX, ref maxY);
                    UpdateBounds(seg.P1, ref minX, ref minY, ref maxX, ref maxY);
                }
            }

            Debug.WriteLine($"Total segments: {totalSegments}");
            Debug.WriteLine($"Actual bounds: ({minX}, {minY}) -> ({maxX}, {maxY})");
            Debug.WriteLine($"Actual size: {maxX - minX} x {maxY - minY}");

            if (totalSegments == 0)
            {
                Debug.WriteLine("ERROR: No segments found!");
                FillSolid(bmp, 255, 0, 0);
                return bmp;
            }

            // Check for suspicious values
            if (float.IsInfinity(minX) || float.IsNaN(minX))
            {
                Debug.WriteLine("ERROR: Invalid bounds (infinity/NaN)");
                FillSolid(bmp, 255, 128, 0);
                return bmp;
            }

            float outlineWidth = maxX - minX;
            float outlineHeight = maxY - minY;

            if (outlineWidth < 0.01f || outlineHeight < 0.01f)
            {
                Debug.WriteLine($"WARNING: Outline too small ({outlineWidth} x {outlineHeight})");
                FillSolid(bmp, 255, 255, 0);
                return bmp;
            }

            if (outlineWidth > 10000 || outlineHeight > 10000)
            {
                Debug.WriteLine($"WARNING: Outline too large ({outlineWidth} x {outlineHeight})");
                FillSolid(bmp, 128, 0, 255);
                return bmp;
            }

            // Calculate scale to fit outline into target area
            float scaleX = df.Width / outlineWidth;
            float scaleY = df.Height / outlineHeight;
            float scale = Math.Min(scaleX, scaleY);

            Debug.WriteLine($"Scale: {scale} (scaleX={scaleX}, scaleY={scaleY})");

            // Clear to black
            FillSolid(bmp, 0, 0, 0);

            // Draw outline segments
            unsafe
            {
                byte* buffer = (byte*)bmp.Buffer;
                int pitch = bmp.Pitch;

                foreach (var contour in outline.Contours)
                {
                    if (contour?.Segments == null) continue;

                    foreach (var seg in contour.Segments)
                    {
                        if (seg == null) continue;

                        // Transform points to bitmap space
                        var p0 = TransformPoint(seg.P0, minX, minY, scale, df.Padding);
                        var p1 = TransformPoint(seg.P1, minX, minY, scale, df.Padding);

                        // Draw line segment
                        DrawLine(buffer, pitch, totalWidth, totalHeight, p0, p1, 255, 255, 255);
                    }
                }
            }

            Debug.WriteLine("Outline rendered successfully");
            return bmp;
        }

        private static Vector2 TransformPoint(Vector2 p, float minX, float minY, float scale, int padding)
        {
            // Transform: (p - min) * scale + padding
            return new Vector2(
                (p.X - minX) * scale + padding,
                (p.Y - minY) * scale + padding
            );
        }

        private static void UpdateBounds(Vector2 p, ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }

        private static unsafe void FillSolid(CharacterBitmapRgba bmp, byte r, byte g, byte b)
        {
            byte* buffer = (byte*)bmp.Buffer;
            int pitch = bmp.Pitch;

            for (int y = 0; y < bmp.Rows; y++)
            {
                byte* row = buffer + y * pitch;
                for (int x = 0; x < bmp.Width; x++)
                {
                    int offset = x * 4;
                    row[offset + 0] = r;
                    row[offset + 1] = g;
                    row[offset + 2] = b;
                    row[offset + 3] = 255;
                }
            }
        }

        private static unsafe void DrawLine(byte* buffer, int pitch, int width, int height, 
            Vector2 p0, Vector2 p1, byte r, byte g, byte b)
        {
            // Simple Bresenham line drawing
            int x0 = (int)p0.X;
            int y0 = (int)p0.Y;
            int x1 = (int)p1.X;
            int y1 = (int)p1.Y;

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int maxSteps = width + height; // Safety limit
            int steps = 0;

            while (steps++ < maxSteps)
            {
                // Plot point if in bounds
                if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
                {
                    byte* pixel = buffer + y0 * pitch + x0 * 4;
                    pixel[0] = r;
                    pixel[1] = g;
                    pixel[2] = b;
                    pixel[3] = 255;
                }

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
    }
}
